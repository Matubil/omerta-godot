# Etapa 3 — Negocio y economía

## Alcance implementado

- Taller El Faro como único negocio del slice.
- Propiedad independiente del estado operativo.
- Rafael como comerciante con salud, derrota y bloqueo de producción.
- Punto operativo saboteable cuando pertenece al rival y reparable después de la conquista.
- Dos lotes de mercadería destructible con presión ambiental.
- Extorsión directa con reacciones contextuales según la presión acumulada.
- Conquista pacífica al completar la resistencia mediante una exigencia directa.
- Conquista por combate sólo después de derrotar al defensor y exigir la rendición a Rafael.
- Consecuencia operativa por violencia ambiental excesiva.
- Ingreso periódico sólo cuando el taller pertenece al jugador y está operativo.
- Fondos, propietario, operatividad y presión visibles en el HUD.
- Reinicio con `T` que restaura combate, negocio, mercadería y economía inicial.

## Reglas conservadas desde Unity

| Parámetro | Valor |
| --- | ---: |
| Fondos iniciales | $100 |
| Resistencia de Taller El Faro | 100 |
| Presión por interacción directa | 25 |
| Presión por lote destruido | 15 |
| Durabilidad por lote | 25 |
| Salud del comerciante | 50 |
| Ingreso base | $50 |
| Intervalo de ingreso | 5 s |

La presión ambiental se limita a `resistencia - 1`: destruir objetos nunca captura el
negocio automáticamente. Sabotear el punto operativo o derrotar al comerciante detiene
la producción, pero tampoco transfiere la propiedad.

## Arquitectura

- `Scripts/Domain/Businesses`: propiedad, operaciones, extorsión, toma forzada e ingresos.
- `Scripts/Domain/Economy`: saldo y transacciones.
- `Scripts/GodotAdapters/Businesses`: composición de Taller El Faro y adaptación de nodos.
- `Scripts/GodotAdapters/Economy`: enlace del saldo puro con Godot y el HUD.
- `Resources/Businesses/WorkshopElFaro.tres`: parámetros editables del negocio.

El dominio no referencia Godot. Las áreas de interacción, colisiones, temporización y
presentación permanecen en los adapters.

## Validación

- Compilación C# sin errores ni advertencias.
- 10 tests NUnit aprobados (4 de combate y 6 de negocio/economía).
- Importación completa con Godot 4.7.2 .NET.
- Ejecución headless de la escena principal sin errores de scripts ni recursos.
- Reglas verificadas para presión ambiental, captura directa, toma forzada, sabotaje,
  comerciante derrotado e ingreso condicionado.
