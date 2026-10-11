# Etapa 6 — evaluación y decisión de migración

## Recomendación

**Usar Godot como implementación principal para el próximo incremento y conservar el
proyecto Unity como referencia histórica de sólo lectura.** No conviene borrar Unity:
sus pruebas y sistemas todavía son una especificación útil para las siguientes
migraciones.

La recomendación técnica queda aprobada. La aprobación definitiva de sensación de
control requiere un playtest manual breve, porque cámara, respuesta del movimiento y
disparo no pueden evaluarse subjetivamente en headless.

## Evidencia automatizada

- Compilación de juego y tests: cero errores y cero advertencias.
- 23 pruebas de dominio y persistencia aprobadas.
- Smoke de persistencia: guardado, mutación y restauración exacta aprobados.
- Smoke de Etapa 6: composición del slice, parámetros de referencia y 120 frames de
  estabilidad aprobados.
- Medición de referencia del smoke: 212 nodos y 104–111 FPS reportados en dos corridas
  headless. Este
  número sirve para detectar regresiones del mismo escenario; no reemplaza un benchmark
  gráfico ni representa el rendimiento final.
- Paridad automática con Unity: movimiento 5 m/s, cámara 18 m, pitch 58°, FOV 50°,
  pistola de 25 de daño y cadencia de 0,35 s.
- El HUD incorpora un marcador espacial con nombre y distancia al objetivo.
- `F10` muestra FPS, nodos, frecuencia física, velocidad del jugador, cámara/FOV y fase
  actual de la misión durante un playtest.

El comando reproducible es:

```powershell
Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -- --stage-six-smoke
```

## Comparación

| Criterio | Estado Godot | Evaluación |
| --- | --- | ---: |
| Movimiento, cámara y disparo | Parámetros equivalentes, ADS y marcador funcionales; falta valoración manual | 4/5 provisional |
| Velocidad de iteración | Seis etapas incrementales, escenas y recursos editables sin generador monolítico | 5/5 |
| Estabilidad C# y herramientas | Build limpio, tests y dos smoke tests headless reproducibles | 5/5 |
| Interiores y ciudad | Exterior/interior de El Faro claros, pero todavía graybox | 4/5 |
| Rendimiento | Slice pequeño estable; faltan navegación y prueba con muchos NPC | 3/5 |
| Tiempo invertido | Historial incremental claro, pero no se registraron horas comparables | 4/5 provisional |

Resultado técnico provisional: **25/30**. El umbral recomendado para continuar es 22/30.

Como indicador de complejidad, la implementación Godot actual contiene 54 archivos C# y
aproximadamente 5.128 líneas en `Scripts`; Unity contiene 144 archivos y unas 17.400
líneas bajo `Assets/_Game`. No es una comparación de alcance equivalente: Unity incluye
sistemas futuros que deliberadamente todavía no se migraron. Godot tiene 23 pruebas
frente a 118 en Unity, por lo que esa cobertura debe crecer junto con cada nuevo sistema.

## Playtest manual de aceptación

Completar una vuelta de entre cinco y diez minutos:

1. Caminar en las cuatro direcciones, correr y agacharse.
2. Apuntar y disparar en movimiento, desde cadera y con ADS.
3. Aceptar la misión y comprobar el marcador hasta El Faro.
4. Conquistar por presión o por rendición tras combate.
5. Dar una orden de movimiento y una orden de ataque a la crew.
6. Llamar refuerzos y observar la llegada del vehículo.
7. Provocar un testigo y resolver la policía por escape o soborno.
8. Defender o dejar prosperar la represalia.
9. Guardar, alterar el mundo y cargar.
10. Activar `F10` y comprobar que no haya caídas perceptibles ni crecimiento continuo de
    nodos durante el recorrido.

Registrar de 1 a 5 estas cuatro sensaciones: movimiento, cámara, disparo y claridad del
objetivo. Si ninguna queda por debajo de 3, la migración total queda aceptada.

## Riesgos pendientes

- Los aliados y enemigos se desplazan en línea directa; pueden trabarse con geometría
  urbana compleja.
- El rendimiento todavía no fue probado con una población representativa de NPC.
- La cámara aún no resuelve oclusión contra edificios altos.
- Cobertura y vault permanecen sólo en Unity.
- La cobertura de pruebas Godot es menor porque sólo migró el vertical slice acordado.

## Próximo incremento recomendado

Después del playtest manual, el siguiente incremento debería ser **navegación compartida
para peatones y vehículos**, usando `NavigationRegion3D`/`NavigationAgent3D` y rutas
explícitas. Es el riesgo técnico más importante antes de sumar otro negocio o ampliar la
ciudad. Debe incluir una prueba de estrés gradual con NPC, sin introducir todavía una
campaña ni arte definitivo.
