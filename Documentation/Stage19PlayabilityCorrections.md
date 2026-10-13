# Incremento posterior a la Etapa 6 — Correcciones de jugabilidad básica

## Motivo

Una prueba manual breve reveló problemas que los smokes técnicos anteriores no estaban
evaluando con suficiente rigor: `W/S` estaban invertidas respecto de la cámara, el techo
interior continuaba estorbando la visión y era posible ignorar el portal de salida y caer
fuera de la geometría. Además, el rival podía iniciar combate durante la introducción,
antes de aceptar el primer encargo.

Este incremento prioriza que el recorrido básico sea coherente antes de seguir sumando
sistemas o contenido.

## Movimiento relativo a cámara

`Input.GetVector` entrega `W` como eje Y negativo. La conversión del jugador estaba
aplicando ese valor directamente al vector frontal de cámara, por lo que avanzaba hacia
atrás. `PlayerController` invierte ahora ese eje al construir la dirección tridimensional.
La conducción ya realizaba esa conversión y no necesitó cambios.

## Interiores legibles y cerrados

Los interiores de Taller El Faro, Bar La Niebla y Depósito San Telmo incorporan un
`InteriorVisibilityController`. Detecta al protagonista dentro de los límites del local y
oculta completamente el techo; al salir lo restaura. Esto funciona también después de
cargar una partida o teletransportar al jugador, sin depender exclusivamente del portal.

Cada salida posee ahora una pared física invisible. El área de interacción continúa al
alcance, pero el jugador no puede atravesar el borde abierto: debe pulsar `E` para volver
a la calle. `PortalInteraction` utiliza una operación de teleport segura que limpia
velocidad, traversal y cobertura.

## Recuperación de caídas

El protagonista registra periódicamente su última posición apoyada en el suelo y también
actualiza ese punto al usar un portal o restaurar una partida. Si cae por debajo de la
altura de seguridad, vuelve allí con velocidad nula y recibe un mensaje en el HUD. Es una
red de seguridad global adicional a las paredes físicas, no un reemplazo para ellas.

## Inicio sin combate forzado

El rival inicial permanece neutral mientras el encargo de El Faro está disponible o el
jugador todavía se dirige al taller. Se activa cuando la misión pasa a la fase de toma de
control o si el jugador decide atacarlo primero. Su indicador también permanece oculto
mientras está neutral. Esto evita perder salud, generar testigos y disparar la respuesta
policial antes de que el jugador pueda orientarse.

## Validación

Se agregó el smoke `--playability-smoke`, que verifica dentro de Godot:

- que `W` avance en la dirección frontal de la cámara;
- que el rival empiece neutral;
- que los tres interiores tengan límite físico y portal de salida;
- que entrar a El Faro oculte el techo y salir lo restaure;
- que el viaje entrada/salida se complete usando los portales;
- que una caída simulada recupere la última posición segura.

También se ejecutan nuevamente la compilación .NET, las 58 pruebas de dominio y el smoke
general del vertical slice.

## Próximo paso recomendado

Antes de ampliar la ciudad, conviene realizar otra sesión manual de diez a quince minutos
centrada exclusivamente en el primer encargo: inicio, conversación, llegada, combate,
conquista, salida, guardado y carga. El siguiente incremento debe corregir cualquier
fricción encontrada allí y recién después continuar con el personaje riggeado definitivo.
