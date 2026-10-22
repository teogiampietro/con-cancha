# Plan de desarrollo por módulos

Base: [FEATURES-PENDIENTES.md](FEATURES-PENDIENTES.md), [maqueta/index.html](maqueta/index.html) y el [PLAN-DE-ACCION.md](PLAN-DE-ACCION.md) (fases 0 a 5, ya hechas).

Cada módulo se puede decidir por separado: **entra** o **no entra**. En cada uno figura qué incluye, de qué depende, qué hay que construir, cuánto cuesta y qué decisión hace falta. Los módulos con dependencias dicen cuáles se arrastran.

## Convenciones para todos los módulos

- Se respeta [ESTANDARES-DE-CODIGO.md](ESTANDARES-DE-CODIGO.md): código en inglés y UI en español, DTOs `record` en `ConCancha.Contracts`, servicios en `Application` (interfaz) e `Infrastructure` (implementación), `CancellationToken` en todo método async, Blazor con `.razor.cs`, sin `@code`.
- Toda entidad nueva implementa `IBelongsToClub` (filtro global por club). Cada módulo con datos trae un test de aislamiento entre clubes.
- Cada endpoint declara su acceso (dueño, encargado o `[AllowAnonymous]`); el test de la matriz de permisos lo exige.
- Hora en UTC, convertida a la zona del club solo al mostrar.
- Cada módulo cierra con: tests de dominio y de API, pantalla recorrida en navegador y celular, y una actualización de [PLAN-DE-ACCION.md](PLAN-DE-ACCION.md).
- Estimaciones en días de una persona, sin pruebas manuales en celular real.

## 1. Mapa de módulos

| Id | Módulo | Grupo | Esfuerzo | Depende de | Prioridad sugerida |
|---|---|---|---|---|---|
| **M1** | Navegación y estructura del panel | Núcleo | 2–3 d | — | Alta |
| **M2** | Agenda: mover, deshacer y atajos | Núcleo | 3–4 d | — | Alta |
| **M3** | Configuración del club | Núcleo | 3–4 d | M1 | Alta |
| **M4** | Equipo (encargados) | Núcleo | 2–3 d | M7 | Alta |
| **M5** | Inicio guiado y Resumen | Núcleo | 3–4 d | M1 | Media |
| **M6** | Sitio público: mejoras | Núcleo | 2–3 d | — | Media |
| **M7** | Envío real de mails | Núcleo | 1–2 d | — | Alta (piloto) |
| **M8** | Señas con comprobante | Opcional | 6–8 d | M3, M7 | Media (siguiente natural) |
| **M9** | Lista de espera | Opcional | 4–5 d | M6 | Media |
| **M10** | Métricas | Opcional | 3–4 d | — | Media |
| **M11** | Turnos fijos | Opcional | 4–5 d | M2 | Media |
| **M12** | Buffet | Opcional | 6–8 d | — | Baja |
| **M13** | Cuenta de jugador | Opcional | 7–9 d | M7 | Baja |
| **M14** | Puntos y premios | Opcional | 4–5 d | M13 | Baja |
| **M15** | "Busco jugadores" | Opcional | 3–4 d | M13 | Baja |
| **M16** | Torneos | Opcional | 5–7 d (básico) | — | Baja |
| **M17** | Asistente con IA | Opcional | 6–8 d | M2 | Baja |
| **M18** | Marca avanzada, país y secciones | Opcional | 3–4 d | M3 | Baja |
| **M19** | Notificaciones y push | Opcional | 5–7 d | M13 (push de jugador) | Baja |
| **M20** | Planes y cobro | Opcional | 6–10 d | M10 | Baja |
| **M21** | Exportes reales (xlsx y PDF) | Opcional | 2–3 d | — | Baja |

Los módulos M1 a M7 completan lo que el plan original ya daba por MVP y la maqueta muestra; no agregan líneas de negocio nuevas. Del M8 en adelante cada uno es una decisión de producto.

### Dependencias

```
M7 Mails ──► M4 Equipo
          ├─► M8 Señas (avisos de aprobación)
          └─► M13 Cuenta de jugador ──► M14 Puntos y premios
                                    └─► M15 Busco jugadores
M1 Navegación ──► M3 Configuración ──► M8 Señas (config de cobros)
                                   └─► M18 Marca avanzada
M2 Agenda ──► M11 Turnos fijos
          └─► M17 Asistente IA
M6 Sitio público ──► M9 Lista de espera
M10 Métricas ──► M20 Planes y cobro
Archivos (parte de M8) ──► M18 logo
```

