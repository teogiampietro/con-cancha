# Análisis de una app de referencia y alcance de la app propia

> La app de referencia ("app real" en este documento) está anonimizada: no se menciona su nombre, dominio, clubes ni cuentas. Todo lo observado se hizo con cuentas descartables propias y sin consultar su backend.

Este documento es la base para pasar de la maqueta ([maqueta/index.html](maqueta/index.html)) al desarrollo en .NET 10 / Blazor / EF Core / PostgreSQL / Railway / Swagger.

## 1. Fuentes y nivel de confianza

| Etiqueta | Significa |
|---|---|
| **[Obs]** | Visto en ejecución en la app real (navegador). |
| **[Cód]** | Deducido del código público del cliente (nombres de entidades y rutas). No se probó contra el backend. |
| **[Txt]** | Texto que la app real le muestra al usuario (reglas declaradas, no verificadas en comportamiento). |
| **[Maq]** | Decisión o invento de la maqueta. **No** está confirmado en la app real. |

Alcance del análisis:
- Páginas públicas de un club real y del panel (login).
- Un **club de prueba propio** (panel de dueño) y una **cuenta de jugador de prueba**, recorridos en escritorio y en celular.
- Escrituras en el club de prueba: se cargó un turno y se canceló (queda uno cancelado en su historial). Nada en el club real.
- Segunda pasada (alta desde cero): se creó una cuenta descartable en `/panel/registro` y se recorrió el asistente completo hasta el resumen final (paso 11 de 11). **No** se confirmó el último paso.
- No se consultó el backend directamente ni se usaron credenciales de terceros.

## 2. Stack de la app real (referencia, no es el nuestro)

- **[Cód]** Aplicación web tipo PWA con notificaciones push, sobre un backend como servicio (base de datos, autenticación y archivos), con la lógica sensible en funciones del lado del servidor.
- **[Cód]** Rutas: `/{slug}` (sitio de cada club), `/panel/*` (dueños/encargados), `/jugador/*` (cuenta de jugador) y endpoints propios para el asistente de IA y los avisos masivos.
- **[Cód]** Al crear contraseña se consulta HaveIBeenPwned.
- **[Obs]** Contraseña mínima de 10 caracteres (cambio de contraseña).
- **Nuestro stack:** .NET 10, EF Core, PostgreSQL, Blazor, Swagger, Railway.

## 3. Roles

| Rol | Qué hace | Fuente |
|---|---|---|
| Jugador anónimo | Ve horarios y reserva con nombre + WhatsApp (+ mail opcional); cancela sus turnos del club; se anota en lista de espera. | [Obs] |
| Jugador con cuenta | Lo anterior + puntos, canjes, "busco jugadores", sus turnos de todos los clubes, perfil, avisos. | [Obs] |
| Dueño | Todo el panel, incluida Configuración, Mi plan y Equipo. | [Obs] |
| Encargado (mostrador) | Carga turnos, aprueba señas, marca jugado, anota buffet; **no** ve ni cambia el plan, ni suma o saca gente. Debe tener una cuenta creada. | [Txt] |

## 4. Reglas de negocio

### Turnos y grilla
- Cada cancha tiene deporte y duración de turno (Pádel 90′, Fútbol 60′). **[Obs]**
- Los turnos arrancan a horas fijas por cancha y terminan antes de medianoche (Pádel 09:00, 10:30 … 22:30; Fútbol 08:00 … 23:00). **[Obs]** La regla general de generación (inicio a las 09:00 si dura 90′, 08:00 si dura 60′) es **[Maq]**.
- En el alta se define **un solo horario para todo el club** (por defecto abre 8 hs y cierra a las 12 de la noche, 00:00); "si un día es distinto, lo cambiás después". **[Obs]** Las excepciones por cancha son **[Maq]** (decisión propia).
- La duración del turno se elige por deporte en el alta (60 / 90 / 120 min; sugeridos Pádel 90, Fútbol 5 60). **[Obs]**
- Estados visibles al jugador: *Pasó*, *Libre* (con precio), ocupado. **[Obs]**
- Un turno puede ser **reserva** o **bloqueo** (con motivo: lluvia, mantenimiento…). **[Obs]**
- "Sin doble reserva nunca". **[Txt]**
- Precio por franja: reglas por cancha o para todas, por día de la semana o todos, con horario desde–hasta. **[Obs]** Que gane la regla más específica es **[Maq]**.
- En el alta el precio se pide **por deporte** (aplica a todas las canchas de ese deporte) con una sola variante: "¿a la noche, desde las 18 hs, cobrás distinto?" y un segundo precio. **[Obs]** Cómo se ajusta después por cancha o por día no se recorrió. Nuestra decisión **[Maq]**: pedirlo por deporte, copiarlo a cada cancha y permitir ajustar por cancha con franjas por días y horario.

