# Configuración

El fichero es `Mods/CompetentAI/CompetentAI.config.json` (JSON con comentarios `//` y `/* */` permitidos). **Todas las claves son opcionales**: lo que no pongas conserva su valor por defecto. Al arrancar, el mod escribe `CompetentAI.config.effective.json` con **todos los valores realmente en vigor** (útil para comprobar que tu cambio se ha leído) y registra en el log cualquier valor inválido (`config: ignored bad value…`). Un fichero roto nunca impide arrancar: se usan los valores por defecto.

La referencia completa de parámetros (con valores por defecto, rangos reales y descripción) está generada desde el código en **[CONFIG.reference.md](CONFIG.reference.md)**. Los rangos son los que el mod aplica en realidad: un valor fuera de rango se **recorta** en silencio al límite.

## Reglas de combinación

1. **Menú in-game (Mods → Competent AI) AND JSON**: el menú solo puede **apagar** módulos respecto a lo que permite el JSON. Si en el JSON `Feints` es `false`, no hay forma de encenderlo desde el menú.
2. `Log level`: se usa el **más detallado** entre el JSON y el menú.
3. **Diccionarios** (`ByBrainId`, `ClassBias`…): tus entradas se **fusionan** con las de serie; basta añadir las tuyas.
4. **Perfiles**: lo que escribas de un perfil se **superpone** al perfil por defecto del mismo nombre. Para cambiar solo la probabilidad de fintas de los veteranos:
   ```json
   "Profiles": { "Veteran": { "FeintChance": 0.3 } }
   ```
   Para crear uno nuevo basado en otro, usa `"Extends"`:
   ```json
   "Profiles": { "Boss": { "Extends": "Duelist", "FeintChance": 0.45, "MaxParriesBeforeFatigue": 9 } },
   "Assignment": { "ByCreatureId": { "MiCreaturaJefe": "Boss" } }
   ```
5. Los cambios de `Assignment`/`Profiles` se aplican a las criaturas **que aparezcan después** de reiniciar el juego (el fichero se lee una vez al cargar). El menú in-game sí actúa en vivo sobre los módulos.

## Arquetipos y asignación

| Arquetipo | Intención | Brains de serie mapeados |
|---|---|---|
| **Recruit** | Reacciona tarde, esquiva poco, no finta, se deja engañar casi siempre, se agota pronto | `HumanEasy` |
| **Soldier** | Equilibrado; finta poco; lee la mitad de las fintas | `HumanMedium` |
| **Veteran** | Rápido, coloca el arma (IK), finta y encadena, lee la mayoría de las fintas | `HumanHard`, `HumanHardHeavy` |
| **Duelist** | Muy rápido, esquiva de lado, finta mucho, casi no se deja engañar | `HumanVIP` |

Precedencia al elegir arquetipo: **creatura > facción > Brain > dificultad del Brain > `DefaultProfile`**, y después `GlobalLevelShift` (−6…6; p. ej. +1 sube a todos un escalón, tope en los extremos de `Ladder`). Cada criatura recibe además una variación aleatoria **determinista** de ±`Jitter` (8 % por defecto) en sus multiplicadores para que un pelotón no sean clones.

Los multiplicadores se aplican **sobre el valor que ya tenga la criatura** (el de su Brain, posiblemente modificado por otro mod), no sobre un valor absoluto.

## Cosas útiles de configurar

| Quiero… | Cambia |
|---|---|
| Probar el mod "de serie" sin nada | `Modules.Enabled=false` (o la opción *Enabled* del menú) |
| Aislar un fallo | Apaga módulos de uno en uno (ver [TESTING.md](TESTING.md)) |
| Más/menos dificultad global | `Assignment.GlobalLevelShift` o la opción *Difficulty shift* del menú |
| Fintas más/menos frecuentes | `Profiles.<X>.FeintChance`, `DoubleFeintChance`, `ReactiveFeintBoost` |
| NPCs más fáciles de engañar | Baja `Profiles.<X>.FeintReadSkill`, sube `FooledDuration` |
| Hacerlos menos imbatibles | Baja `MaxParriesBeforeFatigue` o sube `FatigueDuration` |
| Decirle al mod qué hace un arma modded | `Rules.TraitOverrides`: `{"MiKatana": "Edge,Point"}`; y `Rules.ClassOverrides`: `{"MiKatana": "Sword"}` |
| Integrar un pack de animaciones de ataque | `Rules.DirectionOverrides`: `{"<id o address del ataque>": "Down"}` (se mide solo tras unas cuantas repeticiones) |
| Menos ruido en el log | `Logging.Level=Warn` |

## Rangos recomendados (los de uso normal)

- Multiplicadores de velocidad/tiempo (`*Mul`): 0,7 – 1,5. Más allá se ve poco natural (animaciones aceleradas o NPCs "telepáticos").
- Probabilidades (`FeintChance`, `ComboChance`, `CounterAfterDodgeChance`): ≤ 0,5 para que las acciones no se vuelvan constantes.
- `FeintReadSkill`: 0,4 – 0,9. Con 0,98 casi nunca se dejan engañar.
- `ReversalAngle`: 60 – 100°. Valores bajos hacen que giros normales de tu espada cuenten como fintas.

## Ficheros que genera el mod
- `CompetentAI.config.effective.json`: configuración efectiva (se sobrescribe en cada arranque; **no la edites**, edita `CompetentAI.config.json`).
- `calibration.json`: direcciones medidas de cada animación de ataque (se puede borrar sin consecuencias; se vuelve a aprender).