Piezas compartidas que conviene construir una sola vez, dentro del primer módulo que las necesite:

| Pieza | La construye | La reutilizan |
|---|---|---|
| Almacenamiento de archivos (`IFileStore`) | M8 | M18 |
| Canal de avisos al jugador (WhatsApp con enlace) | M9 | M8 |
| Servicio de notificaciones internas | M19 | M8, M9, M13, M15 |
| Selector de 7 días y componente de tira de días | M2 | M6 |

---

## 2. Módulos del núcleo

### M1. Navegación y estructura del panel

**Para qué:** que el panel se parezca a la maqueta y se use bien en celular. Hoy hay una barra superior con 6 enlaces.

**Incluye**
- Menú lateral agrupado (Hoy / El club / Crecer) con íconos; los grupos de módulos no activos no se muestran.
- Barra inferior móvil (Inicio, Agenda, Caja, Espera, Asistente, Más) y hoja "Todo el panel". Los ítems cuyo módulo no entró no aparecen.
- Marca del club en el menú (inicial, nombre, link) y botón **Compartir link** que copia la URL; el mismo botón en la Agenda.
- "Ver mi página" y Cerrar sesión desde el menú.
- Redirección de `/panel` según el estado del alta.

**Construir**
- Web: `PanelLayout.razor` (+ `.razor.cs`), `PanelNav.razor`, `BottomNav.razor`, `MoreSheet.razor` (reutiliza `Sheet`), `ShareLinkButton.razor`; un catálogo `PanelNavItems` con ítem, ruta, ícono, grupo y rol permitido. `PanelHeader` se reemplaza.
- Íconos: set SVG inline como en la maqueta (`IC`), en un componente `Icon.razor`.
- Sin cambios de API.

**Tests:** del catálogo de navegación (qué ve el dueño y qué ve el encargado); recorrido manual en celular.

**Decisión pendiente:** ninguna.

**Listo cuando:** el dueño y el encargado ven el menú correcto en escritorio y celular, y copiar el link funciona desde el menú y la agenda.

---

### M2. Agenda: mover, deshacer y atajos

**Incluye**
- **Mover turno:** mismo día, misma duración, a un horario libre de cualquier cancha. Valida con la restricción de exclusión.
- **Selector de 7 días** (tira Hoy / Mañana / Jue 23…).
- **Autocompletar clientes** al cargar un turno.
- **"¿Ya pagó?"** (Debe / Pagó) en el formulario de carga, con método si pagó.
- **Deshacer estado** (Jugado / No vino → confirmado), **marcar que debe** (deshacer pago) y **cambiar método de pago**.
- **WhatsApp** desde el detalle del turno.
- Línea de "ahora" y scroll inicial hacia la hora actual, horarios pasados atenuados (verificar contra el cambio sin commitear de Agenda).
- Alta de cancha con **precio base** en un paso (crea la regla base).
- Detalle de turno abierto desde las filas de Reservas.

**Construir**
- Dominio: `Booking.Move(...)`, `Booking.UndoAttendance()`, `Booking.MarkUnpaid()`, `Booking.ChangePaymentMethod(...)`, con reglas y excepciones tipadas.
- Contracts: `MoveBookingRequest`, `CreateBookingRequest` con `Paid` y `PaymentMethod` opcionales; `CustomerSuggestionDto`.
- API: `POST api/bookings/{id}/move`, `POST api/bookings/{id}/unpay`, `POST api/bookings/{id}/attendance/reset`, `PUT api/bookings/{id}/payment-method`, `GET api/customers/suggestions?q=`.
- Web: `MoveBookingSheet`, `DayStrip` (componente reutilizable), `CustomerAutocomplete`, ajustes a `BookingSheet` y `BookingDetailSheet`.

**Tests:** concurrencia de mover al mismo horario (una sola gana), mover a otra cancha de otra duración rechazado, deshacer pago actualiza la Caja, aislamiento por club.

**Decisión pendiente:** si "Mover" permite cambiar de día (la maqueta solo mismo día). Recomendado: solo mismo día, igual que la maqueta.

**Listo cuando:** se mueve un turno desde el detalle y la agenda, la Caja y los indicadores se actualizan.

---

### M3. Configuración del club

**Incluye (versión núcleo)**
- **Marca básica:** nombre y teléfono del club.
- **Cancelación:** ventana en horas (ya existe en dominio y API; falta la pantalla).
- **Tu cuenta:** cambiar contraseña (actual, nueva ≥ 10, repetir, con el rechazo de contraseñas filtradas que ya existe en el registro).
- Estructura en pestañas ya preparada para sumar **Cobros** (M8), **País y secciones** (M18) y **Notificaciones** (M19).

