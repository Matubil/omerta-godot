# Omertà: Rise of the Family

Reconstrucción en Godot 4.7.2 .NET del vertical slice original de Unity.

- Nombre técnico y namespace: `Omerta`
- Lenguaje principal: C#
- Plataforma inicial: Windows
- Estado actual: Etapas 0 a 6 + navegación, traversal, territorio y tres misiones

## Ejecutar

Abrir `project.godot` con la edición .NET de Godot 4.7.2 y ejecutar el proyecto.

## Controles actuales

| Control | Acción |
| --- | --- |
| `WASD` | Movimiento relativo a cámara / conducción arcade |
| `Shift` | Sprint |
| `Ctrl` o `C` | Alternar crouch |
| `Espacio` | Vault contextual sobre obstáculos bajos |
| `Alt` | Entrar o salir de cobertura |
| `E` | Interactuar / entrar o salir de un vehículo |
| Botón derecho | ADS |
| Botón izquierdo | Disparar |
| `R` | Recargar |
| `1` | Equipar/desenfundar pistola |
| `2` | Equipar/desenfundar arma larga si está encima |
| `H` | Enfundar o desenfundar el arma seleccionada |
| `Q` | Orden contextual a la crew: mover o atacar |
| `G` | Reagrupar la crew con el protagonista |
| `F5` | Guardar partida |
| `F9` | Cargar partida |
| `F10` | Mostrar diagnóstico de playtest |
| `T` | Reiniciar encuentro |

## Slice actual

Taller El Faro ya puede conquistarse mediante presión directa o después de derrotar al
defensor y exigir la rendición. La mercadería agrega presión ambiental, el punto
operativo puede sabotearse sin transferir propiedad y el taller genera $50 cada cinco
segundos únicamente cuando pertenece a la familia y está operativo.

La primera misión se acepta con Don Vittorio y guía la conquista de El Faro. Un
compañero sigue al protagonista y acepta órdenes puntuales; el teléfono público llama
dos aliados que llegan físicamente en vehículo. Los disparos próximos a un testigo
generan un patrullero, búsqueda por última posición conocida y la opción contextual de
sobornar al agente. Una conquista operativa también programa una represalia rival que
ahora funciona como misión defensiva urgente: hay 12 segundos de aviso, llegan dos
atacantes físicos y deben ser eliminados antes de completar 6 segundos de sabotaje. La
victoria paga $125 una sola vez; el fallo deja el taller inactivo sin transferir su
propiedad, y repararlo permite reintentar la defensa.

Después de conquistar El Faro se habilita un informante con un segundo encargo. Bar La
Niebla tiene exterior e interior propios: la misión exige sabotear su punto operativo,
paga $100 y deja explícitamente la propiedad en manos rivales.

Depósito San Telmo completa los tres negocios de South District. Puede conquistarse
mediante presión directa, produce $65 cada cinco segundos si pertenece a la familia y
está operativo, y comparte las mismas interacciones reutilizables de comerciante,
mercadería y punto operativo. El control del distrito se obtiene con una mayoría de dos
negocios y se deriva en tiempo real: no se duplica como dato persistido.

Compañeros, rivales, agentes, atacantes y vehículos ya calculan rutas sobre una malla
compartida. Peatones y vehículos usan capas separadas para que el patrullero y los
refuerzos respeten el trazado de calles mientras la IA a pie rodea la geometría del
bloque. Los peatones activos también se apartan entre sí mediante evitamiento local;
los actores desactivados salen de la simulación para no consumir procesamiento.

La cámara isométrica acorta su brazo frente a paredes para evitar clipping. Los techos
y las grandes fachadas que quedan sobre la línea de visión se atenúan gradualmente y
recuperan su opacidad cuando dejan de tapar al protagonista.

La dirección de `WASD` respeta ahora el frente visual de la cámara (`W` avanza y `S`
retrocede). Los interiores de los tres negocios ocultan completamente su techo mientras
el protagonista está dentro, tienen límites físicos en sus salidas y requieren `E` para
volver a la calle. Si el jugador cae fuera de la geometría, regresa al último punto
seguro. El rival inicial permanece neutral hasta que comienza la toma de El Faro o el
jugador lo provoca, evitando combate y policía durante los primeros segundos.

El protagonista puede entrar en cobertura alta o baja, desplazarse lateralmente sin
perder el contacto con la superficie y saltar obstáculos bajos cuando existe un
aterrizaje seguro. El vault cancela ADS y bloquea el arma hasta recuperar la colisión.
El graybox incluye tres superficies rotuladas para probar estas reglas.

La cápsula azul del protagonista fue reemplazada por un humanoide provisional articulado
con traje, corbata y fedora. Una capa de animación procedural ya representa idle,
caminar, correr, crouch, ADS, cobertura y vault sin alterar la colisión ni los sistemas
de armas existentes. Sigue siendo una referencia de escala y silueta: el próximo salto
visual será sustituirlo por una malla riggeada con `Skeleton3D` y clips retargeteados.

