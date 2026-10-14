# KOLOTOPIR — prototipo jugable (Capítulos I y II)

RPG de acción y aventura para un jugador, en primera persona, ambientado en **Orom**. Nilo, un chico de unos dieciséis años de la **Tribu del Reverso** (pelo negro azabache, ojos azul claro y la túnica morada que le regaló su abuela Gina), cruza por primera vez los límites de su territorio para descubrir qué les pasó a sus padres, Eira y Daren, y qué esconden las **Cápsulas**.

Este repositorio implementa el **«Prototipo jugable prioritario»** del dossier de diseño (sección 15) siguiendo las **decisiones cerradas** (sección 22): Unity + URP, PC con teclado y ratón, primera persona, un jugador.

Todo el contenido (terreno, edificios, personajes, criaturas, texturas, iconos, sonidos y música) **se genera por código**: el proyecto no necesita importar recursos externos.

---

## Cómo abrirlo

1. Instala **Unity 6** (`6000.0.23f1` o cualquier versión 6000.0 LTS posterior) desde Unity Hub con el módulo de compilación para tu sistema.
2. En Unity Hub: **Add › Add project from disk** y elige la carpeta del repositorio.
3. Al abrirse por primera vez, el configurador del proyecto (`Assets/Kolotopir/Scripts/Editor/ProjectSetup.cs`) hace automáticamente lo siguiente:
   - crea el asset de **URP** (`Assets/Kolotopir/Settings/`) y lo activa en *Graphics* y *Quality*;
   - genera las plantillas de materiales en `Assets/Kolotopir/Resources/KolotopirMaterials/` (así los shaders de URP entran en las compilaciones);
   - abre la escena `Assets/Kolotopir/Scenes/Main.unity`.

   Se puede repetir en cualquier momento desde el menú **KOLOTOPIR › Configurar proyecto (URP y materiales)**.
4. Pulsa **Play**. El juego arranca desde cualquier escena: si no está la escena principal, se inicia solo.

Si por algún motivo URP no se configura, el juego funciona igualmente con el pipeline integrado (sin postprocesado).

Otros comandos del menú **KOLOTOPIR**: abrir la escena principal, borrar las partidas guardadas y validar el contenido narrativo (busca referencias rotas entre misiones, diálogos, objetos y Resonancias).

### Compilar un ejecutable

*File › Build Profiles* (o *Build Settings*) › Windows › *Build*. La escena principal ya está incluida.

---

## Controles (reasignables en Ajustes › Controles)

| Acción | Tecla |
|---|---|
| Moverse | W A S D (o flechas) |
| Mirar | Ratón |
| Correr / Saltar / Agacharse | Mayús / Espacio / C |
| Interactuar, hablar, avanzar diálogo | E |
| Golpe rápido / golpe fuerte (mantener) | Clic izquierdo |
| Bloquear (bloqueo perfecto al brillar el golpe enemigo) | Clic derecho |
| Esquivar | Q |
| **Escucha de Resonancias** (mantener) | R |
| Usar objeto rápido / cambiar objeto | F / X (o 1–4, rueda) |
| Habilidad activa (Pulso de Resonancia) | Z |
| Catalejo (rueda: zoom) | V |
| Pedir pista a Miga | H |
| Inventario y personaje | Tab o I |
| Cuaderno / Atlas | J / M |
| Pausa | Esc o P |
| Información de rendimiento | F3 |

---

## Qué hay en el prototipo

### Correspondencia con el «Prototipo jugable prioritario» (dossier, sección 15)