**Construir**
- Contracts: `UpdateClubProfileRequest`, `UpdateCancellationRequest`, `ChangePasswordRequest`.
- API: `PUT api/club/profile`, `PUT api/club/cancellation`, `POST api/auth/change-password`. Solo dueño (el encargado no ve configuración).
- Web: `Pages/Settings.razor` con pestañas y un componente por pestaña.
- Cambiar el nombre **no** cambia el slug (decisión explícita); cambiar el slug queda fuera.

**Tests:** el encargado recibe 403 en todos los endpoints; la contraseña filtrada se rechaza; la ventana nueva se aplica a la reserva pública.

**Decisión pendiente:** ¿se permite editar el slug? Recomendado: no en el MVP, porque rompe los links ya compartidos.

**Listo cuando:** el dueño cambia la ventana a 24 h y el jugador deja de poder cancelar dentro de esas 24 h.

---

### M4. Equipo (encargados)

**Incluye**
- Sumar un encargado por mail, quitarlo, y ver su estado (invitado / activo).
- Texto de lo que puede y no puede hacer el rol.
- La lista deja de vivir en el Inicio y pasa a su pantalla.

**Construir**
- Dominio: `ClubInvitation` (club, mail, token hash, vencimiento, rol, estado); `ClubMember` ya existe.
- API: `POST api/club/team/invitations`, `DELETE api/club/team/{memberId}`, `POST api/auth/accept-invitation`. Solo dueño.
- El invitado recibe un mail con link para crear su contraseña (reutiliza el flujo de restablecer). Si el mail ya tiene cuenta, se suma directo como encargado.
- Web: `Pages/Team.razor`, `AcceptInvitation.razor`.

**Tests:** la invitación vence; un encargado no puede invitar; una persona con cuenta en otro club puede ser encargada en este sin ver el otro.

**Decisión pendiente:** límite de encargados por club. Recomendado: 5.

**Depende de:** M7 (sin mails reales la invitación no llega).

**Listo cuando:** el dueño invita a un encargado, este entra y ve solo lo permitido.

---

### M5. Inicio guiado y Resumen

**Incluye**
- **Inicio:** checklist real de primeros pasos con estado (canchas, precios, primer turno, compartir link; seña si M8 está activo) y botón "Ir". Guía por secciones con tarjetas (solo de los módulos activos).
- **Resumen (`/panel/resumen`):**
  - Tarjeta "Se puede escapar en los próximos 7 días".
  - Indicadores de hoy: ocupación, entró, falta cobrar, turnos jugados.
  - "Espera una decisión tuya": turnos con plata por cobrar (más señas y lista de espera si M8 y M9 existen).
  - Barras de ocupación de los próximos 7 días.

**Construir**
- Contracts: `OnboardingChecklistDto`, `SummaryDto`.
- Application: `ISummaryService` que reutiliza las fórmulas de `AgendaService` (extraerlas a una clase `AgendaMetrics` compartida; la usan también M10 y M20).
- API: `GET api/onboarding`, `GET api/summary`.
- Web: `Pages/Summary.razor`, `ChecklistCard`, `GuideCard`; reescribir `Dashboard`.
- Videos de ayuda: no entran; las tarjetas de guía llevan al módulo.

**Tests:** fórmulas con casos fijos (la misma agenda da el mismo número que la Agenda); el checklist cambia al completar cada paso.

**Decisión pendiente:** si se quieren videos de ayuda (hay que producirlos).

**Listo cuando:** un club nuevo ve el checklist avanzar solo y el Resumen coincide con la Agenda del día.

---

### M6. Sitio público: mejoras

**Incluye**
- Tira de 7 días con flechas, horarios "Pasó", "No disponible" (bloqueos) y "Tu turno" (si el visitante lo reservó).
- **"Tus turnos en {club}"** con Cancelar, para el visitante anónimo, usando su teléfono; el link con token sigue funcionando.
- Texto de privacidad en la hoja de reserva y aviso "ya no se puede cancelar online, llamá al club" con la ventana en horas.
- Pie con marca de la plataforma (opcional).

**Construir**
- Contracts: `PublicMyBookingsDto`; `PublicSlotState` suma `Mine` y `Blocked` si hace falta.
- API: `GET api/public/clubs/{slug}/my-bookings` identificado por un token de visitante guardado en cookie o `localStorage` (no por teléfono suelto, para no filtrar turnos ajenos).
- Web: `PublicClub` reutiliza `DayStrip` (M2) y suma `MyBookingsCard`.