El loadout del protagonista está limitado a una pistola y una sola arma larga. La
escopeta recortada comienza guardada en el vehículo familiar que llega con los
refuerzos: se retira o devuelve desde su baúl con `E`, conserva su munición y se muestra
en la espalda cuando se transporta sin estar equipada. Los autos comunes no acceden al
arsenal; esa capacidad queda reservada para vehículos propios o de la familia.

Hay un sedán robado conducible en la calle. Puede usarse de inmediato con `E` y `WASD`,
pero su baúl no accede al arsenal. Después de conquistar y reparar El Faro se puede
estacionar dentro de su garaje y pagar $75 para registrarlo: cambia a vehículo familiar,
habilita el baúl compartido y conserva posición y propiedad al guardar/cargar. El auto de
refuerzos utiliza la misma conducción una vez que termina su llegada y baja la crew.
Si el testigo presencia el robo, el sedán queda marcado y el patrullero persigue su
posición dinámica. Abandonarlo no cancela la búsqueda: la policía llega hasta el coche y
continúa a pie. El garaje rechaza autos buscados hasta que se pierda o resuelva la alerta.
Los choques dañan motor y conductor; el patrullero puede golpear el sedán y completar una
intercepción si logra inutilizarlo o mantenerlo detenido. Una alerta baja se resuelve con
una multa de $90 si hay fondos; de lo contrario termina en arresto y recuperación desde
el último guardado. El Faro repara vehículos familiares dañados por $40.

La partida manual se guarda como JSON versionado bajo `user://save_0.json`. Conserva
pose, salud, munición, economía, propiedad y operatividad de los tres negocios, y
progreso de misión. Tras una
derrota se restaura el último guardado o el estado inicial si todavía no existe uno;
crew temporal, búsqueda policial y represalias en curso se reinician al cargar.

La dirección y el alcance de cada etapa están documentados en
[`Documentation/GodotMigrationPlan.md`](Documentation/GodotMigrationPlan.md).

Los detalles de la implementación actual están en
[`Documentation/Stage03Implementation.md`](Documentation/Stage03Implementation.md).

La persistencia está detallada en
[`Documentation/Stage04Implementation.md`](Documentation/Stage04Implementation.md).

El ciclo jugable de misión, crew y policía está detallado en
[`Documentation/Stage05Implementation.md`](Documentation/Stage05Implementation.md).

La evaluación técnica y la recomendación de migración están en
[`Documentation/Stage06MigrationDecision.md`](Documentation/Stage06MigrationDecision.md).

La base de navegación posterior a la Etapa 6 está detallada en
[`Documentation/Stage07NavigationImplementation.md`](Documentation/Stage07NavigationImplementation.md).

El evitamiento y la prueba con 20 NPC están detallados en
[`Documentation/Stage08CrowdAvoidance.md`](Documentation/Stage08CrowdAvoidance.md).

La resolución de oclusión de cámara está detallada en
[`Documentation/Stage09CameraOcclusion.md`](Documentation/Stage09CameraOcclusion.md).

El traversal y la cobertura están detallados en
[`Documentation/Stage10TraversalAndCover.md`](Documentation/Stage10TraversalAndCover.md).

Bar La Niebla y su misión encadenada están detallados en
[`Documentation/Stage11BarLaNiebla.md`](Documentation/Stage11BarLaNiebla.md).

Depósito San Telmo y el control territorial están detallados en
[`Documentation/Stage12TerritoryControl.md`](Documentation/Stage12TerritoryControl.md).

La misión defensiva urgente de Taller El Faro está detallada en
[`Documentation/Stage13DefenseMission.md`](Documentation/Stage13DefenseMission.md).

El loadout limitado y el arsenal del vehículo familiar están detallados en
[`Documentation/Stage14WeaponLoadoutAndVehicleArsenal.md`](Documentation/Stage14WeaponLoadoutAndVehicleArsenal.md).

La conducción, propiedad de autos y el garaje de El Faro están detallados en
[`Documentation/Stage15DriveableVehiclesAndGarage.md`](Documentation/Stage15DriveableVehiclesAndGarage.md).

El delito vehicular y la persecución dinámica están detallados en
[`Documentation/Stage16VehicleCrimeAndPolicePursuit.md`](Documentation/Stage16VehicleCrimeAndPolicePursuit.md).

El daño, las reparaciones y la intercepción policial están detallados en
[`Documentation/Stage17VehicleDamageAndInterception.md`](Documentation/Stage17VehicleDamageAndInterception.md).

El humanoide provisional y su animación procedural están detallados en
[`Documentation/Stage18PrototypeHumanoid.md`](Documentation/Stage18PrototypeHumanoid.md).

La corrección integral de movimiento, interiores y arranque está detallada en
[`Documentation/Stage19PlayabilityCorrections.md`](Documentation/Stage19PlayabilityCorrections.md).
