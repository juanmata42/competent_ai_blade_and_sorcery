# Diseño

Versión del diseño: 0.1.0 · Objetivo: Blade & Sorcery 1.3.1.260730005 (PC). Contexto y decisiones: [research/SUMMARY.md](research/SUMMARY.md). Causas raíz de las quejas: [research/game-ai-internals.md §7](research/game-ai-internals.md).

## 1. Principios
1. **Extender, no reemplazar**: ningún catálogo sobrescrito, ningún parche de código, ningún módulo del juego sustituido.
2. **Verificar contra el DLL real**: cada miembro del juego usado compila contra `ThunderRoad.dll` 1.3.1 y se comprueba al arrancar (`ApiSelfCheck`). Lo no verificable en ejecución está marcado "NO VERIFICADO" (§9).
3. **Fallar hacia el juego de serie**: toda entrada está dentro de `try/catch`; 4 excepciones desactivan el controlador de esa criatura; el autochequeo apaga el mod entero si la API cambió; ante datos desconocidos (armas modded) el mod no restringe.
4. **Todo observable y apagable**: logs por niveles con limitador, 11 interruptores independientes (JSON + menú).
5. **Lógica pura separada y testeada**: las decisiones viven en `CompetentAI.Core` (sin Unity), la integración es fina.

## 2. Arquitectura

```
ThunderScript (CompetentAIScript)
 ├─ lee config JSON, autochequeo de API, opciones del menú  → Runtime (config, flags efectivos, calibración)
 ├─ cada 0,5 s: añade BrainModuleCompetent al BrainData de los NPCs nuevos
 └─ eventos: onCreatureAttacking → módulo del atacante · onCreatureAttackParry → módulo del que para · onLevelUnload

BrainModuleCompetent (uno por NPC, al final de brain.instance.modules)   ← Core (puro)
 ├─ Tuning      arquetipo → campos de Melee/Defense                       SkillProfile, ProfileResolver
 ├─ Attack      OnStanceChange → StanceCurator (reglas)                   AttackRules, WeaponClassifier, DirectionResolver
 │              PlanTick (variedad)  · OnAttackStage (windup/end)         AttackMemory, VarietyScorer, WeightedPicker
 │              FeintTick · combos · CounterTick · calibración             FeintPlanner, DirectionCalibration
 └─ Defense     DefenseTick (tras el módulo de defensa de serie)          StrikeTracker, FeintReader, ParryFatigue
```

### Por qué un módulo añadido al final
`BrainData.FixedUpdate/Update/LateUpdate` recorren `modules` en orden (verificado). El módulo propio corre **inmediatamente después** del de defensa/melee de serie en el mismo tick, así que puede corregir sus efectos sin parches. Cada NPC tiene su propia copia de `BrainData`, de modo que tocar sus campos no afecta a otros ni al catálogo.

## 3. Capa de datos (perfiles de Brain)

Aplicada **en runtime a la copia de cada NPC** (no por JSON de catálogo, para no pisar a otros mods). Parámetros por arquetipo, todos multiplicativos/aditivos sobre el valor que ya tenga la criatura:

| Parámetro del perfil | Campo del juego que modula | Efecto |
|---|---|---|
| `ParryAdjustSpeedMul` | `BrainModuleDefense.parryAdjustSpeed` (7) | Rapidez con que la guardia sigue el golpe |
| `AboutToBeHitMul` | `aboutToBeHitTiming` (0,5 s) | Anticipación del bloqueo/esquiva |
| `DodgeChanceMul` | `dodgeChanceCurve` | Frecuencia de esquivas |
| `RiposteChanceShift`, `RiposteDelayMul` | `BrainModuleMelee.recoilNothingRiposteChance`, `minMaxRiposteDelay` | Probabilidad y retraso del riposte tras parar |
| `AttackIntervalMul`, `AnimSpeedMul` | `minMaxTimeBetweenAttack`, `animationSpeedMultiplier` | Ritmo de ataque |
| `OneHandIK`, `OneHandIKPos/Rot` | `oneHandUsesIK` + pesos (+`EnableIK`) | Colocación activa del arma al parar (experimental) |
| `SideDodge` | `sideDodgeEnabled` | Esquiva lateral (experimental) |

Arquetipos: **Recruit / Soldier / Veteran / Duelist**, asignación por `creatura > facción > Brain > dificultad > por defecto`, más `GlobalLevelShift` y variación `Jitter` determinista por criatura. Tabla completa de valores: [CONFIG.reference.md](CONFIG.reference.md).

## 4. Capa de código

