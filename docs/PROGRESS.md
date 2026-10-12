# PROGRESS — Competent AI (Blade & Sorcery)

## Estado actual (2026-10-12)
**Versión 0.1.0 construida, con tests unitarios en verde y documentada. NO probada en el juego** (no se puede ejecutar desde el entorno de desarrollo). Siguiente hito: que el usuario la pruebe con [TESTING.md](TESTING.md).

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
4. **Nueve direcciones** = 8 cortes + estocada (interpretación mía; ver SUMMARY §3).
5. Arquitectura: módulo propio **añadido** a `brain.instance.modules` (sin Harmony, sin sustituir módulos, sin catálogos). Una sola DLL (Core compilado dentro).
6. El mod **mide** la dirección real de cada animación (no se fía de los nombres) y la persiste en `calibration.json`.
7. Menú in-game solo puede apagar (efectivo = JSON AND menú).
8. `OneHandIK` y `SideDodge` quedan **activados para Veteran/Duelist pero marcados experimentales** (apagables por separado).

## Preguntas abiertas (no bloquean)
- ¿Confirmas la interpretación de las 9 direcciones?
- ¿Mods de animación/IA que quieras usar con esto? (hoy ninguno)
- Resultados de las pruebas de TESTING.md (sobre todo F3 sufrir fintas, F7 IK, F8 esquiva lateral, F1 aspecto de las fintas).

## Riesgos principales (ver DESIGN §9)
Todo lo de integración está sin ejecutar. IK de una mano, esquiva lateral y el aspecto visual de las fintas son los puntos más inciertos.

## Siguiente paso
1. El usuario ejecuta TESTING.md §1–2 (arranque + armas) y reporta con la plantilla.
2. Según el reporte: ajustar o desactivar módulos problemáticos; afinar etiquetas de dirección con `calibration.json`.
3. Candidatos a v0.2: selección de guardia por clase de golpe (usar `Zones` para elegir/forzar la pose), esquiva direccional según el corte entrante, ataques de escudo reactivos, integración con un pack de animaciones con cortes verticales.

## Registro de cambios de proceso
- 2026-10-12: Fase 0 → Fase 1 → … → 4 ejecutadas de una tacada por mandato del usuario. Revisión independiente del código de integración encargada a un subagente (ver sección siguiente cuando termine).
