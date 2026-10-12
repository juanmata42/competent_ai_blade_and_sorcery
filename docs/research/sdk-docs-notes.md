# Notas sobre la documentación oficial del SDK de Blade & Sorcery

Fecha de consulta de todas las fuentes: **2026-10-12**. Juego objetivo: 1.3.1.260730005.
Marcas: **[C]** confirmado en la fuente citada; **[T]** observado en proyectos de terceros (no oficial); **[I]** inferido por mí.

## 0. Advertencia principal: la documentación oficial está desactualizada respecto a 1.3.1

- El sitio oficial es **https://kospy.github.io/BasSDK/** (wiki "bas-wiki", tema Just the Docs) [C]. El repositorio https://github.com/KospY/BasSDK dice en su README: "This repo has migrated from github to azure. This repo will no longer be updated" y apunta a Azure DevOps; menciona un "1.1.0 SDK" [C] (49 commits visibles; no hay fecha de último commit en la página).
- El texto de las guías sigue hablando de **U12 / 12.1 / 12.2**: "As for the U12 version, the game use around 30 types of JSON" (JSONModding), "They're loaded only once currently (as of 12.2)" (ThunderScript), "as of U12.1, custom ModOptionParameter subtypes are not supported" (ModOptions) [C]. Existe una "1.0 SDK Changelog" (`.../ThunderRoad/1.0Changelog.html`) cuyo contenido útil es sobre herramientas de animación facial y shaders, **nada sobre IA, manifest ni DLL** [C]. La guía "U12 to 1.0 migration" trata solo de iconos, shaders LitMoss, imbues y mapas [C].
- **Consecuencia [I]**: todo lo que sigue es la mejor referencia oficial disponible pero hay que verificarlo contra `ThunderRoad.dll` de 1.3.1 (descompilar o inspeccionar con un decompilador). No hay documentación oficial de 1.1, 1.2 ni 1.3.
- Qué no pude comprobar: no hay changelog oficial de 1.1-1.3 en el sitio; el Azure DevOps del SDK no es accesible sin cuenta (no lo intenté).

## 1. Estructura de un mod con código y `manifest.json`

Fuente: https://kospy.github.io/BasSDK/Components/Guides/SDK-HowTo/JSONModding.html [C].

- Un mod es una carpeta propia dentro de `Mods`:
  - PCVR: `[Blade & Sorcery]\BladeAndSorcery_Data\StreamingAssets\Mods`
  - Nomad: `[almacenamiento]\Android\data\com.Warpfrog.BladeAndSorcery\files\Mods`
- Fuente: https://kospy.github.io/BasSDK/Components/Guides/HowToInstallMods.html [C]: la carpeta del mod va **directamente** en `Mods`, no en una subcarpeta; los mods de versiones anteriores "may be obsolete and unplayable"; con el gestor de mods del juego se actualizan automáticamente si el autor publicó actualización, "otherwise, it is disabled".
- **`manifest.json` obligatorio**: "A manifest.json file is required in the mod folder, without it, the folder/mod will be ignored by the game" [C]. Ejemplo oficial (literal):
  ```json
  {
      "Name": "MyMod",
      "Description": "This is my cool mod",
      "Author": "Someone",
      "ModVersion": "1.0.0",
      "GameVersion": "1.0.0.0",
      "Thumbnail": "thumbnail.png"
  }
  ```
  Campos oficiales: `Name`, `Description`, `Author`, `ModVersion`, `GameVersion`, `Thumbnail` [C]. `Thumbnail` es ruta relativa a la carpeta del mod o una URL http(s) [C]. El SDK de Unity tiene un "manifest.json creator" en el Asset Bundle Group (solo si se usa el editor) [C].
