# Features pendientes: maqueta vs. app desarrollada

Comparación entre [maqueta/index.html](maqueta/index.html) y el código actual (`ConCancha.Api` + `ConCancha.Web`, fases 0 a 5 del [PLAN-DE-ACCION.md](PLAN-DE-ACCION.md)). Lista todo lo que la maqueta muestra o simula y la app todavía no tiene.

## Cómo se hizo el análisis

- La maqueta es un único HTML estático (716 líneas, JS inline, datos en `localStorage`). Se analizó **estáticamente**, leyendo sus pantallas (`PANELS.*`, sitio del club, cuenta de jugador, asistente de alta) y sus acciones (`data-act`, `data-form`).
- Las herramientas REA no aplican a este caso: necesitan un target de navegador por CDP o una app empaquetada (ASAR), y acá el artefacto es un archivo local de fuente legible. No se usó ninguna.
- Lo desarrollado se midió contra las rutas de [PageRoutes.cs](src/ConCancha.Web/Services/PageRoutes.cs), los controladores de la API y los componentes Blazor.
- **Prioridad:** `MVP` = está en el alcance del plan y falta o quedó incompleto; `Post-MVP` = el plan lo deja fuera a propósito; `Decidir` = hace falta una decisión de producto antes.

## 1. Resumen

| Área | Maqueta | App | Estado |
|---|---|---|---|
| Alta de club y asistente de configuración | Sí | Sí | Completo |
| Login / recuperar contraseña | Sí | Sí | Completo |
| Agenda (grilla, cargar, bloquear, pagar, jugado, no vino, cancelar, precio) | Sí | Sí | Falta Mover turno y accesos extra |
| Reservas, Recordar, Caja, Clientes | Sí | Sí | Faltan detalles (ver §3) |
| Canchas y precios | Sí | Sí | Faltan el alta con precio base y los turnos fijos |
| Sitio público `/{slug}` y gestión por token | Sí | Sí | Faltan lista de espera, "Tus turnos", marca del club y seña |
| Inicio / Resumen / Métricas | Sí | Parcial | El panel "Inicio" es mínimo; Resumen y Métricas no existen |
| Configuración, Equipo, Mi plan | Sí | No | No existen |
| Lista de espera, Señas, Buffet, Torneos, Premios | Sí | No | No existen |
| Cuenta de jugador, "Busco jugadores" | Sí | No | No existen |
| Asistente (chat) y notificaciones | Simulados | No | No existen |

## 2. Navegación y estructura del panel

| # | Feature | Detalle en la maqueta | Prioridad |
|---|---|---|---|
| 2.1 | Menú lateral agrupado (Hoy / El club / Crecer) con íconos | La app tiene solo una barra superior con 6 enlaces; no hay agrupación ni íconos **Hecho en M1: menú lateral con grupos e íconos.** | MVP |
| 2.2 | Barra inferior móvil (Inicio, Agenda, Caja, Espera, Asistente, Más) y hoja "Todo el panel" | No hay navegación móvil específica **Hecho en M1: barra inferior (Inicio, Agenda, Caja, Más) y hoja "Todo el panel"; Espera y Asistente aparecen cuando existan sus módulos.** | MVP |
| 2.3 | Marca del club en el menú (inicial, nombre, link `/{slug}` y botón **Compartir link** que copia la URL) | El link solo aparece en el Inicio **Hecho en M1: marca, link y botón Compartir en el menú.** | MVP |
| 2.4 | Botón compartir link también en la Agenda | No existe **Hecho en M1: `ShareLinkButton` en la Agenda.** | MVP |
| 2.5 | Secciones del menú que se ocultan según lo activado (Buffet, Torneos) | No aplica todavía: los módulos no existen | Post-MVP |
| 2.6 | Barra superior con estado de prueba ("Prueba sin arrancar", 14 días desde el primer turno) y campana de avisos | No existe | Post-MVP |
| 2.7 | Banner "Activá los avisos" (push) | No existe | Post-MVP |
| 2.8 | Cerrar sesión desde el menú y acceso a "Ver mi página" | Existe "Salir"; falta "Ver mi página" **Hecho en M1: "Ver mi página" y Cerrar sesión en el menú.** | MVP |
| 2.9 | Ruta `/panel` con redirección según el estado del alta | Existe parcialmente; revisar el caso de club recién creado sin canchas **Sin cambios en M1: el Inicio ya redirige al asistente de alta si está pendiente; falta recorrer el caso en navegador.** | MVP |

