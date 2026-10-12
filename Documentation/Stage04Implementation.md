# Etapa 4 — Persistencia

## Alcance implementado

- Guardado manual con `F5` y carga manual con `F9`.
- Slot JSON versionado en `user://save_0.json`.
- Escritura atómica mediante archivo temporal y reemplazo del slot final.
- Lectura posterior al guardado para verificar que el archivo generado sea válido.
- Validación completa de versión, IDs, salud, munición, propietario, saldo y pose antes
  de modificar el mundo.
- Restauración del último guardado manual después de la derrota.
- Restauración del snapshot inicial cuando todavía no existe un guardado válido.
- Reinicio de estado transitorio al cargar: ADS, recarga, percepción rival, temporizador de
  ingresos, presión incompleta y mercadería destruida.

## Estado persistente

| Área | Datos guardados |
| --- | --- |
| Protagonista | Posición, rotación y salud |
| Combate | Salud del rival y de los comerciantes de El Faro y San Telmo |
| Arma | Munición del cargador y reserva |
| Economía | Saldo actual |
| Taller El Faro | Propietario y estado del punto operativo |
| Bar La Niebla | Propietario y estado del punto operativo |
| Depósito San Telmo | Propietario y estado del punto operativo |
| Misiones | Fase y recompensa de conquista, sabotaje y defensa de El Faro |

El estado disponible del comerciante se deriva de su salud al cargar. La posibilidad de
exigir una rendición forzada se deriva de la salud del defensor. Así se evita guardar el
mismo hecho dos veces.

## Estado deliberadamente transitorio

- ADS y postura agachada.
- Recarga, cadencia y temporizadores de arma.
- Alerta, sospecha y última posición conocida por el rival.
- Presión parcial de extorsión y mercadería rota.
- Tiempo acumulado hasta el próximo pago.

## Arquitectura

- `Scripts/Domain/Persistence/SaveGameData.cs`: DTOs puros y versión del formato.
- `Scripts/Infrastructure/Persistence/SaveGameSerializer.cs`: JSON con
  `System.Text.Json` y validación de versión.
- `Scripts/Infrastructure/Persistence/LocalSaveGameStorage.cs`: slot local y escritura
  atómica independiente de Godot.
- `Scripts/GodotAdapters/Persistence/PersistenceController.cs`: captura y restauración
  del árbol de escena, entrada y mensajes del HUD.

## Validación

- Compilación de juego y tests sin errores ni advertencias.
- 40 tests NUnit aprobados en el estado actual del proyecto.
- Round trip JSON y rechazo explícito de versiones incompatibles.
- Reemplazo atómico de archivo y limpieza del temporal.
- Prueba integrada headless: guardar, alterar dinero, pose, salud, munición y negocio,
  cargar y comprobar igualdad exacta del estado restaurado.
- Carga normal de la escena principal sin errores.

## Ampliación posterior

El formato actual es versión 5 e incluye Bar La Niebla, Depósito San Telmo y las tres
misiones de campaña. Las partidas de versión 1, 2, 3 y 4 se migran conservando sus datos
existentes; la ausencia de un negocio incorporado después se interpreta como su estado
inicial, sin inventar avance ni recompensas. El controlador de South District se deriva
de los propietarios de sus tres negocios y por eso no se serializa por separado.
