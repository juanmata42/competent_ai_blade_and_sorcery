# Síntesis de la investigación y decisión de arquitectura (Fase 1.4)

Fecha: 2026-10-12 · Juego: 1.3.1.260730005 (PC, Unity 2021.3.38f1, Mono) · Fuentes: [game-ai-internals.md](game-ai-internals.md) (código real del juego), [existing-mods.md](existing-mods.md) (web), [sdk-docs-notes.md](sdk-docs-notes.md) (doc oficial).

> Nota de proceso: el plan original pedía parar aquí a esperar decisión. El usuario autorizó expresamente seguir sin parar ("confía en tu criterio") y recentró el objetivo: **fintas y contraataques hechos y sufridos por los NPCs, y ataque/defensa en 9 direcciones adaptadas a cada arma (o arma + escudo)**. Las decisiones de abajo las he tomado yo con ese mandato; quedan abiertas a revisión.

## 1. Qué se puede cubrir recomendando mods existentes

| Necesidad | Mod que la cubre (según su autor, sin probar) | Por qué no basta |
|---|---|---|
| Más agresividad / tempos de ataque / ripostes más rápidas | *Ai Adjuster* (DLL + ModOptions, 2025-12), *Better AI* (+30 % velocidad), *Relentless NPCs Reborn* | Solo ajustan tiempos; ninguno cambia **qué** ataque se elige ni **cómo** se defiende |
| Fintas / predicción / contraventanas | *AdaptiveAI 1.5* (DLL cerrada, 2026-01) | Cerrado y sin fuente: no se puede verificar ni extender; no declara versión de juego; no filtra por arma |
| Ataques por clase de arma (lanza, 2H, puños) | Packs de *Palpatine* (JSON + bundles de animación) | Cada clase necesita su propio brain/stance/animaciones; no es un filtro general |
| Contraataque tras bloquear, más movimientos | *BossaNova's Dueling Melee Bots* (JSON, declara 1.3.1) | Sobrescribe tablas de creatura en una variante; solo datos |

Conclusión: **ningún mod encontrado** combina (a) filtro de ataques por lo que el arma puede hacer realmente, (b) fintas realizadas y sufridas, (c) elección de ataque con memoria por dirección, y (d) código abierto y verificable. Ningún mod con DLL publica su fuente. (existing-mods.md §2.2)

## 2. Huecos que justifican un mod propio

1. **Queja 3 (ataque↔arma)**: causa raíz confirmada en el DLL — el filtro nativo compara solo `Swing/Thrust` del item data con el tipo *declarado* de cada ataque. Resultado real: dagas sin filo (`DaggerSentaraStiletto`, `DaggerRaktaSai`) que cortan; `ViennoiseSword` (solo cabeza contundente) que estoquea; lanzas con solo 2 ataques porque `CounterThrust` está mal tipado en los datos.
2. **Queja 2 (repetición)**: causa raíz confirmada — a un rango dado solo 2–3 ataques son candidatos por pose y la pose cambia cada 5–10 s: el patrón es A-B-A-B (el único anti-repetición es `lastPick ×0,01`).
3. **Queja 1 (defensa)**: causa raíz confirmada — todos los Brains de serie tienen `oneHandUsesIK=false` (no hay colocación activa del arma con un arma), el bloqueo sigue el punto de impacto sin decisión ni compromiso, la esquiva es aleatoria y sin laterales, no hay contraataque tras esquivar y las fintas no pueden funcionar.
4. **Fintas hechas y sufridas**: no existen en el juego para NPCs (el juego solo ignora armas del jugador con la skill *Feinting Swings*).

## 3. Decisión de arquitectura

**Mod solo código para PC** (el usuario eligió "Solo PC"), una DLL (`CompetentAI.dll`, `netstandard2.1`) + `manifest.json` + `CompetentAI.config.json`. Sin Unity, sin SDK, sin catálogos sobrescritos, sin Harmony.

Capas:
1. **Core puro** (`CompetentAI.Core`): `Directions` (9 ángulos), `Zones` (rejilla 3×3 defensiva), `AttackRules` + `WeaponClassifier`, `Variety` (memoria + ponderación), `Feints`, `Defense` (detector de reversión, lector de fintas, fatiga de paradas), `Calibration`, `Profiles`/`Config`. 124 tests.
2. **Integración** (`CompetentAI.Mod`): `ThunderScript` + un `BrainData.Module` propio **añadido al final** de `brain.instance.modules` de cada NPC.
3. **Datos**: JSON de configuración con arquetipos (Recruit/Soldier/Veteran/Duelist); no se toca ningún JSON del juego.

Por qué un módulo añadido en lugar de sustituir o parchear:
- `BrainData.FixedUpdate/Update` recorre los módulos en orden de lista → el módulo propio corre **justo después** de los de serie en el mismo tick (verificado en `BrainData.cs`), sin necesidad de hooks.
- Cada criatura tiene su propia copia de `BrainData` (clon JSON/pool) → modificar campos públicos por criatura es seguro y no contamina el catálogo.
- No hay Harmony en `Managed/`; los mods de 2026 que lo empaquetan se arriesgan a copias duplicadas. Evitarlo reduce conflictos.
- Es lo que pide la regla "extender sobre reemplazar" y deja el juego intacto si el módulo falla (4 errores → el módulo se desactiva solo para esa criatura).