## 3. Funciones del día a día (panel)

### 3.1 Inicio

La maqueta muestra una guía de bienvenida; la app muestra datos del club y 3 pasos.

- **Checklist de primeros pasos completo** con estado real y botón "Ir": cargar canchas, definir precios, cargar el primer turno, activar la seña, compartir el link (la app tiene 3 pasos fijos, de los cuales 2 están marcados siempre como hechos). `MVP`
- **Guía por secciones** (Para arrancar, Hoy, El club, Crecer, Tu cuenta) con una tarjeta por módulo. `Post-MVP`
- **Videos de ayuda** por sección ("Cómo funciona"). `Post-MVP` (el contenido no existe; en la maqueta es un toast simulado).
- Gestión del **equipo** (ver §5.2): hoy el Inicio solo lista a los miembros.

### 3.2 Agenda

Ya está: navegación por día, grilla con columnas por cancha, indicadores Ocupación / Entró / Se escapa, cargar turno, bloquear, detalle, pagar con método, jugado, no vino, cancelar con confirmación, cambiar precio.

| # | Falta | Prioridad |
|---|---|---|
| 3.2.1 | **Mover turno** (mismo día y misma duración, a un horario libre de cualquier cancha). Falta endpoint y hoja en la UI | MVP |
| 3.2.2 | **Selector de 7 días** (tira de días Hoy / Mañana / Jue 23…) además de las flechas ‹ › | MVP |
| 3.2.3 | **Línea de "ahora"** sobre la grilla y scroll inicial hacia la hora actual | MVP (verificar contra el diff de Agenda sin commitear) |
| 3.2.4 | Horarios pasados atenuados / "Pasó" con su precio | MVP (verificar) |
| 3.2.5 | **Autocompletar clientes** al cargar turno (la maqueta usa un `datalist` con los clientes existentes) | MVP |
| 3.2.6 | Cargar el turno con **"¿Ya pagó?" Debe / Pagó** en el mismo formulario | MVP (verificar) |
| 3.2.7 | **Deshacer estado** (volver de Jugado / No vino a confirmado) | MVP |
| 3.2.8 | **Marcar que debe** (deshacer el pago) y **cambiar el método** (efectivo ↔ transferencia) desde el detalle | MVP |
| 3.2.9 | **Botón WhatsApp** en el detalle del turno | MVP |
| 3.2.10 | Etiquetas en la grilla: "fijo", "online", "busca jugadores"; seña pendiente | Depende de los módulos de §4 |
| 3.2.11 | **Alta de cancha con precio base** en un paso (la maqueta pide nombre, deporte, duración y precio base, y crea la regla base automáticamente) | MVP (verificar el formulario actual) |

### 3.3 Reservas

Ya está: filtros Hoy / 7 / 30 / Historial, Todas / A cobrar / Online, búsqueda por nombre o teléfono.

- Abrir el **detalle del turno** al tocar una fila (en la maqueta, cada fila abre la misma hoja de acciones que la agenda). `MVP` (verificar)
- Marca "online" visible en cada fila. `MVP` (verificar)

### 3.4 Recordar turnos

Ya está: Mañana / Hoy, mensaje `wa.me` armado, marca "Enviado". No faltan features; solo contrastar el texto del mensaje con el de la maqueta.

### 3.5 Caja

Ya está: navegación por día, desglose por método, falta cobrar, tabla de turnos, imprimir, cerrar y reabrir.

| # | Falta | Prioridad |
|---|---|---|
| 3.5.1 | Tarjeta **Señas** en el desglose (monto de señas de turnos aún no cobrados) | Post-MVP (depende de Señas) |
| 3.5.2 | **Ventas de Buffet** sumadas a efectivo / transferencia y listadas en la caja | Post-MVP (depende de Buffet) |
| 3.5.3 | Mostrar hora de cierre ("Caja cerrada 23:10 · $X") | MVP |

### 3.6 Clientes

Ya está: búsqueda, filtros "deben" y "faltaron", exportar CSV, imprimir, WhatsApp por cliente.

