# Incremento posterior a la Etapa 6 — navegación compartida

## Objetivo

Resolver el principal riesgo técnico detectado al cerrar el plan de migración: que los
actores no atraviesen edificios ni recorran la ciudad en línea recta. Este incremento
mantiene a Godot como implementación principal y al proyecto Unity como referencia de
sólo lectura.

## Arquitectura

`SharedNavigationWorld` crea una única malla de navegación con dos capas lógicas:

- capa 1: peatones;
- capa 2: vehículos.

Las superficies del bloque están segmentadas en polígonos que comparten vértices y
bordes completos. Esto permite rutas continuas en intersecciones y evita que el
patrullero recorte en diagonal desde la calle norte. El interior separado de El Faro
conserva una superficie peatonal propia, sin fingir una conexión exterior inexistente.

`NavigationMotion` concentra el cálculo del siguiente tramo mediante
`NavigationAgent3D`. El movimiento directo sólo se usa durante los primeros frames, si
el mapa de navegación todavía no fue sincronizado por `NavigationServer3D`.

## Actores migrados

- compañero inicial y aliados de refuerzo;
- rival del encuentro;
- policía a pie;
- atacante de la represalia;
- vehículo de refuerzos;
- patrullero.

El protagonista conserva movimiento directo relativo a cámara: su control no debe
sentirse condicionado por la navegación de la IA.

## Validación reproducible

```powershell
Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -- --navigation-smoke
```

El smoke espera la sincronización del mapa y comprueba:

1. ruta del compañero hasta El Faro;
2. ruta del atacante desde el acceso oeste;
3. ruta vehicular de refuerzos;
4. giro obligatorio del patrullero por la intersección;
5. carga sintética gradual de 8, 16, 32 y 64 consultas alternadas entre peatones y
   vehículos.

La medición exacta se informa en cada ejecución. Es una línea base headless para
detectar regresiones de cálculo, no un benchmark gráfico ni una simulación completa de
multitudes.

## Límites conocidos y próximo paso

- La malla está construida para el bloque graybox actual; la ciudad futura deberá
  generarla a partir de su geometría definitiva.
- El evitamiento local entre agentes todavía no está activado.
- La carga gradual mide consultas de ruta, no percepción, combate y renderizado de una
  multitud real.
- Los aliados esperan fuera cuando el protagonista entra a un interior desconectado.

El siguiente incremento recomendado es habilitar evitamiento local y crear un escenario
de estrés reproducible con 20 NPC activos. Después de estabilizarlo conviene sumar el
segundo negocio y ampliar la ciudad, sin multiplicar contenido sobre una navegación no
probada.