### Cobro y señas
- Pago en el club: *Debe* / *Pagó*, con método (efectivo / transferencia). **[Obs]** (método: efectivo/transferencia por la Caja; el detalle exacto de cómo se elige es **[Maq]**)
- Seña por transferencia: el club carga monto y alias/CBU; el jugador transfiere y sube el comprobante; el club lo aprueba o rechaza desde Señas. La plata va directo a la cuenta del club, la plataforma no cobra por él. **[Txt]**
- Estado intermedio "esperando seña" que retiene el horario hasta que se envía/valida el comprobante: **[Maq]**.

### Cancelación y antifraude
- Ventana de cancelación configurable en horas (por defecto **12**). **[Obs]**
- "Pedir el mail la primera vez": el que reserva por primera vez confirma su mail con un código; hasta que juegue puede tener **2 turnos por delante**; los que ya jugaron en ese club no lo ven. **[Txt]**
- Marcar **no vino** (no-show) y **jugado**. **[Obs]**

### Lista de espera
- El jugador se anota desde la página del club cuando el turno que quería está tomado. Al cancelarse un turno se avisa a todos los anotados y lo toma el primero que reserve. El club también puede dárselo a alguien. **[Txt]**
- Avisar por WhatsApp con texto armado: **[Maq]** (el real avisa por push/mensaje; no se vio el mecanismo).

### Puntos y premios
- El club activa si da puntos. Los puntos se suman cuando el turno se marca **jugado** y **solo si el jugador reservó con su cuenta**. **[Txt]**
- Premios: nombre, descripción, puntos que cuesta, stock (vacío = ilimitado), activo. Los jugadores canjean; el club marca "entregado". **[Obs]**
- Cantidad de puntos por turno configurable: **[Maq]** (el real solo muestra el interruptor).

### "Busco jugadores"
- El jugador publica su turno buscando jugadores (cupo, categoría); otros piden sumarse y el dueño del turno responde. **[Cód]** (hay operaciones de servidor para pedir sumarse, responder la solicitud y listar búsquedas abiertas).
- El perfil guarda deportes que juega y categoría, que se usan al publicar. **[Txt]**

### Turnos fijos
- "El que juega siempre el mismo día y hora. Se agenda solo las próximas **12 semanas** y bloquea el horario." **[Txt]**

### Indicadores
- Agenda: *Ocupación*, *Entró*, *Se escapa*. **[Obs]** Fórmulas exactas **no confirmadas**; la maqueta usa: ocupación = turnos ocupados / turnos del día; entró = lo cobrado + señas; se escapa = valor de los horarios libres que ya pasaron. **[Maq]**
- Resumen: "se puede escapar en los próximos 7 días" = valor de los turnos libres futuros. **[Obs]**
- Métricas (Día/Semana/Mes/Año): se escapó en huecos vacíos, cobrado, por cobrar, turnos jugados, ocupación, ocupación por horario y por cancha. **[Obs]**

### Planes
- Precio mensual por cantidad de canchas activas, 14 días de prueba que arrancan al cargar el primer turno, descuento por pagar cada 6 meses, actualización por inflación cada 6 meses. Buffet y Torneos son parte de un plan "ecosistema". **[Obs]/[Txt]**
- Los montos de la maqueta son inventados. **[Maq]**

## 5. Flujos y pantallas

### Jugador (sitio del club y cuenta)
| Pantalla / flujo | Qué hay | Fuente |
|---|---|---|
| Sitio del club | Banner "Creá tu cuenta", selector de 7 días, pestañas *Ver horarios / Preguntar*, selector de cancha, lista de turnos, "¿Preferís llamar?". | [Obs] |
| Reservar | Hoja con nombre, WhatsApp, mail opcional; precio y aviso de seña. | [Obs] |
| Preguntar | Asistente que contesta disponibilidad y precios. | [Obs] (respuestas reales son de IA; las de la maqueta son reglas simples [Maq]) |
| Mis turnos del club | Lista con cancelar. | [Cód] |
| Lista de espera | Anotarse en un turno ocupado. | [Txt] |
| Inicio | Saludo, "Cómo funciona" (videos), turnos próximos y puntos. | [Obs] |
| Reservar (cuenta) | Si no hay clubes: "Todavía no estás en ningún club…" (se entra por el link del club). | [Obs] |
| Mis turnos | Próximos e historial. | [Obs] |
| Perfil | Nombre, teléfono, correo (no editable), correo de respaldo, deportes (Pádel, Fútbol, Tenis, Pickleball, Fútbol 5, Vóley, Básquet), categoría. | [Obs] |
| Premios y puntos en el perfil | Canjear con puntos. | [Cód]/[Maq] (ubicación en pantalla no confirmada) |

