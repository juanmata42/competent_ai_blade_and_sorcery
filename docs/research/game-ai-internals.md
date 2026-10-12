# Cómo funciona la IA cuerpo a cuerpo de B&S (1.3.1.260730005)

**Fuente de todo lo de este documento:** `ThunderRoad.dll` 1.3.1.260730005 (instalado en el equipo, consulta local con ilspycmd 9.1.0.7988, 2026-10-12) y los JSON base extraídos de `StreamingAssets/Default/bas.jsondb` (un zip). El código descompilado y los JSON **no** están en el repositorio (ver `.gitignore`).
Convención: **[C]** = confirmado en el código/datos del juego; **[I]** = inferido; **[NV]** = no verificado.

## 1. Cómo se define una criatura y su Brain

- Una criatura usa un `BrainData` (catálogo `Brains/`, p. ej. `HumanEasy`, `HumanMedium`, `HumanHard`, `HumanHardHeavy`, `HumanVIP`) **[C]**.
- `BrainData` tiene `difficulty` (1/2/3), `treeID` (árbol de comportamiento `Human` → `HumanCombatMelee`) y una lista `modules` de `BrainData.Module` **[C]**.
- Al spawnear, `Brain.LoadCoroutine` toma una instancia del pool o **clona el JSON** (`CloneJsonAsync`): **cada criatura tiene su propia instancia** de `BrainData` y de cada módulo **[C]** (`Brain.cs`, `BrainData.TakeFromPool`). Por tanto se pueden modificar campos de los módulos por criatura sin afectar a las demás ni al catálogo.
- `BrainData.FixedUpdate/Update/LateUpdate` recorren `modules` **en orden de lista** **[C]** (`BrainData.cs`). Un módulo añadido al final de `modules` se ejecuta **después** de los de serie dentro del mismo tick.
- Módulos de combate relevantes (todos en el namespace `ThunderRoad`): `BrainModuleMelee`, `BrainModuleDefense`, `BrainModuleStance`, `BrainModuleDetection`, `BrainModuleHitReaction`, `BrainModuleEquipment`, `BrainModuleMove` **[C]**.

### Valores reales de los Brains de serie (HumanEasy/Medium/Hard/HardHeavy)
| Campo | Easy | Medium | Hard | HardHeavy |
|---|---|---|---|---|
| `BrainModuleMelee.meleeMax` | 1 | 2 | 3 | 3 |
| `minMaxTimeBetweenAttack` | 2–3 s | 0.5–1 s | 0–0.5 s | 0–0.5 s |
| `recoilNothingRiposteChance` (x,y) | 0.3, 0.35 | 0.1, 0.15 | 0.0, 0.05 | 0.0, 0.05 |
| `minMaxRiposteDelay` | 1.5–2 s | 0.5–1.2 s | 0.5–1 s | 0.5–1 s |
| `BrainModuleDefense.aboutToBeHitTiming` | 0.5 | 0.5 | 0.5 | 0.5 |
| `parryAdjustSpeed` | 7 | 7 | 7 | 7 |
| `dodgeChanceCurve` | 0.5→1.0→0.0 | igual | igual | 0→0.6→0 |
| `sideDodgeEnabled` | **false** | false | false | false |
| `oneHandUsesIK` | **false** | false | false | false |
| `thrustSensitivityVelocity/Direction` | 0.1 / 0.1 | igual | igual | igual |

Hallazgo clave: **la IK de parada con un arma a una mano está desactivada en todos los Brains** (`oneHandUsesIK=false`), así que un NPC con una sola arma no coloca activamente el arma en la trayectoria del golpe: solo mezcla poses de guardia animadas. Con escudo sí hay IK (`shieldUsesIK=true`). **[C]**

## 2. Cómo se selecciona un ataque

