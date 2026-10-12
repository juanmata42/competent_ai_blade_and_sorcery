# Mods existentes de IA de combate cuerpo a cuerpo para Blade & Sorcery

Fecha de consulta de TODAS las fuentes: **2026-10-12**. Juego objetivo: B&S 1.3.1.260730005 (PC).
Quejas objetivo: **(1)** defensa/parada/esquiva/contraataque pobres; **(2)** ataques repetitivos; **(3)** ataques que no corresponden al arma.

## 0. Metodología y límites (leer primero)

- **Nexus Mods**: las páginas web devuelven HTTP 403 (Cloudflare) a WebFetch y a curl. Los datos de Nexus de este documento proceden de la **API GraphQL pública** `https://api.nexusmods.com/v2/graphql` (consulta anónima, gameId 2673 = bladeandsorcery): nombre, autor, versión, fechas, descripción, lista de ficheros con su fecha, y **listado del contenido de los zips** (`modFileContents`: nombres y tamaños de ficheros, no su contenido). Eso permite afirmar con certeza "lleva DLL" o "solo JSON". La URL de cada mod (`https://www.nexusmods.com/bladeandsorcery/mods/<id>`) es la página pública; la leí vía API, no vía web. Se cita como `[NX <id>]`.
- **No pude leer** el `manifest.json` interno de los mods de Nexus (no se descargan ficheros sin cuenta/API key), por tanto el campo `GameVersion` real de cada mod es **"no consta"**; solo consigno la compatibilidad que el autor **declara en texto** (título, descripción, changelog).
- **Licencia**: el panel de permisos de Nexus no se expone en la API pública ni en WebFetch. Para todos los mods de Nexus la licencia es **"no consta"** salvo indicación. Ninguno enlaza código fuente salvo lo indicado.
- **mod.io**: la web es una SPA (Vue); solo se obtienen las metaetiquetas Open Graph (título, descripción, imagen). La API de mod.io exige api_key (401). No hay versión, autor ni fecha fiable; las fechas que deduzco de la marca de tiempo del nombre de la miniatura están marcadas **inferido**.
- **Reddit**: el dominio no es accesible para el rastreador de WebSearch (error 400 "domains not accessible"). **No hay datos de r/BladeAndSorcery.** Discord: no hay contenido indexado utilizable (solo se cita un enlace a una guía de animaciones personalizadas, ver mod 12107; no pude leerla).
- **GitHub**: la API pública funcionó (búsqueda de repos, árbol de ficheros, raw). La búsqueda de código requiere login (no disponible), así que **no pude buscar `BrainModule` en el código de otros repos**.
- Marcas: **[C]** = confirmado en la fuente citada; **[I]** = inferido por mí (razonamiento a partir de datos [C]); **[A]** = afirmación del autor del mod en su descripción, no verificada por mí en juego.
- Ningún mod fue probado en el juego. Todo lo que dicen las descripciones sobre comportamiento es [A].

---

## 1. Mods relevantes (ficha por mod)

### 1.1 AdaptiveAI - Ascension Update V2 (Nexus 12790)
- **Autor**: evilbenny. **Versión**: 1.5 [C]. **Creado** 2025-11-01; **última actualización** 2026-01-08 [C] (fichero MAIN 1.5 fechado 2026-01-08). 50 747 descargas, 368 endorsements [C].
- **URL**: https://www.nexusmods.com/bladeandsorcery/mods/12790 — Versión Nomad: https://www.nexusmods.com/bladeandsorcerynomad/mods/5818 (aparece en resultados de búsqueda; no leí su ficha).
- **Compatibilidad declarada**: **no consta** ninguna versión de juego (ni en título, ni en descripción ni en changelog) [C]. Fue publicado tras 1.0, así que 1.2/1.3 es plausible [I] pero no verificado.
- **Qué cambia**: **solo código (DLL)**. El zip contiene `AdaptiveAI/AA.dll` (95 232 B) + `manifest.json`; versiones anteriores usaban `AdaptiveAI.dll` [C, NX API modFileContents]. Sin JSON de datos. No se incluye `0Harmony.dll` (puede usar Harmony cargado por otro mod o no usarlo: **no consta**).
- **Quejas**: (1) sí, según [A]: "predictive blocking" (changelog 1.1), "Adaptive Counterwindows", "Intelligent Feint Recognition" (esquivar/bloquear/contraatacar/fintar ante fintas del jugador), "Head Protection System", "Behaviour Shift Profiles (Punisher, Defensive, Predictive, Pressure)". (2) sí, según [A]: "Attack Prediction", cadenas de Markov, "favourites certain attack types and changes them", ritmo/timing alterado. (3) no abordada.
- **Notas honestas**: la descripción presume de "mini neural network", "Multi-Layered Behaviour Trees", coordinación y flanqueo; son afirmaciones de marketing [A] y no hay código fuente para comprobarlas. El peso del DLL (95 KB) es plausible para heurísticas/estadísticas pero no para una red neuronal real [I]. Menciona "Dynamic Stamina & Pressure Scaling" y "Anti-Grab Intelligence" (fuera de nuestro alcance). Permite "version switching" V1/V2/Evolution [C, descripción].
- **Licencia / código abierto**: no consta; no hay enlace a fuente → **cerrado en la práctica** [I]. **Conflictos conocidos**: no consta; sin requisitos Nexus [C].