### Club (panel)
| Pantalla | Qué hay | Fuente |
|---|---|---|
| Login / registro | Mail + contraseña, recuperar contraseña. | [Obs] |
| Alta de club (ver siguiente sección) | Asistente en dos tramos: cuenta y club (3 pasos) y configuración (11 pasos con 2 deportes). | [Obs] |
| Inicio | "Bienvenido", videos por sección agrupados (Para arrancar, Hoy, El club, Crecer, Tu cuenta). | [Obs] |
| Agenda | Navegación por día, grilla por hora con columna por cancha; libre → *Cargar turno* o *Bloquear horario*; turno → detalle. | [Obs] |
| Cargar turno | Nombre (autocompleta clientes), WhatsApp opcional, precio, ¿Ya pagó? Debe/Pagó. | [Obs] |
| Detalle del turno | Marcar como pagado, Marcar como jugado, No vino, Cancelar turno con confirmación. | [Obs] |
| Reservas | Filtros Hoy / 7 días / 30 días / Historial y Todas / A cobrar / Online; búsqueda por nombre o teléfono. | [Obs] |
| Recordar turnos | Mañana / Hoy; mensaje de WhatsApp ya escrito para enviar. | [Obs] (envío por wa.me es [Maq]) |
| Caja | Cierre del día por método de pago; imprimir. | [Obs] (botón "Cerrar caja" es [Maq]) |
| Buffet | Vender (productos, cobrar o cargar a una cancha) y Productos (tipo, precios, stock, aviso). | [Obs]/[Txt] |
| Lista de espera | Anotados; avisar o dar el turno. | [Txt] |
| Canchas | Canchas, Precios, Turnos fijos. | [Obs] |
| Clientes | Visitas, gasto, deuda, faltas; filtros "solo los que deben / faltaron"; exportar Excel y PDF. | [Obs] (los botones de exportar quedan simulados en la maqueta) |
| Señas | Pendientes de validar con aprobar / rechazar. | [Obs] (lista vacía; el contenido es [Txt]/[Maq]) |
| Resumen y Métricas | Ver sección de indicadores. | [Obs] |
| Torneos | Crear (nombre, deporte, formato, cupo, inscripción, desde/hasta) y anotar equipos. | [Obs] (armado de zonas, partidos y horarios: **no visto**) |
| Premios | Regla de puntos, canjes pendientes, premios. | [Obs] |
| Asistente | Chat con IA que consulta la agenda y puede cargar turnos. | [Obs] (maqueta simulada) |
| Configuración | Marca (nombre, teléfono, logo, colores), país (hora y moneda), secciones del menú, cobros (seña, alias), cancelación, contraseña, notificaciones. | [Obs] |
| Mi plan | Plan y precio, cuánto se escapó, alias de transferencia, "Ya transferí, avisar". | [Obs] |
| Equipo | Sumar encargados por mail. | [Obs] |
| Compartir link | Copia el link público del club. | [Obs] |

### Alta de club: flujo observado (`/panel/registro` → `/panel/empezar`)
**Tramo 1: cuenta y club (3 pasos, indicador de puntos).** **[Obs]**

| Paso | Qué pide |
|---|---|
| 1. Creá tu cuenta | Mail y contraseña (mínimo 10 letras o números, botón mostrar/ocultar). "Es gratis para probar y no te pedimos tarjeta. En 3 minutos tenés tu club andando." Sin confirmación de contraseña ni verificación de mail visible. Acepta Términos y Política de privacidad. |
| 2. ¿Cómo se llama tu club? | Nombre. El link se arma solo (`{dominio}/{slug}`) y se edita con "Cambiar esta dirección" ("sólo letras, números y guiones"). No se probó qué pasa con un link tomado o reservado. |
| 3. ¿A qué número te pueden llamar? | Teléfono (lo ven los jugadores en la página), país (Argentina, con "Cambiar") y un campo opcional "¿Alguien te recomendó?". |

**Tramo 2: configuración del club (progreso "Paso N de M", M depende de los deportes; 11 con dos).** **[Obs]**

| Paso | Qué pide |
|---|---|
| 1 | Deportes (selección múltiple de 7 + "buscar otro"). |
| 2…k | Una pregunta por deporte: cuántas canchas (contador + / −, empieza en 1). |
| Horario | Hora de apertura (default 8 hs) y de cierre (default 12 de la noche, 00:00), para todo el club. |
| Por deporte | Duración del turno (60 / 90 / 120), precio y "¿a la noche, desde las 18 hs, cobrás distinto?" con segundo precio. |
| Encuesta | "¿Cómo tomás los turnos hoy?" (cuaderno o planilla, WhatsApp, otra app, recién abro) para decidir por dónde arrancar. |
| Seña | Sí/No; si es sí, monto fijo y alias o CBU. |
| Buffet y torneos | Sí/No cada uno; si es no, no se muestra el módulo. |
| Final (11) | Resumen por bloque con "Cambiar horario", "Cambiar canchas", "Cambiar precio" y "Cambiar los deportes". |

