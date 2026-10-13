# Incremento posterior a la Etapa 6 — Daño e intercepción vehicular

## Estado del vehículo

Cada vehículo conducible posee ahora una condición de motor/carrocería de 100 puntos.
Los contactos leves no producen daño; un choque que supera 4 m/s en dirección a la
superficie convierte el exceso de velocidad en daño. El impacto reduce bruscamente la
velocidad y una parte del daño alcanza al conductor.

La presentación graybox oscurece la carrocería a medida que empeora su condición y la
etiqueta muestra el porcentaje de motor. Al llegar a cero, el motor queda inutilizado,
el auto se detiene y el protagonista debe abandonarlo.

La condición todavía es transitoria: cargar o reiniciar restaura el vehículo. Esto evita
guardar un auto inutilizado lejos de un taller antes de implementar remolque y asistencia.

## Intercepción policial

Durante una persecución el patrullero intenta cerrar la distancia con el vehículo
marcado. Dentro de 3,5 metros puede aplicar un golpe policial cada 1,2 segundos, con 18
puntos de daño y una pérdida fuerte de velocidad.

La intercepción se completa cuando ocurre una de estas condiciones:

- el auto permanece casi detenido dentro del radio policial durante 2,5 segundos;
- el patrullero inutiliza el motor dentro del radio de intercepción.

Con alerta de nivel 1 y al menos $90 disponibles, la resolución es una multa automática.
El auto conserva sus daños. Con alerta superior o fondos insuficientes, el protagonista
es arrestado y se reutiliza la recuperación por derrota: se carga el último guardado o el
estado inicial. Salir del auto antes de la intercepción mantiene la alternativa existente
de huir a pie y perder la línea de visión.

El golpe policial es una regla de proximidad, todavía no una simulación rígida de choque
entre dos carrocerías. El patrullero visual continúa siendo un actor de navegación.

## Reparación

El garaje de Taller El Faro ahora detecta vehículos familiares dañados y ofrece una
reparación completa por $40. Registrar un auto robado por $75 incluye su reparación para
que ingrese a la familia en estado operativo. Un auto marcado por la policía continúa
bloqueado hasta resolver la búsqueda.

## Validación

- Compilación de juego y tests sin errores ni advertencias.
- 58 pruebas puras aprobadas.
- Casos de contacto leve, choque destructivo, reparación, detención continua, reinicio
  del tiempo de intercepción, multa y arresto.
- El smoke `--vehicle-ownership-smoke` comprueba además daño y reparación al registrar.

La advertencia `.NET assemblies not found` quedó identificada como un efecto del entorno
restringido de automatización al impedir que Godot cree su carpeta de datos de usuario;
no faltaban ensamblados en la distribución portable. La ejecución headless real con
Godot 4.7.2 .NET ya completa el smoke del proyecto correctamente fuera de ese entorno.

## Próximo incremento recomendado

Este incremento fue realizado en la Etapa 18 con un humanoide articulado procedural. El
siguiente paso es reemplazar sus primitivas por una malla riggeada con `Skeleton3D` y
animaciones retargeteadas, manteniendo los mismos estados de gameplay.
