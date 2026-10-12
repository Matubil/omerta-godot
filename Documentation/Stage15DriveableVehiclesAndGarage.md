# Incremento posterior a la Etapa 6 — Vehículos conducibles y garaje familiar

## Regla de propiedad

Un vehículo robado puede conducirse inmediatamente, pero sigue siendo robado: no da
acceso al arsenal y no pertenece a la familia. Para convertirlo hay que:

1. conquistar Taller El Faro;
2. mantener el taller operativo;
3. estacionar el auto dentro del radio del garaje;
4. pagar $75 en el puesto de registro.

El registro cambia la identidad sistémica del auto, no sólo su color: entra al grupo de
vehículos familiares, habilita el arsenal compartido y su estado pasa a ser persistente.

## Conducción

- `E` junto a un auto disponible permite entrar.
- `WASD` acelera, retrocede y gira con un modelo arcade isométrico.
- `E` mientras se conduce permite salir por el lateral.
- Al entrar se enfunda el arma, se oculta el cuerpo del protagonista y se desactivan su
  colisión, combate e interacciones a pie.
- La cámara continúa siguiendo al protagonista, cuya posición se sincroniza con el auto.
- No se permite guardar manualmente mientras se conduce; hay que bajar primero para
  evitar poses ambiguas.

El auto de refuerzos reutiliza el mismo componente, pero permanece bloqueado mientras
está en ruta y se vuelve conducible únicamente después de estacionar y desplegar a los
aliados.

## Arquitectura

- `VehicleOwnership` contiene la regla pura de registro, disponibilidad del garaje,
  cobro único y restauración.
- `DriveableVehicleController` adapta propiedad, entrada/salida, conducción, colisión,
  presentación y estado persistente a Godot.
- `PrototypeCar.tscn` unifica carrocería, colisión, navegación, zona de entrada y baúl.
- `GarageRegistrationInteraction` conecta El Faro, economía y el vehículo robado más
  cercano sin duplicar la regla de dominio.
- `VehicleEntryInteraction` mantiene la entrada dentro del contrato general de
  interacciones del jugador.
- `VehicleArsenalInteraction` ahora consulta propiedad real: cualquier auto familiar
  detenido accede al mismo arsenal; uno robado nunca lo hace.

## Persistencia

El formato sube a versión 7. Para cada vehículo persistente se guardan ID estable,
posición, rotación y pertenencia familiar. Los autos de misión, como el vehículo de
refuerzos, siguen siendo transitorios. Las versiones 1 a 6 se migran sin inventar autos
registrados y la ubicación vieja del baúl se convierte al identificador del arsenal
familiar compartido.

## Validación

- Compilación de juego y tests sin errores ni advertencias.
- 50 pruebas puras aprobadas, incluyendo cobro, fondos insuficientes, registro único,
  restauración de propiedad y migración de guardado 6 → 7.
- Verificación estática de referencias de todas las escenas y recursos modificados.
- Smoke integrado `--vehicle-ownership-smoke` para entrada/salida, restricción del auto
  robado, registro en El Faro, cobro y desbloqueo del baúl.
- El smoke de persistencia altera y restaura también posición y pertenencia del sedán.

La ejecución headless sigue pendiente hasta corregir el Godot portable que muestra
`.NET assemblies not found`; Codex no inicia ese ejecutable para evitar ventanas de error.

## Próximo incremento recomendado

Agregar **delito vehicular y persecución policial**: que robar un auto pueda ser visto y
denunciado, que el nivel de búsqueda siga al vehículo y que el patrullero existente pueda
perseguirlo por las rutas de tránsito. Esto evita que robar autos sea una acción gratuita
y conecta conducción, testigos, policía, sobornos y garaje en un mismo ciclo.
