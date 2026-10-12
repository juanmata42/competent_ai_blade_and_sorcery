# PROGRESS — Competent AI (Blade & Sorcery)

## Estado actual
**Fase 0 (Entorno): casi cerrada; falta solo la lista de mods que usa el usuario.** No se ha escrito código del mod.

## Hallazgos de entorno (verificados en el equipo, 2026-10-12)
| Elemento | Valor |
|---|---|
| Juego | Steam appid 629730, buildid 24471221 |
| Ruta | `D:\SteamLibrary\steamapps\common\Blade & Sorcery` |
| Versión del juego | **1.3.1.260730005** (según `Player.log`); `BladeAndSorcery.exe` FileVersion 1.3.1.27797 |
| Unity del juego | **2021.3.38f1** (según `UnityPlayer.dll`) |
| ThunderRoad.dll | `BladeAndSorcery_Data\Managed\ThunderRoad.dll` (4.0 MB, 2026-08-30). Incluye `.pdb` y `ThunderRoad.xml` (documentación XML, útil para verificar API) |
| Ensamblados auxiliares | `ThunderRoad.Manikin`, `.Utilities`, `.Reveal`, `.EventLinker`, `.PointCache`, `Unity.Addressables`, etc. |
| Carpeta de mods | `BladeAndSorcery_Data\StreamingAssets\Mods` — **vacía** (sin mods instalados ahora mismo) |
| .NET SDK | 9.0.306 |
| Git | 2.55.0 |
| ilspycmd | 9.1.0.7988 instalado globalmente (la última versión no instala con SDK 9). Ruta: `%USERPROFILE%\.dotnet\tools` |
| Unity Hub | Presente; único editor instalado: **2020.3.1f1** (NO coincide con el 2021.3.38f1 que usa el juego) |
| SDK de B&S | No localizado en Documents/Desktop/D:\ |
| Directorio de trabajo | Vacío, no es repo Git |

## Preguntas abiertas (Fase 0)
1. Mods de animaciones e IA que usas (la carpeta Mods está vacía; ¿los gestionas desde otra ruta/mod.io?). No bloquea la Fase 1, pero la tabla de compatibilidad depende de ello.

## Decisiones tomadas
- Plataforma: **solo PC** (datos + código C#). Sin capa Nomad.
- Versión objetivo: **1.3.1.260730005** (la instalada).
- Sin SDK de Unity: el mod se compilará como DLL con .NET referenciando los ensamblados del juego. El SDK/Unity 2021.3.38f1 solo se instalaría si hiciera falta empaquetar bundles/assets.
- Se instaló ilspycmd 9.1.0.7988 (herramienta global de dotnet) para consulta local del DLL.

## Siguiente paso
Esperar respuestas; después, Fase 1 (investigación).