Observaciones de interfaz: el avance vive solo en memoria del navegador (recargar vuelve al paso 1); las respuestas Sí/No son obligatorias y un cartel rojo "Tocá una de las respuestas para seguir" queda pegado aunque ya se haya elegido. No se vio qué hace el botón final ni cómo se nombran las canchas creadas (el resumen dice "Pádel: 2 canchas").

### Diseño mobile (observado)
- Panel: barra inferior (Inicio, Agenda, Caja, Espera, Asistente, Más); *Más* abre una hoja "Todo el panel".
- Agenda: grilla con eje de horas; turnos con alto proporcional a la duración; libres en rojo; pasados tachados.
- Modales: hojas inferiores con borde de arrastre.

## 6. Modelo de datos

**Observado en el código [Cód]** (entidades de la app real, nombres genéricos): clubes, miembros del club, canchas, reglas de precio, reservas, pagos de reserva, pagos, reservas fijas, lista de espera, búsqueda de jugadores, perfiles, premios, canjes, torneos, inscripciones y partidos de torneo, suscripciones push, marca y comprobantes.

**Campos de turno** [Cód]/[Obs]: cancha, inicio, fin, tipo (reserva | bloqueo), estado (confirmada, cancelada…), cliente (nombre, teléfono), precio, estado de pago, método de pago, motivo (bloqueo), cancelado en, no-show.

**Entidades propuestas para nuestra base (a validar):** Club, Cancha, ReglaDePrecio, Turno (con estado, pago y origen), PagoDeSeña (comprobante), TurnoFijo, ListaDeEspera, Jugador (perfil), Notificación, SuscripciónPush, Premio, Canje, Torneo, InscripciónTorneo, PartidoTorneo, Producto, VentaBuffet, MiembroDelClub (rol), Plan/Suscripción.

## 7. Lo que NO se vio (hay que decidir o investigar)

- Seña en vivo (el club de prueba la tiene desactivada) y qué pasa si nadie sube el comprobante.
- Lista de espera con datos y el mecanismo real de aviso.
- Torneos: zonas, partidos, horarios, carga de resultados por sets.
- Cobro y facturación reales del plan.
- Cómo el jugador anónimo "recupera" sus turnos (se vincula por teléfono o mail con una operación de servidor).
- Notificaciones push reales y avisos masivos.
- Qué hace exactamente el asistente de IA y con qué permisos.
- Cierre de caja: si el real "cierra" o solo informa.
- Alta: qué hace el botón final del paso 11, cómo se nombran las canchas creadas, cómo se ajusta después el precio por cancha o por día, y qué validaciones tiene el link (tomado, reservado, longitud).
- Alta: qué cambia en el panel según la encuesta "¿cómo tomás los turnos hoy?" y según se active seña, buffet o torneos.

## 8. Diferencias intencionales de la maqueta

**Alta de club adoptada (adaptada) en la maqueta:** cuenta (3 pasos con puntos) y asistente de configuración con barra de progreso: deportes → canchas por deporte → horario del club → duración y precio por deporte (con variante desde las 18 hs) → resumen con "Cambiar". Diferencias deliberadas: no incluye la encuesta ni el campo de referido; seña, buffet y torneos quedan fuera del MVP (se activarán luego desde Configuración); el país es fijo (Argentina); el progreso se guarda y se retoma; el link muestra disponibilidad en vivo, con slugs reservados y tomados; las canchas se nombran "Pádel 1, Pádel 2…". Los precios cargados por deporte se copian a cada cancha y después se ajustan por cancha, con franjas por días (por ejemplo viernes a domingo) y horario. El horario es del club con excepción por cancha.

La maqueta agrega o simplifica cosas que no son de la app real: hora simulada, datos de ejemplo, asistente por reglas, "Mover turno" y "Cambiar precio" en el detalle, estado "esperando seña", cierre de caja, avisos por WhatsApp con enlace, plan y precios inventados, alias de pago de ejemplo.

## 9. Próximos pasos propuestos

1. Cerrar el alcance del MVP (qué de lo anterior entra primero).
2. Modelo de datos y API (Swagger) para .NET 10 + EF Core + PostgreSQL.
3. Autenticación y multi-club (slug por club, roles dueño / encargado / jugador).
4. Implementación en Blazor siguiendo la maqueta.
