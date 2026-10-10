# Plan de migración a Godot

## Decisión recomendada

Sí, **Omertà: Rise of the Family** puede trasladarse a Godot. No existe una conversión automática confiable desde Unity: la migración debe tratarse como una reconstrucción controlada del vertical slice.

La recomendación es mantener este proyecto de Unity intacto y funcional como referencia, crear un proyecto Godot separado y alcanzar paridad por incrementos. No conviene borrar ni reemplazar el repositorio actual hasta que la versión Godot pueda completar un slice mínimo de movimiento, combate, propiedad y guardado.

Se utilizará:

- nombre comercial: **Omertà: Rise of the Family**;
- identificador técnico y namespace raíz: `Omerta`;
- Godot con soporte .NET;
- C# para conservar las reglas puras ya desarrolladas y reducir la reescritura innecesaria;
- GDScript sólo si una herramienta puntual del editor lo justifica, no como segundo lenguaje principal.

## Qué descargar en Windows

Estado verificado el 9 de octubre de 2026:

1. Descargar **Godot Engine – .NET 4.7.2, Windows x86_64** desde <https://godotengine.org/download/windows/>.
2. Extraer el archivo ZIP en una carpeta estable, por ejemplo `C:\Tools\Godot\4.7.2-dotnet`. Godot es portable: no requiere instalador.
3. Ejecutar el archivo de Godot `.NET` de 64 bits.

No descargar para este proyecto:

- la edición `Standard`, porque no incluye C#;
- la versión de Steam o Epic Games Store, porque esas distribuciones no incluyen soporte .NET/C#;
- Godot 3;
- export templates por ahora. Se instalarán recién cuando haya una build para exportar.

La computadora ya tiene **.NET SDK 10.0.102 x64** y los runtimes .NET 8 y 10. No es necesario instalar otro SDK para comenzar. Si Godot pidiera una actualización, debe instalarse el SDK estable x64 desde <https://dotnet.microsoft.com/download> y no solamente el runtime.

Un editor externo es opcional. Codex puede trabajar directamente sobre los archivos del proyecto. Si se desea editar C# manualmente, Visual Studio Code con la extensión C# o Visual Studio con la carga `.NET desktop development` son opciones válidas.

## Qué está construido actualmente

El proyecto Unity actual no es sólo una escena de movimiento. Es un vertical slice integrado y sirve como especificación ejecutable de la migración.

### Base técnica

- Proyecto en Unity `6000.6.4f1`.
- Assemblies separados: `Omerta.Core`, `Omerta.Gameplay`, `Omerta.Infrastructure`, `Omerta.Presentation`, `Omerta.Bootstrap` y tests.
- IDs estables para facciones, personajes, rangos, negocios, armas y slots.
- Reglas de dominio C# separadas en buena medida de `MonoBehaviour`.
- Composition root explícito, sin contenedor de dependencias ni event bus global.
- Tests EditMode para dominio/infraestructura y PlayMode para integración.
- Escena reproducible generada por herramientas del editor.

### Protagonista, cámara y traversal

- Movimiento directo relativo a cámara con `WASD`.
- Cámara isométrica elevada, seguimiento y distancia configurable.
- Sprint, crouch, salto y vault geométrico sobre obstáculos bajos.
- Cobertura alta/baja con `Q`, alineación a superficie y movimiento lateral.
- ADS, transición de cámara y apuntado al mundo mediante cursor.
- Ajustes provisionales de distancia de cámara y sensibilidad.
- Entrada a un primer interior separado de Taller El Faro conservando el mismo estado de negocio.

### Combate

- Salud, daño y estado de personaje derrotado.
- Arma hitscan compartida por jugador, aliados y enemigos.
- Pistola y escopeta con slots, cargadores y reservas independientes.
- Cambio de arma, enfundado, recarga, cadencia, precisión y pellets.
- Disparo desde cadera y ADS.
- Trazadores visibles desde el arma hacia el impacto.
- Modelos low-poly procedurales propios con arma en mano.
- Primera animación procedural de idle, caminar, correr, ADS, retroceso y caída.