### 4a. Filtro de ataques por arma (`AttackRules`, `StanceCurator`)
1. **Clasificación del arma** (`WeaponInspector` → `WeaponProfile`): rasgos reales desde los *damagers* del item (`Damager.type`: Slash→`Edge`, Pierce→`Point`, Blunt→`Blunt`, ignorando los de mango `Handle*`), flags `weaponAttackTypes`, manos, clase (por id y, si no, por rasgos). Se puede forzar con `Rules.TraitOverrides/ClassOverrides`.
2. **Tipo real del ataque** (`ResolveKind`): por id/address (`thrust|stab` → estocada, `swing|slash` → corte, `push|bash` → empujón) en lugar del tipo declarado (que es erróneo p. ej. en `CounterThrust`).
3. **Tabla de reglas** (explícita, en `AttackRules.Permits`):

| Tipo de ataque | Exige en el arma | Si no lo tiene |
|---|---|---|
| Corte (Swing) | `Edge` **o** `Blunt` | se quita (p. ej. daga sin filo) |
| Estocada (Thrust) | `Point` | se quita (p. ej. espada de cabeza contundente) |
| Empujón (Bash) | ser un escudo | se quita |
| cualquiera, en arma **desconocida** (sin damagers legibles) | — | **se mantiene** (`UnknownWeaponsAreUnrestricted`) |

   Ataques a dos manos exigen que ambas armas lo permitan. Los ataques de la mano izquierda se juzgan contra el arma izquierda. Resultado auditado sobre las 92 armas cuerpo a cuerpo del juego base: solo cambian 4 ([research/vanilla-weapon-rules.md](research/vanilla-weapon-rules.md)): `DaggerRaktaSai`, `DaggerSentaraStiletto` (pierden cortes), `ViennoiseSword`, `SwordGreatExecution` (pierden estocadas).
4. **Reponderación por clase** (`ClassBias`): p. ej. Rapier ×2 estocada/×0,25 corte; Dagger ×1,3 estocada/×0,8 corte/×0,6 estocada con paso.
5. **Readmisión**: ataques que el filtro del juego quitó solo por estar mal tipados y que son legítimos se devuelven (copia en el clon por criatura). Efecto real: las lanzas recuperan `CounterThrust`.
6. **Riposte de estocada**: si tras filtrar no quedan ripostes (todos los de serie son cortes), se añade uno copiando la estocada base y se enlaza a las siete guardias.
7. **Red de seguridad**: nunca se deja una lista de ataques vacía (se conserva la de serie).

### 4b. Variedad (`AttackMemory`, `VarietyScorer`, `PlanNext`)
- **Memoria** de los últimos N (6) ataques con su dirección. **Frescura** = producto de penalizaciones decrecientes con la antigüedad: mismo ataque ×0,12, misma dirección ×0,45, dirección vecina (45°) ×0,8, y ×1,4 si la dirección no aparece en memoria. Intensidad por arquetipo (`VarietyStrength`).
- **Director**: tras cada ataque elige el siguiente entre los motions con rango válido (tolerancia = `maxRangeDelta`); amortigua el resto (×0,04) para orientar al selector de serie, y solo cambia la pose de guardia (`ChangeIdlePose`) si el ataque deseado exige otra y compensa (los de otra pose pesan ×0,35, porque el cambio bloquea ataques ≈0,8 s).
- **Combos**: al final de un ataque sin parar/golpear, con `ComboChance`, se dispara su `chainAttack` (el del propio juego, ya filtrado por arma).
- **Fintas** (`FeintPlanner`): en el *wind-up* se decide (probabilidad por arquetipo; ×`ReactiveFeintBoost` si el arma del objetivo ya cubre la zona final del ataque; decae con fintas seguidas; no en empujones, estocadas con paso, ventanas < 0,28 s ni a más de 4 m). Se aborta entre el 35 % y el 70 % del tiempo previo al daño con `StopAttack` y se lanza otro ataque, **de dirección lo más distinta posible** (`ChooseRedirect`), con `TriggerAttack`. `DoubleFeintChance` encadena una segunda finta.
- **Direcciones** (`Directions`, `DirectionResolver`): cada ataque tiene etiqueta de las 9, resuelta por (1) `Rules.DirectionOverrides`, (2) **medición** (posición de la punta del arma en el sistema local del atacante entre el inicio del daño y el pico; ≥3 muestras con ≥70 % de acuerdo), (3) pista del nombre (solo `RtL/LtR/TtL/TtR/Thrust`), (4) `Unknown` (neutral para la memoria).

