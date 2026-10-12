# Plan de pruebas manuales

El mod **no se ha podido ejecutar** durante su desarrollo: todo lo que sigue está por verificar. Este documento es la lista de comprobaciones y la plantilla para reportar. Tiempo estimado de una pasada completa: ~45 min.

## 0. Preparación
1. Compila y despliega: `powershell -File tools/build.ps1 -Test -Deploy` (copia a `.../StreamingAssets/Mods/CompetentAI`, conserva tu `CompetentAI.config.json`).
2. En `CompetentAI.config.json` pon `"Logging": { "Level": "Debug" }` para las pruebas.
3. Ten a mano `BladeAndSorcery_Data/StreamingAssets/Logs/Player.log` (el log del juego; el del arranque anterior es `Player-prev.log`). Todas las líneas del mod empiezan por `[CompetentAI]`.
4. Para cada escenario, **sube el nivel de log solo si hace falta**; el limitador corta a 30 líneas/s.
5. Entorno recomendado: Sandbox/arena con creaturas humanas. Para dar un arma concreta a un NPC: invoca criaturas cuya tabla lleve esa arma, o suelta el arma junto a un NPC desarmado (los Brains humanos tienen un árbol *EquipMelee* para recoger armas; **no verificado** que recoja armas sueltas del suelo).

## 1. Arranque (1 min) — *imprescindible*
Busca en `Player.log` tras llegar al menú principal:

| Línea esperada | Significado |
|---|---|
| `[CompetentAI] API self-check OK (N members)` | Todas las piezas del juego que usa el mod existen |
| `[CompetentAI] v0.1.0 loaded from '…\Mods\CompetentAI' (config found)` | El `ThunderScript` se instanció |
| `[CompetentAI] effective settings: enabled=True …` | Flags en vigor |
| *(sin)* `[ModManager] … Incompatible with game version` ni errores `[ModManager][...][CompetentAI]` | Manifest correcto |

Si el autochequeo dice `FAILED … missing X`: copia esa línea completa; el mod se auto-desactiva (el juego queda como de serie).
En el menú de opciones debe aparecer la sección **Competent AI** (Mods) con 13 opciones.

## 2. Escenarios por arma
Para cada uno: lucha 1 contra 1 durante ~2 minutos, luego copia las líneas del log. Qué mirar:

| # | Escenario | Comprobar en juego | Logs a buscar |
|---|---|---|---|
| A | NPC con **espada** 1H (`SwordLongCommon`) | Varía de ángulos (no solo 2 cortes alternos); finta alguna vez; contraataca tras parar | `attached to …`; `stance 'HumanMelee1hStance' … curation: removed=0`; muchos `plan -> X` con ataques distintos; `windup … FEINT(random)`; `FEINT A -> B` |
| B | NPC con **lanza** (`SpearBladed`) | Solo estocadas, pero **más de dos tipos** (estocada, estocada con paso, contra-estocada); riposte = estocada | `curation: … readmitted=1 … +CounterThrust (declared Swing, really Thrust)`, `synthesized=1 … +riposte CompetentRiposteThrust` |
| C | NPC con **hacha larga/alabarda** (`AxeLongHalberd`) | Solo cortes (como de serie), sin estocadas | `curation: removed=…` (las estocadas que ya quitaba el juego no aparecen) |
| D | NPC con **maza** (`MaceShortCommon`/`MaceLongMaul`) | Solo golpes, nunca estocadas | idem; clase `Mace` en la línea `weapon …` (Debug) |
| E | NPC con **daga sin filo** (`DaggerSentaraStiletto`, `DaggerRaktaSai`) | **Nada de cortes**: solo estocadas | `curation: removed=… swing needs an edge or a blunt head (DaggerSentaraStiletto has neither)` |
| F | NPC con **Viennoise sword** (`ViennoiseSword`) | Solo golpes (arma contundente), nunca estocadas | `thrust needs a point (ViennoiseSword has none)` |
| G | **Arma modded** (cualquiera) | El NPC ataca igual que sin el mod (comportamiento conservador) | `weapon …[Unknown,None,… UNKNOWN]` en Debug; sin `removed` |
| H | **Espada + escudo** | Empujes de escudo siguen funcionando; fintas y variedad en los cortes | `stance 'HumanMeleeShieldStance' …`; no debe haber `removed` de `Push*` |
| I | **Doble empuñadura** (dos armas) | Cortes con ambas manos; si una arma es sin filo, esos cortes desaparecen | `stance 'HumanMeleeDualWieldStance'` |
| J | **1 contra N** (3–4 NPC) | Ninguno se queda parado; no más de los habituales atacan a la vez | Ningún `error in …` repetido |

