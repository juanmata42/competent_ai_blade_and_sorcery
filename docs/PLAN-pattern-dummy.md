# PLAN — Maniquí que ataca en patrones (práctica de series y combinaciones)

Estado: **solo planificado, nada implementado**. Escrito el 2026-10-12 sobre la versión 0.6.0.

## 1. Qué es

Un NPC de práctica que, en lugar de pelear, **ejecuta series de golpes que tú defines o eliges** (por ejemplo «corte abajo-izquierda → corte abajo-derecha → estocada»), con un ritmo configurable, para que practiques pararlas, esquivarlas o contraatacar en el punto exacto. Sirve también para ver cómo se encadenan los golpes de cada arma.

No es el «demostrador» de la v0.3.1 (que recorría todos los ataques del arma de uno en uno, sin orden pensado) ni el «muñeco de paradas» (que solo paraba). Es la pieza que une las dos: **atacante con guion**.

## 2. Qué ya tenemos y se reutiliza

| Pieza | Dónde | Para qué sirve aquí |
|---|---|---|
| Clasificación de los ataques de cada arma en 9 direcciones + estocada, con medición real | `Directions`, `Calibration`, `StanceCurator` | Traducir «DownLeft, Thrust…» al ataque concreto de la biblioteca del arma |
| Encadenar sin reiniciar la animación | `ChainInto` (`BrainModuleCompetent.Attack.cs`) | Pasar de un paso al siguiente con el fundido del propio juego |
| Protección contra el retroceso por parada | `ArmRecoilGuard` (`.Aggression.cs`) | Que el guion no se corte cuando lo paras |
| Mutación corte→estocada, secuencias, avance, fintas | `.Openings.cs`, `.Kata.cs`, `FeintTick` | Pasos especiales del guion (`Feint`, `Advance`) |
| Eventos de parada y de daño | `onCreatureAttackParry`, `OnDamageEvent` | Puntuar tu práctica |
| Texto en pantalla (opción «Extra info») | `Hud.cs` | Mostrar el paso actual y el siguiente |
| Congelar el árbol de comportamiento y mirar al jugador | modo «Demo» de la etiqueta `sparring-v0.3.1` (`src/CompetentAI.Mod/Sparring.cs`) | Que el maniquí no haga nada fuera de guion |

## 3. Lección de la primera versión (por qué fallaron los hechizos)

El usuario dijo «los hechizos funcionan fatal». No conservamos el motivo concreto. Antes de reutilizar nada: **revisar `Player.log` de esa prueba** si existe, y asumir que la parte frágil era la invocación por hechizo (clase propia `SpellCastData`, hecha como habilidad por defecto) y no el comportamiento del NPC. Por eso este plan **no usa hechizos**.

## 4. Diseño

### 4.1 Cómo aparece el maniquí (a decidir; recomendación en negrita)

1. **Criatura propia en el catálogo** (`CreatureData` en JSON, p. ej. `CompetentPatternDummy`) que sale en el menú/libro de aparición del juego, y el mod la reconoce por su id (igual que ya asigna perfiles por id de criatura). **Menos código frágil, aparece donde ya aparecen los demás.**
2. Opción de menú «Spawn pattern dummy» que actúa como interruptor momentáneo: al activarla aparece delante de ti. Requiere comprobar la API de aparición de criaturas (no verificada).
3. Hechizo (lo de antes): descartado salvo que 1 y 2 no sean viables.

Arma del maniquí: un contenedor de equipo por tipo de arma, o **una opción de menú «Dummy weapon»** que elige entre las armas ya definidas por el mod en la primera versión (espada, hacha, lanza, daga, mandoble, maza, espada y escudo…). El contenido de la v0.3.1 (`Container_CompetentSpar_*.json`, `tools/gen-sparring-content.py`) se recupera de la etiqueta.

### 4.2 Qué es un patrón

Un patrón es una lista de pasos, definida en `CompetentAI.patterns.json` (fichero separado del de configuración, con comentarios):

```jsonc
{
  "Patterns": [
    {
      "Name": "Cruz y estocada",
      "Weapons": ["Sword", "Rapier"],          // vacío = cualquier arma con esos ataques
      "Rest": 2.5,                             // segundos de pausa antes de repetir
      "Repeat": true,
      "Steps": [
        { "Dir": "DownLeft",  "Kind": "Swing"  },
        { "Dir": "DownRight", "Kind": "Swing", "Gap": 0.0 },   // Gap = pausa tras el paso anterior (0 = encadenado)
        { "Dir": "Thrust",    "Kind": "Thrust", "Advance": true },
        { "Dir": "Left",      "Feint": 0.5 }                   // finta: se cancela al 50 % del armado y redirige al siguiente
      ]
    }
  ]
}
```