### 1.2 Ai Adjuster (Nexus 12919; Nomad 5934; mod.io `ai-adjuster`)
- **Autor**: BREAD (uploader BReadymans). **Versión**: 1.0, creado y actualizado 2025-12-10 [C]. 10 187 descargas [C].
- **URL**: https://www.nexusmods.com/bladeandsorcery/mods/12919 ; https://www.nexusmods.com/bladeandsorcerynomad/mods/5934 ; https://mod.io/g/blade-and-sorcery/m/ai-adjuster (meta description [C]; miniatura con marca de tiempo 2026-02-04 [I] → probable subida/actualización a mod.io en esa fecha).
- **Compatibilidad declarada**: **no consta** [C].
- **Qué cambia**: **solo código (DLL)**: `AiAdjuster.dll` (20 480 B), `AiAdjuster.pdb`, `Saves/{Easy,Medium,Hard,CustomSave}.sav`, `manifest.json` [C]. Todo vía menú ModOptions in-game.
- **Opciones documentadas [C, descripción]**: perfil Easy/Medium/Hard/Custom; Melee: "Chain Body Attacks", Attack Speed multiplier, Min/Max Time Between Attack, **Min/Max Riposte Time**, Max Attackers; Movement speed; arco (tiempos apuntar/disparar, draw speed); mago (delay de lanzamiento); Hit Reactions: Recovery Speed, **Riposte CoolDown**.
- **Quejas**: (1) parcial: solo temporización/cooldown de ripostas y recuperación; no hay opciones de esquiva ni de calidad de parada. (2) parcial: solo ritmo (intervalos) y encadenado de ataques "body"; no variedad de movimientos. (3) no.
- **Valor para nosotros**: es el ejemplo más claro, en uso real y reciente, de **mod de código + ModOptions que modifica parámetros de la IA en runtime** [I]. Fromsoftware Parry Rekindled lo recomienda en su descripción (según resumen de WebSearch, no verificado en la fuente).
- **Licencia**: no consta; sin fuente. Conflictos: no consta.

### 1.3 Relentless NPCs Reborn (Nexus 14570) y Relentless NPCs (Nexus 13449)
- **Autor**: supermodder1039. Reborn: v1, 2026-09-09 [C], 5 808 descargas; tags "AI Assisted", "AI Media" [C]. Original 13449: v1, 2026-04-02 [C], 3 381 descargas.
- **URLs**: https://www.nexusmods.com/bladeandsorcery/mods/14570 ; https://www.nexusmods.com/bladeandsorcery/mods/13449
- **Compatibilidad declarada**: no consta (ambos) [C].
- **Qué cambia**: **solo código**. Reborn: `RelentlessNPCs.dll` (33 792 B) + **`0Harmony.dll` (2 461 696 B)** + `Harmony-LICENSE.txt` + `README.md` + `manifest.json` [C, NX API]. Original: `RelentlessNPCs.dll` (13 824 B) + manifest [C]. **Es evidencia confirmada de que se empaqueta Harmony en mods de B&S en 2026.**
- **Opciones (Reborn) [C, descripción]**: Attack recovery, Group pressure, Continuous combos, Fast decisions, **No defensive Dodges**, All-Out Offense, No hesitation, Chain after hitting, Hold Ground, Attack Speed, Anti stun.
- **Quejas**: (2) parcial: "continuous combos"/"chain after hitting" (más encadenado, no más variedad). (1) **empeora o ignora** la defensa por diseño (opción de quitar esquivas y "no defensive"). (3) no.
- **Conflictos/bugs conocidos [C, descripción del 13449]**: "if an enemy loses their weapon they will NOT go and get it; this mod had to override AI a LOT to stop the circling/strafing". Reborn se anuncia como rehecho "hopefully no bugs" [A].
- **Licencia**: no consta; incluye licencia de Harmony (MIT) por el DLL empaquetado [I: el fichero se llama Harmony-LICENSE.txt]. Código fuente: no consta.
- **Observación [I]**: dos mods distintos empaquetando su propio `0Harmony.dll` (también KOKO, ver 1.4) implica riesgo de cargar varias copias/versiones de Harmony en el mismo proceso; conviene usar un ID de Harmony único y no asumir que otro mod lo provee.

