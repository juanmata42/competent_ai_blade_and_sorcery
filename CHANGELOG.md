# Changelog

Formato basado en *Keep a Changelog*. Versionado semántico (`ModVersion` del manifest).

## [0.1.0] — 2026-10-12 (primera versión, sin probar en el juego)

### Añadido
- **Core puro** testeado (94 tests): nueve direcciones de ataque, zonas defensivas 3×3, clasificación de armas por daño real (filo/punta/cabeza contundente), reglas de ataque por arma, memoria de ataques con ponderación, planificador de fintas, detector de reversión de golpe, lector de fintas, fatiga de paradas, calibración de direcciones, configuración con perfiles superponibles y rangos seguros.
- **Integración** con ThunderRoad 1.3.1: módulo por criatura añadido al `BrainData` de cada NPC (sin sustituir ni parchear módulos del juego); curación del clon de `MeleeStanceData` (quita ataques ilegítimos, devuelve los mal tipados, riposte de estocada para armas de punta); director de variedad con cambio de pose; fintas, combos y contraataque tras esquivar; lectura de fintas y fatiga; ajuste por arquetipo; IK de parada de una mano y esquiva lateral (experimentales); calibración en runtime de la dirección de cada animación.
- Cuatro arquetipos (Recruit, Soldier, Veteran, Duelist) mapeados a los Brains de serie, con asignación por creatura/facción/Brain/dificultad y desplazamiento global.
- Configuración JSON comentada + menú in-game (todos los módulos desactivables por separado) + logging por niveles con limitador de líneas + autochequeo de API al arrancar.
- Scripts `tools/build.ps1` / `package.ps1`, generador de la referencia de configuración.
- Documentación: investigación (mods existentes, internals del juego, notas del SDK), diseño, configuración, pruebas, compatibilidad.

### Limitaciones conocidas
Ver [docs/DESIGN.md §9](docs/DESIGN.md) y [docs/TESTING.md](docs/TESTING.md). Resumen: no probado en el juego; sin animaciones nuevas (los cortes verticales puros no existen como clip); IK de una mano y esquiva lateral sin verificar visualmente; las etiquetas de dirección de los clips de serie son inferidas hasta que la calibración las mide.
