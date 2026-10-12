# Incremento posterior a la Etapa 6 — Depósito San Telmo y territorio

## Objetivo

Completar los tres negocios definidos para South District y derivar el control del
distrito mediante mayoría simple, manteniendo como fuentes de verdad únicamente los
propietarios de cada negocio.

## Contenido jugable

- Depósito San Telmo tiene exterior, interior, encargado, dos grupos de mercadería,
  punto operativo, entrada y salida propios.
- La presión directa se aplica en cuatro exigencias de 25 puntos y permite conquistar
  el depósito sin depender de la derrota de un defensor.
- La mercadería destruida suma presión ambiental, pero no captura automáticamente.
- El encargado derrotado desactiva la producción y la negociación sin cambiar el dueño.
- El punto operativo puede sabotearse o repararse sin transferir la propiedad.
- Un San Telmo propio y operativo genera $65 cada cinco segundos.
- South District cambia de controlador cuando una facción posee al menos dos de los
  tres negocios: Taller El Faro, Bar La Niebla y Depósito San Telmo.

## Arquitectura

- `TerritoryState` es dominio C# puro: valida negocios únicos, escucha cambios de dueño,
  calcula la mayoría y conserva el controlador anterior si no existe mayoría.
- `TerritoryController` compone los tres negocios de la escena y actualiza el HUD.
- `IBusinessMerchantHost`, `IBusinessPressureHost` e `IBusinessOperationsHost` permiten
  reutilizar las mismas interacciones sin acoplarlas a Taller El Faro.
- El formato de guardado sube a versión 4 e incorpora el estado del depósito y de su
  encargado. Las versiones 1, 2 y 3 se migran sin inventar progreso.
- El territorio no se guarda: se reconstruye automáticamente a partir de los dueños de
  los negocios restaurados.

## Validación reproducible

```powershell
dotnet test Tests\Omerta.Domain.Tests\Omerta.Domain.Tests.csproj --no-build --no-restore
Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -- --territory-smoke
Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -- --persistence-smoke
```

La validación cubre 34 pruebas unitarias y confirma en la escena real:

- estado inicial con tres negocios rivales;
- mayoría requerida de dos propiedades;
- conservación del control rival después de conquistar sólo El Faro;
- transferencia del distrito al conquistar también San Telmo;
- independencia del propietario de Bar La Niebla;
- guardado, mutación y restauración exacta del depósito;
- ausencia de estado territorial duplicado en el JSON.

También se ejecutaron las regresiones de composición general, misión de La Niebla,
cámara, navegación, multitud y traversal.

## Próximo incremento recomendado

Convertir la represalia existente contra Taller El Faro en una **misión defensiva
urgente**. El sistema ya crea el atacante y puede sabotear operaciones; el siguiente
paso es darle estados de misión, objetivo reactivo, temporizador legible, derrota y
reintento, recompensa y persistencia. Esto completa la tercera misión prevista en el
plan antes de ampliar armamento y arte.

Este incremento quedó completado y está documentado en
`Documentation/Stage13DefenseMission.md`.
