# Incremento posterior a la Etapa 6 — Bar La Niebla

## Objetivo

Incorporar el segundo negocio del vertical slice y la misión encadenada de sabotaje,
manteniendo separadas propiedad y operatividad. El resultado debe demostrar que
sabotear un recurso rival no entrega automáticamente el negocio al jugador.

## Contenido jugable

- Bar La Niebla tiene exterior propio, interior separado, entrada, salida y punto
  operativo interactuable.
- Al completar la conquista de Taller El Faro se desbloquea un informante.
- El informante inicia el encargo para llegar a La Niebla y sabotear su equipo.
- Al acercarse al exterior, el objetivo cambia al punto operativo interior.
- El sabotaje deja al bar fuera de servicio, conserva al Círculo Ferro como propietario
  y otorga una única recompensa de $100.
- Una derrota durante el encargo lo deja reintentable desde el informante.

El marcador y el HUD priorizan la misión de El Faro mientras siga activa y pasan a la
misión de La Niebla cuando se completa el requisito.

## Arquitectura

- `BusinessSabotageMission` contiene el flujo puro `Locked`, `Available`,
  `ReachProperty`, `SabotageTarget`, `Completed` y `Failed`.
- `SabotageMissionController` adapta posición, derrota, negocio, HUD, marcador y
  persistencia al árbol Godot.
- `BarLaNieblaController` compone `BusinessState` y `BusinessOperationsState` sin
  ofrecer todavía una vía de conquista.
- `IBusinessOperationsHost` permite que la misma interacción de operaciones funcione
  tanto en El Faro como en La Niebla.
- El formato de guardado sube a versión 3. Las versiones 1 y 2 se migran sin inventar
  estado para el bar ni para la misión nueva.

## Validación reproducible

```powershell
Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -- --bar-sabotage-smoke
```

La prueba completa automáticamente la misión requisito y verifica:

- bloqueo inicial de la misión de sabotaje;
- desbloqueo después de conquistar El Faro;
- aceptación y transición al llegar a Bar La Niebla;
- sabotaje efectivo del punto operativo;
- conservación exacta del propietario rival;
- pago único de $100.

La prueba `--persistence-smoke` también altera y restaura ambos negocios y ambas
misiones, comparando el JSON antes y después de la carga.

## Próximo incremento recomendado

Agregar **Depósito San Telmo y control territorial por mayoría de negocios**. El
tercer negocio permitirá reemplazar adaptadores demasiado específicos por una
composición reutilizable y calcular el control de South District como estado derivado:
dos de tres negocios controlados, sin duplicarlo en el archivo de guardado.

Este incremento quedó completado y está documentado en
`Documentation/Stage12TerritoryControl.md`.
