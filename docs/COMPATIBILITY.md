# Compatibilidad

**Estado: ningún mod ha sido probado junto a Competent AI** (el mod no se ha podido ejecutar). Lo de abajo es el análisis de riesgo a partir del código del juego y de lo que cada mod declara ([existing-mods.md](research/existing-mods.md)). Actualiza esta tabla con tus pruebas.

## Cómo evita conflictos (estrategia)
| Principio | Qué implica |
|---|---|
| **No sobrescribe catálogos** | No hay ningún JSON de Brains/Stances/Items/Creatures en el mod. No puede pisar ficheros de otros mods |
| **No parchea el código del juego** | Sin Harmony ni reemplazo de módulos: no choca con parches de otros mods sobre los mismos métodos |
| **Trabaja sobre la copia por criatura** | Cada NPC tiene su propio `BrainData`; el mod ajusta campos de esa copia y añade **un** módulo propio al final de su lista |
| **Multiplicativo, no absoluto** | Los arquetipos multiplican/desplazan el valor que ya tenga la criatura (el del Brain, ya modificado por otros mods en JSON) |
| **Aditivo en la IA de ataque** | Opera sobre el `MeleeStanceData` **resultante** (incluidas animaciones y ataques añadidos por otros mods): si un pack añade ataques nuevos, se curan y puntúan igual |
| **Todo con interruptor** | Cada módulo se apaga por separado (JSON o menú); `Enabled=false` devuelve el comportamiento de serie |
| **Falla en silencio hacia el juego** | Tras 4 excepciones el módulo se desactiva solo para esa criatura; el autochequeo de API desactiva el mod entero si el juego cambió |

## Orden de carga
- El mod se carga como cualquier DLL de la carpeta `Mods` (el juego ordena por dependencias y alfabético; Competent AI no depende de otros mods).
- Su módulo por criatura se **añade ≤0,5 s después de que el NPC tenga su Brain cargado**, es decir **después** de los módulos del Brain (de serie o de otros mods), por lo que se ejecuta después de ellos en cada tick. Los ajustes numéricos se leen en ese momento: si otro mod cambia esos mismos campos **después** (p. ej. un mod con menú que reescribe valores cada pocos segundos), puede deshacer o duplicar el efecto.
- Recomendación: **no actives a la vez dos mods que cambien los mismos tiempos de ataque/riposte/esquiva**; deja uno.

## Mods conocidos y riesgo previsto
Leyenda: ✅ sin solape previsible · ⚠️ solape parcial · ❌ solape directo (recomendado usar uno solo). Todo **sin probar**.

| Mod | Qué hace (según su autor) | Riesgo | Notas |
|---|---|---|---|
| **AdaptiveAI** (Nexus 12790, DLL cerrada) | Bloqueo predictivo, fintas, contraventanas, variedad de ataques, perfiles de comportamiento | ❌ | Mismo ámbito (fintas, variedad, contraataque). Sin fuente: no se puede saber qué campos toca. Úsalos por separado para comparar |
| **Ai Adjuster** (Nexus 12919 / mod.io, DLL + ModOptions) | Tiempos de ataque, tiempos/cooldown de riposte, máx. atacantes, velocidad | ⚠️ | Escribe los mismos campos de `BrainModuleMelee` (`minMaxTimeBetweenAttack`, `minMaxRiposteDelay`…). Si aplica sus valores al spawn y nosotros multiplicamos después, el resultado es el producto. Desactiva `ProfileTuning` si quieres que mande Ai Adjuster |
| **Relentless NPCs / Reborn** (Nexus 13449 / 14570, DLL + Harmony) | Combos continuos, "no defensive dodges", sin vacilación | ❌ | Quita esquivas y mueve el ataque a su manera: choca con `DefenseReading`, `CounterAfterDodge`, `Combos`. Parchea con Harmony (puede interceptar los mismos métodos que usamos como API pública) |
| **Better AI** (Nexus 4907, DLL; declara 1.0) | +30 % velocidad de ataque | ⚠️ | Se combina multiplicativamente con `AnimSpeedMul`/`AttackIntervalMul`; el resultado puede ser demasiado rápido: baja esos multiplicadores |
| **KOKO's NPC tweaks / adjustables** (Nexus 14490 / 14363, DLL + Harmony + JSON) | Reacción, agresividad, velocidad, animaciones propias | ⚠️ | Solape en reacción/agresividad; sus animaciones nuevas se integran con la calibración automática |
| **Palpatine** *Brains + Attacks*, *Enhanced Difficulty*, *Spear/2H enemies*, *Fist Fighters* (12107/12108/11374/14139/10802; JSON + bundles) | Brains y *stances* propios, animaciones de ataque por clase de arma | ✅ / ⚠️ | Sus stances son `MeleeStanceData` normales: el mod los cura igual. Sus brains tienen otros ids: asígnales arquetipo en `Assignment.ByBrainId` (si no, se asigna por `difficulty`). Un stance propio con ataques mal tipados se arregla solo; las direcciones de sus clips se miden en runtime |
| **BossaNova's Dueling Melee Bots** (Nexus 2359, JSON, declara 1.3.1) | Contraataque tras bloqueo, más movimientos, más HP | ⚠️ | Solo datos; la variante Crystal Hunt sobrescribe creature tables (otro tema). Puede solapar en probabilidad de contraataque (`RiposteChanceShift`) |
| **Outcome AI Plus** (GitHub, MIT, JSON) | Brains propios `aip*` | ✅ | Ids de Brain nuevos: asigna arquetipo en `ByBrainId` |
| **Mods de animación de parada/ataque del jugador** (p. ej. *Fromsoftware Parry*) | Lado jugador | ✅ | No tocan la IA del NPC |
| **Hand Dynamics**, **Hit Reactions**, mods de armas/armaduras | Otros aspectos | ✅ | Los items nuevos con *damagers* estándar se clasifican solos (filo/punta/contundente) |

## Qué pasa con armas y animaciones de otros mods
- **Arma modded con *damagers* estándar** (`Damager.type` Slash/Pierce/Blunt): se clasifica por daño real y por id (clase). Si no se puede leer, el mod **no restringe** (comportamiento de serie).
- **Arma modded con id engañoso** (p. ej. "MiEspada" que es una maza): usa `Rules.ClassOverrides` / `TraitOverrides`.
- **Pack de animaciones de ataque**: sus ataques entran en el `MeleeStanceData`. Direcciones: se miden solas (necesita ≥3 muestras coherentes por ataque); puedes fijarlas con `Rules.DirectionOverrides`.
- **Pack con Brain/stance propio**: funciona si el stance es `MeleeStanceData` y el Brain contiene los módulos de serie `BrainModuleMelee/Defense/Stance/Detection`. Si no (módulos personalizados), el mod se aparta de esa criatura sin error.

## Reportar un conflicto
Incluye la lista de mods con orden, el escenario de [TESTING.md](TESTING.md), el resultado con `Modules.Enabled=false` (¿persiste el problema sin Competent AI?) y las líneas `[CompetentAI]` del log.
