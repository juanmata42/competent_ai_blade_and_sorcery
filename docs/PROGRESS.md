# PROGRESS — Competent AI (Blade & Sorcery)

## Estado actual (2026-10-12)
**Versión 0.6.0 (138 tests en verde) y documentada. Probada parcialmente por el usuario (0.4.x); los cambios de 0.5.0 y 0.6.0 aún no** (no se puede ejecutar desde el entorno de desarrollo). Siguiente hito: que el usuario la pruebe con [TESTING.md](TESTING.md).

| Fase | Estado |
|---|---|
| 0 Entorno | Hecha |
| 1 Investigación | Hecha: [game-ai-internals](research/game-ai-internals.md), [existing-mods](research/existing-mods.md), [sdk-docs-notes](research/sdk-docs-notes.md), [SUMMARY](research/SUMMARY.md), [vanilla-weapon-rules](research/vanilla-weapon-rules.md) |
| 2 Diseño | Hecha: [DESIGN](DESIGN.md) |
| 3 Implementación | Hecha (incrementos 1–6 fusionados en la práctica; ver commits) |
| 4 Cierre | Hecha salvo validación en el juego: build/zip, [TESTING](TESTING.md), [CONFIG](CONFIG.md), [COMPATIBILITY](COMPATIBILITY.md), [CHANGELOG](../CHANGELOG.md) |

## Entorno (verificado en el equipo)
Juego `D:\SteamLibrary\steamapps\common\Blade & Sorcery`, versión **1.3.1.260730005**, Unity 2021.3.38f1 (Mono), `GameSettings.minModVersion = "1.0.0.0"` (por eso `manifest.GameVersion = "1.0.0.0"`). .NET SDK 9.0.306, Git 2.55, ilspycmd 9.1.0.7988 (global). Sin SDK de B&S (no hace falta: mod solo-código compilado contra `Managed/`).

## Decisiones tomadas
1. **Solo PC**, solo código + JSON de configuración propio (decisión del usuario).
2. Versión objetivo 1.3.1 (decisión del usuario). Sin SDK de Unity.
3. El usuario no usa mods de IA; su prioridad: fintas y contraataques hechos y sufridos + ataque/defensa en 9 direcciones adaptadas al arma/escudo. Autorizó seguir sin paradas y commitear en `main`.
4. **Nueve direcciones** = 8 cortes + estocada (**confirmado por el usuario**), también **avanzando** (en 8 direcciones además de en el sitio) y **encadenadas en secuencias tipo kata asumiendo que el primer golpe será parado, interrumpibles** (v0.2.0). Parar "igual": en el juego no hay clips de parada con paso; se modela como presión tras parar.
4b. Menú in-game: interruptor general + **nivel de destreza** (Auto/1–4) en vivo (v0.2.0).
5. Arquitectura: módulo propio **añadido** a `brain.instance.modules` (sin Harmony, sin sustituir módulos, sin catálogos). Una sola DLL (Core compilado dentro).
6. El mod **mide** la dirección real de cada animación (no se fía de los nombres) y la persiste en `calibration.json`.
7. Menú in-game solo puede apagar (efectivo = JSON AND menú).
8. `OneHandIK` y `SideDodge` quedan **activados para Veteran/Duelist pero marcados experimentales** (apagables por separado).

## Preguntas abiertas (no bloquean)
- ¿Cómo quieres que sea "parar avanzando"? (hoy: tras parar, se favorecen ataques con paso; no hay animaciones de parada con paso).
- ¿Mods de animación/IA que quieras usar con esto? (hoy ninguno)
- Resultados de las pruebas de TESTING.md (sobre todo F3 sufrir fintas, F7 IK, F8 esquiva lateral, F1 aspecto de las fintas).

## Riesgos principales (ver DESIGN §9)
Todo lo de integración está sin ejecutar. IK de una mano, esquiva lateral y el aspecto visual de las fintas son los puntos más inciertos.

## Siguiente paso
1. El usuario ejecuta TESTING.md §1–2 (arranque + armas) y reporta con la plantilla.
2. Según el reporte: ajustar o desactivar módulos problemáticos; afinar etiquetas de dirección con `calibration.json`.
3. Candidatos a v0.2: selección de guardia por clase de golpe (usar `Zones` para elegir/forzar la pose), esquiva direccional según el corte entrante, ataques de escudo reactivos, integración con un pack de animaciones con cortes verticales.

## Revisión independiente del código (hecha)
Un subagente revisó el código de integración contra el del juego (12 hallazgos, sin cuelgues garantizados pero 1 de severidad alta). **Los 12 hallazgos de código están corregidos.** Siguen sin cobertura de tests la `StanceCurator` y el `BrainModuleCompetent`, que no se pueden ejecutar fuera de Unity (riesgo cubierto por logs y TESTING.md). Además, el test del manifest solo comprueba coherencia interna (la verificación real de `GameVersion=1.0.0.0` está en `game-ai-internals.md §5`). Detalle en CHANGELOG.

## Primera prueba en el juego (2026-10-12)
- Carga correcta: `API self-check OK (68 members)`, 14 ModOptions, script instanciado. Sin excepciones del mod.
- Los hechizos de práctica **funcionaban mal** según el usuario → retirados (etiqueta `sparring-v0.3.1`).
- Del log salieron cuatro fallos reales ya corregidos en 0.4.0 (ver CHANGELOG): armas de punta sin ataques por el filtro de dificultad, medición de direcciones sesgada a estocada, nombres de ataque invertidos, detector de fintas demasiado sensible.
- **Aún sin ver en el log**: peleas reales (fintas del NPC, katas, contraataque tras esquivar) fuera de la demostración; hace falta una prueba de combate normal.

## Segunda y tercera prueba (0.4.1–0.4.2) → 0.5.0
- Feedback: mejor al parar, pero «atacan al tanteo», dudan, rodean, no encadenan 3–5 golpes; las piernas y las estocadas rectas siempre entran.
- Causas halladas en el log y el código del juego: el retroceso por parada (`recoilMinWeaponSpeed` + `StopAttack`) corta la secuencia; mi regla «golpe al cuerpo termina la kata»; los perfiles solo multiplicaban los tiempos del cerebro (un `HumanEasy` forzado a Duelist seguía lento); huecos entre ventanas de distancia; `defensePreferenceChance` del nodo `AttackMelee`; sin reacción a golpes bajos/estocadas.
- 0.5.0 los aborda (ver CHANGELOG) y añade el log `HIT …` para diagnosticar qué golpes entran. **Pendiente de validar en el juego.**

## 0.6.0 — petición del usuario: golpear donde hay huecos y cortar→estocada
- Pedido: «la IA debería atacar donde vea huecos; si no hay, atacar por un lado para entrar por el opuesto; y la técnica de dar un corte sabiendo que lo van a parar y cambiar a estocada». Hecho como módulo `Openings` (ver CHANGELOG). **Pendiente de validar en el juego**; el evento de parada del juego se dispara antes del retroceso (BrainModuleMelee), por eso la mutación puede proteger el golpe.

## Planificado, no desarrollado
- [PLAN-pattern-dummy.md](PLAN-pattern-dummy.md): maniquí que ataca en patrones (práctica de series) y, como ampliación, sitio de entrenamiento con maniquí de armadura ajustable y medidor de daño. Esperan decisiones del usuario (sección «Preguntas abiertas»).

## Registro de cambios de proceso
- 2026-10-12: Fase 0 → Fase 1 → … → 4 ejecutadas de una tacada por mandato del usuario. Revisión independiente del código de integración hecha por un subagente (ver sección anterior).
