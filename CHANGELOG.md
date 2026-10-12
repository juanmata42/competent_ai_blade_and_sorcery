# Changelog

Formato basado en *Keep a Changelog*. Versionado semántico (`ModVersion` del manifest).

## [0.5.0] — 2026-10-12

### Cambiado (a raíz de «atacan al tanteo, dudan, giran en círculo; los golpes a las piernas y las estocadas rectas siempre entran»)
- **Las secuencias ya no se cortan al ser paradas.** Causa (código del juego): al parar un ataque, `OnWeaponCollision` aplica un retroceso (`TryPush(Parry)`) cuando la velocidad del arma supera `recoilMinWeaponSpeed`, y la reacción del cerebro llama a `StopAttack`. Ahora los golpes de una secuencia (y, con probabilidad `ParryRecoilResist` por perfil, también los golpes sueltos) suben ese umbral mientras dura el ataque y se restaura al terminar.
- Una secuencia **ya no termina cuando un golpe acierta en el cuerpo** (`Katas.ChainAfterBodyHit`, true por defecto; antes copiaba la regla de la postura).
- **Menos dudas**: nuevos límites por perfil que hacen real un nivel forzado aunque el cerebro sea fácil: `AttackIntervalCapMin/Max` (tope del intervalo entre ataques tras el multiplicador), `ParryAdjustSpeedFloor`, `RangeDeltaMul` (ensancha las ventanas de distancia de ataque, que dejaban huecos de ~0,5 m donde el NPC solo rodeaba) y `AttackDespiteDanger` (probabilidad, cada 0,5 s, de que el nodo `AttackMelee` ignore su «preferencia por defender» y ataque aunque tu arma se mueva peligrosamente). Los nodos se buscan en el árbol propio de cada criatura (`TreeTweaks`) y se restauran al desactivar el mod.
- **Defensa contra piernas y estocadas**: esquiva reactiva (`LegDodgeChance`, `ThrustDodgeChance`) cuando un golpe a la altura de las piernas o una estocada recta va a impactar, y enfriamiento de esquiva más corto (`DodgeCooldownMul`) para poder esquivar varias estocadas seguidas.
- **Diagnóstico**: en nivel Debug cada golpe que recibe un NPC queda en el log (`HIT daño (tipo) a altura 0.xx`, guardia, si esquivaba/atacaba/estaba agotado) y cada esquiva reactiva también.
- Valores por defecto (Recruit → Duelist): ParryRecoilResist 0 / 0,25 / 0,5 / 0,75; AttackDespiteDanger 0 / 0,2 / 0,5 / 0,7; RangeDeltaMul 1 / 1,3 / 1,7 / 2; tope máx. de intervalo — / 1,6 s / 0,8 s / 0,35 s.

## [0.4.2] — 2026-10-12

### Añadido
- **Opción de menú «Extra info (on-screen text)»**, desactivada por defecto: muestra en el panel de texto junto a la mano izquierda lo que hacen los NPCs cercanos (`[Veteran] sequence of 4 strikes: …`, `strike 2: …`, `feint: A > B`, `fell for your feint` / `read your feint`, `overwhelmed`, `counter-attack after dodging`, `breaks off the sequence to defend`). Sección de configuración `Hud` (`Enabled`, `MaxDistance` 10 m, `MinSecondsBetween` 0,8 s, `DismissSeconds` 2 s). Sin esta opción el mod **no muestra ningún texto** (los textos que viste en la v0.3.1 eran las etiquetas de los demostradores, ya retirados).

## [0.4.1] — 2026-10-12

### Cambiado (a raíz de «hacen uno, se resetea la animación y hacen otro»)
- **Encadenado suave**: los golpes de una secuencia (y la redirección de una finta) ya **no pasan por `StopAttack`** del juego, que reinicia la fuerza de los brazos y la animación; se lanza el siguiente directamente y el sistema de animación del juego (dos ranuras de inicio alternas) hace el fundido. Solo si no se puede parar limpiamente el `AttackWindow` anterior se vuelve al método antiguo.
- **Sin cambios de postura entre ataques**: el director de variedad solo cambia la postura de guardia (los brazos se recolocan y el juego no deja atacar ≈0,8 s) cuando no hay ningún ataque disponible desde la postura actual.
- **Más secuencias por defecto**: probabilidad de iniciar una Recruit 25 % / Soldier 40 % / Veteran 55 % / Duelist 70 % y longitudes hasta 3 / 4 / 5 / 6. En el log de la prueba solo habían peleado enemigos `HumanEasy` (= Recruit), que antes encadenaban con un 5 % y máximo 2 golpes.
- Se prefiere con fuerza el seguimiento «escrito por los autores» para el ataque anterior (`AttackMotion.GetChainAttack`).
- Log: cada fase de cada ataque (`stage WindUp/Attack/FollowThrough/End/Cancel …`) en nivel Debug, y `kata planned / kata step / kata ended` en nivel Info.

## [0.4.0] — 2026-10-12 (primera versión con datos reales del juego)

### Retirado
- **Hechizos de práctica** (`Spar: weapons`, `Spar: parry dummy`), sus contenedores, el modo demostrador, los roles y la sección `Sparring` de la configuración: en la primera prueba funcionaban mal. El código sigue en la etiqueta de git `sparring-v0.3.1` por si se quiere retomar de otra forma.

### Corregido a partir del primer `Player.log` real
- **NPCs con arma de punta sin ningún ataque** (lanza en cerebros de dificultad 1–2; daga sin filo): ver DESIGN §4a.7. Ahora se rescatan los ataques legítimos ignorando la dificultad cuando la lista quedaría vacía. Además la red de seguridad "conservar la lista de serie" ya no devuelve cortes a un arma sin filo.
- **Medición de direcciones**: casi todos los cortes se medían como estocada (el avance del brazo dominaba). Ahora la estocada se decide por el eje de la hoja y el corte por su parte lateral. Los ficheros `calibration.json` anteriores se descartan.
- **Etiquetas por nombre**: `RightToLeft/LeftToRight/TopToLeft/TopToRight` están escritos desde el punto de vista del defensor (medido: `RtL` viaja hacia la derecha del atacante); invertidas.
- **Detector de fintas** demasiado sensible (varios avisos por segundo con solo mover la espada): ahora exige que el golpe viniera en curso de impacto (`Defense.RequireHitCourse`).
- **Limitador de log**: las líneas Info/Warn/Error ya no se descartan (53 líneas de arranque se perdieron en una ráfaga de apariciones).

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