| Requisito del documento | Implementación |
|---|---|
| Nilo en primera persona: movimiento, cámara, interacción y salto | `PlayerController` (caminar, correr, agacharse, saltar con tiempo de gracia, nadar en lagos, trepar enredaderas con remate, equilibrio en pasarelas estrechas, balanceo del puente colgante, sentarse, recuperación tras caídas), `PlayerCamera` (catalejo, encuadres de diálogo y planos cinematográficos), `ViewModel` (brazos con la túnica morada, espada, catalejo y vendas), `PlayerInteractor`. |
| Una zona exterior compacta y un pequeño asentamiento | Mundo de 1 km² en 16 sectores de 250 m: la **Tribu del Reverso** (meseta con casas, plaza con fuego comunal, campos, almacén, taller, Piedras de la Migración, Árbol de los Mayores, puerta, puesto del puente), el **Abismo** con el puente colgante y los **Prados que Recuerdan** (Lago de Boro con islote, arco de piedra, la Parada Antigua, Peña del Mirador, Colina de las Nubes, guaridas de jabalíes y el Camino del Este). |
| Entre 5 y 10 NPC con rutinas observables | 10 habitantes (Gina, Oren, Haro, Lía, Tomé, Suri, Kiko, Boro, Bruma y Copo) con horarios diarios, rutas, actividades (dormir, cocinar, recolectar, vigilar, jugar, meditar...), cambios de rutina por acontecimientos, conversaciones ambientales entre ellos, frases contextuales, reacciones al peligro y a la espada, y **simulación por distancia** (completa cerca, simplificada a media distancia, abstracta lejos). |
| Salud, daño, derrota y al menos un método de curación | Salud de **100 puntos en `float`**, defensa y resistencias, vendas y ungüentos con curación canalizada, comida con regeneración, descanso en lugares seguros. **Derrota sin pérdida de objetos**: Nilo despierta en el último lugar de descanso una hora después. |
| Una espada y un enemigo con comportamiento básico | Espada con combo de tres golpes, golpe fuerte cargado, bloqueo y **bloqueo perfecto** con contraataque, esquiva con invulnerabilidad breve. Enemigos: **ecos** (cercan por turnos, temen el fuego, oyen el ruido y la Escucha), **jabalí de musgo** (avisa antes de cargar; se le puede calmar con bayas), **ciervo de niebla** (huye) y el **Guardián de Ecos** de la Parada (golpe, barrido e invocación; o se le puede calmar devolviéndole su recuerdo). |
| Experiencia, subida de nivel y una mejora de atributo | Experiencia por misiones, exploración, descubrimientos, combate y Cápsulas; nivel máximo 30; cinco atributos (Vigor, Fuerza, Agilidad, Percepción, Resonancia) y tres árboles de habilidades (Explorador, Superviviente, Resonancia). |
| Inventario, equipamiento y al menos dos piezas de armadura | Ranuras de cabeza, torso, brazos, piernas y dos accesorios (decisión cerrada). Siete piezas de equipo (espada, capucha, chaleco, peto, brazales, grebas y el yelmo de la Parada), comparación con lo equipado, vista previa de Nilo con el equipo puesto, objetos rápidos, recetas basadas en conocimiento y **mejoras exclusivas** (una por pieza). |
| Una Cápsula con una interacción de Resonancia | **Dos Cápsulas**: la Cápsula sellada de la hondonada y la Cápsula dañada de la Parada (examinar, escuchar, **reparar**, **reconstruir un recuerdo fragmentado** con pistas graduales y **decidir** qué hacer con él: compartir, ocultar, conservar o dejarlo inaccesible, con alcance local, comunitario o global). Además, Resonancias en lugares y objetos, memorias privadas que piden consentimiento y huellas luminosas que revela la Escucha. |
| Una misión principal corta y una secundaria | Dos misiones principales (**El silencio del Reverso** y **Los Prados que Recuerdan**) y tres secundarias (**La colección de Boro**, **El nombre de la nube** y **El taller de Tomé**), con objetivos que se pueden cumplir en otro orden y pistas graduales. |
| Guardado y carga del progreso | Guardado **automático en hitos** y **manual en lugares de descanso** (decisión cerrada), en JSON: misiones, inventario, equipo, habilidades, cuaderno, relaciones, decisiones, Cápsulas, objetos recogidos, criaturas derrotadas y zonas exploradas. |

### Otras piezas del dossier presentes

