# Fase 1 — Estabilización automatizada del vertical slice

## Objetivo

Cerrar el hueco entre los smokes aislados y el recorrido principal sin agregar mecánicas,
alterar controles ni cambiar el esquema de guardado. La validación manual sigue siendo
necesaria para evaluar sensación, claridad y posibles bloqueos de interacción.

## Smoke encadenado

`--vertical-slice-smoke` ejecuta en una sola instancia de Godot:

1. reinicio controlado del estado del slice;
2. aceptación del encargo de El Faro;
3. llegada al objetivo;
4. disparo presenciado y despacho policial;
5. conquista normal mediante presión;
6. recorrido del patrullero y activación del agente;
7. resolución por soborno y pago único de la recompensa;
8. advertencia y defensa ante la represalia;
9. entrada y salida del sedán robado;
10. restauración exacta de pose, economía, propiedad y progreso de misiones.

El avance policial usa el mismo controlador y la misma navegación del juego. El método
diagnóstico añadido solamente permite adelantar tiempo de forma determinista en headless.

## Ejecución

```powershell
Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -- --vertical-slice-smoke
```

## Validación de regresión

- Compilación completa: 0 errores, 0 advertencias.
- Pruebas de dominio: 58/58 correctas.
- Smokes de integración: 13/13 correctos.
- El smoke de persistencia continúa usando un slot aislado.
- `SaveGameData.CurrentVersion` permanece en 7.

## Pendiente manual

Antes de declarar finalizada la estabilización hay que completar una partida interactiva:
inicio, movimiento, encargo, conquista, policía, interiores, vehículo, represalia,
guardado, cierre real de Godot, nueva ejecución y carga. Los smokes verifican estados e
integraciones, pero no sustituyen la evaluación humana de controles, cámara y legibilidad.
