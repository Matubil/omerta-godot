# Omertà: Rise of the Family

Reconstrucción en Godot 4.7.2 .NET del vertical slice original de Unity.

- Nombre técnico y namespace: `Omerta`
- Lenguaje principal: C#
- Plataforma inicial: Windows
- Estado actual: Etapas 0, 1, 2, 3 y 4 del plan de migración

## Ejecutar

Abrir `project.godot` con la edición .NET de Godot 4.7.2 y ejecutar el proyecto.

## Controles actuales

| Control | Acción |
| --- | --- |
| `WASD` | Movimiento relativo a cámara |
| `Shift` | Sprint |
| `Ctrl` o `C` | Alternar crouch |
| `E` | Interactuar |
| Botón derecho | ADS |
| Botón izquierdo | Disparar |
| `R` | Recargar |
| `F5` | Guardar partida |
| `F9` | Cargar partida |
| `T` | Reiniciar encuentro |

## Slice actual

Taller El Faro ya puede conquistarse mediante presión directa o después de derrotar al
defensor y exigir la rendición. La mercadería agrega presión ambiental, el punto
operativo puede sabotearse sin transferir propiedad y el taller genera $50 cada cinco
segundos únicamente cuando pertenece a la familia y está operativo.

La partida manual se guarda como JSON versionado bajo `user://save_0.json`. Conserva
pose, salud, munición, economía, propiedad y operatividad. Tras una derrota se restaura
el último guardado o el estado inicial si todavía no existe uno.

La dirección y el alcance de cada etapa están documentados en
[`Documentation/GodotMigrationPlan.md`](Documentation/GodotMigrationPlan.md).

Los detalles de la implementación actual están en
[`Documentation/Stage03Implementation.md`](Documentation/Stage03Implementation.md).

La persistencia está detallada en
[`Documentation/Stage04Implementation.md`](Documentation/Stage04Implementation.md).