Además, **patrones generados** sin escribir nada:
- *Cadena natural del arma*: sigue `AttackMotion.GetChainAttack` (lo que los autores del juego enlazaron).
- *Kata aleatoria*: usa `KataPlanner` con el perfil elegido.
- *Corte y estocada*: corte + estocada de mutación.
- *Barrido de direcciones*: recorre las 8 direcciones en orden de reloj, una por ciclo.

### 4.3 Cómo se traduce un paso a un ataque real

`PatternResolver` (Core, sin Unity, con tests): para cada paso busca en los ataques del arma (aperturas y seguimientos del stance curado) el de `Dir` y `Kind` pedidos; si hay varios elige por peso y memoria de variedad; si no hay ninguno usa el de dirección más cercana (`Directions.Steps`) y **lo dice** (`step 2 DownRight not available for Sword, using Right`). Un patrón se valida al cargarlo (direcciones y tipos conocidos, longitud ≤ 12, pausas razonables) y los fallos van al log, sin romper el juego.

### 4.4 Ejecución

Un `PatternRunner` por maniquí (módulo `BrainModuleCompetent` con rol «Pattern»):
- Mira siempre al jugador, árbol de comportamiento congelado, sin esquivas ni contraataques propios (rol «solo guion»).
- Para cada paso: `TriggerAttack` (primero) / `ChainInto` (siguientes, sin `StopAttack`), con el recoil protegido; espera al `End` o a un límite de seguridad por paso; luego `Gap`.
- Si el jugador lo derriba o lo desarma a mitad: reinicia el patrón tras 3 s.
- **Ritmo**: multiplicador global (0,5×–1,5×) vía `animationSpeedMultiplier`; opcional «rampa»: sube 5 % tras cada serie limpia (sin daño recibido).
- **Telegrafía opcional**: el paso actual y el siguiente en el panel de texto (`[Cruz y estocada] 2/4 DownRight → Thrust`).

### 4.5 Seguridad y modos

- **Modo práctica** (por defecto): daño del maniquí al jugador reducido (a comprobar cómo: multiplicador de daño de la criatura, arma de entrenamiento romo, o invulnerabilidad del jugador solo frente a él — NO VERIFICADO). Alternativa mínima: el maniquí golpea con armas romas y el daño se informa en el log.
- **Modo real**: daño normal.
- El maniquí no pierde vida (como el muñeco de paradas de la v0.3.1: 100 000 de vida) para poder contraatacarlo sin que muera.
- Un solo maniquí a la vez; reaparecer lo retira.

### 4.6 Puntuación (fase opcional)

Con `onCreatureAttackParry` y `OnDamageEvent` se sabe, por paso, si lo paraste, te dio o lo esquivaste. Resumen al terminar la serie en el panel y en el log: `serie 3/4 paradas; falló el paso 3 (Thrust, impacto en el pecho)`. Con esa información, el modo «adaptativo» puede repetir más veces el paso que más fallas.

### 4.7 Menú del juego (opciones nuevas)

«Pattern dummy: pattern» (lista de patrones), «Pattern dummy: weapon», «Pattern dummy: speed» (0,5–1,5), «Pattern dummy: mode» (Práctica / Real), «Pattern dummy: spawn».

## 5. Fases

| Fase | Contenido | Verificable sin el juego |
|---|---|---|
| P0 Investigación | API real para: aparecer una criatura de catálogo delante del jugador, congelar/liberar el árbol, reducir o anular daño, vida infinita, botón/interruptor de menú. Releer `Sparring.cs` de la etiqueta. Revisar el log de la prueba fallida si existe. | Sí (decompilación) |
| P1 Núcleo | `Pattern`, `PatternStep`, parser JSON, validación, `PatternResolver`, patrones generados. Tests: resolución por arma, direcciones ausentes, límites, aleatoriedad con semilla. | Sí (xUnit) |
| P2 Ejecución | `PatternRunner`, rol «Pattern», integración con `ChainInto`, recoil guard, fintas y avances, reintentos. | Compila; prueba en el juego |
| P3 Aparición y menú | Criatura de catálogo + contenedores, opciones de menú, panel de texto. | Tests de coherencia de JSON; prueba en el juego |
| P4 Puntuación y rampa | Contadores por paso, resumen, adaptativo. | Núcleo con tests; resto en el juego |
| P5 Documentación | `TESTING.md` (F14+), `CONFIG`, `CHANGELOG`, `PROGRESS`. | Sí |

Estimación: P0–P1 y P5 son rápidas y seguras; P2–P3 concentran el riesgo (todo depende de APIs sin verificar).

## 6. Riesgos

| Riesgo | Mitigación |
|---|---|
| La aparición de criaturas vuelve a fallar | Elegir primero la vía 1 (catálogo del juego); si no, vía 2; registrar cada paso en el log |
| El árbol congelado deja al maniquí sin apuntar o sin armar el arma | Reutilizar lo que ya funcionó en la v0.3.1 (desenfundaba y se congelaba) |
| El arma no tiene la dirección pedida | Resolver con la más cercana y avisar; mostrar en el menú qué direcciones tiene el arma |
| Un patrón mal escrito rompe el juego | Validación en el arranque + `try/catch` con autodesactivación, como el resto del mod |
| Daño demasiado alto en práctica | Modo práctica por defecto con daño reducido (si se consigue) o armas romas |

