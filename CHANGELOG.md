# Changelog

Formato basado en *Keep a Changelog*. Versionado semántico (`ModVersion` del manifest).

## [0.1.0] — 2026-10-12 (primera versión, sin probar en el juego)

### Añadido
- **Core puro** testeado (104 tests): nueve direcciones de ataque, zonas defensivas 3×3, clasificación de armas por daño real (filo/punta/cabeza contundente), reglas de ataque por arma, memoria de ataques con ponderación, planificador de fintas, detector de reversión de golpe, lector de fintas, fatiga de paradas, calibración de direcciones, configuración con perfiles superponibles y rangos seguros.
- **Integración** con ThunderRoad 1.3.1: módulo por criatura añadido al `BrainData` de cada NPC (sin sustituir ni parchear módulos del juego); curación del clon de `MeleeStanceData` (quita ataques ilegítimos, devuelve los mal tipados, riposte de estocada para armas de punta); director de variedad con cambio de pose; fintas, combos y contraataque tras esquivar; lectura de fintas y fatiga; ajuste por arquetipo; IK de parada de una mano y esquiva lateral (experimentales); calibración en runtime de la dirección de cada animación.
- Cuatro arquetipos (Recruit, Soldier, Veteran, Duelist) mapeados a los Brains de serie, con asignación por creatura/facción/Brain/dificultad y desplazamiento global.
- Configuración JSON comentada + menú in-game (todos los módulos desactivables por separado) + logging por niveles con limitador de líneas + autochequeo de API al arrancar.
- Scripts `tools/build.ps1` / `package.ps1`, generador de la referencia de configuración.
- Documentación: investigación (mods existentes, internals del juego, notas del SDK), diseño, configuración, pruebas, compatibilidad.

### Corregido tras la revisión independiente del código (antes de la primera prueba)
- La IK de una mano se activaba antes de que existieran los anclajes del módulo de defensa: el `HeldItemsChange` del juego podía lanzar una excepción al desenfundar. Ahora se activa solo tras la primera defensa y un fallo la desactiva de forma permanente (sin bucles de reintento).
- La fatiga de paradas la deshacía el árbol de comportamiento (`SetDefenseState`) en un ciclo de IA: ahora se re-afirma cada tick y se agrupan los eventos de parada.
- El armado normal de un golpe (alejarse y luego venir) se detectaba como finta: ahora solo cuenta un golpe que *se acercaba* y cambia de dirección. Tests nuevos.
- Re-planificación cada frame (y cadenas de texto) cuando el objetivo está fuera de rango: limitada a 0,4 s y con etiquetas de dirección cacheadas.
- `_pendingDouble` huérfano, `parryEnabled` sin restaurar al desactivarse por errores, `NonPlannedWeight=0` posible, contraataque tras esquivar sin las comprobaciones de ángulo/pose/grupo, registro de módulos vaciado al descargar nivel, umbral de estocada (0,65→0,8) que clasificaba cortes con avance como estocadas.

### Limitaciones conocidas
Ver [docs/DESIGN.md §9](docs/DESIGN.md) y [docs/TESTING.md](docs/TESTING.md). Resumen: no probado en el juego; sin animaciones nuevas (los cortes verticales puros no existen como clip); IK de una mano y esquiva lateral sin verificar visualmente; las etiquetas de dirección de los clips de serie son inferidas hasta que la calibración las mide.