- **`GameVersion`**: la única definición oficial es "GameVersion is the game mod version compatible with the mod" [C]. **No hay documentación oficial** sobre si el juego rechaza, avisa o ignora un `GameVersion` distinto del actual, ni sobre el formato exacto para 1.3.1 (¿`1.3.1.0`? ¿`1.3.1.260730005`?). **No encontrado.**
  - Observación de terceros [T]: mods de 2025-2026 que funcionan en 1.x declaran `"GameVersion": "1.0.0.0"` (Outcome-AI-Plus, AMP, KillRewards de Coomzy, ReturnByDeath de Tweety-Lab, este último con la clave `"Version"` en lugar de `"ModVersion"`, generada por la librería AliLib). Sugiere [I] que el valor no se aplica estrictamente, pero **no está confirmado**. Un comentario de Steam recogido por WebSearch dice que el número de versión se ve en el manifest (anecdótico, sin enlace fiable).
  - Nexus: un artículo de Nexus dice que los autores deben incluir `manifest.json` para que el juego lo cargue y Vortex lo detecte [C según resumen de WebSearch: https://www.nexusmods.com/hacklastrecode/news/14058, noticia genérica "Vortex support added: Blade & Sorcery"].
- Un mod con DLL: "you just need to compile your dll for the game and include a manifest.json as normal when making mods. The game will automatically find any ModOptions in your dll and load them" (https://kospy.github.io/BasSDK/Components/Guides/ModOptions/) [C]. La página de ThunderScript añade: "If one or multiple ThunderScript classes are present in your DLL, the mod loading process will pick them up and run their methods" [C]. Es decir: **un mod solo-código = carpeta con `manifest.json` + `*.dll`** (no hacen falta bundles) [C/T: confirmado también por los zips de AdaptiveAI, AI Adjuster, Relentless NPCs en Nexus, que contienen solo `manifest.json` + dll(s), ver `existing-mods.md`].
- Los JSON de datos pueden vivir en la misma carpeta (cualquier subcarpeta); el juego identifica cada entrada por `$type`, `id` y `version`; para modificar algo existente se crea un JSON con el **mismo `id`** y **solo los campos a cambiar** "so other mods can change different fields in the same JSON without conflicting" [C]. Ejemplo oficial: `{"$type": "ThunderRoad.CreatureData, ThunderRoad", "id": "PlayerDefaultMale", "version": 6, "health": 1000, "locomotionSpeed": 5.0}` [C].
  - Un JSON puede referenciar "custom class type from DLL plugins" [C] (el JSON hace referencia al tipo por `$type`, ver §5).
  - Tras una actualización del juego que cambie el JSON editado, la recomendación oficial es partir de una copia nueva del JSON [C].
- Los JSON base están empaquetados en `StreamingAssets\Default` como `.jsondb` (zip) y sueltos en el proyecto del SDK en `BuildStaging\Catalogs\Default` [C].

## 2. `ThunderScript` y su ciclo de vida

Fuentes: https://kospy.github.io/BasSDK/Components/Guides/ThunderScript/ [C] (la página homónima en `ThunderRoad/Scripting/Thunderscript.html` es un stub sin contenido [C]).

- Definición: clase base de scripts "globales" de un mod; carga sin JSON adicional; **sustituye a los LevelModules colgados de `Level_Master` en U11 y anteriores**; un mod puede tener tantas clases `ThunderScript` como quiera [C].
- **Cuándo carga**: "when the mod itself loads, only after the 'Play' button is pressed in the main menu screen"; no se puede modificar el menú principal sin herramientas de terceros; "loaded only once currently (as of 12.2)", puede cambiar en el futuro [C].
- Uso: heredar de `ThunderScript` y sobrescribir lo necesario. Métodos (todos `public override`, llamar a `base.X()`):
  | Método | Cuándo / para qué [C] |
  |---|---|
  | `ScriptLoaded(ModManager.ModData modData)` | Una vez al cargar. Suscribir eventos **estáticos** (p. ej. `EventManager.onLevelLoad`, `EventManager.onCreatureSpawn`), log de arranque, fijar el singleton. `modData.Name` y `modData.fullPath` (ruta de la DLL) disponibles. (Es `ScriptLoaded`, no `ScriptLoad`.) |
  | `ScriptEnable()` | Al habilitar; en 12.1 solo corre una vez tras cargar. Buen sitio para suscribir eventos. |
  | `ScriptFixedUpdate()` | Cada frame de física (`Time.fixedDeltaTime`): fuerzas, velocidades. |
  | `ScriptUpdate()` | Cada frame antes de animación/IK/render (`Time.deltaTime`). |
  | `ScriptLateUpdate()` | Cada frame tras IK y animación; parte del IK de B&S corre en LateUpdate, puede requerir trabajo extra de alineación. |
  | `ScriptDisable()` | Una vez al deshabilitar; en 12.1 **nunca ocurre**; desuscribir eventos. |
  | `ScriptUnload()` | Una vez al descargar; "doesn't get executed at all" (12.x); limpiar singleton. |
- Ejemplo oficial relevante: `EventManager.onCreatureKill += handler` con firma `(Creature creature, Player player, CollisionInstance collisionInstance, EventTime eventTime)`; el evento se dispara dos veces (`EventTime.OnStart` y `OnEnd`); `creature.IsEnemy(Player.currentCreature)`; `Player.currentCreature` es null si muere el jugador [C]. Un `[ModOption] public static float printFrequency;` coexiste en la misma clase [C].
- Implicación [I]: `ScriptLoaded` es el punto natural para **registrar un Harmony patch o suscribirse a `EventManager.onCreatureSpawn`** y engancharse a la IA de cada NPC; `ScriptEnable` para activar lo que dependa de que el nivel esté cargado. Los eventos disponibles de `EventManager` en 1.3.1 hay que listarlos de `ThunderRoad.dll` (un repo de terceros, https://github.com/Neeshka76/EventListDLL, "tool to list all the Events inside ThunderRoad.dll", 2022, existe [T]).

## 3. Atributos `ModOption` (menú de opciones in-game)

Fuentes: https://kospy.github.io/BasSDK/Components/Guides/ModOptions/ [C]; la página `ThunderRoad/Scripting/ModOptions.html` es un stub [C].

- Se aplican a **miembros `static`**: campos, propiedades o métodos; cualquier tipo de clase (static, MonoBehaviour, ThunderBehaviour, CatalogData, ThunderScript...) [C]. "ModOptions load after all mods' assemblies, addressable bundles, and catalogs, and before ThunderScripts" [C].
- Atributos [C]: `ModOption` (principal; admite toda la configuración), `ModOptionCategory(nombre, orden, localizationId)`, `ModOptionOrder`, `ModOptionTooltip` (anula el del ModOption), `ModOptionUI` (tipo de interacción `ModOption.InteractionType`), `ModOptionArrows`, `ModOptionButton`, `ModOptionSlider`, `ModOptionSave`, `ModOptionDontSave`, `ModOptionSaveValue(bool)`. **`ModOptionTweak` no aparece en la documentación** (lo pedía la tarea; **no encontrado**).
- Orden de menú: categoría (orden), nombre de categoría, orden de opción, nombre de opción [C].
- Nombre automático: sin nombre, se genera desde el miembro (`EnableMyCoolFeature` → "Enable My Cool Feature"); bool → "Disabled"/"Enabled" [C]. Enums: todos los valores se convierten en opciones automáticamente [C].
- Guardado: por defecto los valores se guardan en el archivo de opciones del jugador; se desactiva con `saveValue = false` o `[ModOptionDontSave]` [C].
- Listas de valores propias: array estático de subtipos de `ModOptionParameter` (p. ej. `ModOptionFloat`), referenciado con `valueSourceName`; `defaultValueIndex` elige el valor por defecto; **en U12.1 los subtipos personalizados de `ModOptionParameter` no están soportados** [C].
- Ejemplo oficial con categoría: `[ModOptionCategory("Magic", 1)] [ModOptionOrder(1)] [ModOption("Gravity Blast Force", "Sets the amount of force gravity blast uses")] private static int blastForce;` [C].
- Firma real vista en código de terceros (slider) [T, https://github.com/Bread-Hockey/EyesCloseOnDeath, rama por defecto, ~2026]: `[ModOption("Close Chance", defaultValueIndex = 100, interactionType = ModOption.InteractionType.Slider, valueSourceName = nameof(slider1), tooltip = "...")] public static float slider; internal static ModOptionFloat[] slider1() { return ModOptionFloat.CreateArray(0, 100, 1); }`. Confirma los parámetros con nombre `defaultValueIndex`, `interactionType`, `valueSourceName`, `tooltip` y `ModOptionFloat.CreateArray(min, max, step)`.
- No documentado oficialmente [no encontrado]: cuándo se leen los valores respecto al spawn de cada NPC (Better AI dice que "speed is changed for every new enemy that spawns in" [C, Nexus 4907]); cómo reaccionar a un cambio de opción en caliente (¿callback?); localización de nombres.

## 4. Cómo se compila un mod con DLL

**La documentación oficial no lo explica.** Solo dice "compile your dll for the game" (§1) [C]. No hay página sobre versión de .NET, referencias, IDE, ni plantilla. **No encontrado** en https://kospy.github.io/BasSDK/ (revisé: Home, Guides, ThunderScript, ModOptions, SDK-HowTo/{JSONModding, UnitySDKSetup, ...}, FAQ, Installing Mods; un FAQ/General dio 404).

- **Unity del SDK**: "Blade and Sorcery uses Unity 2021.3.38f1. A version with the same major.minor LTS release is generally fine, but using the exact version BAS uses gives the best compatibility" (https://kospy.github.io/BasSDK/Components/Guides/SDK-HowTo/UnitySDKSetup.html) [C]. El SDK se clona de KospY/BasSDK (maestro = última versión; cambiar de rama para otras versiones) [C]. **Esta página no menciona código, DLL, Visual Studio/Rider ni assembly definitions** [C]. **No pude verificar si 1.3.1 sigue en Unity 2021.3.38f1**; hay que comprobarlo en la carpeta del juego (`UnityPlayer.dll`/`globalgamemanagers`) [I].
- **¿Hace falta el SDK/editor de Unity para solo código?** Oficialmente, el SDK de Unity se documenta para componentes, items, niveles y asset bundles. Para un mod solo-código **no hace falta** abrir Unity [T/I]: los proyectos de terceros son csproj normales que referencian las DLL del juego y copian el resultado a `Mods/<mod>/` (ver abajo). Si hace falta crear prefabs/assets/animaciones (p. ej. un asset bundle de animaciones) sí se necesita el SDK y la versión exacta de Unity.
- **Prácticas observadas en proyectos de código abierto de 2024-2026** [T] (no oficiales):
  - `EyesCloseOnDeath` (Bread-Hockey): csproj clásico, `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>`, `OutputType Library`; referencias `ThunderRoad` (`...\Blade & Sorcery\BladeAndSorcery_Data\Managed\ThunderRoad.dll`), `UnityEngine`, `UnityEngine.CoreModule`. https://github.com/Bread-Hockey/EyesCloseOnDeath
  - AMP (AdammantiumMultiplayer): `net 4.7.2` (`v4.7.2`), refs a `GameDllDirectory`: ThunderRoad, Unity.Addressables, Unity.InputSystem, Unity.ResourceManager, Unity.TextMeshPro, UnityEngine.{AIModule, AnimationModule, AudioModule, CoreModule, ImageConversionModule...}, Newtonsoft.Json, Sirenix.OdinInspector.Attributes, PerfectCulling. https://github.com/AdammantiumMultiplayer/AMP
  - `ReturnByDeath` / `QuickHack` (Tweety-Lab): csproj SDK-style `netstandard2.0`, `LangVersion latest`, referencias HintPath a `..\..\Managed\*.dll` (carpeta `Managed` del juego copiada/enlazada): ThunderRoad, ThunderRoad.Reveal, Unity.InputSystem, Unity.RenderPipelines.{Core.Runtime,Universal.Runtime}, Unity.ResourceManager, Unity.TextMeshPro, UnityEngine, UnityEngine.{AnimationModule, AudioModule, CoreModule, PhysicsModule}. Genera el `manifest.json` desde código mediante la librería AliLib. https://github.com/Tweety-Lab/ReturnByDeath , https://github.com/Tweety-Lab/QuickHack
  - **Conclusión [I]**: dos configuraciones en uso real, **.NET Framework 4.7.2** y **netstandard2.0**; el runtime de Unity 2021.3 (Mono, API .NET Standard 2.1/Framework) acepta ambas. Elegir la que permita referenciar sin errores las DLL de `BladeAndSorcery_Data\Managed`. Verificar contra el `Managed` de 1.3.1 qué DLL de Unity son necesarias (ThunderRoad referencia `Unity.AI.Navigation`/`UnityEngine.AIModule` para NavMesh, `Unity.Addressables`, etc.).
  - El resultado va a `Mods\<NombreMod>\` con `manifest.json` + la DLL (+ opcionalmente `.pdb`; AI Adjuster lo distribuye) [T, zip de Nexus 12919].
- Nomad: los mods con código también existen para Nomad en Nexus (AdaptiveAI 5818, AI Adjuster 5934) [C]. Un artículo antiguo de UploadVR decía que los mods con scripting probablemente no funcionarían en Quest (2021, desactualizado) [C según WebSearch]. No hay doc oficial de limitaciones de código en Nomad más allá de FAQ "Android/Nomad" (no leída en detalle). Si el objetivo incluye Nomad, **no hay información oficial**.

## 5. Harmony / parcheo en runtime y `BrainModule` personalizados

### 5.1 Harmony
- **La documentación oficial no menciona Harmony, `HarmonyLib` ni parcheo en runtime** (comprobado por texto en ThunderScript, ModOptions, JSONModding, FAQ, Installing Mods) [C: búsqueda de cadena sobre el HTML]. **No hay política oficial** (ni permitido ni prohibido).
- Evidencia de uso real [C/T]: los zips de Nexus de **Relentless NPCs Reborn (14570)** (`0Harmony.dll` 2 461 696 B + `Harmony-LICENSE.txt`) y **K O K O's adjustables 1.3 (14363)** (`0Harmony.dll` 2 195 456 B) empaquetan su propia copia de Harmony en 2026; ambos contra 1.3 [C, API de Nexus `modFileContents`, ver existing-mods.md]. Hay además una copia descompilada de `0Harmony.2` en https://github.com/DaemonOnUnix/BladeAndSorceryReversedMods [T]. Por tanto **Harmony funciona (o al menos se distribuye) en B&S 1.3** [I: que cargue sin problemas no se ha probado por mí].
- Riesgos [I]: varias copias de `0Harmony.dll` de distintos mods en el mismo proceso (usa un ID único de Harmony y `Harmony.PatchAll` solo sobre tu ensamblado); fragilidad ante actualizaciones de `ThunderRoad.dll` (nombres de métodos internos cambian entre versiones, como muestra el hecho de que la doc sea U12).

### 5.2 `BrainModule`, `BrainData` y Stances
- **No hay documentación oficial de `BrainData`, `BrainModule*` ni `Stance`** [C]. La página https://kospy.github.io/BasSDK/Components/ThunderRoad/Creatures/Brain.html dice solo: "This component requires a NavMeshAgent on the same gameObject!"; "creatures use the Brain to navigate terrain, make decisions"; "The Unity component is blank as all fields are defined in the matching BrainData JSON" [C]. En la sección `ThunderRoad/JSON/` solo existen páginas de Bundle, ColliderGroup, DamageModifier, Damager, Effect, GameModes, Item, Level, Maps, Material, SkillTree, Skills, Status, Wave [C]; las URLs `JSON/Brain(s).html`, `JSON/Stance(s).html`, `JSON/BrainData.html`, `JSON/BehaviorTree.html` devuelven 404 [C].
- La lista de tipos JSON de U12 en JSONModding incluye "**BehaviorTrees** Behaviour trees used by AI" y "**Brains** AI parameters (aim precision, strafe distance, attack speed…)" (el texto original pierde el separador pero se lee así) [C]; los 30 tipos oficiales incluyen también Animations ("Dynamic animations reference that can be used on NPC"), Containers, Creatures, CreatureTables, Damagers, Waves, etc. [C].
- **Los módulos de un brain se instancian por `$type` en el JSON** (`"$type": "ThunderRoad.BrainModuleMelee, ThunderRoad"`), y un brain puede incluir un módulo de **otro ensamblado**: `"$type": "FirearmAIFix.BrainModuleFirearm, FirearmAIFix"` en `Brain_aipHighSkillFighter.json` del repo https://github.com/ryanoutcome20/Outcome-AI-Plus [T, confirmado leyendo el JSON]. Por tanto un **BrainModule personalizado en una DLL de mod es posible** y se activa desde un `BrainData` JSON sin parches [I, a partir de lo anterior]. El mod "Firearm AI for 1.3" (Nexus 8734, HuJohner) lo implementa con `FirearmAIFix.dll` + `BehaviorTree_HumanCombatFirearm.json` + brains [C]. No he visto su código fuente ni la clase base, así que **cómo se hereda de `BrainModule` y su ciclo de vida (Load/Update/Unload) no consta**; hay que inspeccionarlo con un decompilador sobre `ThunderRoad.dll` o la DLL de ese mod.
- Cómo se asocia un brain a un NPC [T, Nexus 12107, descripción de Palpatine]: en la **CreatureTable** `"overrideBrain": true, "overrideBrainID": "<id>"` [C]. El brain referencia un `treeID` (p. ej. `"Human"`) que apunta a un JSON `BehaviorTree_*` [T] y un `moduleStance` (stances en JSON propios: `Stance_HumanMelee2HStance.json`, `Stance_HumanMeleeDualWieldStance.json`, `Stance_VYHighStance.json` [T/C, contenidos de zips de Nexus 14139, 12107, 9856]). Las animaciones de ataque se entregan como asset bundle Addressables y se referencian por "addressable names" (la guía de Palpatine está en el Discord oficial, canal 1248289469204009022; **no leída**).
- Campos reales de `BrainModuleMelee` y `BrainModuleDefense` en la era 1.0: ver `existing-mods.md` §2.1 [T].

## 6. Resumen de lo que no encontré
- Versión de .NET / lenguaje exigida para DLL de mods; lista de referencias necesarias; plantilla de proyecto oficial. (Solo ejemplos de terceros.)
- Documentación oficial de `GameVersion` más allá de una frase; comportamiento ante versión distinta; formato para 1.3.1.
- Cualquier mención oficial a Harmony o a parcheo.
- Documentación oficial de `Brain`, `BrainData`, `BrainModule*`, `BehaviorTree`, `Stance`, ni cómo crear un módulo propio.
- `ModOptionTweak` (no existe en la doc), eventos de cambio de ModOption, lista de eventos de `EventManager`.
- Changelogs oficiales de 1.1, 1.2, 1.3 y 1.3.1; si Unity sigue siendo 2021.3.38f1.
- Páginas del Discord oficial (no indexadas) y de Reddit (acceso bloqueado al rastreador).

## 7. Fuentes consultadas (2026-10-12)
- https://kospy.github.io/BasSDK/ (home y navegación)
- https://kospy.github.io/BasSDK/Components/Guides/ThunderScript/
- https://kospy.github.io/BasSDK/Components/ThunderRoad/Scripting/Thunderscript.html (stub)
- https://kospy.github.io/BasSDK/Components/Guides/ModOptions/
- https://kospy.github.io/BasSDK/Components/ThunderRoad/Scripting/ModOptions.html (stub)
- https://kospy.github.io/BasSDK/Components/Guides/SDK-HowTo/JSONModding.html
- https://kospy.github.io/BasSDK/Components/Guides/SDK-HowTo/UnitySDKSetup.html
- https://kospy.github.io/BasSDK/Components/Guides/HowToInstallMods.html
- https://kospy.github.io/BasSDK/Components/ThunderRoad/Creatures/Brain.html
- https://kospy.github.io/BasSDK/Components/ThunderRoad/JSON/ (índice) y https://kospy.github.io/BasSDK/Components/ThunderRoad/1.0Changelog.html
- https://kospy.github.io/BasSDK/Components/Guides/U12-to-1.0-migration.html
- https://github.com/KospY/BasSDK (README)
- Terceros: https://github.com/Bread-Hockey/EyesCloseOnDeath , https://github.com/Tweety-Lab/ReturnByDeath , https://github.com/Tweety-Lab/QuickHack , https://github.com/AdammantiumMultiplayer/AMP , https://github.com/Coomzy/BladeAndSorcery_CoomzyCollection , https://github.com/ryanoutcome20/Outcome-AI-Plus , https://github.com/DaemonOnUnix/BladeAndSorceryReversedMods , Nexus API https://api.nexusmods.com/v2/graphql (mods 14570, 14363, 8734, 12107, 9856, 14139).