**Tests:** un visitante no ve turnos de otro teléfono; el límite de tasa cubre el endpoint nuevo.

**Decisión pendiente:** cómo se identifica al visitante en el mismo navegador. Recomendado: cookie firmada emitida al reservar con token de visitante.

**Listo cuando:** quien reserva desde el celular vuelve al link del club y ve y cancela su turno sin buscar el mail.

---

### M7. Envío real de mails

**Incluye:** reemplazar `LoggingEmailSender` por un proveedor real para recuperar contraseña, invitaciones (M4) y avisos de módulos futuros.

**Construir**
- `SmtpEmailSender` (o el cliente del proveedor elegido) detrás de `IEmailSender`, con plantillas en español y configuración por variables de entorno en Railway.
- Cola simple (hosted service con reintentos) para no bloquear el request.
- Registro en logs sin el contenido del mail.

**Tests:** el sender falso del entorno de pruebas; plantillas con datos de ejemplo.

**Decisión pendiente (bloqueante):** proveedor (Resend, SendGrid, SES, SMTP propio) y dominio remitente. Depende de la compra del dominio (pregunta abierta nº 1 del plan).

**Listo cuando:** "Olvidé mi contraseña" llega a una casilla real.

---

## 3. Módulos opcionales

### M8. Señas con comprobante

**Para qué:** asegurar el turno con una transferencia y reducir ausencias. Es el candidato natural que ya marca el plan.

**Incluye**
- **Cobros** en Configuración: pedir seña, monto fijo, alias o CBU.
- Estado de turno **`PendingDeposit`** que retiene el horario ("Reserva en curso"), con vencimiento configurable.
- **Jugador:** aviso de seña al reservar, hoja "Confirmá con la seña" (alias copiable, importe), **subida de comprobante** (foto o PDF), "Lo hago después", y reenvío desde la gestión por token.
- **Club:** pantalla **Señas** con comprobantes pendientes, **Aprobar** y **Rechazar** con motivo; contador en el menú y tarjeta en el Resumen.
- **Caja:** tarjeta Señas y señas en el desglose.
- Aviso al jugador al aprobar o rechazar (mail y, si M19 está, notificación).

**Construir**
- Dominio: `BookingStatus.PendingDeposit`, `DepositPayment` (booking, monto, comprobante, estado, revisado por, fecha); `ClubDepositSettings` (activa, monto, alias, horas de vencimiento).
- Infraestructura: `IFileStore` con almacenamiento en disco o bucket (Railway volume o S3 compatible), validación de tipo y tamaño (máx. 5 MB, imagen o PDF), nombres opacos, descarga solo con permiso del club.
- API: `PUT api/club/deposit-settings`, `POST api/public/clubs/{slug}/bookings/{token}/deposit` (multipart), `GET api/deposits?status=pending`, `POST api/deposits/{id}/approve`, `POST api/deposits/{id}/reject`, `GET api/deposits/{id}/proof`.
- Job de fondo que **vence** las reservas sin comprobante y libera el horario.
- Web: pestaña Cobros, `Pages/Deposits.razor`, `DepositSheet` (público), badge en el menú.

**Tests:** concurrencia (el horario retenido bloquea a otros), vencimiento libera el turno, rechazo vuelve a liberar, el comprobante de un club no es descargable por otro, límite de turnos activos cuenta los pendientes.

**Decisiones pendientes**
1. Vencimiento de la reserva sin comprobante (propuesta: 2 horas, configurable).
2. Si el límite de 2 turnos activos cuenta los pendientes de seña (recomendado: sí).
3. Dónde se guardan los archivos en producción.

**Listo cuando:** un jugador reserva, sube el comprobante desde el celular, el club lo aprueba y el turno queda confirmado; si nadie sube nada, el horario se libera solo.

---

### M9. Lista de espera

**Incluye**
- **Jugador:** tocar un horario ocupado abre "anotate y te avisamos" (nombre y WhatsApp).
- **Club (`/panel/espera`):** anotados con estado (esperando / avisado / tomado), indicador de turno liberado, **Avisar por WhatsApp** (mensaje armado con el link del club), **Darle el turno** y **Quitar**.
- Al cancelar un turno, los anotados de ese horario pasan a "avisar".
- Contador en el menú y en el Resumen.

