# Etapa 2 — Combate

## Alcance implementado

- Salud y daño como reglas C# puras, sin referencias a Godot.
- Especificación y estado runtime de armas separados de la física del motor.
- Pistola hitscan compartida por protagonista y rival.
- Cargador, reserva, cadencia, recarga y bloqueo durante recarga.
- Dispersión diferenciada entre disparo desde cadera y ADS.
- Trazadores breves que muestran la trayectoria hitscan resuelta.
- ADS con orientación al cursor, reducción de velocidad y transición de cámara/FOV.
- Rival con percepción gradual, línea de visión, último punto conocido, aproximación,
  distancia de parada, disparo y recarga.
- Derrota visual del rival y bloqueo del protagonista derrotado.
- Reinicio completo del encuentro con `T`.
- HUD de salud, arma, munición, ADS y estado de alerta rival.

## Parámetros conservados desde Unity

| Parámetro | Valor |
| --- | ---: |
| Salud protagonista/rival | 100 |
| Daño de pistola | 25 |
| Cargador | 7 |
| Reserva protagonista/rival | 28 / 21 |
| Cadencia | 0,35 s |
| Recarga | 1,4 s |
| Alcance | 30 m |
| Dispersión cadera/ADS | 3,5° / 0,65° |
| Detección rival | 13 m |
| Confirmación visual | 1,2 s |
| Distancia de parada | 7 m |
| Velocidad rival | 2,25 m/s |
| Distancia de cámara ADS | 14,5 m |

## Controles añadidos

- Botón derecho: mantener ADS.
- Botón izquierdo: disparar.
- `R`: recargar.
- `T`: reiniciar el encuentro.

## Validación

- Compilación C# sin errores ni advertencias.
- Tests NUnit de salud, cadencia, recarga y percepción.
- Importación completa con Godot 4.7.2 .NET.
- Simulación headless del encuentro con percepción, movimiento y disparos.
- Captura renderizada para revisar composición y HUD.