## 7. Preguntas abiertas (necesito tu decisión antes de P2)

1. **¿Cómo prefieres invocarlo?** Menú del juego (opción momentánea), criatura en el libro/menú de aparición del propio juego, o ambos.
2. **¿Daño?** ¿Quieres un modo práctica sin daño (o muy reducido) por defecto?
3. **¿Cómo definirás los patrones?** ¿Te vale un fichero JSON que editas, o quieres elegir solo entre patrones ya hechos desde el menú (más sencillo)? Recomiendo empezar con presets del menú + el fichero para los avanzados.
4. **¿Qué armas primero?** Recomiendo espada, lanza y espada con escudo.
5. **¿Puntuación?** ¿La quieres desde el principio o la dejamos para una segunda tanda?

## 8. Ampliación: «sitio de entrenamiento» con medidor de daño (solo planificado)

Petición del usuario: un mapa muy sencillo donde, al golpear al maniquí —al que se le puede poner más o menos armadura—, se vea el daño que hace o que sufre.

### 8.1 El sitio

Tres opciones, de menos a más trabajo:
1. **Zona de entrenamiento dentro de un nivel que ya existe** (recomendada para empezar): al activar la opción se coloca el maniquí (y un par de marcas en el suelo) delante del jugador, en el nivel actual. Sin crear ningún mapa. Funciona en cualquier sitio, incluida la casa (`Home`).
2. **Reutilizar un nivel del propio juego** que ya sea una sala sencilla: en los datos hay `Level_Arena`, `Level_Debug_AreaTestLevel` y `Home` (`.local/basjson/Levels/`). Habría que comprobar si se puede cargar desde el mod y si sirve para esto (NO VERIFICADO).
3. **Mapa propio**: un nivel nuevo es una escena de Unity empaquetada con el SDK del juego; no se puede crear solo con JSON y C#. Queda fuera salvo que el usuario quiera usar el SDK y el editor (no verificado qué haría falta).

### 8.2 Armadura ajustable

El daño que el juego aplica se calcula en `Damager` multiplicando por el nivel del arma, por el material golpeado y por el multiplicador de la parte del cuerpo (`ragdollPart.data.damageMultiplier`), después de la armadura que lleve puesta la criatura. Dos formas de graduar la armadura del maniquí:
- **Por equipo**: niveles «sin armadura / cuero / malla / placas», cada uno un contenedor de prendas del catálogo del juego (más fiable: usa datos reales).
- **Por multiplicador**: un deslizador que escala el daño recibido por el maniquí (más cómodo, menos realista; hay que comprobar dónde se puede aplicar sin alterar a otras criaturas — NO VERIFICADO).

Recomendación: empezar con los niveles por equipo (menú «Dummy armor: none/light/medium/heavy») y dejar el deslizador para después.

### 8.3 Medidor de daño

- **Daño hecho al maniquí**: `OnDamageEvent` de su criatura (ya lo usamos para el log `HIT`) da el daño de cada impacto, el tipo (corte, perforación, contundente), la altura y la parte del cuerpo. Se muestra en el panel de texto: `Corte, torso: 12,4  | total 87,1 | media 9,3 | mejor 21`.
- **Daño que sufres**: lo mismo sobre la criatura del jugador cuando el golpe viene del maniquí (junto al patrón: `paso 3 Thrust → te da 18`).
- **Resumen por serie** y botón/opción «reiniciar contador».
- **Comparar armas/armaduras**: guardar los últimos N impactos por arma y armadura en el log para hacer tablas (p. ej. cuánto bajan la perforación y el corte contra malla).
- La vida del maniquí no baja (100 000) para que el contador no se interrumpa.

### 8.4 Fases añadidas

| Fase | Contenido |
|---|---|
| T0 | Verificar: cómo colocar el maniquí y marcas en el nivel actual, qué contenedores de armadura existen en el catálogo, que `OnDamageEvent` trae daño y parte del cuerpo para criaturas con armadura, si se puede cargar `Level_Arena` o similar |
| T1 | Núcleo: acumulador de estadísticas de daño (total, media, máximo, por tipo y por zona), con tests |
| T2 | Maniquí con selector de armadura + medidor en el panel |
| T3 | (opcional) cargar un nivel existente / marcas en el suelo |

Dependencias: el maniquí del plan principal (P2–P3), con el que comparte la aparición y el panel.

### 8.5 Preguntas abiertas añadidas

6. ¿Te basta con una zona colocada en el nivel actual, o quieres cargar un nivel concreto?
7. ¿Armadura por niveles de equipo, por deslizador, o ambos?
8. ¿Qué datos quieres ver: daño por impacto, total, media, por tipo y por zona del cuerpo?