### Negocios, economía y territorio

- Tres negocios vinculados a South District: Taller El Faro, Bar La Niebla y Depósito San Telmo.
- Propiedad independiente del estado operativo.
- Ingresos periódicos sólo para negocios propios y operativos.
- Extorsión mediante presión y demanda final al comerciante.
- Comerciantes con temperamentos cauteloso, orgulloso y estoico.
- Mercadería destructible que agrega presión sin capturar automáticamente.
- Punto operativo saboteable/reparable que deshabilita el bonus o ingreso sin transferir propiedad.
- Comerciante derrotado que deja el negocio fuera de servicio sin conquistarlo.
- Recuperación médica pagada de un comerciante propio.
- Control territorial derivado por mayoría de negocios, no duplicado en el guardado.
- Represalia rival física después de una conquista; puede sabotear operaciones pero no transferir propiedad automáticamente.

### Misiones

- Misión de conquista de Taller El Faro con sabotaje opcional, presión o combate, resolución policial y recompensa.
- Misión encadenada para sabotear Bar La Niebla sin conquistarlo.
- Misión defensiva urgente contra la represalia de Taller El Faro.
- Marcador de objetivo en pantalla y estados persistentes/reintentables.

### Crew y navegación

- Compañero que sigue, se reagrupa y acepta órdenes contextuales.
- Orden de movimiento, ataque explícito, sabotaje/reparación y vigilancia de zona.
- Ataque con alcance y línea de tiro sin agresión autónoma permanente.
- Teléfono público que llama dos aliados.
- Los aliados llegan en un vehículo desde fuera de cámara y bajan al estacionar; no aparecen de la nada.
- Grafo A* compartido con conexiones diferenciadas para peatones y vehículos.

### Policía e instituciones

- Disparo como incidente audible, filtrado por testigos conscientes y cercanos.
- Patrullero que llega físicamente y despliega dos agentes.
- Búsqueda por última posición conocida, actualización por línea de visión y escape por pérdida de contacto.
- Soborno contextual de una búsqueda activa.
- Acuerdo temporal de “vista gorda” limitado a una zona.
- Dirección futura documentada para corrupción persistente, policía avanzada, Iglesia e influencia comunitaria.

### Persistencia

- Save local versionado en JSON con migración desde la versión anterior.
- `F5` guarda y `F9` carga un slot manual.
- Conserva economía, propiedad y operatividad de negocios, protagonistas, comerciantes, defensores, armas y misiones.
- La derrota restaura el último guardado manual o el snapshot inicial si nunca se guardó.
- Los aliados temporales, búsquedas policiales, ADS, recargas y ataques en curso se tratan como estado transitorio.

### Presentación

- HUD compacto: salud/dinero, arma/munición y territorio como información persistente.
- Presión, alarma, cobertura, represalia, negocio e interacción como información contextual.
- Menú provisional de ajustes.
- Geometría graybox y assets procedurales; todavía no hay arte, rigs ni animaciones definitivos.

## Visión del juego que debe conservarse

**Omertà: Rise of the Family** será un juego de acción criminal en tiempo real, no un RTS ni una copia de *Desperados*. La cámara será isométrica/elevada para facilitar lectura, desarrollo y mando de compañeros, pero el jugador controlará directamente a un protagonista.

Pilares:

- movimiento directo con `WASD`, interacción personal y combate en tiempo real;
- apuntado con cursor, ADS, armas, crouch, vault y cobertura;
- crew reducida que acompaña al protagonista y recibe órdenes puntuales;
- aliados y enemigos que llegan mediante vehículos, puertas o accesos comprensibles;
- ciudad transitable ficticia inspirada inicialmente en Buenos Aires y Argentina;
- negocios e interiores propios para extorsiones, sabotajes, asedios y defensas;
- conquista por presión, chantaje, negociación o combate, no únicamente por matar personas;
- sabotear recursos o derrotar comerciantes corta beneficios, pero no entrega por sí solo la propiedad;
- familias rivales capaces de atacar los negocios del jugador bajo las mismas reglas generales;
- policía, testigos, corrupción, sobornos y acuerdos de tolerancia;
- Iglesia y otras instituciones como futura fuente de influencia, legitimidad y relaciones, no como decoración aislada;
- personalización posterior de vestimenta, colores, emblema familiar, miembros y vehículos;
- expansión eventual a más barrios, ciudades argentinas y, sólo si aporta a la campaña, otros países.

