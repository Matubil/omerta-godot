# Etapa 5 — misión, crew y policía

La Etapa 5 completa el primer ciclo jugable comparable al vertical slice de Unity sin
ampliar todavía el alcance hacia conducción, campaña o policía avanzada.

## Misión de Taller El Faro

- Don Vittorio, ubicado cerca del punto inicial, entrega el encargo persistente
  `mission.el-faro-takeover`.
- La misión pasa por llegada al taller, toma de control y, cuando corresponde,
  resolución de una búsqueda policial.
- La conquista puede seguir usando presión o rendición tras combate. La recompensa de
  `$150` se entrega una sola vez.
- Una derrota falla el encargo activo y permite reintentarlo desde el contacto.

## Crew y refuerzos

- El compañero inicial sigue al protagonista y no ataca de forma autónoma permanente.
- `Q` ordena moverse al punto del cursor o atacar al combatiente hostil señalado.
- `G` cancela la orden y reagrupa a los compañeros.
- El teléfono público llama un vehículo desde fuera del área jugable. Al estacionar,
  baja dos aliados temporales y los incorpora a la crew.

## Testigo y respuesta policial

- Un disparo del jugador dentro del radio audible de un testigo consciente inicia el
  incidente.
- Tras tres segundos aparece un patrullero que recorre físicamente su ruta y despliega
  un agente.
- El agente se dirige a la última posición conocida, actualiza esa posición con línea
  de visión y abre fuego dentro de alcance.
- Ocho segundos sin contacto resuelven la búsqueda por escape.
- Durante una búsqueda de nivel uno y un solo testigo, `E` junto al agente permite un
  soborno de `$60`; derrotarlo también resuelve la respuesta, con una consecuencia
  explícita en el mensaje de estado.

## Represalia

- Conquistar El Faro mientras está operativo inicia un aviso de doce segundos.
- Después aparece físicamente un atacante del Círculo Ferro y avanza hacia el taller.
- Si permanece seis segundos en el objetivo, sabotea el punto operativo y corta los
  ingresos. La propiedad continúa siendo de la familia.
- Derrotarlo antes completa la defensa sin dañar las operaciones.

## Persistencia

El formato JSON sube a versión 2 e incorpora el estado y la recompensa de la misión.
Los saves de versión 1 se migran automáticamente. Búsquedas, aliados llamados,
vehículos y ataques en curso continúan siendo transitorios y se limpian al cargar.
Si se guarda una misión esperando una búsqueda policial, la carga limpia esa búsqueda
y completa esa fase, otorgando entonces la recompensa pendiente una sola vez.

## Verificación

- Compilación C# sin advertencias ni errores.
- 23 pruebas automatizadas de dominio y persistencia.
- Importación y arranque de la escena principal en Godot 4.7.2 .NET headless.
- Smoke test de guardado, mutación y restauración exacta con el esquema versión 2.
