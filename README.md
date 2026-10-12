# Competent AI — mod de IA cuerpo a cuerpo para Blade & Sorcery

Mod **solo para PC** (código C# sobre `ThunderRoad`, juego **1.3.1.260730005**) que hace a los NPCs competentes en combate cuerpo a cuerpo:

- **Atacan y defienden en nueve ángulos** (ocho cortes + estocada), con un director de ataques con memoria: se acabó el patrón A-B-A-B.
- **Hacen fintas** (empiezan un ataque, lo abortan y lo redirigen) y **sufren fintas** (un cambio brusco de dirección del golpe del jugador puede dejar su guardia fuera de sitio).
- **Contraatacan**: tras parar (ajuste de probabilidad/retraso de contra) y tras esquivar.
- **Encadenan secuencias de golpes tipo kata** (2–6 según el nivel) suponiendo que el primero será parado, eligiendo cada golpe para que fluya del anterior (el corte contrario es el retorno natural), **con y sin avance**, y **las interrumpen** si te vuelves peligroso: el novato se empeña a ciegas, el duelista corta la secuencia y defiende.
- **Menú en el juego** (Opciones → Mods → *Competent AI*): activar/desactivar la IA del mod **en vivo** y elegir el **nivel de destreza** (Auto por tipo de enemigo, 1 Recruit, 2 Soldier, 3 Veteran, 4 Duelist) o desplazarlo.
- **Solo atacan con lo que su arma puede hacer de verdad**: sin cortes con dagas sin filo, sin estocadas con armas romas, con lanzas que recuperan los ataques que el juego les quita por un error de datos.
- Cuatro **arquetipos** configurables (Recruit, Soldier, Veteran, Duelist) asignados por Brain, facción o creatura, sin sobrescribir ningún catálogo ni parchear código del juego.
- Todo es **desactivable por separado** (JSON y menú in-game), con **logging configurable** para diagnosticar.

> Estado: **v0.4.0, sin probar en el juego** (no se puede ejecutar desde el entorno de desarrollo). Léelo antes de jugar: [docs/TESTING.md](docs/TESTING.md) tiene el plan de pruebas y la plantilla para reportar resultados.

## Instalar

1. Compila (ver abajo) o toma `dist/CompetentAI-0.4.0.zip`.
2. Copia la carpeta `CompetentAI` a `BladeAndSorcery_Data/StreamingAssets/Mods/` (debe quedar `.../Mods/CompetentAI/manifest.json`).
3. Arranca el juego. En `BladeAndSorcery_Data/StreamingAssets/Logs/Player.log` busca las líneas `[CompetentAI]`: `API self-check OK`, `v0.4.0 loaded…`.
4. Menú de opciones → pestaña Mods → *Competent AI*: interruptor general, nivel de destreza y cada módulo por separado (actúan en vivo). Detalles finos en `CompetentAI.config.json` (ver [docs/CONFIG.md](docs/CONFIG.md)).

## Construir

Requisitos: .NET SDK 9, el juego instalado (se compila contra `ThunderRoad.dll` real; **no se redistribuye ningún DLL del juego**).

```powershell
powershell -File tools/build.ps1 -Test            # compila + tests + dist/CompetentAI
powershell -File tools/build.ps1 -Deploy          # además lo copia a la carpeta Mods del juego (conserva tu config)
powershell -File tools/package.ps1                # dist/CompetentAI-<versión>.zip
```

La ruta del juego se toma de `-Game`, de `$env:BAS_GAME` o, por defecto, `D:\SteamLibrary\steamapps\common\Blade & Sorcery`.

## Estructura

| Carpeta | Contenido |
|---|---|
| `src/CompetentAI.Core` | Lógica pura (direcciones, reglas por arma, variedad, fintas, defensa, configuración). Sin dependencias de Unity: se prueba con xUnit. |
| `src/CompetentAI.Mod` | Integración con el juego (`ThunderScript`, módulo por criatura, curación del `MeleeStanceData`, opciones). Compila **a una sola DLL** `CompetentAI.dll` que incluye el Core. |
| `tests/` | Tests unitarios del Core y del fichero de configuración distribuido. |
| `mod/CompetentAI` | `manifest.json` y `CompetentAI.config.json` por defecto (con comentarios). |
| `tools/` | Scripts de build/package y el generador de la referencia de configuración. |
| `docs/` | Investigación, diseño, configuración, pruebas, compatibilidad. |

## Documentación

- [docs/PROGRESS.md](docs/PROGRESS.md) — estado, decisiones, preguntas abiertas, siguiente paso.
- [docs/research/SUMMARY.md](docs/research/SUMMARY.md) — síntesis de la investigación y decisiones de arquitectura.
- [docs/research/game-ai-internals.md](docs/research/game-ai-internals.md) — cómo funciona la IA del juego y causa raíz de las tres quejas.
- [docs/research/existing-mods.md](docs/research/existing-mods.md) · [sdk-docs-notes.md](docs/research/sdk-docs-notes.md) — mods existentes y documentación oficial.
- [docs/DESIGN.md](docs/DESIGN.md) · [docs/CONFIG.md](docs/CONFIG.md) · [docs/TESTING.md](docs/TESTING.md) · [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md) · [CHANGELOG.md](CHANGELOG.md)

## Licencia y ética de modding

- El código de este repositorio es propio. No contiene código descompilado de ThunderRoad ni de otros mods (la descompilación se hizo solo para consulta local; `.local/` está en `.gitignore`).
- No se redistribuye ninguna DLL del juego. De otros mods solo se reutilizan **ideas** descritas, no su código.
