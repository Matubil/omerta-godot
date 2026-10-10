# Omertà: Rise of the Family

Reconstrucción en Godot 4.7.2 .NET del vertical slice original de Unity.

- Nombre técnico y namespace: `Omerta`
- Lenguaje principal: C#
- Plataforma inicial: Windows
- Estado actual: Etapas 0, 1, 2 y 3 del plan de migración

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
| `T` | Reiniciar encuentro |

## Slice actual

Taller El Faro ya puede conquistarse mediante presión directa o después de derrotar al
defensor y exigir la rendición. La mercadería agrega presión ambiental, el punto
operativo puede sabotearse sin transferir propiedad y el taller genera $50 cada cinco
segundos únicamente cuando pertenece a la familia y está operativo.

La dirección y el alcance de cada etapa están documentados en
[`Documentation/GodotMigrationPlan.md`](Documentation/GodotMigrationPlan.md).

Los detalles de la implementación actual están en
[`Documentation/Stage03Implementation.md`](Documentation/Stage03Implementation.md).