**Construir**
- Dominio: `WaitlistEntry` (cancha, fecha, hora, cliente, estado, creado). Cliente por teléfono normalizado, como en la reserva.
- API: `POST api/public/clubs/{slug}/waitlist`, `GET api/waitlist`, `POST api/waitlist/{id}/notified`, `POST api/waitlist/{id}/assign`, `DELETE api/waitlist/{id}`.
- Servicio: al cancelar una reserva, `BookingService` marca las entradas del mismo horario como "avisar" (sin enviar nada automáticamente en el MVP del módulo, igual que Recordar).
- Web: `Pages/Waitlist.razor`, `WaitlistSheet` en el sitio público.

**Tests:** una persona no puede anotarse dos veces al mismo horario; "darle el turno" respeta la restricción de exclusión; límite de tasa en el alta pública.

**Decisión pendiente:** si el aviso es solo manual por WhatsApp (como la maqueta) o automático. Recomendado: manual primero.

**Listo cuando:** se cancela un turno con anotados y el club les avisa y asigna el horario desde la lista.

---

### M10. Métricas

**Incluye**
- Selector Día / Semana / Mes / Año.
- Se escapó en huecos vacíos, cobrado, por cobrar, turnos jugados, ocupación.
- **Ocupación por horario** y **por cancha** (turnos, %, monto).

**Construir**
- Reutiliza `AgendaMetrics` (M5); si M5 no entró, se extrae acá.
- API: `GET api/metrics?range=day|week|month|year`.
- Web: `Pages/Metrics.razor` con barras en CSS (sin librería de gráficos).
- Consulta agregada en SQL (por rango), no turno por turno en memoria, para que el rango "año" sea viable.

**Tests:** valores fijos sobre una agenda sembrada; coincidencia con Agenda y Resumen en el rango "día".

**Decisión pendiente:** el plan no tiene esta pantalla en ninguna fase; definir si el piloto la necesita antes de Planes (M20).

**Listo cuando:** las cifras del rango "día" coinciden con la Agenda y el rango "mes" carga en menos de 1 s con un año de datos de prueba.

---

### M11. Turnos fijos

**Incluye**
- Pestaña **Turnos fijos** en Canchas: grilla semanal por cancha (Lun a Dom × horarios), casillero libre para asignar, ocupado para cambiar o quitar.
- Al crear, se agendan las **próximas 12 semanas** como turnos que bloquean el horario, saltando las fechas ocupadas (y avisando cuáles).
- Eliminar un fijo cancela los turnos futuros; editarlo regenera los futuros.
- Etiqueta "fijo" en agenda, reservas y detalle.
- Renovación: un job que mantiene la ventana de 12 semanas hacia adelante.

**Construir**
- Dominio: `RecurringBooking` (cancha, día, hora, cliente, precio, activo) y `Booking.RecurringBookingId`.
- Servicio de generación idempotente (`RecurringBookingScheduler`) con reporte de conflictos.
- API: `GET/POST/PUT/DELETE api/courts/{id}/recurring-bookings`.
- Web: `RecurringGrid.razor`, `RecurringSheet`.

**Tests:** generación idempotente; conflicto con un turno existente se informa y no pisa; cambiar la duración de la cancha con fijos activos se rechaza o se resuelve explícitamente; borrar no toca turnos pasados.

**Decisiones pendientes**
1. Qué pasa con los fijos si cambia el precio de la franja (recomendado: el fijo guarda su propio precio, como la maqueta).
2. Qué pasa si la cancha cambia de horario.

**Listo cuando:** un fijo creado hoy llena 12 semanas de agenda y se renueva solo.

---

### M12. Buffet

**Incluye**
- **Productos:** nombre, tipo, precio, costo (muestra ganancia), stock, aviso de stock bajo, "Cargar de la lista de siempre", editar y borrar.
- **Vender:** grilla de productos, carrito, cobrar en efectivo o transferencia, o **cargar a una cancha** (suma al precio del turno).
- Total vendido de hoy; ventas incluidas en la **Caja** del día.
- Se activa desde Configuración (M18) o queda siempre disponible.

**Construir**
- Dominio: `Product`, `BuffetSale`, `BuffetSaleItem` (con precio y costo congelados al vender), `StockMovement` opcional.
- API: ABM `api/products`, `POST api/buffet/sales` (con `bookingId` opcional), `GET api/buffet/sales?date=`.
- Caja: `CashDayDto` suma ventas por método; el total y la caja cerrada las incluyen.
- Web: `Pages/Buffet.razor` con pestañas Vender / Productos, `Cart` componente.

**Tests:** el stock nunca queda negativo bajo concurrencia; cargar a una cancha suma al precio del turno una vez; la Caja cuadra con ventas y turnos.

**Decisiones pendientes**
1. Si "cargar a una cancha" se muestra como renglón aparte en el turno o se suma al precio (la maqueta lo suma). Recomendado: renglón aparte, para no perder el precio base.
2. Si el stock es obligatorio.