- Exportar **Excel real (.xlsx)** y **PDF generado en servidor**: hoy son CSV y la impresión del navegador (decisión documentada en el plan; se retoma si el piloto lo pide). `Post-MVP`
- Ficha del cliente con su historial de turnos. No está en la maqueta, pero es la extensión natural de la lista. `Decidir`

### 3.7 Canchas, Precios y Turnos fijos

Ya está: horario del club, alta y edición de canchas (con horario propio y activa/inactiva), reglas de precio por cancha.

| # | Falta | Prioridad |
|---|---|---|
| 3.7.1 | **Turnos fijos**: grilla semanal por cancha (Lun a Dom × horarios), tocar un casillero para asignar o cambiar un fijo, agenda automática de las **próximas 12 semanas** que bloquea el horario, eliminar un fijo cancela los turnos futuros | Post-MVP (el plan lo deja fuera) |
| 3.7.2 | Aviso del impacto en el plan al sumar canchas ("Cada cancha extra suma al plan") | Post-MVP (depende de Mi plan) |
| 3.7.3 | Pestañas Canchas / Precios / Turnos fijos con la vista de precios por cancha agrupada | Revisar la organización actual |

## 4. Módulos de la maqueta que no existen en la app

### 4.1 Lista de espera (`/panel/espera`)

- **Jugador:** en el sitio del club, tocar un horario ocupado abre "Ese turno está ocupado, anotate y te avisamos" (nombre y WhatsApp).
- **Club:** lista de anotados con estado (esperando / avisado / tomado), indicador de si el turno se liberó, **Avisar por WhatsApp** (mensaje armado con el link del club), **Darle el turno** y **Quitar**.
- **Regla:** al cancelar un turno se avisa a los anotados; lo toma el primero que reserve.
- Entidad nueva: lista de espera (cancha, fecha, hora, nombre, teléfono, estado). `Post-MVP`

### 4.2 Señas con comprobante (`/panel/senas`)

- **Configuración:** activar seña, monto fijo, alias o CBU.
- **Jugador:** al reservar, aviso de seña y botón "Continuar"; hoja "Confirmá con la seña" con alias copiable, importe y subida de comprobante (foto o PDF); botón "Lo hago después".
- **Estado nuevo:** `pendiente_sena`, que retiene el horario ("Reserva en curso").
- **Club:** pantalla de señas pendientes con comprobante, **Aprobar** y **Rechazar** (con aviso al jugador).
- Faltan además el almacenamiento de archivos y decidir qué pasa si nadie sube el comprobante (vencimiento). `Post-MVP` (candidato natural, según el plan) · `Decidir` el vencimiento

### 4.3 Buffet (`/panel/buffet`)

- **Vender:** grilla de productos, carrito, cobrar en efectivo o transferencia, o **cargar a una cancha** (suma al precio del turno). Descuenta stock.
- **Productos:** nombre, tipo (bebidas, comida, accesorios, otro), precio de venta, costo (muestra ganancia), stock, aviso de stock bajo, "Cargar de la lista de siempre", editar y borrar.
- Total vendido "hoy". Entidades: Producto, VentaBuffet. `Post-MVP`

### 4.4 Torneos (`/panel/torneos`)

- Crear torneo (nombre, deporte, formato, cupo, inscripción, desde / hasta), estados abierto / cerrado / finalizado, anotar equipos con cupo, marcar pagó / debe, quitar, borrar.
- La propia maqueta marca como pendiente la generación de zonas, partidos y horarios. `Post-MVP`

### 4.5 Premios y puntos (`/panel/premios`)

- **Club:** activar puntos, puntos por turno jugado, canjes pendientes con "Marcar entregado", ABM de premios (nombre, descripción, puntos, stock, activo).
- **Regla:** los puntos se suman al marcar **jugado** y solo si el jugador reservó con su cuenta. `Post-MVP` (depende de la cuenta de jugador, §6)

### 4.6 Resumen (`/panel/resumen`)

- Tarjeta roja **"Se puede escapar en los próximos 7 días"** (valor de los turnos libres futuros).
- Indicadores del día: ocupación, entró, falta cobrar, turnos jugados.
- Sección **"Espera una decisión tuya"**: señas por validar, anotados en lista de espera, turnos con plata por cobrar.
- Gráfico de barras de ocupación de los próximos 7 días. `MVP` (la parte que no depende de módulos postergados: escapa 7 días, indicadores, por cobrar y barras)

