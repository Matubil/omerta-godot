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

La ejecución headless sigue pendiente hasta corregir el Godot portable que muestra
`.NET assemblies not found`; no se inicia ese ejecutable para evitar ventanas de error.

## Próximo incremento recomendado

Reemplazar el protagonista graybox por un **personaje humano provisional riggeado** y una
máquina de animación para idle, caminar, correr, agacharse, apuntar, cobertura, vault y
entrada/salida de vehículos. Conviene hacerlo antes de agregar más contenido porque hará
visibles los problemas de escala, manos, armas y transiciones que la cápsula actual oculta.