**Criterio de fallo grave**: NPC que deja de atacar o se queda inmóvil, brazos retorcidos, caída de FPS, o una cascada de `[CompetentAI] … error in …`. En ese caso desactiva módulos (sección 4) y reporta.

## 3. Comportamiento (fintas, defensa, contraataque)

| # | Prueba | Cómo | Qué debe pasar | Log |
|---|---|---|---|---|
| F1 | **NPC finta** | Sube `FeintChance` de `Soldier` a 0.6 y pelea 1 min | Un ataque se corta a medio armar y sale otro de dirección distinta, sin daño intermedio | `windup … FEINT(…) at +0.xxs` seguido de `FEINT A -> B` |
| F2 | **Finta reactiva** | Pelea con el arma del jugador alta/guardando | Más fintas cuando tu arma cubre la zona del ataque | `FEINT(reactive)` |
| F3 | **NPC sufre tu finta** | Con un Recruit/Soldier: empieza un golpe, **cambia de dirección bruscamente** antes de impactar | A veces su guardia se queda donde estaba y recibe el golpe; a veces lo lee | `strike changed direction by NNN° -> Fooled` / `-> Read`; `fooled: commits to a dodge` |
| F4 | **Fatiga** | Golpea al NPC repetidamente con golpes que él pare (4+ en 3 s) | Deja de parar ~1 s: queda abierto | `has parried too much …`, `overwhelmed: parry disabled` |
| F5 | **Contra tras esquiva** | Ataca desde lejos para que esquive hacia atrás | Tras esquivar, a veces contraataca enseguida | `counter-attack after dodge` |
| F6 | **Combos** | Deja que ataque sin pararlo | A veces encadena 2.º ataque sin pausa | `combo A -> B` |
| F7 | **Parada con IK** (Veteran/Duelist) | Mira sus brazos al parar un golpe con una sola arma | La hoja sale a tu golpe; **sin brazos retorcidos ni temblor** | `one-hand parry IK enabled for …`; si falla: `could not enable weapon-placement IK` |
| F8 | **Esquiva lateral** (Duelist) | Ataca con una estocada a un Duelist (`HumanVIP`) | Puede apartarse de lado (si la animación existe) | `tuning applied … side=True` |
| F9 | **Direcciones medidas** | Tras ~20 ataques revisa `calibration.json` | Contiene una entrada por ataque con su dirección | `measured SwingTopToLeft: DownLeft (travel …)` |

Para F9 compara: ¿coincide la dirección medida con lo que ves? (En el log, `stance … openers=[id:Kind/Direction/source]`: `source` es `name` (inferida del nombre), `measured` o `unknown`.)

## 4. Aislamiento por módulos (si algo va mal)
Orden recomendado de bisección (apaga en `Modules` o desde el menú, reinicia el nivel):
1. `OneHandIK` → 2. `SideDodge` → 3. `Feints` + `Combos` → 4. `DefenseReading` → 5. `Variety` → 6. `WeaponRules` → 7. `ProfileTuning` → 8. `Enabled=false` (debe comportarse como el juego de serie).

## 5. Rendimiento
Con 8–10 NPC en combate, comprueba FPS con y sin el mod (`Enabled=false`). Coste esperado: despreciable (sin asignaciones en el bucle por frame; trabajo por ataque, no por frame). Si observas picos, reporta con `Logging.Level=Warn`.

## 6. Compatibilidad
Prueba con los mods que uses (animaciones de ataque/parada, otros de IA). Anota orden de carga y si el comportamiento cambia con el mod activado/desactivado. Ver [COMPATIBILITY.md](COMPATIBILITY.md).

---

## Plantilla de reporte (copia y rellena)

```
Versión del mod: 0.1.0     Versión del juego:
Mods instalados (orden de carga):
Escenario(s) probado(s) (A–J / F1–F9):
Resultado:  OK / PARCIAL / FALLA
Qué esperaba:
Qué pasó:
Se reproduce siempre?  sí / no / a veces
Módulo(s) que lo arreglan al apagarlos:
Líneas relevantes de Player.log (con Level=Debug):
  <pega aquí 20-50 líneas que contengan [CompetentAI]>
Captura/vídeo (opcional):
Contenido de calibration.json (si el tema son direcciones):
```