1. `AttackMelee` (nodo del árbol `HumanCombatMelee`) llama a `BrainModuleMelee.EvaluateNode`, que comprueba estado/rango/ángulo/aliados y, si todo está bien, llama a `MeleeIdle.PickBestMotion(attacker, target, maxRangeDelta)` **[C]**.
2. `MeleeIdle` (la **pose de guardia actual**) guarda `attackMotions`: los `openingAttacks` del `MeleeStanceData` cuyo `sourceIdle` es `"Any"` o la pose actual **[C]** (`MeleeIdle.PopulateLists`).
3. `PickBestMotion` filtra por **rango predicho**: `|motion.minRange·escala − distancia_predicha| ≤ maxRangeDelta·2·shift` (≈0,16–0,24 m con `maxRangeDelta=0.2`) y elige con `WeightedFilteredSelect` usando `motion.weight × deltaRatio`; el último ataque (`lastPick`) recibe peso ×0,01 **[C]**.
4. El `MeleeStanceData` por criatura es un **clon filtrado por arma** (`StanceData.GetFilteredClone` → `MeleeStanceData.MakeFilteredClone`): se descartan los ataques con `difficulty` mayor que la del Brain y los cuyo `attackMotion` (Swing/Thrust) no coincide con `ItemModuleAI.weaponAttackTypes` de la(s) arma(s) **[C]**.
5. Hay solo **3 datos de postura**: `HumanMelee1hStance`, `HumanMeleeDualWieldStance`, `HumanMeleeShieldStance` **[C]**. Las armas a dos manos (lanzas, hachas largas, espadones…) usan `HumanMelee1hStance` (su `stanceInfosByOffhand` solo tiene `SameItem → ""` y cae en `defaultStanceInfo`) **[C]**.
6. Ataques de `HumanMelee1hStance` (12 openers, 5 chains, 6 ripostes): RtL, LtR, TtL, TtR (con variantes *Step*/*Lunge*), `SwingCombo`, `Thrust`, `LungeThrust`; chains `CounterSwing*`/`CounterThrust`; ripostes `Riposte{High,Mid,Low}{Left,Right}`. **No hay cortes verticales puros (arriba/abajo) ni ascendentes verticales**. **[C]** (`Stance_HumanMelee1hStance.json`)
7. Las animaciones son **clips fijos** (`AttackMotion.address` → `AnimationClip`): sin SDK/Unity no se pueden crear clips nuevos. **[C]**

## 3. Cómo se decide parar/esquivar

- `BrainModuleDetection.CheckIncomingStrike` recorre `ParryTarget.list` (armas empuñadas con componente `ParryTarget`) y rellena `attackInfoLeft/Right` (`ParryTarget.ParryInfo`: `colliderPoint`, `targetPointVelocity`, `onHitCourse`, `windingUp`, `distance`…). Ignora armas con `ignoredByDefense` y las que estén "feinting" (variable `"Feinting"` del skill de fintas del jugador) **[C]**.
- `BrainModuleDefense.FixedUpdate` calcula `attackDangerLevel` a partir de la velocidad del golpe (`StanceDefenseSettings.minMaxIncomingAttackSpeed`), y mueve dos floats del Animator, `BlockSide`/`BlockHeight`, hacia el punto de impacto normalizado a `parryAdjustSpeed` u/s. Eso mezcla 7 poses de guardia (`GuardUpperLeft/Right`, `GuardMidLeft/Right`, `GuardNeutral`, `GuardLowerLeft/Right`) **[C]**.
- `Brain.hashBlock` ("Block") sube cuando el golpe está cerca; con escudo/arma+IK se llama a `UpdateSideIK`. **[C]**
- Esquiva: `UpdateMeleeProtect` programa `DodgeWait` si `attackInfo.onHitCourse` y `UnityEngine.Random.value <= dodgeChanceCurve(1 - vida%)`; `TryToDodge` elige al azar entre Back/Right/Left (los laterales solo si `sideDodgeEnabled`), y el alto de la esquiva trasera (High/Med/Low) por la altura del golpe. `TriggerDodge(Dodge)` es público **[C]**.
- **Qué se usa del golpe entrante**: posición del punto de impacto, velocidad (módulo), si va en curso de colisión, si es estocada (`incomingPierce`, solo para decidir esquivar en vez de parar). **Qué se ignora**: la *dirección del corte* (tipo diagonal/horizontal/vertical), el tipo de daño, y cualquier "intención" (fintas). No hay compromiso: el bloqueo sigue al punto de impacto en tiempo real.
- Tras parar: `BrainModuleDefense.OnWeaponCollision` + `BrainModuleHitReaction.StaggerCoroutine` deciden recoil/nada/**riposte** con `recoilNothingRiposteChance`, `minMaxRiposteDelay`; el riposte sale de `GuardPose.riposteOptions` (`BlockRiposte`) o de `AttackMotion.GetChainAttack` **[C]**.
- Límites: `canBlock` exige no estar atacando, no estar con el animator ocupado, etc. Un NPC que ataca no puede parar. **[C]**

## 4. Qué es configurable por JSON/catálogo y qué requiere código

| Aspecto | JSON/catálogo | Código |
|---|---|---|
| Campos de `BrainModuleMelee/Defense/Stance` | Sí (por Brain, con merge por `$type`) | Sí (por criatura en runtime) |
| Lista de ataques, pesos, rangos, `sourceIdle`, combos | Sí (`Stances/*.json`, `JsonMergeKey`=`id`) | Sí (clon por criatura) |
| Qué ataques admite cada arma | Solo `ItemModuleAI.weaponAttackTypes` (Swing/Thrust) | Reglas propias |
| Fintas, redirección de ataques, contraataque tras esquiva | No | Sí (`BrainModuleMelee.StopAttack/TriggerAttack/AttackTarget` son públicos) |
| Clips de animación nuevos | Addressables (SDK/Unity) | No |
| Opciones in-game | — | Atributos `ModOption*` (`ThunderRoad`) |

## 5. Puntos de extensión del SDK (verificados)

- `ThunderScript` (`ScriptLoaded/ScriptEnable/ScriptUpdate/ScriptFixedUpdate/ScriptLateUpdate/ScriptUnload`); el `ModManager` instancia por reflexión toda subclase no abstracta de `ThunderScript` en las DLL de la carpeta del mod **[C]**.
- `manifest.json` (`ModManager.ModData`: `Name, Description, Author, ModVersion, GameVersion, Thumbnail`). **`GameVersion` debe ser exactamente `1.0.0.0`**: `ManifestVersionCheck` compara con `GameSettings.minModVersion`, que en este juego es `"1.0.0.0"` (leído del asset `ThunderRoad.Settings` en `default_assets_all.bundle`) **[C]**.
- Eventos estáticos en `EventManager`: `onCreatureSpawn`, `onCreatureAttacking` (WindUp/Attack/FollowThrough/End/Cancel), `onCreatureAttackParry`, `onCreatureKill`, `onCreatureDespawn`, `onLevelLoad/Unload`… **[C]**.
- Eventos de módulo: `BrainModuleStance.OnStanceChange` (se emite justo después de crear el clon filtrado del `StanceData` y antes de fijar la pose), `Brain.OnAttackEvent`, `Creature.OnDamageEvent`… **[C]**.
- `BrainData.modules` es una `List<Module>` pública: se puede **añadir un módulo propio** a la instancia de una criatura y llamar a su `Load(creature)` **[C]**.
- Atributos `ModOption`, `ModOptionCategory`, `ModOptionTooltip`, `ModOptionOrder`, `ModOptionFloatValues`, `ModOptionIntValues`, `ModOptionButton`: sobre campos/propiedades/métodos **estáticos** **[C]**.
- **No hay Harmony/MonoMod en `Managed/`** (solo `Mono.Cecil`) **[C]**: no se asume parcheo en runtime.

## 6. Diferencias entre versiones que afectan al diseño
- Esta documentación es solo para **1.3.1.260730005** (Unity 2021.3.38f1, Mono). **[I]** No se ha comparado con versiones anteriores; las firmas usadas deben revalidarse al actualizar el juego (el proyecto compila contra el DLL real, así que un cambio de firma rompe la compilación en vez de fallar en silencio).

## 7. Diagnóstico por causa raíz de las 3 quejas

### Queja 1 — Defienden mal
| Causa [C] | Dónde | Solución |
|---|---|---|
| IK de parada de un arma desactivada (`oneHandUsesIK=false`) → solo poses animadas | `Brain_Human*.json`, `BrainModuleDefense.UpdateMeleeProtect` | Datos/ajuste por criatura (activar IK con pesos moderados) |
| Seguimiento continuo del impacto sin compromiso ni lectura de dirección; no hay "fintas" posibles ni errores humanos | `UpdateMeleeProtect` | Módulo propio (capa de decisión por encima) |
| Esquiva elegida al azar entre direcciones, sin mirar la dirección del corte; laterales apagados | `TryToDodge` (virtual pero no sustituible sin reemplazar el módulo) | Módulo propio llamando a `TriggerDodge` |
| Parar cuesta `canBlock` (no atacando) y no hay contraataque tras esquivar | `BrainModuleDefense.canBlock` | Módulo propio (contraataque tras esquiva) |
| Riposte controlado solo por azar y retraso fijo | `recoilNothingRiposteChance`, `minMaxRiposteDelay` | Ajuste de campos por arquetipo |

### Queja 2 — Atacan siempre igual
| Causa [C] | Solución |
|---|---|
| Solo 2–3 ataques son candidatos por pose a la vez (filtro de **rango** muy estrecho + `sourceIdle`), y la pose solo cambia cada 5–10 s: el patrón resultante es A-B-A-B | Director de ataques: elegir un ataque deseado con memoria de dirección y mover la pose (`ChangeIdlePose`) a su `sourceIdle`; reponderar pesos |
| Pesos casi uniformes (1.0) y solo `lastPick` penalizado ×0,01 | Reponderación por memoria de los últimos N ataques |
| `CounterThrust` está marcado como `Swing` en los datos (error de datos): las lanzas (solo `Thrust`) nunca lo reciben, así que solo tienen 2 ataques | Reclasificar por id/dirección en el clon por criatura |
| Sin fintas ni combos controlados | Módulo propio |

### Hallazgo del primer log real (2026-10-12): armas de punta sin ataques
El clon de serie de `MeleeStanceData` descarta también los ataques con `difficulty` > `BrainData.difficulty` (`TooDifficult`). **Las estocadas del stance base (`Thrust`, `LungeThrust`, `CounterThrust`) tienen dificultad 3.** Resultado observado en el juego: una lanza (solo `Thrust`) con un Brain de dificultad 2 quedó con `openers(0)=[] chains(0)=[] ripostes(0)=[]` — un NPC con lanza en `HumanEasy/HumanMedium` **no tiene ningún ataque** en el juego de serie. **[C: log del juego]**

### Queja 3 — Ataques que no corresponden al arma
| Causa [C] | Ejemplos reales |
|---|---|
| El filtro nativo solo distingue Swing/Thrust del **item data** (`weaponAttackTypes`), no el tipo de daño real de los *damagers* | `DaggerSentaraStiletto`, `DaggerRaktaSai` (solo `EdgelessDaggerPierce`) tienen `Swing, Thrust` → hacen cortes sin filo; `ViennoiseSword` (solo `Mace1H` contundente) tiene `Swing, Thrust` → estocadas sin punta |
| Armas a dos manos usan animaciones de espada a una mano | Lanzas/hachas largas/mazas |
| Un item modded sin `ItemModuleAI` completo cae en los valores por defecto (`Swing, Thrust`) | cualquier arma modded |

Clasificación de la solución: **ajuste por criatura en runtime (extender, no reemplazar)** para las tres; no hace falta sobrescribir ningún catálogo.

## 8. Limitaciones técnicas descubiertas
- Sin Unity/SDK no se pueden crear animaciones: las "9 direcciones" se implementan como **capa lógica** sobre los ~25 clips existentes; las direcciones sin clip (corte vertical descendente puro, ascendente puro) se aproximan con el más cercano y quedan documentadas. Un pack de animaciones externo se integra automáticamente (calibración por medición en runtime + reglas por id/dirección).
- La dirección real de cada clip **no se ha verificado visualmente** (no se puede ejecutar el juego): el mod la **mide** en runtime (posición de la punta del arma en el sistema local de la criatura) y registra el resultado en el log.
