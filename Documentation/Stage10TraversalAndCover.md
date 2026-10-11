# Incremento posterior a la Etapa 6 — traversal y cobertura

## Objetivo

Recuperar del prototipo Unity el vault geométrico y la cobertura alta/baja sin
acoplar consultas físicas, input y combate en una sola clase. `Q` permanece reservado
para las órdenes de crew; `Espacio` ejecuta traversal y `Alt` alterna cobertura.

## Arquitectura

- `PlayerTraversalController` detecta frente, altura, espesor, aterrizaje y espacio
  libre mediante consultas a `PhysicsDirectSpaceState3D`. También controla la
  transición inicio–ápice–aterrizaje y restaura la colisión al finalizar.
- `PlayerCoverController` clasifica la superficie mediante sondas baja y alta,
  conserva su identidad física, alinea al protagonista y limita el movimiento al eje
  lateral mientras la superficie continúe siendo válida.
- `PlayerController` coordina input y movimiento del `CharacterBody3D`, pero delega
  las reglas geométricas a los dos componentes anteriores.
- `PlayerCombatController` cancela ADS y bloquea disparo/recarga durante el vault.
- `IsometricCameraController` desplaza el foco lateralmente en cobertura y lo reduce
  en cobertura baja.

El vault conserva los valores de referencia de Unity: obstáculos de 0,40 a 1,25 m,
ancho máximo de 1,50 m y transición de 0,55 s. La cobertura conserva una detección de
1,35 m y movimiento lateral de 2,60 m/s.

## Comportamiento jugable

- `Alt`, mirando una pared cercana, entra en cobertura; vuelve a salir con `Alt`.
- Las superficies con altura suficiente se clasifican como cobertura alta; las que
  sólo interceptan la sonda inferior se clasifican como cobertura baja.
- En cobertura sólo se utiliza el eje lateral de `WASD`; el personaje se mantiene
  orientado hacia la superficie.
- `Espacio` frente a un obstáculo bajo inicia un vault si existe aterrizaje estable y
  el volumen del personaje queda libre.
- `Espacio` desde cobertura baja cruza esa misma superficie. La cobertura alta no se
  puede saltar.
- Crouch se abandona antes de un vault únicamente si hay espacio para ponerse de pie.

El bloque graybox incluye superficies rotuladas de cobertura alta, cobertura baja y
vault para hacer una comprobación manual inmediata.

## Validación reproducible

```powershell
Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -- --traversal-smoke
```

La prueba posiciona al protagonista ante las superficies de entrenamiento y verifica:

- entrada y clasificación de cobertura alta;
- entrada y clasificación de cobertura baja;
- inicio y finalización del vault desde cobertura baja;
- cruce efectivo del obstáculo;
- restauración del collider del protagonista;
- paridad de los parámetros principales con Unity.

## Próximo incremento recomendado

Implementar **Bar La Niebla y la misión de sabotaje encadenada**. Ya están validados
movimiento, combate, negocio, persistencia, misión, crew, policía, navegación,
evitamiento, cámara y traversal; un segundo negocio comprobará que las reglas no están
codificadas exclusivamente para El Faro. El alcance debería mantenerse acotado a
sabotear el punto operativo sin conquistar el bar y a persistir ese resultado.