### 4.7 Métricas (`/panel/metricas`)

- Selector Día / Semana / Mes / Año.
- Se escapó en huecos vacíos, cobrado, por cobrar, turnos jugados, ocupación.
- **Ocupación por horario** (barras; en rojo los horarios flojos) y **por cancha** (turnos, %, monto). `MVP`/`Post-MVP`: el plan no lo lista en ninguna fase; la fórmula se puede reutilizar de `AgendaService`. `Decidir`

### 4.8 Asistente con IA (`/panel/ia` y pestaña "Preguntar" del sitio)

- **Club:** chat con sugerencias ("¿Cómo viene hoy?", "¿A quién le tengo que cobrar?", "Anotá a Martín el sábado a las 20") que consulta la agenda y puede **cargar turnos**.
- **Jugador:** pestaña "Preguntar" que responde disponibilidad y precios.
- En la maqueta es por reglas con expresiones regulares; la versión real requiere un LLM y definir permisos. `Post-MVP`

## 5. Configuración, equipo y plan

### 5.1 Configuración (`/panel/config`, 4 pestañas)

El plan menciona que "la ventana de cancelación aún no se edita desde Configuración". En general **no existe ninguna pantalla de Configuración**.

| Pestaña | Contenido en la maqueta | Prioridad |
|---|---|---|
| **Marca** | Nombre del club, teléfono, **logo** (PNG/JPG, máx. 2 MB), **color principal y secundario** aplicados al sitio público (variable `--club`), vista previa | MVP el nombre y el teléfono; Post-MVP logo y colores |
| **País** | País del club (hora y moneda) | Post-MVP (el plan fija Argentina) |
| **Secciones del menú** | Mostrar u ocultar Buffet y Torneos | Post-MVP |
| **Cobros** | Pedir seña, monto, alias o CBU | Post-MVP (con Señas) |
| **Cancelación** | **Ventana de cancelación en horas** (existe en el dominio y la API, falta la UI) y **"Pedir el mail la primera vez"** con código de verificación | MVP la ventana; Post-MVP el mail |
| **Tu cuenta** | **Cambiar contraseña** (actual, nueva ≥ 10, repetir) y activar notificaciones | MVP el cambio de contraseña |

### 5.2 Equipo (`/panel/equipo`)

- Hoy: solo `GET api/club/team` y la lista en el Inicio.
- Falta: **sumar encargado por mail** (crear la membresía o invitación), **quitar encargado**, y el texto de qué puede y qué no puede hacer el rol. El plan define el rol encargado y sus permisos; falta el alta. `MVP`

### 5.3 Mi plan (`/panel/plan`)

- Cuánto se escapó en 30 días, turnos jugados, ocupación.
- Precio por cantidad de canchas activas, descuento semestral, aviso de ajuste por inflación, alias de pago de la plataforma y botón "Ya transferí, avisar".
- Los montos de la maqueta son inventados; el plan deja fuera planes y cobro. `Post-MVP`

## 6. Sitio público y cuenta de jugador

### 6.1 Sitio del club `/{slug}`

Ya está: selector de cancha, lista de turnos, hoja de reserva con nombre, WhatsApp y mail opcional, "¿Preferís llamar?", gestión por token con cancelación.

| # | Falta | Prioridad |
|---|---|---|
| 6.1.1 | **Marca del club**: color propio aplicado al sitio y logo (ver §5.1) | Post-MVP |
| 6.1.2 | **Selector de 7 días** con tira de días y flechas (verificar si ya está) | MVP |
| 6.1.3 | Horarios **"Pasó"** atenuados, **"No disponible"** para bloqueos, **"Tu turno"** para los turnos del propio visitante | MVP (verificar) |
| 6.1.4 | Sección **"Tus turnos en {club}"** con Cancelar, para el visitante anónimo (la app solo ofrece el link con token) | MVP |
| 6.1.5 | **Anotarse en la lista de espera** desde un horario ocupado | Post-MVP |
| 6.1.6 | Pestaña **Preguntar** (asistente) | Post-MVP |
| 6.1.7 | Sección **"Buscan jugadores"** con "Pedir sumarme" | Post-MVP |
| 6.1.8 | Banner **"Creá tu cuenta"** y CTA tras reservar | Post-MVP |
| 6.1.9 | Texto de privacidad en la hoja de reserva ("{club} guarda tu nombre y WhatsApp…") | MVP |
| 6.1.10 | Pie "Con tecnología de…" | Decidir |
| 6.1.11 | Mensaje de aviso de que ya no se puede cancelar online ("llamá al club") con la ventana en horas | MVP (verificar en la pantalla de gestión) |