### 1.4 K O K O's NPC tweaks v1.3 (Nexus 14490) y K O K O's adjustables 1.3 (Nexus 14363)
- **Autor**: K O K O. NPC tweaks: v3 (fichero MAIN "PCVR V1.4.5" v4), creado 2026-08-23, actualizado 2026-09-27 [C]; tags "Quality of Life", "AI Assisted". Adjustables: v10 (fichero v11), 4 874 descargas, tags incluyen "AI-Generated Content" [C].
- **URLs**: https://www.nexusmods.com/bladeandsorcery/mods/14490 ; https://www.nexusmods.com/bladeandsorcery/mods/14363
- **Compatibilidad declarada**: "1.3" en el título de ambos [C] (declarada solo por nombre).
- **Qué cambia**: **ambos** (DLL + JSON). NPC tweaks: `KOKOsNpcTweaks.dll` (6,3 MB en una revisión, 131 KB en otra), 29 JSON (creature tables, containers, waves, creature) [C]. Adjustables: `KOKOsAdjustables.dll` (866 816 B) + **`0Harmony.dll`** + manifest [C, fichero archivado v9].
- **Opciones [C, descripción 14490]**: "NPC reaction time, aggression, movement speed, swing speed and bunch of other stuff", "custom animations for NPCs", NPC invencible custom.
- **Quejas**: (1) parcial vía "reaction time/aggression"; (2) no claro ("custom animations": no especifica para qué armas); (3) no.
- **Notas**: el propio autor dice "This is more of a cleanup... currently it's worse as it has the same things that were in my adjustables mod before". Documenta mal; mucho contenido generado con IA [C, tags]. Calidad incierta.
- **Licencia**: no consta. Fuente: no consta. Conflictos: el autor pide logs y lista de mods; no enumera conflictos concretos.

### 1.5 Better AI (1.0) (Nexus 4907)
- **Autor**: Piepop101. **Versión** 1.5. Creado 2021-11-02; página marcada actualizada 2026-05-23, pero el **último fichero data de 2024-06-20** ("Updated to 1.0") [C]. 184 188 descargas, 1 892 endorsements [C]. Tag "NPC Behaviours (AI)".
- **URL**: https://www.nexusmods.com/bladeandsorcery/mods/4907
- **Compatibilidad declarada**: **"1.0"** en nombre y changelog (U10, U11, U12, U12.3 antes) [C]. 1.3 no consta.
- **Qué cambia**: **código**: `Better AI/BetterAI.dll` (43 008 B) + manifest [C]. (Las versiones U10–U12 eran JSON de 2,5 KB y luego pasaron a DLL con "customizable options" en U12.3.)
- **Qué hace [C, resumen/descripción]**: ataque +30 % más rápido (multiplicador configurable en el libro de ModOptions; "se aplica a cada NPC nuevo que aparece"); el resumen dice "better blocking/footwork" pero la descripción detallada solo lista la velocidad de ataque [C].
- **Quejas**: (1) apenas (el "mejor bloqueo" no está detallado); (2) no; (3) no. Es un ajustador de velocidad.
- **Licencia/fuente**: no consta. Conflictos: no consta (la descripción solo lista mods "que combinan bien").

### 1.6 Enhanced Difficulty Remake - 1.07 (Nexus 12108) + Palpatine's Brain and idle IDs (Nexus 12107)
- **Autor**: Palpatine (uploader Palpatlne). Ambos v1.0, 2025-05-30 [C]. 13 070 y 9 802 descargas.
- **URLs**: https://www.nexusmods.com/bladeandsorcery/mods/12108 ; https://www.nexusmods.com/bladeandsorcery/mods/12107
- **Compatibilidad declarada**: "1.07" en el nombre (=1.0.7) [C]; 1.3 no consta. 12107: no consta.
- **Qué cambia**: **solo datos JSON** (12108: 16 JSON — CreatureTable, Item_Weapon_SwordLongCommonAI, LootTable…; 12107: 6 JSON incl. `Stance_HumanMeleeDualWieldStance.json`, catálogo `catalog_Palp Brains + Attacks.json`, **un asset bundle** (14,6 MB total) con animaciones de ataque, capturas "Stance addressable names") [C]. 12108 **requiere** 12107 ("Core Requirement") [C].
- **Mecanismo [C, descripción de 12107]**: la creature table fija `"overrideBrain": true, "overrideBrainID": "HumanMeleeExpert"`. Incluye una guía (en el Discord oficial de B&S, canal 1248289469204009022, **no leída**) para crear animaciones de ataque propias y asignarlas.
- **Quejas**: (1) **sí** según [A]: "better blocking + footwork and confusing attacks", "harder to stagger", pensado para que el jugador deba ser "more defensive". (2) parcial: "confusing attacks" + paquete de ataques para modders. (3) 12107 es la infraestructura para ataques propios por arma (ver Spear/2H abajo).
- **Licencia/fuente**: no consta. **Aporta ejemplo de patrón "brain ID + stance + animaciones en addressables"** [I].