**Listo cuando:** se vende de un toque y la Caja del día muestra turnos y buffet por método de pago.

---

### M13. Cuenta de jugador

**Incluye**
- Registro e ingreso (mail + contraseña), recuperar contraseña.
- **Inicio** (saludo, próximos turnos, avisos), **Mis turnos** (próximos con Cancelar dentro de la ventana, e historial), **Reservar** (clubes donde ya reservó), **Perfil** (nombre, teléfono, correo de respaldo, deportes, categoría).
- **Verificación de mail** por código.
- Reservar con cuenta vincula el turno al usuario; **vinculación de turnos anteriores por teléfono** al verificar.
- Banner "Creá tu cuenta" en el sitio público y tras reservar.
- Navegación propia (lateral y barra inferior).

**Construir**
- Identity: rol `Player` además de los de club; una persona puede ser dueña y jugadora con el mismo mail.
- Dominio: `PlayerProfile` (usuario, nombre, teléfono, respaldo, deportes, categoría, mail verificado); `Booking.PlayerUserId` opcional.
- API: `api/player/me`, `api/player/bookings`, `PUT api/player/profile`, `POST api/player/verify-email`, `POST api/player/verify-email/confirm`, `POST api/player/link-bookings`.
- Web: `Pages/Player/*` con su propio layout.
- El jugador **no** tiene `ClubId` (cruza clubes); el filtro global no aplica a sus consultas, por lo que cada consulta lleva su propio test de que solo devuelve turnos propios.

**Tests:** un jugador no ve turnos de otro; la vinculación por teléfono exige verificar primero; roles: un jugador recibe 403 en todo `api/*` del club.

**Decisiones pendientes**
1. Verificación de mail (pregunta abierta nº 2 del plan).
2. Si el mail del jugador es el mismo que el del dueño (una sola cuenta con dos roles). Recomendado: sí.

**Depende de:** M7.

**Listo cuando:** un jugador crea su cuenta, ve todos sus turnos y cancela uno desde "Mis turnos".

---

### M14. Puntos y premios

**Incluye**
- **Club:** activar puntos, puntos por turno jugado, ABM de premios (nombre, descripción, puntos, stock, activo), canjes pendientes con "Marcar entregado".
- **Jugador:** puntos en el inicio y el perfil, lista de premios con "Canjear", mis canjes.
- Los puntos se suman al marcar **jugado**, solo si el turno es de una cuenta de jugador; se restan al canjear.

**Construir**
- Dominio: `Reward`, `Redemption`, `PointsLedger` (movimientos, no un saldo mutable) y `ClubPointsSettings`.
- Hook en `BookingService.MarkPlayed` que inserta el movimiento de forma idempotente (marcar y desmarcar no duplica).
- API: `api/rewards`, `api/redemptions`, `PUT api/club/points-settings`, `POST api/player/redemptions`.
- Web: `Pages/Rewards.razor` (club) y secciones en el perfil (jugador).

**Tests:** idempotencia al deshacer "jugado"; no se puede canjear sin saldo ni sin stock, incluso con dos canjes simultáneos; los puntos son por club.

**Decisión pendiente:** si los puntos vencen. Recomendado: no.

**Depende de:** M13, M2 (deshacer estado).

**Listo cuando:** un turno jugado suma puntos, el jugador canjea y el club marca la entrega.

---

### M15. "Busco jugadores"

**Incluye**
- El jugador publica su turno (cupo y categoría); otros piden sumarse con un mensaje; el dueño del turno acepta o rechaza.
- Sección "Buscan jugadores" en el sitio del club; etiqueta en el turno de la agenda.
- Solicitudes visibles en el inicio del jugador.

**Construir**
- Dominio: `PlayerSearch` (booking, cupo, categoría, estado) y `JoinRequest` (búsqueda, jugador, mensaje, estado).
- API: `POST api/player/bookings/{id}/search`, `DELETE ...`, `GET api/public/clubs/{slug}/searches`, `POST api/public/searches/{id}/requests`, `POST api/player/requests/{id}/accept|reject`.
- Los datos de contacto del solicitante se muestran solo al aceptar.

**Tests:** solo el dueño del turno responde; no se pueden pedir más lugares que el cupo; una búsqueda se cierra sola cuando pasa el turno.

**Decisión pendiente:** si el pedido de sumarse exige cuenta (la maqueta lo permite anónimo). Recomendado: exigir cuenta para evitar abuso.

**Depende de:** M13.

**Listo cuando:** un jugador publica, otro pide sumarse y el primero acepta.

