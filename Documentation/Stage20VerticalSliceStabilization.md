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

La persistencia entre cierres reales se comprueba con dos procesos independientes y un
slot aislado de `save_0.json`:

```powershell
Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -- --persistence-restart-write-smoke
Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -- --persistence-restart-read-smoke
```

La primera ejecución guarda un estado determinista y termina. La segunda inicia el juego
desde cero, carga ese archivo, compara todos los datos restaurados y elimina el slot de prueba.

## Validación de regresión

- Compilación completa: 0 errores, 0 advertencias.
- Pruebas de dominio: 58/58 correctas.
- Smokes de integración existentes: 13/13 correctos.
- Persistencia entre procesos: 2/2 fases correctas (escritura y lectura tras reinicio).
- Los smokes de persistencia usan slots aislados y nunca leen ni sobrescriben `save_0.json`.
- `SaveGameData.CurrentVersion` permanece en 7.

## Pendiente manual

Antes de declarar finalizada la estabilización hay que completar una partida interactiva:
inicio, movimiento, encargo, conquista, policía, interiores, vehículo, represalia,
guardado y carga mediante F5/F9. El cierre real, nueva ejecución y restauración exacta ya
quedan cubiertos automáticamente; los smokes no sustituyen la evaluación humana de controles,
cámara y legibilidad.