### 1.7 Palpatine: Spear enemies (11374), Two handed enemies remaster (14139), Fist Fighters (10802; Nomad 4932)
- **Autor**: Palpatine (Fist Fighters: "Palpatine and BREAD"). Fechas: Spear 1.2 → 2025-01-18; 2H remaster v1.1 → 2026-07-03; Fist Fighters v5.4 → 2025-04-30 [C].
- **URLs**: https://www.nexusmods.com/bladeandsorcery/mods/11374 ; https://www.nexusmods.com/bladeandsorcery/mods/14139 ; https://www.nexusmods.com/bladeandsorcery/mods/10802
- **Compatibilidad declarada**: Spear "U1.06" y "1.06"; Fist Fighters "1.0"/"Updated to 1.04"; 2H remaster no consta [C].
- **Qué cambia**: Spear: **solo JSON + bundle** (`spearanimations_assets_all.bundle`, Item_Weapon_SpearBoar, Item_Weapon_AxeLongHalberd, creature table, creature, container, waves) [C]. 2H: **JSON + bundle** (`2handednpcsremake_assets_all.bundle`, `Stance_HumanMelee2HStance.json`, `Item_Weapon_SwordLong*.json`) [C]. Fist Fighters: **JSON + bundle + un DLL pequeño `AITest.dll` (6 656 B)** (`Brain_HumanFistMelee.json`, `EnemyConfig_IronFist.json`…), requiere "Melee Fighters (1.0)", ModOptions de BREAD (daño de NPCs, velocidad de ataque) [C].
- **Quejas**: **(3) sí**, de forma local: añaden animaciones de ataque y stance propios para una clase de arma concreta (lanza/alabarda, espada a dos manos, puños) y la IA los usa en esos NPC [C, descripciones: "custom attack animations"]. (2) parcial: Fist Fighters v5.2 "NPCs can combo you / cannot stagger so easily" [C, changelog]. No son un arreglo general: cada clase de arma necesita su propio brain/stance/animaciones.
- **Licencia/fuente**: no consta. Conflictos: "Fixed the range bug" (Fist Fighters 5.4) indica problemas de alcance [C]; el 2H remaster "made compatible with mods requiring the old version" [C].

### 1.8 BossaNova's Dueling Melee Bots (Nexus 2359)
- **Autor**: BossaNova (uploader thesparks007). v1.2; creado 2020-07-25; **actualizado 2026-08-11** [C]. 20 642 descargas, tags "Gameplay", "NPC Behaviours (AI)".
- **URL**: https://www.nexusmods.com/bladeandsorcery/mods/2359
- **Compatibilidad declarada**: **"For B&S 1.3.1"** (resumen) y "Blade and Sorcery 1.3.1 — 08/10/2026" (descripción) [C]. **Es el único mod encontrado que declara 1.3.1 explícitamente.**
- **Qué cambia**: **solo JSON** (waves, creature files, containers; el fichero principal "no sobrescribe ningún JSON vanilla", sus ficheros llevan sufijo "Bossa") [C]. La versión **Crystal Hunt SÍ sobrescribe las creature tables base de todos los tipos melee** [C] → conflicto potencial con otros mods de IA/creatures.
- **Qué hace [C, descripción; A en cuanto a efectividad]**: solo enemigos melee; "NPCs attack more frequently and mercilessly. After blocking one of your attacks NPCs may swiftly counter attack"; más HP; armas de respaldo si les desarman; resistentes al stagger; "**Expanded Attack Animations: the bots now have access to a wider move pool**".
- **Quejas**: (1) contraataque tras bloqueo [A]; (2) **sí**, "wider move pool" [A]; (3) no. Aviso del autor: con mods/ajustes que hacen al jugador un dios no será divertido; los enemigos "no son humanos... pueden ser trampeados".
- **Licencia/fuente**: no consta.

### 1.9 Combat Overhaul (mod.io `combat-overhaul`) y otros listados de mod.io
- **URL**: https://mod.io/g/blade-and-sorcery/m/combat-overhaul — descripción [C, og:description]: "a remake of my other mod, combat overhaul... NPCs will now combo you, LOTS of new ATTACK animations and are very fast. Its a better version of enhance difficulty". WebSearch lo marca como WIP (no verificado). Miniatura con nombre `image_2024-08-16...` → fecha ≈ 2024-08-16 [I]. Autor, versión, licencia, compatibilidad: **no consta**. Qué cambia (JSON vs DLL): **no consta**.
- https://mod.io/g/blade-and-sorcery/m/enhanced-difficulty — "An attempt to encourage the player to play more defensively and focus on counter attacks" [C]; miniatura fechada 2022-06-25 [I] → casi seguro obsoleto (U11).
- https://mod.io/g/blade-and-sorcery/m/difficult-and-relastic-combat-u12 — "Makes Enemies Faster, Stronger, and more Buff. Makes players Weaker." [C]; "U12" en el título.
- https://mod.io/g/blade-and-sorcery/m/hand-dynamics — Hand Dynamics (ver 1.11).
- Todos: **no es posible leer fecha/versión/licencia/conflictos** (SPA). Quejas: Combat Overhaul → (2) por [A].

