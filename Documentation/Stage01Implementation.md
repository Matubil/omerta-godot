# Etapas 0 y 1 — Base Godot

## Identidad

- Nombre técnico, assembly y namespace raíz: `Omerta`.
- Nombre comercial visible: **Omertà: Rise of the Family**.
- Motor objetivo: Godot 4.7.2 .NET con C# y .NET 8 para escritorio.

## Arquitectura inicial

La estructura mantiene la frontera propuesta en el plan de migración:

- `Scripts/Domain`: reglas e identidad sin referencias a Godot.
- `Scripts/Application`: contratos de casos de uso, también independientes del motor.
- `Scripts/GodotAdapters`: input, personaje, cámara, interacción y proyección del cursor.
- `Scripts/Presentation`: HUD y feedback visual.
- `Scenes`: escenas reutilizables agrupadas por responsabilidad.
- `Resources`: ubicación reservada para datos de facciones, personajes, negocios y armas.
- `Tests`: ubicación reservada para tests .NET de dominio.

No se trasladaron `MonoBehaviour`, prefabs ni tipos de Unity.

## Slice jugable

La escena principal contiene una manzana graybox, un protagonista `CharacterBody3D`,
una cámara isométrica elevada, cursor proyectado al mundo, Taller El Faro, un interior
separado, un comerciante y dos portales de interacción.

Controles:

- `WASD`: movimiento relativo a cámara.
- `Shift`: sprint (velocidad ×1,55).
- `Ctrl` o `C`: alternar postura agachada (velocidad ×0,55).
- `E`: interactuar con el objetivo más cercano.
- Mouse: posicionar el cursor proyectado sobre el mundo.

Los parámetros iniciales conservan la referencia Unity: movimiento 5 m/s, cámara a
18 m, pitch 58°, yaw 45° y FOV 50°.

## Alcance deliberado

Esta entrega cubre únicamente Etapa 0 y Etapa 1. Salto, vault, cobertura, ADS,
combate, reglas de negocio, economía y persistencia permanecen fuera de este incremento.