---

### M16. Torneos

**Incluye (versión de la maqueta)**
- Crear torneo (nombre, deporte, formato, cupo, inscripción, desde y hasta), estados abierto / cerrado / finalizado.
- Anotar equipos con control de cupo, marcar pagó o debe, quitar, borrar.

**Fuera de esta versión:** zonas, partidos, horarios y resultados (la maqueta también los marca como pendientes). Si se quiere el torneo completo, se arma como submódulo **M16b** (≈ 10–15 d adicionales) con fixture, calendario en las canchas y carga de sets.

**Construir**
- Dominio: `Tournament`, `TournamentEntry`.
- API: ABM `api/tournaments` y `api/tournaments/{id}/entries`.
- Web: `Pages/Tournaments.razor`.
- Opcional: **bloquear canchas** durante el torneo reutilizando los bloqueos existentes.

**Tests:** cupo bajo concurrencia; aislamiento por club.

**Decisión pendiente:** si el piloto necesita el torneo completo o alcanza con la inscripción.

**Listo cuando:** se crea un torneo, se anotan equipos hasta el cupo y se cierra la inscripción.

---

### M17. Asistente con IA

**Incluye**
- **Club (`/panel/ia`):** chat que responde cómo viene el día, qué hay libre, a quién cobrarle, y **carga turnos** a pedido ("Anotá a Martín el sábado a las 20").
- **Jugador:** pestaña "Preguntar" en el sitio del club con disponibilidad y precios (solo lectura).

**Construir**
- Servicio `IAssistantService` con un modelo de lenguaje detrás de una interfaz, usando **tool calling** sobre las operaciones que ya existen (agenda del día, huecos libres, deudas, crear turno); el modelo no accede a la base.
- Permisos por rol: el asistente del club actúa como el usuario que escribe; el del sitio público es de **solo lectura** y con límite de tasa estricto.
- Confirmación explícita antes de crear un turno; el turno creado queda registrado con origen "asistente".
- Registro de conversaciones sin datos personales innecesarios.
- Configuración de modelo, límites de gasto por club y desactivación por club.

**Tests:** el asistente público no puede crear ni cancelar; el del club no escapa del club del usuario; casos de inyección de instrucciones en nombres de clientes.

**Decisiones pendientes**
1. Proveedor y modelo, y quién paga el consumo.
2. Si se expone al público (riesgo de abuso y costo).

**Depende de:** M2 (operaciones de turno estables).

**Listo cuando:** el dueño pide "¿a quién le tengo que cobrar?" y obtiene la lista correcta, y un turno pedido por chat aparece en la agenda previa confirmación.

---

### M18. Marca avanzada, país y secciones del menú

**Incluye**
- **Logo** (PNG o JPG, máx. 2 MB) y **colores principal y secundario** aplicados al sitio público.
- **País** del club (zona horaria y moneda).
- **Secciones del menú:** mostrar u ocultar Buffet y Torneos (y los módulos que se sumen).

**Construir**
- Reutiliza `IFileStore` (M8); si M8 no entró, se construye acá.
- Dominio: `Club.LogoPath`, `PrimaryColor`, `SecondaryColor`, `CountryCode`, `EnabledModules`.
- Web: pestañas en Configuración; el sitio público toma las variables CSS del club; contraste mínimo validado (WCAG) para que un color no vuelva ilegible el sitio.
- País: hoy fijo Argentina. Cambiarlo implica **moneda, formato de teléfono y wa.me por país**, por lo que es la parte más costosa; se puede dejar fuera y entregar solo marca y secciones.

**Tests:** colores inválidos o con bajo contraste rechazados; un archivo que no es imagen rechazado; `EnabledModules` oculta ítems en la navegación y bloquea los endpoints.

**Decisión pendiente:** si hay multi-país (el plan lo deja fuera).

**Listo cuando:** el club sube su logo, cambia el color y el sitio público lo refleja.

---

### M19. Notificaciones y push

**Incluye**
- **Campana** con avisos internos: reserva online nueva, seña recibida o aprobada, anotado en lista de espera, solicitud para sumarse.
- **Push web (PWA)** con "Activar avisos" para el club y, con M13, para el jugador.
- Pestaña **Notificaciones** en Configuración y barra "Activá los avisos".

**Construir**
- Dominio: `Notification` (destinatario, tipo, texto, leída, creada) y `PushSubscription`.
- Servicio `INotificationPublisher` que los demás módulos invocan; Web Push con VAPID.
- Web: manifest, service worker, componente `NotificationBell`, permisos del navegador.
- Blazor Server no tiene push nativo: requiere service worker en `wwwroot` y un endpoint de suscripción.