## Qué se puede reutilizar

La migración debe empezar por clasificar el código, no por copiar carpetas completas.

### Reutilizable con cambios pequeños

- IDs estables y validaciones.
- Estado de facciones, personajes y negocios.
- Economía e ingresos.
- Salud y reglas de daño.
- Sesiones de extorsión, presión y toma forzada.
- Operatividad, recuperación, territorio y reglas de represalia.
- DTOs de guardado y parte de la serialización JSON.
- Reglas de armas/loadout que no dependan de tipos Unity.
- Buena parte de los tests puros, trasladados a un proyecto NUnit independiente o a tests .NET.

Antes de copiar una clase se deben eliminar dependencias accidentales de `UnityEngine`, como `Vector3`, `Mathf`, `Time` o `ScriptableObject`. Para tipos matemáticos se puede usar un tipo propio en el dominio o convertir en los adapters de cada motor.

### Debe reconstruirse en Godot

- escenas Unity, prefabs y archivos `.unity`/`.prefab`;
- `MonoBehaviour`, ciclo de vida y composition root de escena;
- Input System y bindings;
- `CharacterController`, física, raycasts y colliders;
- cámara, proyección de cursor y transiciones visuales;
- navegación, vehículos y movimiento de NPC;
- Canvas/Text, HUD y marcadores;
- generador de escena y herramientas Editor de Unity;
- `ScriptableObject` de configuración;
- tests PlayMode dependientes de escenas Unity.

### Equivalencias iniciales

| Unity | Godot |
| --- | --- |
| `GameObject` / `Transform` | `Node` / `Node3D` |
| `MonoBehaviour` | script que hereda de `Node`, `Node3D`, `Control`, etc. |
| Prefab | `PackedScene` (`.tscn`) |
| `ScriptableObject` | `Resource` (`.tres`) o datos C# puros |
| `CharacterController` | `CharacterBody3D` |
| `Camera` | `Camera3D` |
| Input Actions | `InputMap` y acciones nombradas |
| `Physics.Raycast` | consultas de `PhysicsDirectSpaceState3D` |
| Canvas / Text / Image | `Control`, `Label`, `Panel`, `TextureRect` |
| `Application.persistentDataPath` | `user://` |
| SceneManager | `SceneTree` y `PackedScene` |

## Estructura propuesta para Godot

Crear un repositorio separado, por ejemplo `omerta-godot`, con una solución organizada por responsabilidad:

```text
OmertaGodot/
  project.godot
  OmertaGodot.csproj
  Scenes/
    Bootstrap/
    World/
    Characters/
    Businesses/
    UI/
  Scripts/
    Domain/
    Application/
    GodotAdapters/
    Presentation/
  Resources/
    Factions/
    Characters/
    Businesses/
    Weapons/
  Tests/
    Omerta.Domain.Tests/
  Documentation/
```

`Domain` no debe referenciar Godot. `GodotAdapters` resuelve input, nodos, físicas, cámaras, tiempo y archivos. Esta frontera conserva la mejor decisión arquitectónica del prototipo Unity sin copiar sus detalles de motor.

## Plan por etapas

### Etapa 0 — Preparación

- Instalar/descomprimir Godot .NET.
- Crear el repositorio paralelo.
- Configurar `.gitignore`, proyecto C# y escena inicial.
- Copiar este documento y las direcciones de diseño relevantes.
- Mantener Unity como referencia ejecutable.

