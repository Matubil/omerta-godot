# Incremento posterior a la Etapa 6 — Misión defensiva urgente

## Objetivo

Convertir la represalia técnica de Taller El Faro en la tercera misión de campaña, con
prioridad visual, resolución jugable, recompensa, fallo, reintento y persistencia.

## Flujo jugable

- Conquistar El Faro operativo programa automáticamente una misión urgente.
- El HUD y el marcador priorizan la defensa sobre los encargos normales y muestran un
  aviso de 12 segundos.
- Al terminar el aviso aparecen dos atacantes físicos del Círculo Ferro, cada uno con
  navegación, arma, salud y evitamiento.
- Si ambos son derrotados, El Faro continúa operativo y la economía recibe $125 una
  sola vez.
- Si un atacante mantiene el objetivo durante 6 segundos, o el protagonista cae durante
  el asalto, el punto operativo queda saboteado.
- El fallo nunca transfiere la propiedad. Reparar el taller vuelve a programar el aviso
  y permite reintentar la defensa.

## Arquitectura

- `BusinessDefenseMission` traduce los estados de `BusinessCounterattack` a `Waiting`,
  `Warning`, `Defending`, `Succeeded` y `Failed`, y concede la recompensa exactamente
  una vez.
- `BusinessCounterattack.ForceSabotage` resuelve la derrota del protagonista durante el
  asalto. Reparar después de un fallo vuelve a programar el mismo flujo de dominio.
- `StageFiveController` sigue siendo el dueño de los actores físicos y adapta el estado
  puro al HUD, marcador, economía y árbol de escena, sin crear reglas paralelas.
- La misión urgente tiene prioridad temporal sobre la misión de conquista y el sabotaje
  de La Niebla.

## Persistencia

El formato de guardado sube a versión 5. Se conservan `Succeeded`, `Failed`, `Waiting`
y el indicador de recompensa. Un estado `Warning` o `Defending` se serializa como
`Waiting`: atacantes, posiciones y temporizadores son estado transitorio y se cancelan
de forma segura al cargar. Las versiones 1 a 4 se migran sin inventar progreso.

## Validación reproducible

```powershell
dotnet test Tests\Omerta.Domain.Tests\Omerta.Domain.Tests.csproj --no-build --no-restore
Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -- --defense-mission-smoke
Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -- --persistence-smoke
```

Las 40 pruebas unitarias y el smoke test integrado verifican:

- transición de aviso a defensa;
- creación y limpieza de dos atacantes físicos;
- victoria con recompensa única de $125;
- fallo por sabotaje sin cambio de propietario;
- reparación y reintento;
- restauración de las tres misiones sin duplicar recompensas.

También pasaron las regresiones de composición, territorio, Bar La Niebla, cámara,
navegación, multitud y traversal.

## Próximo incremento recomendado

Ampliar el combate a un **loadout real de pistola y escopeta**: dos slots con cargadores
y reservas independientes, cambio de arma, enfundado, dispersión por perdigones,
trazadores visibles y persistencia del arma equipada. Las tres misiones ya forman un
ciclo jugable suficiente para evaluar esas armas en conquista, sabotaje y defensa antes
de invertir en arte definitivo.