### 4c. Defensa
- **Ajuste por arquetipo** (§3) y **IK de una mano** (experimental).
- **Lectura de fintas** (`StrikeTracker` + `FeintReader`): se vigila la velocidad del golpe entrante que el juego ya calcula (`attackInfoLeft/Right`). Un giro > 75° en 0,35 s es una finta/redirección. Con probabilidad `1 − FeintReadSkill` la guardia **se queda congelada** (`parryAdjustSpeed ×0,12`, `FooledDuration`), y puede además comprometerse a una esquiva (`TryToDodge`): así *sufre* las fintas. Si la lee, la guardia se corrige más rápido (×1,6) durante 0,3 s.
- **Fatiga de paradas** (`ParryFatigue`): `MaxParriesBeforeFatigue` paradas en `FatigueWindow` (3 s) dejan al NPC sin poder parar `FatigueDuration`: **no es imbatible**.
- **Contraataque**: (i) tras parar, mediante `RiposteChanceShift/RiposteDelayMul` (mecanismo del juego), (ii) **tras esquivar**: al terminar la esquiva, con `CounterAfterDodgeChance` y dentro de 2,6 m, `AttackTarget`, (iii) combos.
- **Clasificación de golpe entrante**: `Directions.DefenderLocalToAttackerLocal` + `Zones` permiten etiquetar el golpe con las mismas 9 direcciones y llevarlo a la rejilla 3×3 → pose de guardia (`Zones.GuardPoseId`). *En esta versión se usa para las fintas reactivas y el log; la selección fina de pose sigue siendo la del juego (mezcla continua de las 7 guardias).*

## 5. Configuración
JSON (`CompetentAI.config.json`) con valores por defecto razonables, comentado, fusionable y recortado a rangos seguros; menú in-game que solo apaga. Ver [CONFIG.md](CONFIG.md).

## 6. Compatibilidad
Estrategia y análisis por mod en [COMPATIBILITY.md](COMPATIBILITY.md). Resumen: sin catálogos, sin parches, multiplicativo, aditivo sobre el stance resultante, cada módulo apagable.

## 7. Rendimiento
- **Por frame y por NPC**: `Update`: comparación de un tiempo y retorno inmediato fuera de combate; en combate, 3 comprobaciones baratas. `FixedUpdate` (defensa): lecturas de campos + `StrikeTracker` (buffer circular fijo de 32 muestras, sin asignaciones). Sin LINQ, sin `new` en el bucle.
- **Por decisión de ataque** (≈1 vez por 1–3 s y NPC): una lista/array reutilizados (`_candScratch`, `_weightScratch`), O(candidatos ≤ 12 × memoria 6).
- **Por spawn/cambio de arma**: curación del stance (asignaciones puntuales, no por frame) y clasificación del arma (cacheada por id de item).
- **Global**: un barrido de `Creature.allActive` cada 0,5 s; guardado de calibración cada 30 s solo si hay cambios.
- Excepciones: nunca en el camino normal; el limitador de log evita inundar.

## 8. Plan de pruebas
Unitarias (94, `dotnet test`) para toda la lógica pura, incl. el fichero de configuración distribuido y el manifest. Manuales: [TESTING.md](TESTING.md).

## 9. Limitaciones conocidas y puntos NO VERIFICADOS
| Punto | Estado |
|---|---|
| El mod **no se ha ejecutado** en el juego | Toda la integración está por verificar; el compilador y el autochequeo solo garantizan que los miembros existen |
| Etiquetas de dirección de los clips de serie | Inferidas por nombre para 5 ataques; el resto `Unknown` hasta medir. **NO VERIFICADO** que `RtL` viaje hacia la izquierda del atacante |
| Cortes verticales puros (↓ ↑) | No existen como clip; la lógica los modela pero no pueden ejecutarse sin animaciones nuevas |
| IK de una mano al parar | **NO VERIFICADO** visualmente; el juego lo tiene apagado en todos los Brains (puede ser por motivos estéticos). Efecto secundario conocido (leído en `Item.SetParryMagic`, que `EnableIK` activa): el arma del NPC pasa a **parar también hechizos** (colliders con tag `ParryMagic`) |
| Esquiva lateral | **NO VERIFICADO** que las animaciones `DodgeType` Left/Right existan/queden bien |
| Cancelación de un ataque en el aire (finta) | Usa la API pública `StopAttack/TriggerAttack`, igual que hace el juego en sus propios ripostes; el aspecto visual (corte brusco de la animación) **NO VERIFICADO** |
| Armas a dos manos usan animaciones de espada a una mano | Limitación del juego (stance 1H), no del mod |
| Cambio de arquetipo en vivo | `GlobalLevelShift` del menú solo afecta a NPCs que aparecen después |
| `ThunderScript.ScriptDisable/ScriptUnload` | Según el SDK no se invocan hoy; el mod limpia en `onLevelUnload` |
| Compatibilidad con otros mods | Sin probar (ver COMPATIBILITY.md) |
