# Incremento posterior a la Etapa 6 — evitamiento local y carga de NPC

## Objetivo

Completar la base de navegación con separación dinámica entre peatones y verificar su
costo con una población mayor que la utilizada por el vertical slice actual.

## Implementación

`NavigationAvoidanceMotor` encapsula el flujo RVO de Godot:

1. el controlador entrega su velocidad deseada al `NavigationAgent3D`;
2. `NavigationServer3D` calcula una velocidad segura;
3. el actor aplica esa velocidad en el siguiente paso de física.

El motor limita el evitamiento a la capa peatonal. Compañeros, rivales, policías y
atacantes comparten esta política. Los actores de reserva o temporalmente inactivos se
desregistran del cálculo y vuelven a registrarse cuando aparecen en el mundo. Tras un
teletransporte se reinicia la velocidad interna del agente para evitar impulsos viejos.

Los vehículos siguen usando rutas sobre su capa independiente. Su evitamiento queda
fuera de este incremento porque el slice sólo presenta un vehículo en movimiento a la
vez.

## Smoke de multitud

El comando reproducible es:

```powershell
Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -- --crowd-smoke
```

La prueba verifica primero la configuración de los actores reales del slice. Después
crea 20 sondas peatonales activas distribuidas en diez carriles enfrentados y las mueve
durante 480 frames de render. Para aprobar exige:

- callbacks de velocidad segura en los 20 agentes;
- correcciones de evitamiento efectivas;
- al menos 100 metros de movimiento acumulado;
- separación mínima de 0,35 metros;
- estabilidad del árbol de escena durante toda la corrida.

Corrida de referencia: 4.020 callbacks, 895 correcciones, 159,3 metros recorridos en
3,34 segundos, separación mínima de 0,70 metros, 263 nodos y 144 FPS headless. Estas
cifras sirven para detectar regresiones en la misma máquina; no representan rendimiento
gráfico final.

## Próximo incremento recomendado

La oclusión de cámara se completó en el incremento siguiente; véase
`Stage09CameraOcclusion.md`. Con navegación, multitudes y lectura visual estabilizadas,
el próximo riesgo jugable es recuperar traversal y cobertura antes de ampliar contenido.