**Tests:** una notificación llega solo al destinatario; las suscripciones vencidas se eliminan; no se envían datos personales en el payload.

**Decisión pendiente:** iOS exige instalar la PWA en la pantalla de inicio para recibir push; confirmar que el piloto lo acepta.

**Listo cuando:** una reserva online hace sonar el aviso en el celular del dueño sin tener el panel abierto.

---

### M20. Planes y cobro

**Incluye**
- Pantalla **Mi plan** con "se te escapó este mes", turnos jugados y ocupación.
- Precio por cantidad de canchas activas, descuento semestral, ajuste por inflación, **prueba de 14 días** que arranca al cargar el primer turno.
- Estado de prueba en la barra superior; avisos de vencimiento.
- Cobro: alias de la plataforma y "Ya transferí, avisar" (manual) o pasarela de pago (automático).

**Construir**
- Dominio: `Subscription` (club, plan, estado, inicio de prueba, vigencia hasta), `PlanPrice` y `SubscriptionPayment`.
- Reglas de bloqueo al vencer: modo solo lectura en vez de borrar datos.
- Back-office mínimo para que la plataforma confirme pagos manuales (un rol de superadministrador, que hoy no existe).

**Tests:** el contador de canchas activas define el precio; la prueba arranca una sola vez; un club vencido no puede crear turnos pero sí ver y exportar.

**Decisiones pendientes (bloqueantes):** modelo de negocio, montos reales, pasarela, facturación fiscal en Argentina. Los montos de la maqueta son inventados.

**Depende de:** M10 (cifras de "lo que se escapó").

**Listo cuando:** la prueba de un club nuevo arranca con el primer turno y al vencer pasa a solo lectura.

---

### M21. Exportes reales (xlsx y PDF)

**Incluye:** generar el **Excel (.xlsx)** y el **PDF** de Clientes en el servidor, y de paso Reservas y Caja.

**Construir**
- Hoy: CSV + impresión del navegador (decisión documentada en el plan).
- Librería de xlsx (por ejemplo ClosedXML) y de PDF (por ejemplo QuestPDF), detrás de `ICustomerExporter` y similares; los endpoints de exportación existentes cambian de formato por parámetro.

**Tests:** el archivo abre y las cifras coinciden con la pantalla.

**Decisión pendiente:** hacerlo solo si el piloto lo pide; las licencias de QuestPDF requieren revisión.

**Listo cuando:** el botón Excel descarga un `.xlsx` con los mismos filtros de la pantalla.

---

## 4. Combinaciones recomendadas

| Escenario | Módulos | Esfuerzo total | Resultado |
|---|---|---|---|
| **A. MVP cerrado para el piloto** | M1–M7 | 16–23 d | La app replica el panel y el sitio de la maqueta sin módulos de negocio nuevos |
| **B. A + cobro anticipado** | A + M8 | 22–31 d | Reserva con seña y comprobante |
| **C. B + captación de demanda** | B + M9 + M10 | 29–40 d | Lista de espera y métricas de lo que se escapa |
| **D. Plataforma completa para jugadores** | C + M13–M15 + M19 | 51–70 d | Cuenta de jugador, puntos, "busco jugadores" y avisos |
| **E. Ecosistema** | D + M11, M12, M16, M17, M18, M20, M21 | 90–130 d | Paridad con la maqueta completa |

Orden recomendado: **M7 → M1 → M2 → M3 → M4 → M6 → M5**, piloto, y recién después elegir entre M8, M9, M10 según lo que pida el club. Los módulos M13 en adelante no se arrancan sin evidencia de uso del piloto.

## 5. Riesgos y puntos transversales

- **M8 y M18 comparten el almacenamiento de archivos:** definir una sola vez dónde viven (disco de Railway vs. bucket) antes de empezar cualquiera de los dos.
- **M13 rompe la regla "todo cuelga de un club":** el jugador cruza clubes; requiere revisar el filtro global y el test de aislamiento.
- **M19 y M17** introducen servicios externos (push, LLM) con costo y límites; conviene un interruptor por club desde el primer día.
- **Sin mails reales (M7)** quedan bloqueados M4, M13 y la utilidad de M8.
- **Cobertura de pruebas:** cada módulo agrega endpoints, por lo que la matriz de permisos por rol debe ampliarse en el mismo PR.
- **Reglas "verificar" de FEATURES-PENDIENTES.md** (línea de ahora, 7 días, formulario de alta de cancha) se resuelven al empezar M2 y M6, no antes.