### 6.2 Cuenta de jugador (`/jugador/*`)

La app no tiene cuentas de jugador. La maqueta incluye:

- **Login / crear cuenta** (mail + contraseña).
- **Inicio:** saludo, puntos en el club, cantidad de próximos turnos, solicitudes para sumarse, avisos, próximos turnos.
- **Reservar:** acceso a los clubes en los que ya reservó; si no hay ninguno, explica que se entra por el link del club.
- **Mis turnos:** próximos (con Cancelar dentro de la ventana, Enviar seña, "Busco jugadores") e historial.
- **Perfil:** nombre, teléfono, correo (no editable) con estado verificado / sin verificar y verificación por código, correo de respaldo, deportes que juega, categoría, puntos y canjes, activar notificaciones.
- Navegación propia (lateral y barra inferior) y campana de avisos. `Post-MVP` (el plan lo excluye explícitamente)

### 6.3 "Busco jugadores"

- El jugador publica su turno con cupo y categoría; otros piden sumarse con un mensaje; el dueño del turno acepta o rechaza. Depende de la cuenta de jugador. `Post-MVP`

## 7. Transversales

| # | Feature | Detalle | Prioridad |
|---|---|---|---|
| 7.1 | **Notificaciones internas** (campana) | La maqueta genera avisos por reserva online nueva, seña aprobada, etc. | Post-MVP |
| 7.2 | **Push web / PWA** | Botones "Activar avisos" y "Activar notificaciones" (simulados) | Post-MVP |
| 7.3 | **Verificación de mail** | Código por mail en el registro del jugador y en "Pedir el mail la primera vez". Hoy `LoggingEmailSender` solo registra en el log | Decidir (pregunta abierta nº 2 del plan) |
| 7.4 | **Envío real de mails** | Recuperar contraseña y verificación usan un sender que solo escribe en el log | MVP para el piloto (necesita un proveedor) |
| 7.5 | **Subida y almacenamiento de archivos** | Logo del club y comprobantes de seña | Post-MVP |
| 7.6 | **Hora simulada y controles de demo** | Solo para la maqueta; no se migra | No aplica |
| 7.7 | **Landing con dos entradas** (jugador / dueño) y registro | La app tiene `/` (Home); comparar el contenido | Decidir |

## 8. Orden sugerido

1. **Completar el MVP (cerrar brechas visibles)**
   - Navegación del panel (2.1 a 2.4) y botón Compartir link.
   - Mover turno, selector de 7 días, deshacer pago y estado, WhatsApp y autocompletar clientes en la Agenda (§3.2).
   - Pantalla de **Configuración** con Marca básica, Ventana de cancelación y Cambiar contraseña (§5.1).
   - **Equipo**: sumar y quitar encargados (§5.2).
   - **Resumen** con "se puede escapar en 7 días" y decisiones pendientes (§4.6).
   - Sección "Tus turnos" en el sitio público (6.1.4) y checklist real de primeros pasos (§3.1).
   - Envío real de mails (7.4).
2. **Primer módulo posterior al piloto:** Señas (§4.2), como ya propone el plan, junto con Configuración de Cobros y la tarjeta de Caja.
3. **Después:** Lista de espera (§4.1), Métricas (§4.7), Turnos fijos (§3.7.1), Buffet (§4.3), cuenta de jugador con puntos y premios (§6.2, §4.5), Torneos (§4.4), Asistente (§4.8), Mi plan (§5.3).

## 9. Puntos para verificar en el navegador

Algunos ítems marcados "verificar" dependen del cambio de Agenda que está sin commitear (`Agenda.razor`, `Agenda.razor.cs`, `app.css`) o de pantallas que no se recorrieron en ejecución: línea de "ahora", horarios pasados, selector de 7 días, formulario de alta de cancha, filas de Reservas y mensaje de cancelación fuera de ventana. Conviene recorrerlos contra la maqueta antes de abrir tickets.
