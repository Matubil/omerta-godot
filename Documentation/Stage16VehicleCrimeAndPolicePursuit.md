# Incremento posterior a la Etapa 6 — Robo de vehículos y persecución policial

## Delito vehicular

Entrar a un vehículo que todavía no pertenece a la familia inicia un uso no autorizado.
Si el testigo está vivo y dentro de 22 metros, denuncia el robo y el auto queda marcado
como `BUSCADO`. Si nadie cercano puede denunciarlo, el jugador conserva el auto robado
sin activar una respuesta policial inmediata.

La marca policial es transitoria. Se elimina al escapar, resolver la respuesta policial,
reiniciar el encuentro o cargar una partida. No se guarda dentro del estado persistente
del vehículo.

## Persecución

`PoliceResponseState` distingue ahora incidentes por disparos y por robo de vehículo. El
segundo agrega el estado `PursuingVehicle` al flujo existente:

1. el testigo denuncia y comienza la demora de despacho;
2. el patrullero aparece en la red vial para vehículos;
3. su destino se actualiza con la posición real del auto marcado;
4. el contacto dentro del radio policial reinicia el tiempo necesario para escapar;
5. fuera de detección, ocho segundos sin contacto terminan la búsqueda;
6. si el jugador abandona el auto, el patrullero sigue hasta él y el agente continúa la
   búsqueda a pie desde la última posición conocida.

El patrullero reutiliza la navegación de tránsito ya existente. Todavía no embiste ni
daña vehículos: su función en este incremento es perseguir, mantener contacto y enlazar
con la respuesta policial a pie.

## Garaje y propiedad

El garaje de Taller El Faro rechaza un vehículo marcado. Primero hay que perder o
resolver la búsqueda; recién entonces puede pagarse el registro de $75. La validación
vive también en `DriveableVehicleController`, por lo que no depende solamente del texto
o del radio de interacción del garaje.

## Validación

- Compilación de juego y tests sin errores ni advertencias.
- 52 pruebas puras aprobadas.
- Casos nuevos para denuncia de robo, inicio de persecución, reinicio del temporizador
  por contacto, continuación a pie y escape fuera de detección.
- Revisión estática de estados policiales, suscripciones de vehículos y bloqueo del
  registro durante una búsqueda.
- El smoke `--vehicle-ownership-smoke` cubre denuncia, marca, rechazo del garaje, escape
  y registro posterior; queda preparado para ejecutarse cuando el Godot portable funcione.

La ejecución headless continúa pendiente hasta corregir el Godot portable que muestra
`.NET assemblies not found`; Codex no inicia ese ejecutable para evitar ventanas de
error.

## Próximo incremento recomendado

Agregar **daño e intercepción de vehículos**: salud del motor y carrocería, choques con
consecuencias, capacidad del patrullero para forzar una detención y una resolución clara
por arresto, multa o huida a pie. Es el paso que convierte la persecución actual en un
riesgo jugable completo antes de sumar tráfico civil o más modelos de autos.