### 1.10 Mods históricos (U7–U11), útiles como diseño pero no compatibles con 1.3.1
Todos pertenecen a versiones anteriores a 1.0; compat con 1.3.1: **no** [I] (obsoletos); se citan por su enfoque.
| Mod | Autor / fecha / versión | Tipo | Qué aporta [C] |
|---|---|---|---|
| SharpAI (Nexus 600) | Siilk, 2019-08-16 / 2019-10-29, v1.1b "reworked for U7" | **DLL + JSON** (`SharpAI.dll` 39 424 B + 27 JSON) | ">1800 líneas de C#" sobre el código de IA original de KospY: **contraataque, retroceso/dash para evadir y devolver el golpe**, más estabilidad. Quejas (1). El más cercano a lo que queremos hacer en código. |
| YACO (Nexus 977) | Yangbang, 2019-10-31 / 2020-03-12, v1.5.2 | JSON | IA más dura + oleadas; la versión "combat overhaul" **sobrescribe damagers, items y creatures** → incompatible con otros que toquen esos ficheros (declarado). "Not compatible with any Update 8" (declarado). |
| PaceAI (Nexus 1611) | Carimya, 2020-04-04 / 2021-08-01, v0.94 (juego 9.3) | JSON (brains) | Reduce delays de ataque, follow-ups, "turned off parry-stagger" (versión alternativa lo reactiva). Recomienda orden de carga tras The Outer Rim. |
| AI Plus U9 (Nexus 3207) | Bat, 2021-02-06 / 2021-04-23, v1.2.4 | JSON (brains) | Más ataques, "parries better", animación +10 %. Requiere Jedi Knight Saber Pack. |
| Skilled AI U8/U9.3 (Nexus 3032) | Chips, 2021-01-03 / 2021-02-21, v0.8 | JSON | Mejor bloqueo y juego de pies (waves separadas); requiere MMP en una variante. |
| Tough AI (U11) (Nexus 5100) | BenzoRanger, 2021-11-26 / 2022-08-04 | JSON + `ToughAI11.dll` (4,6 KB) | Brain de dificultad máxima: +50 % velocidad de ataque, mejor reacción, resistencia al stun. Versión U10 incluía `BehaviorTree_HumanCombatHard.json`. |

### 1.11 Otros mods que tocan comportamiento de NPC (no abordan las 3 quejas)
- **Firearm AI for 1.3** (Nexus 8734) — HuJohner, v4.0.0, 2026-07-30 [C]; "for 1.3" en el título y changelog "updated to 1.3" [C]. https://www.nexusmods.com/bladeandsorcery/mods/8734. 188 850 descargas. v3.0.0 contiene `FirearmAIFix.dll` (29 696 B) + `BehaviorTree_HumanCombatFirearm.json` + brains; **v4.0.0 no lista DLL en el zip** (solo JSON y thumbnail) [C]: no sé dónde reside entonces la lógica (¿integrada en el juego 1.3? ¿otro fichero?) → **no consta**. **Importante**: un brain JSON de otro mod referencia el tipo `FirearmAIFix.BrainModuleFirearm, FirearmAIFix` (ver 2.1) → **demuestra que existen BrainModule personalizados entregados como DLL y enlazados por `$type` en JSON** [C, fichero Brain_aipHighSkillFighter.json del repo Outcome-AI-Plus]. No es de melee.
- **VY's Fireable NPC jsons (BAS 1.0)** (Nexus 9856) — vyvince1997, v2.4 2026-08-29: solo JSON (brains y stances); "stance data that prevents enemies from changing stance" [C]. https://www.nexusmods.com/bladeandsorcery/mods/9856. Muestra que **los stances se pueden sobrescribir por JSON** [I].
- **Heavy Enemies Minibosses 1.0** (Nexus 10606) — PhantomPurpose v1.31, 2026-06-18 [C]: solo JSON (brains propios); "most of their attacks can't be blocked, parried, or interrupted"; "Reworked ... movesets, all openers can be parried". Requiere Expanded Armory. Compat "1.0" en el nombre.
- **Geno's Tough NPCs** (Nexus 13085): solo JSON, HP 850 [C]; solo dificultad por vida. Sin IA.
- **Hand Dynamics** (Nexus 4823; mod.io) — MemeMan v2.3.0.1, fichero 2024-06-21 [C]; DLL `HandDynamics.dll` 18 KB; enemigos se agarran a bordes y se tapan heridas. No aborda las quejas. WebSearch menciona reseñas de cuelgues en mod.io (no verificado).
- **Hit Reactions** (Nexus 14739) — TheSexyWolf, 2026-10-10; "AI-Generated Content"; NPC reaccionan a heridas con la mano. No aborda las quejas. "No he probado con otros mods que cambian el comportamiento de NPC" [C].
- **Fromsoftware Parry - Rekindled** (Nexus 13622, Zarythe, 2026-04-26, v1.0.1; basado en ChillioX, Nexus 4890): **lado jugador** (parada estilo Souls/Sekiro con DLL de 60 KB + JSON + bundles). Es relevante porque define cómo el NPC queda abierto tras la parada (lo hace del lado del jugador). No mejora la IA del NPC.
- **No Temporal Riposte** (11536, Idiot155) y **NoParryTimeSlow** (10904, SixStars): desactivan habilidades/slow-mo del jugador; solo JSON; no tocan la IA.
- **Varied NPC Loadouts** (BonelessGarbage, 12272): kits más variados; no aborda IA de combate.
- **Dismembering Attacks Mod** (mod.io y Nexus 14055): irrelevante.

