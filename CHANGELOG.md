# Changelog

Formato basado en *Keep a Changelog*. Versionado semántico (`ModVersion` del manifest).

## [0.3.1] — 2026-10-12 (sin probar en el juego)

### Cambiado
- **Los NPCs con arma de `Spar: weapons` son ahora demostradores** (`Sparring.AttackerMode = "Demo"`, por defecto): desenfundan, se congelan en su sitio mirándote (se congela el árbol de comportamiento, como hace el juego con el miedo) y ejecutan **todos** los ataques de su stance —aperturas, seguimientos y ripostes, ordenados por tipo y dirección— con `DemoPauseSeconds` (2,5 s) entre uno y otro, la pose de guardia propia de cada ataque, y una etiqueta en pantalla (`Sword 4/17: SwingRightToLeft - Swing to Left (opening)`) del demostrador más cercano a ti. Solo actúan los que están a menos de `DemoActiveRange` (5 m): acércate al que quieras ver. Tras un ataque con paso vuelven a su sitio. De paso **miden la dirección y el avance de cada ataque** (alimentan `calibration.json`). El combate real sigue disponible con `"AttackerMode": "Fight"`.

## [0.3.0] — 2026-10-12 (sin probar en el juego)

### Añadido
- **Hechizos de práctica** en la rueda de hechizos (`Spar: weapons`, `Spar: parry dummy`): clase `SpellSparring` (hereda de `SpellCastData`) declarada por JSON como *habilidad por defecto*, de modo que el juego la añade sola al contenedor del jugador. Invocan, donde apuntas, 13 NPCs hostiles —uno por tipo de arma, con su loadout en un contenedor propio del mod y turno de ataque único (`meleeMax=1`)— o un **muñeco de paradas** (sin ataques, sin esquivas, sin contraataques; solo para) con 100 000 de vida. Volver a lanzar el hechizo retira el grupo. El nivel de destreza del menú también se aplica a ellos, así que sirven para comparar niveles.
- Sección de configuración `Sparring` y 14 contenedores/2 hechizos generados (`tools/gen-sparring-content.py`), con 5 tests de coherencia (119 en total).

## [0.2.0] — 2026-10-12 (sin probar en el juego)

### Añadido
- **Secuencias de golpes encadenados (katas)** interrumpibles: longitud por nivel (Recruit 2 … Duelist 5, máx. 6), primer golpe asumiendo que será parado, siguientes golpes elegidos por fluidez de dirección (el corte contrario es el retorno natural), preferencia por los ataques de seguimiento del propio juego y por los que avanzan; interrupción por *episodio de peligro* con compromiso por nivel (el novato casi nunca corta, el duelista casi siempre), también durante el armado de un golpe de seguimiento. Sustituye al antiguo combo de un solo golpe. Parámetros nuevos: `Profiles.*.ComboMaxLength/ComboCommitment/InterruptDanger/PressureBias`, sección `Katas`.
- **Ataques con avance** (paso/estocada con zancada) reconocidos por nombre y **medidos** (desplazamiento del cuerpo durante el ataque), con presión: tras parar con éxito se favorecen los ataques que avanzan.
- **Menú en el juego** rehecho: interruptor general *Competent AI*, **Skill level** (Auto / 1 Recruit / 2 Soldier / 3 Veteran / 4 Duelist) y desplazamiento, ambos **en vivo** sobre los NPCs ya presentes; el resto de módulos también en vivo.
- 114 tests.

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