Resultado: Godot abre, compila C# y ejecuta una escena vacía versionada.

### Etapa 1 — Slice de control

- Un bloque de ciudad graybox.
- Protagonista `CharacterBody3D`.
- Cámara isométrica, cursor al mundo y sensibilidad.
- Movimiento, sprint, crouch e interacción `E`.
- Un comerciante y una puerta/interior simple.

Resultado: recorrer el bloque, entrar al local e interactuar sin errores.

### Etapa 2 — Combate

- Salud y daño puro reutilizados.
- Pistola hitscan, munición, recarga y trazador.
- ADS y disparo desde cadera.
- Un rival que detecta, se mueve y dispara.
- Recién después: escopeta, cover y vault.

Resultado: encuentro combatible y reiniciable.

### Etapa 3 — Negocio y economía

- Un único negocio: Taller El Faro.
- Propiedad, punto operativo, comerciante y mercadería.
- Presión ambiental y demanda final.
- Sabotaje independiente de conquista.
- Ingreso periódico y HUD contextual.

Resultado: capturar por presión o combate, deshabilitar sin capturar y cobrar sólo si corresponde.

### Etapa 4 — Persistencia

- Guardado JSON versionado bajo `user://`.
- Pose, salud, munición, economía, propiedad y operatividad.
- Restauración tras derrota.
- Tests de ida y vuelta para el estado puro.

Resultado: guardar, alterar el mundo, cargar y recuperar exactamente el estado persistente definido.

### Etapa 5 — Misión, crew y policía

- Primera misión completa del contacto.
- Un compañero con seguir, mover y atacar.
- Llamada de refuerzos y llegada física.
- Testigo, patrullero, búsqueda y soborno.
- Represalia básica al negocio conquistado.

Resultado: ciclo jugable completo comparable al vertical slice Unity.

### Etapa 6 — Decisión de migración total

Comparar ambas versiones por:

- sensación del movimiento, cámara y disparo;
- velocidad de iteración;
- estabilidad de C# y herramientas;
- facilidad para construir interiores y ciudad;
- rendimiento con NPC, navegación y físicas;
- tiempo real invertido en cada incremento.

Sólo entonces se decide abandonar Unity, continuar en Godot o mantener el prototipo Unity como respaldo histórico.

## Primer alcance que no debe crecer

El primer slice Godot debe contener únicamente:

- una calle o manzana;
- protagonista y cámara;
- un aliado como máximo;
- un rival y un comerciante;
- Taller El Faro con exterior/interior mínimo;
- una pistola;
- presión, sabotaje, conquista e ingreso;
- una misión breve;
- save/load.

No deben entrar inicialmente conducción, tres negocios, policía completa, Iglesia, personalización, diplomacia, múltiples ciudades, campaña ni arte definitivo. Esos sistemas ya tienen dirección documentada, pero agregarlos antes de validar la base repetiría todo el costo de integración de una vez.

## Riesgos y límites

- Las escenas, materiales, físicas, navegación y UI no se convierten de manera automática.
- Los modelos procedurales creados en código Unity deben recrearse como escenas Godot o reemplazarse por modelos importados.
- Los assets externos futuros deben revisarse por licencia y compatibilidad con Godot antes de migrarlos.
- El comportamiento físico nunca será idéntico entre motores; se busca equivalencia jugable, no números idénticos.
- C# en Godot 4 funciona en escritorio, pero actualmente no permite exportar a Web; Android/iOS tienen soporte con limitaciones. Esto no afecta el objetivo inicial de Windows.
- Mantener dos implementaciones completas durante mucho tiempo duplicaría trabajo. La comparación debe tener un límite claro: terminar la etapa 4 o 5 y decidir.

## Próximo paso concreto

Después de descargar Godot .NET, abrirlo una vez y confirmar que inicia. A continuación se creará el repositorio `omerta-godot`, la solución C#, la escena bootstrap y una prueba mínima de movimiento/cámara. No se copiará todavía todo el código ni se modificará este proyecto Unity.