### 1.12 Repositorios de GitHub (API pública, 2026-10-12)
| Repo | Qué es | Licencia | Último push | Relevancia |
|---|---|---|---|---|
| https://github.com/ryanoutcome20/Outcome-AI-Plus | "Outcome Expansion - AI Plus": 11 `Brain_aip*.json` (Caster, Marksman, Low/Med/High/Insane SkillFighter + Heavy) y ~60 waves; manifest `GameVersion "1.0.0.0"` | **MIT** [C] | 2025-11-06 | **Único mod de IA con código/datos abiertos hallado.** Solo JSON, hecho "con The Outer Rim en mente". Útil para ver campos reales de `BrainData`. |
| https://github.com/ZeytaVR/vr-combat-agent | Agente PPO con Unity ML-Agents "foundation for adaptive NPC AI in Blade & Sorcery and other combat titles" | sin licencia | 2026-05-28 | 0 estrellas; no es un mod instalable; sin evidencia de integración con ThunderRoad. |
| https://github.com/DaemonOnUnix/BladeAndSorceryReversedMods | Mods .NET descompilados "para tener ejemplos de código" (incluye una copia descompilada de 0Harmony) | sin licencia | 2023-06-18 | Muestra ejemplos antiguos; licencia ausente → no reutilizar. |
| https://github.com/Neeshka76/Revenants, https://github.com/Bread-Hockey/EyesCloseOnDeath, https://github.com/Tweety-Lab/ReturnByDeath, https://github.com/Tweety-Lab/QuickHack, https://github.com/Coomzy/BladeAndSorcery_CoomzyCollection (Unlicense), https://github.com/charlierix/BladeAndSorcery | Mods de código abierto (no IA) | MIT/AGPL/MIT/MIT/Unlicense/sin licencia | 2026 (los cuatro primeros) | Sirven de **plantilla de proyecto** (csproj, ThunderScript, ModOption). Ver `sdk-docs-notes.md`. |
| https://github.com/AdammantiumMultiplayer/AMP | Multijugador U12 (Apache-2.0) | Apache-2.0 | 2026-08-16 | Sincroniza creatures/waypoints; no relevante para IA de melee, pero es un proyecto grande de código. |
- Búsquedas sin resultado útil [C]: `topic:blade-and-sorcery` → 1 repo (Zephlyn/dynamic-surroundings, 2021, C#, sin relación); `ThunderScript` → solo repos de otros proyectos; `BrainModule` → **no se pudo buscar** (code search exige login); consultas en descripción "ai/npc/enemy/brain" → solo los repos de la tabla.
- Nexus Nomad: AdaptiveAI (5818) y Ai Adjuster (5934) tienen versión Nomad [C, búsqueda/API]; los demás mods de IA no. Nomad: la fuente más antigua (Nexus news 2021) dice que el límite de enemigos en pantalla es 3 y que el modding no estaba habilitado en el lanzamiento; está desactualizada [I].

---

## 2. Datos de ejemplo útiles obtenidos (no son mods, pero condicionan el diseño)

### 2.1 Estructura real de un `BrainData` (Outcome-AI-Plus, `Brain_aipHighSkillFighter.json`, GameVersion 1.0.0.0) [C]
URL raw: https://raw.githubusercontent.com/ryanoutcome20/Outcome-AI-Plus/main/Outcome%20Expansion%20-%20AI%20Plus/Brains/Brain_aipHighSkillFighter.json
- Cabecera: `$type: ThunderRoad.BrainData, ThunderRoad`, `id`, `version: 2`, `difficulty: 3`, **`treeID: "Human"`** (árbol de comportamiento referenciado por ID, definido en otro JSON `BehaviorTree_*`), `updateTree`, `cycle`…
- `modules[]` con `$type` por módulo: `BrainModuleSpeak, Move, Melee, Bow, Cast, LookAt, Detection, Patrol, Equipment, Grabbed, HitReaction, Death, Electrocute, Sightable, Stance, Throw, Follow, Defense, Fear` (todos `ThunderRoad.*, ThunderRoad`) **y `FirearmAIFix.BrainModuleFirearm, FirearmAIFix`** (módulo de un DLL de terceros).
- `BrainModuleMelee` (37 claves): `minMaxTimeBetweenAttack`, `attackMaxAngle`, `meleeSafeDistance`, `weaponReachRatio`, `recoilNothingRiposteChance`, `minMaxRiposteDelay`, `riposteSpeedAdditiveModifier`, `damagedIgnoreDefenseWindow`, `mostRecentAttack`, `followingThrough`, `meleeIdle`, `minAttackBehindChance`, `animationSpeedMultiplier`, `moduleStance`, `moduleMove`, `moduleDetection`…
- `BrainModuleDefense` (52 claves): `parryEnabled`, `parryDetectionRadius`, `parryAdjustSpeed`, `riposteBlockRequirement`, `thrustSensitivityVelocity`, `dodgeEnabled`, `dodgeChanceCurve`, `minMaxDodgeCooldown`, `dodgeSpeed`, `sideDodgeEnabled`, `shieldRiposteChance`, `aboutToBeHitTiming`, parámetros de IK de escudo/una mano…
- Implicación [I]: gran parte de la queja (1) (parada/esquiva) y parte de la (2) se pueden afinar **solo con datos**; lo que no se puede (decisión por contexto, selección de ataque según arma, fintas) exige código.
- Nota: es de la era 1.0; los campos pueden haber cambiado en 1.3.1. El proyecto tiene sus propios JSON en `.local/basjson/Brains` (no los he leído).

### 2.2 Qué no existe (explícito)
- **Ningún mod encontrado selecciona/filtra el ataque por el tipo de arma equipada de forma general** (queja 3). Solo hay soluciones por clase de arma (lanza, 2H, puños) con animaciones propias (1.7). **No encontrado.**
- **Ningún mod encontrado cambia en código la lógica de `BrainModuleMelee`/`BrainModuleDefense` y publica el fuente.** Los de código (AdaptiveAI, Ai Adjuster, Relentless, Better AI, KOKO) son cerrados o sin fuente enlazada. **No encontrado.**
- **No hay referencias a Reddit ni a Discord públicos indexados** (ver §0).
- No pude confirmar compatibilidad con 1.3 de AdaptiveAI, Ai Adjuster, Relentless NPCs, Palp Brains ni Enhanced Difficulty. Solo Bossa (1.3.1), KOKO (1.3 por nombre) y Firearm AI (1.3) lo declaran.

---

## 3. Tabla comparativa

Leyenda quejas: 1 = defensa/parada/esquiva/contraataque; 2 = ataques repetitivos; 3 = ataque-arma. "A" = según el autor [A]; "-" = no aborda.

| Mod (Nexus id) | Autor | Versión / últ. act. | Compat. declarada | Tipo [C] | Q1 | Q2 | Q3 | Fuente abierta / licencia |
|---|---|---|---|---|---|---|---|---|
| AdaptiveAI (12790) | evilbenny | 1.5 / 2026-01-08 | no consta | DLL | A (bloqueo predictivo, contraventanas, fintas) | A (predicción, Markov) | - | no consta / cerrado |
| Ai Adjuster (12919) | BREAD | 1.0 / 2025-12-10 | no consta | DLL + .sav | parcial (riposte time/cooldown) | parcial (intervalos) | - | no consta |
| Relentless NPCs Reborn (14570) | supermodder1039 | 1 / 2026-09-09 | no consta | DLL + 0Harmony | empeora (sin esquivas) | parcial (combos continuos) | - | no consta; Harmony MIT empaquetado |
| KOKO NPC tweaks (14490) | K O K O | v3 / 2026-09-27 | "1.3" (título) | DLL + JSON | parcial (reacción) | incierto | incierto | no consta |
| Better AI (4907) | Piepop101 | 1.5 / fichero 2024-06-20 | "1.0" (nombre) | DLL | casi nada | - | - | no consta |
| Enhanced Difficulty Remake (12108) | Palpatine | 1.0 / 2025-05-30 | "1.07" (nombre) | JSON | A (bloqueo, footwork) | A ("confusing attacks") | - | no consta |
| Palp Brains + Attacks (12107) | Palpatine | 1.0 / 2025-05-30 | no consta | JSON + bundle | base | base | base (infraestructura) | no consta |
| Spear enemies (11374) / 2H remaster (14139) / Fist Fighters (10802) | Palpatine (+BREAD) | 2025-01 / 2026-07 / 2025-04 | "U1.06" / no consta / "1.0-1.04" | JSON + bundle (+DLL mínimo en Fist) | - | parcial (combos en Fist) | **sí, solo para su clase de arma** | no consta |
| Bossa's Dueling Melee Bots (2359) | BossaNova | 1.2 / 2026-08-11 | **"1.3.1"** | JSON | A (contraataque tras bloqueo) | A (pool ampliado) | - | no consta |
| Heavy Enemies Minibosses (10606) | PhantomPurpose | 1.31 / 2026-06-18 | "1.0" (nombre) | JSON | (ataques no parables) | A (movesets rehechos) | - | no consta |
| Firearm AI for 1.3 (8734) | HuJohner | 4.0.0 / 2026-07-30 | "1.3" | DLL(v3) + JSON | - | - | - (solo armas de fuego) | no consta |
| Combat Overhaul (mod.io) | desconocido | desconocida / ≈2024-08 [I] | no consta | no consta | - | A (combos, anims) | - | no consta |
| SharpAI (600) | Siilk | 1.1b / 2019 | U7 | DLL + JSON | **sí** (contraataque, retroceso) | - | - | no consta |
| Outcome AI Plus (GitHub) | ryanoutcome20 | 1.0.0 / 2025-11-06 | "1.0.0.0" (manifest) | JSON | parcial (brains por nivel) | - | - | **MIT**, abierto |
| PaceAI / AI Plus / Skilled AI / Tough AI / YACO | varios | 2019-2022 | U7-U11 | JSON (± DLL) | parcial | parcial | - | no consta |

---

## 4. Huecos no cubiertos por ninguno

1. **Queja 3 (ataque según el arma)**: no hay solución general. Solo parches por clase de arma con animaciones propias (lanza, 2H, puños). Nadie publica una lógica que, dado el arma equipada (daga, maza, hacha, lanza, escudo…), restrinja o priorice ataques coherentes. **No encontrado** ningún mod que lo haga.
2. **Defensa contextual**: los mods o suben velocidades/probabilidades (datos) o declaran heurísticas opacas (AdaptiveAI). Ninguno documenta, con código abierto, parada dirigida por trayectoria del arma entrante, elección parar/esquivar/contraatacar según situación o fintas del NPC verificables.
3. **Esquiva**: Relentless NPCs la elimina; Better AI/Ai Adjuster no la tocan; solo AdaptiveAI afirma "dodges" [A]. Sin verificación.
4. **Código abierto**: el único mod de IA con fuente abierta es JSON puro (Outcome AI Plus, MIT). Ningún mod de IA con DLL entrega el código fuente. No hay base reutilizable de BrainModule de melee.
5. **Compatibilidad con 1.3.1**: solo Bossa (JSON) la declara explícitamente; para los de código (AdaptiveAI, Ai Adjuster, Relentless) no consta y habrá que probarlos en 1.3.1 si se quiere medir contra ellos.
6. **Coexistencia**: nadie documenta cómo conviven varios mods de IA (todos pisan los mismos brains/creature tables o los mismos métodos de `ThunderRoad`). Hay riesgo de conflicto con YACO-estilo (overwrites), Bossa Crystal Hunt (sobrescribe creature tables) y mods con `0Harmony.dll` empaquetado. No hay informes de conflictos concretos en las fuentes consultadas.
7. **Medición**: ningún mod publica métricas ni pruebas automatizadas de comportamiento de IA.
8. **Reddit/Discord/mod.io**: no se pudieron revisar (acceso bloqueado o SPA); cabe que existan mods o discusiones relevantes ahí. Esto es una limitación de la investigación, no una prueba de ausencia.

---

## 5. Fuentes principales (todas consultadas 2026-10-12)
- Nexus GraphQL (datos y contenido de zips): https://api.nexusmods.com/v2/graphql ; páginas de mod: `https://www.nexusmods.com/bladeandsorcery/mods/<id>` con ids 12790, 12919, 14570, 13449, 14490, 14363, 4907, 12108, 12107, 11374, 14139, 10802, 2359, 10606, 8734, 9856, 13085, 4823, 14739, 13622, 11536, 10904, 977, 600, 1611, 3207, 3032, 5100.
- mod.io (meta OG): https://mod.io/g/blade-and-sorcery/m/ai-adjuster , /combat-overhaul , /enhanced-difficulty , /difficult-and-relastic-combat-u12 , /hand-dynamics , /growing-npcs1 .
- GitHub API/raw: https://api.github.com/search/repositories (varias consultas), https://github.com/ryanoutcome20/Outcome-AI-Plus , https://github.com/ZeytaVR/vr-combat-agent , https://github.com/DaemonOnUnix/BladeAndSorceryReversedMods , https://github.com/topics/blade-and-sorcery .
- WebSearch (resúmenes, no verificables al 100 %): búsquedas "Blade and Sorcery AI mod" (varias); Steam discussions https://steamcommunity.com/app/629730/discussions/0/3767858179339219611 y /3196993949744432570 (un comentarista pide "remove parry recoil from NPCs" y que las animaciones de NPC "need a complete revamp"; sin fecha; **opinión de usuario, no hecho**).