- **Cuaderno de Nilo** con misiones, atlas con niebla por zonas exploradas, **misterios con evidencias e hipótesis** que distinguen lo que Nilo *sabe*, *ha observado* o *sospecha*, Resonancias, personajes (con relación y recuerdos), bestiario, culturas, objetos y registro de decisiones.
- **Miga**, la polilla de papel, que da pistas cada vez más claras y revolotea hacia el objetivo.
- **Trueque** con Suri y Boro (los kolotapires no usan moneda; la amistad abarata los tratos).
- **Ciclo de día y noche** (una hora de juego por minuto real) que cambia rutinas, luz, sonido y criaturas.
- **Paisaje sonoro por región**, firma sonora de las Resonancias, voces inventadas de humanos y kolotapires, y música usada con moderación.
- **Accesibilidad** (sección 18): tamaño de texto y de subtítulos, fondo de subtítulos, **señales visuales de sonido**, reducción del movimiento de cámara, sensibilidad e inversión del ratón, **controles reasignables**, **pistas graduales**, reducción de destellos y alto contraste.
- **Streaming por sectores de 250 m** con terreno de Unity por sector, versión lejana de baja resolución, vegetación instanciada y estado persistente.

---

## Estructura del proyecto

```
Assets/Kolotopir/
  Scenes/Main.unity              Escena principal (solo contiene el arranque)
  Scripts/
    Core/      (Kolotopir.Core)  Lógica pura en C#, sin dependencias de Unity: estado de la partida,
                                 progresión, inventario, combate, misiones, cuaderno, diálogos,
                                 guardado, forma del mundo y todo el contenido narrativo.
    Runtime/   (Kolotopir.Runtime) Juego en Unity: jugador, combate, mundo y streaming, NPC,
                                 criaturas, Cápsulas, audio sintetizado, interfaz uGUI y flujo de partida.
    Editor/    (Kolotopir.Editor) Configurador del proyecto (URP, materiales, escena).
  Tests/EditMode/                Pruebas de la lógica del núcleo (Test Runner de Unity).
Docs/GUIA.md                     Recorrido de la historia (con spoilers) para probar el prototipo.
```

- `Core/Content/` contiene el contenido del juego como datos: misiones, diálogos, personajes y horarios, objetos y recetas, Resonancias y Cápsulas, entradas del cuaderno y misterios.
- `Core/World/WorldShape.cs` es una función de altura pura (meseta, abismo, lagos, borde del mundo) y `WorldLayout.cs` reúne todas las posiciones, el grafo de navegación de los NPC y los lugares.
- `Runtime/Foundation/GameRoot.cs` orquesta el arranque, el menú, la partida, el reloj, la derrota, el descanso, el guardado y el final del capítulo.

### Pruebas

En Unity: *Window › General › Test Runner › EditMode › Run All* (48 pruebas del núcleo: daño y defensa, progresión, inventario y equipo, trueque, artesanía, misiones, cuaderno, diálogos, guardado y carga, horarios de los NPC, coherencia del contenido y del mapa).

---

## Estado y siguientes pasos (según la hoja de ruta del dossier)

Este prototipo cubre las fases 1 a 4 de la hoja de ruta (movimiento, Resonancia, vida de los NPC y *vertical slice* en los Prados). Quedan para fases posteriores, tal como indica el documento:

- **Mando**, cuando el movimiento y el combate estén estabilizados.
- **Lanza y arco** tras validar el combate con espada.
- **Pociones y economía de recursos** más amplia; fabricación compleja.
- **Capítulos III a VIII** (Bosque del Revés, Ciudad de las Mil Cápsulas... hasta el Umbral Dormido), los tres finales y la escala objetivo de 32 km², región a región, con carga aditiva de escenas o Addressables.
- **Arte y animación definitivos**: los modelos actuales son geometría *low-poly* generada por código y las animaciones son procedurales; sirven para validar la jugabilidad.
- Ajuste del ritmo de combate, la dificultad y la frecuencia de enemigos (decisiones pendientes del dossier).

**Nota de verificación:** el código se ha compilado sin errores ni avisos contra los ensamblados de referencia de Unity (con y sin URP, y el editor) y la lógica del núcleo pasa sus 48 pruebas, pero en el entorno donde se creó no había un editor de Unity con el que ejecutar el juego. La primera vez que lo abras puede hacer falta algún ajuste visual o de equilibrio.