### Las 9 direcciones (decisión de interpretación)
Interpreto "9 direcciones de corte-ataque" como los **nueve ángulos clásicos de esgrima**: 8 cortes (↓, ↙, ←, ↖, ↑, ↗, →, ↘ según hacia dónde viaja el arma en el sistema local del atacante) + la estocada. La defensa usa la misma etiqueta para el golpe entrante y una rejilla 3×3 de zonas (alto/medio/bajo × izq/centro/der) que se corresponde con las 7 poses de guardia del juego.

**Límite técnico honesto**: el juego solo tiene ~25 clips de ataque y sin Unity no se pueden crear animaciones. Las direcciones sin clip (corte vertical descendente puro y ascendente puro) no se pueden *ejecutar*; la capa lógica las modela y cualquier pack de animaciones externo se integra solo (el mod **mide** la dirección real de cada clip en runtime, y se puede forzar por configuración).

### Qué hace cada módulo (todos desactivables)
| Módulo | Qué hace | Toca (en la copia por criatura) |
|---|---|---|
| ProfileTuning | Arquetipo → multiplicadores sobre los valores propios del Brain | `BrainModuleMelee`, `BrainModuleDefense` campos públicos |
| WeaponRules | Quita ataques ilegítimos, devuelve los mal tipados, añade riposte de estocada | clon de `MeleeStanceData` (`openingAttacks/chainAttacks/ripostes`) |
| Variety | Memoria de N ataques + plan; reorienta pesos y la pose de guardia | `AttackMotion.weight`, `ChangeIdlePose` |
| Feints | Aborta un ataque antes de su ventana de daño y lo redirige a otra dirección | `StopAttack` + `TriggerAttack` (API pública) |
| Combos | Encadena el `chainAttack` del ataque tras el final | idem |
| DefenseReading | Detecta reversión del golpe: la guardia cae o lee la finta; fatiga tras muchas paradas | `parryAdjustSpeed`, `parryEnabled` |
| CounterAfterDodge | Contraataca tras esquivar | `AttackTarget` |
| OneHandIK *(experimental)* | Veteran/Duelist colocan el arma en la trayectoria | `oneHandUsesIK` + `EnableIK` |
| SideDodge *(experimental)* | Duelist puede esquivar de lado | `sideDodgeEnabled` |
| Calibration | Mide la dirección real de cada animación y la guarda | solo lectura |

## 4. Riesgos y mitigaciones

| Riesgo | Probabilidad | Mitigación |
|---|---|---|
| Algo falla en el juego y no puedo verlo (no se puede ejecutar) | Alta | Autochequeo de API al arrancar, logs por nivel, `try/catch` por entrada con autodesactivado por criatura, kill-switch por módulo y maestro, plan de pruebas |
| Las fintas se ven "robóticas" (cancelación brusca de animación) | Media | Solo en la ventana previa al daño; probabilidad por arquetipo; `Feints.Enabled`; no en estocadas con paso |
| `OneHandIK` hace brazos extraños | Media | Experimental, solo Veteran/Duelist, pesos moderados, un solo error lo desactiva por criatura |
| Esquiva lateral sin animación válida | Media | Solo Duelist, desactivable; marcado NO VERIFICADO |
| El cambio de pose del director retrasa ataques (el juego bloquea ataques ≈0,8 s durante el cambio) | Media | Penalización ×0,35 a motions de otra pose; solo cambia pose cuando compensa |
| Conflicto con mods de IA que tocan los mismos campos (AdaptiveAI, Ai Adjuster…) | Media | Documentado en COMPATIBILITY.md; multiplicativo sobre el valor actual; módulos independientes; se puede apagar |
| Cambio de API en una actualización del juego | Media | Compila contra el DLL real + autochequeo en runtime que lo desactiva y avisa |

## 5. Alternativas descartadas
- **Solo JSON** (sobrescribir Brains/Stances): imposible hacer fintas, filtro por arma real ni elección con memoria; y pisaría catálogos que otros mods tocan.
- **Harmony** para parchear `PickBestMotion`/`TryToDodge`: más potente, pero añade dependencia ausente en el juego, riesgo de duplicados y de choque con parches ajenos.
- **Sustituir `BrainModuleDefense`** por una subclase: invasivo (cachés del `BrainData`, referencias en otros módulos y nodos del árbol) y poco compatible.
- **Red neuronal / aprendizaje**: sin posibilidad de entrenar ni validar sin ejecutar el juego.

## 6. Preguntas para el usuario (no bloquean; mi propuesta por defecto entre paréntesis)
1. ¿Cuáles son los nueve ángulos que tienes en mente? (ocho cortes + estocada).
2. ¿Quieres que `OneHandIK`/`SideDodge` vengan activados? (sí para Veteran/Duelist; se apaga en el menú).
3. ¿Mods de animación que vayas a usar con esto? (ninguno por ahora; hay `DirectionOverrides` para integrarlos).
4. ¿Esquema de dificultad deseado? (4 arquetipos mapeados a los Brains de serie; `GlobalLevelShift` y menú para subirlo/bajarlo).
