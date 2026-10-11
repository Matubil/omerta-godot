# Omertà: Rise of the Family

Reconstrucción en Godot 4.7.2 .NET del vertical slice original de Unity.

- Nombre técnico y namespace: `Omerta`
- Lenguaje principal: C#
- Plataforma inicial: Windows
- Estado actual: Etapas 0 a 6 + navegación, evitamiento, oclusión, traversal y cobertura

## Ejecutar

Abrir `project.godot` con la edición .NET de Godot 4.7.2 y ejecutar el proyecto.

## Controles actuales

| Control | Acción |
| --- | --- |
| `WASD` | Movimiento relativo a cámara |
| `Shift` | Sprint |
| `Ctrl` o `C` | Alternar crouch |
| `Espacio` | Vault contextual sobre obstáculos bajos |
| `Alt` | Entrar o salir de cobertura |
| `E` | Interactuar |
| Botón derecho | ADS |
| Botón izquierdo | Disparar |
| `R` | Recargar |
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
puede sabotear el taller, pero nunca transferir su propiedad.

Compañeros, rivales, agentes, atacantes y vehículos ya calculan rutas sobre una malla
compartida. Peatones y vehículos usan capas separadas para que el patrullero y los
refuerzos respeten el trazado de calles mientras la IA a pie rodea la geometría del
bloque. Los peatones activos también se apartan entre sí mediante evitamiento local;
los actores desactivados salen de la simulación para no consumir procesamiento.

La cámara isométrica acorta su brazo frente a paredes para evitar clipping. Los techos
y las grandes fachadas que quedan sobre la línea de visión se atenúan gradualmente y
recuperan su opacidad cuando dejan de tapar al protagonista.

El protagonista puede entrar en cobertura alta o baja, desplazarse lateralmente sin
perder el contacto con la superficie y saltar obstáculos bajos cuando existe un
aterrizaje seguro. El vault cancela ADS y bloquea el arma hasta recuperar la colisión.
El graybox incluye tres superficies rotuladas para probar estas reglas.

La partida manual se guarda como JSON versionado bajo `user://save_0.json`. Conserva
pose, salud, munición, economía, propiedad, operatividad y progreso de misión. Tras una
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
