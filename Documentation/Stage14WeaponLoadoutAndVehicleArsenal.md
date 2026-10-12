# Incremento posterior a la Etapa 6 — Loadout limitado y arsenal vehicular

## Decisión de diseño

El protagonista no lleva un inventario infinito. La regla base inspirada en *Red Dead
Redemption 2* es:

- una ranura de arma corta;
- una única ranura de arma larga visible;
- el resto del arsenal permanece en vehículos propios o de la familia;
- robar un auto común no concede acceso mágico al arsenal familiar;
- en un incremento futuro, un vehículo robado podrá pasar por un garaje para registrarse
  como vehículo familiar y recién entonces habilitar su baúl.

El vehículo de refuerzos es el primer vehículo reconocido como familiar. Su baúl permite
sacar o guardar la escopeta sin recrearla y conserva exactamente su cargador y reserva.

## Flujo jugable

- El protagonista comienza con la pistola equipada y la escopeta guardada.
- `1` equipa y desenfunda la pistola.
- `2` equipa y desenfunda la escopeta sólo si ocupa la ranura de arma larga.
- `H` enfunda o desenfunda el arma seleccionada.
- El teléfono público llama al vehículo familiar de refuerzos.
- Al llegar, `E` junto al baúl transfiere la escopeta entre el arsenal vehicular y el
  protagonista.
- Guardar la escopeta cambia automáticamente a la pistola; intentar seleccionarla desde
  el baúl muestra una indicación clara en el HUD.

La escopeta conserva los parámetros del prototipo Unity: 12 de daño por perdigón, ocho
perdigones, alcance 22, cargador de cinco cartuchos, reserva inicial de 20, cadencia de
1,05 segundos y dispersión de 10° desde cadera / 6° en ADS.

## Arquitectura

- `WeaponLoadout` es dominio C# puro y aplica compatibilidad, ocupación, selección y
  enfundado de slots.
- `PlayerWeaponLoadoutController` mantiene los dos runtimes de munición, adapta la
  selección al jugador y actualiza la representación en mano/espalda.
- `HitscanWeaponController` dispara uno o varios rayos por cartucho; un disparo de
  escopeta consume un solo cartucho y genera ocho perdigones.
- `VehicleArsenalInteraction` sólo se encuentra en el vehículo marcado como familiar y
  realiza la transferencia física del arma larga.
- Los NPC siguen usando el controlador hitscan individual, por lo que el cambio no altera
  sus armas actuales.

## Persistencia

El formato sube a versión 6. Para cada arma se guarda cargador, reserva y ubicación
estable (`weapon-slot.sidearm`, `weapon-slot.long-gun` o
`vehicle.family.reinforcement.trunk`). También se guardan el arma equipada y el estado
enfundado. Las versiones 1 a 5 se migran con pistola equipada y escopeta en el vehículo.

## Validación

- La solución completa compila sin errores ni advertencias.
- Las 45 pruebas puras pasan, incluidas compatibilidad de slots, almacenamiento sin perder
  munición, enfundado, parámetros de perdigones y migración de versión 5 a 6.
- Existe un smoke test integrado `--weapon-loadout-smoke` para verificar escena, baúl,
  transferencia, selección, enfundado y consumo de cartuchos.
- El smoke de persistencia ahora altera y restaura también ubicación, arma equipada,
  enfundado y munición de la escopeta.

La ejecución headless de Godot queda pendiente hasta corregir la instalación portable que
muestra el diálogo `.NET assemblies not found`; no se debe iniciar ese ejecutable desde
Codex mientras conserve ese problema.

## Próximo incremento recomendado

Construir el primer **vehículo conducible y sistema de propiedad de autos**: entrar/salir,
conducción arcade isométrica, identificación de vehículo robado frente a familiar y un
garaje de El Faro que permita registrar un auto. Eso hará que la regla del arsenal tenga
consecuencias sistémicas: sólo los autos registrados compartirán acceso a las armas de la
familia.
