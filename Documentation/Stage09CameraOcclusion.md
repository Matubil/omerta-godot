# Incremento posterior a la Etapa 6 — oclusión de cámara isométrica

## Objetivo

Mantener al protagonista visible en calles e interiores sin permitir que la cámara
atraviese paredes. La solución separa colisión física de cámara y atenuación puramente
visual para no modificar las reglas de movimiento.

## Capas

- capa 1: colisión normal del mundo y personajes;
- capa 3, valor `4`: paredes que limitan el brazo de cámara;
- capa 4, valor `8`: techos y fachadas que pueden atenuarse.

Las paredes de Taller El Faro pertenecen a las capas 1 y 3. Los edificios laterales
pertenecen a las capas 1 y 4. Los techos sólo usan la capa 4, por lo que son visibles y
consultables por la cámara sin bloquear al protagonista ni los disparos.

## Comportamiento

`IsometricCameraController` calcula primero su distancia de ADS y después consulta la
línea entre el foco y la posición deseada:

1. ante una pared, acorta inmediatamente el brazo con un margen configurable;
2. al despejarse la línea, recupera suavemente la distancia normal;
3. ante un techo o fachada atenuable, interpola la transparencia de sus mallas;
4. al dejar de ocluir, restaura su transparencia original;
5. al salir de la escena, restaura cualquier material visual que siguiera atenuado.

El graybox incorpora techos exterior e interior en El Faro para validar el sistema con
geometría real. Los edificios oeste y este también participan como fachadas atenuables.

## Validación reproducible

```powershell
Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -- --camera-occlusion-smoke
```

La prueba mueve al protagonista al interior, espera la transición visual y comprueba:

- selección y transparencia efectiva del techo;
- detección de las dos fachadas y los dos techos configurados;
- reducción del brazo contra una pared de 10 m a 4,38 m;
- reducción hasta el mínimo seguro de 0,10 m cuando el foco está pegado a una pared;
- detección independiente de techo y edificio atenuables;
- conservación de la distancia completa en una línea de visión despejada.

## Próximo incremento recomendado

Completado en `Stage10TraversalAndCover.md`: vault contextual con `Espacio` y cobertura
alta/baja con `Alt`, manteniendo `Q` para la crew. El siguiente bloque recomendado es
Bar La Niebla y su misión de sabotaje.
