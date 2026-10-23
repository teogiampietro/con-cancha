# Panel de plataforma (M22)

Panel propio de ConCancha para ver el uso que hacen los clientes (los dueños de club) de la aplicación: qué clubes existen, cuántas canchas tiene cada uno, cuántos turnos cargan en el período elegido y quiénes son sus clientes. Tiene un inicio de sesión separado del panel de club; hoy lo usa una sola persona.

Se rige por [ESTANDARES-DE-CODIGO.md](ESTANDARES-DE-CODIGO.md) y las convenciones de [PLAN-DE-MODULOS.md](PLAN-DE-MODULOS.md). Las decisiones tomadas en la revisión están en la sección 7.

---

## 1. Objetivo

| Pregunta del dueño de ConCancha | Dónde se responde |
|---|---|
| ¿Cuántos clubes hay y cuántos usan la app de verdad? | Totales de la pantalla principal |
| ¿Cuántas canchas tiene cada club? | Columna *Canchas* del listado |
| ¿Cuántos turnos tuvo cada club en un período (hoy, esta semana, un rango cualquiera)? | Selector de fechas y columnas del listado |
| ¿Qué club se está enfriando o nunca arrancó? | Estado *Nuevo / Activo / Inactivo* y *Último turno* |
| ¿Cómo evolucionó un club en particular? | Detalle del club: turnos por día o por semana, turnos por cancha |
| ¿Quiénes son los clientes de cada club? | Detalle del club: lista de clientes |

**Fuera de alcance de esta versión:** editar o suspender clubes, entrar al panel de un club "como" su dueño, facturación (eso es M20), exportaciones (CSV, Excel), segundo factor de ingreso, más de un administrador.

---

## 2. Definiciones (qué se cuenta)

- **Turno:** una reserva (`BookingKind.Reservation`) en estado confirmado **a la que el jugador no faltó**. No cuentan los **bloqueos** de horario, los turnos **cancelados** ni los marcados como **No vino** (`Attendance.NoShow`). Sí cuentan los turnos fijos (cada ocurrencia es un turno).
- **Se cuenta el día en que se juega**, no el día en que se cargó. Un período que incluye días futuros cuenta los turnos ya reservados para esos días.
- **Semana:** de lunes a domingo.
- **Zona horaria:** Argentina (`ClubDefaults.TimeZoneId`). Hoy todos los clubes están en Argentina, así que un único "hoy" sirve para todos y la base cuenta todos los clubes en una sola consulta. Si se abre otro país, esto se revisa (ver riesgos).
- **Canchas:** se muestran las canchas activas; en el detalle aparecen también las inactivas, marcadas.
- **Online:** de los turnos del período, cuántos llegaron por el sitio público (`BookingOrigin.Online`). Indica si el club comparte su link.
- **Último turno:** fecha en que el club cargó (o recibió) su última reserva, sea del día que sea. Mide actividad real aunque los turnos sean a futuro.
- **Estado del club** (no depende del período elegido; siempre se mira desde hoy):
  - *Activo:* tiene turnos esta semana o la anterior.
  - *Nuevo:* sin turnos en esas dos semanas y dado de alta hace menos de 14 días (todavía se está configurando).
  - *Inactivo:* sin turnos en las dos últimas semanas y con más de 14 días de alta.
- **Cliente:** cada `Customer` del club (se crea al cargar un turno con nombre y WhatsApp, desde el panel o desde el sitio público).

---

## 3. Pantallas

URLs en español, como el resto de la Web.

### 3.1 Ingreso — `/plataforma/ingresar`

- Usuario y contraseña. Sin "registrarme" ni "olvidé mi contraseña" en esta pantalla (la contraseña se reinicia desde el hosting, ver 4.3).
- Error único para cualquier rechazo: "Usuario o contraseña incorrectos." Si la IP quedó bloqueada por intentos fallidos, un aviso con el tiempo de espera.
- Al entrar, va a `/plataforma`.

### 3.2 Selector de fechas (resumen y detalle)

- Atajos: **Hoy**, **Esta semana** (por defecto), **Semana pasada**, **Este mes**, **Últimos 30 días** y **Personalizado** (desde / hasta).
- Rango máximo: 366 días. "Hasta" puede ser futuro (para ver turnos ya reservados).
- El período elegido viaja en la URL (`?desde=2026-10-01&hasta=2026-10-31`), así se mantiene al pasar del resumen al detalle de un club y al recargar la página.
- Cada cifra se compara con el **período anterior de igual duración** (por ejemplo, esta semana contra la semana pasada; 1 al 31 de octubre contra 31 días antes).

### 3.3 Resumen — `/plataforma`

- **Fila de totales:** clubes (y cuántos activos), canchas activas, turnos del período (con variación contra el período anterior) y cuántos fueron online.
- **Listado de clubes**, ordenado por turnos del período (de mayor a menor):

  | Club | Estado | Canchas | Turnos del período | Período anterior | Promedio por día | Online | Clientes | Último turno | Alta |
  |---|---|---|---|---|---|---|---|---|---|

  - Nombre con link al sitio público (`/{slug}`) y mail del dueño debajo.
  - *Clientes:* cantidad total de clientes del club.
  - Filtro por estado y buscador por nombre (en la Web, sobre la lista ya cargada).
  - En celular, cada club es una tarjeta con nombre, estado, canchas y turnos del período; el resto se ve en el detalle.
- Barra propia (logo, "Plataforma", salir). No reutiliza la navegación del panel de club.

### 3.4 Detalle de club — `/plataforma/clubes/{id}`

- Encabezado: nombre, estado, mail y teléfono del dueño, fecha de alta, link público.
- Las mismas cifras del listado para el período elegido.
- **Turnos en el tiempo**, como barras en CSS (mismo criterio que M10: sin librería de gráficos): una barra por día si el período tiene hasta 31 días, una por semana si es más largo.
- **Turnos por cancha** en el período, con deporte y si está activa.
- **Clientes:** lista sencilla con nombre, WhatsApp, mail (si lo tiene), turnos en el período, último turno y fecha de alta. Ordenada por último turno (los más recientes primero), con buscador por nombre o teléfono y paginada de a 50.

---

## 4. Acceso e inicio de sesión

Un único administrador, con usuario y contraseña propios de la plataforma. No usa las cuentas de club (Identity), no usa mail y no necesita tablas nuevas.

### 4.1 Diseño

- **Credenciales en variables de entorno** del servicio de la API (Railway o Render), nunca en el repo ni en `appsettings.json`:
  - `Platform__AdminUser`: usuario genérico, por ejemplo `admin`.
  - `Platform__AdminPasswordHash`: **hash** de la contraseña (PBKDF2 con sal, el `PasswordHasher` de ASP.NET, en base64: no lleva caracteres que molesten en una variable de entorno). La contraseña en sí no se guarda en ningún lado.
- **Contraseña larga y aleatoria** (unos 24 caracteres, de un gestor de contraseñas). Como el usuario es adivinable, toda la seguridad está en la contraseña.
- **Login propio:** `POST api/platform/auth/login` con usuario y contraseña. Compara el usuario y verifica el hash siempre (aunque el usuario sea incorrecto), así la respuesta y el tiempo no revelan cuál de los dos falló. Cualquier rechazo da el mismo 401: "Usuario o contraseña incorrectos."
- **Falla cerrada:** si falta alguna de las dos variables, el login rechaza siempre y la API lo avisa una vez al iniciar (Warning). El panel queda apagado, nunca abierto.
- **Freno de fuerza bruta:** sin Identity no hay bloqueo de cuenta, así que se usa un freno en memoria por IP del cliente: 5 fallos en 15 minutos bloquean esa IP durante 15 minutos (HTTP 429, mensaje en español). Se suma al rate limiting `Auth` que ya tiene la API. Al reiniciar la API el contador se borra, y es aceptable para un solo administrador. Se bloquea por IP y no de forma global para que nadie pueda dejarte afuera a propósito.
- **Sin segundo factor:** se puede agregar más adelante sin cambiar el diseño.
- **Token:** el mismo JWT de la API, sin club y con rol `PlatformAdmin`, más un claim con una huella corta del hash vigente. Misma duración que la sesión de club.
- **Autorización:** política `PlatformAdmin` en los endpoints de plataforma: exige el rol **y** que la huella del token coincida con la del hash actual. Cambiar la contraseña (ver 4.3) invalida al instante todas las sesiones abiertas, sin esperar a que venzan.
- **Aislamiento con el panel de club:** un token de club no entra a la plataforma (le falta el rol) y el token de plataforma no entra a los endpoints de club (no tiene club). La Web guarda la sesión de plataforma con una clave propia, así se puede tener abierta la sesión de un club y la de plataforma en el mismo navegador sin pisarse.
- **Logs:** ingreso correcto (Information) y rechazos o bloqueos (Warning), con la IP y sin el usuario ni la contraseña que se probó. **Ni la contraseña ni el hash se escriben nunca en un log**: los logs del hosting los ve cualquiera con acceso al proyecto, y un hash expuesto se puede intentar descifrar sin límite de intentos.

### 4.2 Alta inicial

1. Generar el hash en la computadora propia:
   ```sh
   dotnet run --project src/ConCancha.Api -- hash-password
   ```
   Pide la contraseña sin mostrarla y escribe el hash solo en la terminal. No toca la base ni los logs.
2. Cargar `Platform__AdminUser` y `Platform__AdminPasswordHash` en las variables del servicio de la API en Railway o Render y redeployar.
3. En local, usar *user secrets* del proyecto Api: `dotnet user-secrets set "Platform:AdminUser" "admin"` y `dotnet user-secrets set "Platform:AdminPasswordHash" "<hash>"`.
4. Entrar por `/plataforma/ingresar`.

### 4.3 Si se olvida la contraseña

No hay pantalla ni mail de recuperación: se reinicia desde el hosting, que es lo único que puede hacerlo.

1. Correr de nuevo `hash-password` con una contraseña nueva.
2. Reemplazar `Platform__AdminPasswordHash` en Railway o Render y redeployar.
3. La contraseña anterior deja de servir y las sesiones abiertas se cierran solas (por la huella del hash).

---

## 5. API

Las fechas van como `yyyy-MM-dd` (días locales de Argentina, ambos incluidos). Sin `from`/`to`, se toma la semana actual.

| Método y ruta | Acceso | Devuelve |
|---|---|---|
| `POST api/platform/auth/login` | Anónimo (rate limit `Auth` y freno por IP) | `SessionResponse` (`Club = null`, `Role = "PlatformAdmin"`) |
| `GET api/platform/overview?from=&to=` | Política `PlatformAdmin` | `PlatformOverviewDto`: período, período anterior, totales y lista de `ClubUsageDto` |
| `GET api/platform/clubs/{clubId}?from=&to=` | Política `PlatformAdmin` | `ClubUsageDetailDto`: uso del club, canchas y serie (por día o por semana, con su granularidad). 404 si no existe |
| `GET api/platform/clubs/{clubId}/customers?from=&to=&search=&page=` | Política `PlatformAdmin` | `PlatformCustomersPageDto`: `Items` (nombre, teléfono, mail, turnos en el período, último turno, alta), `Total`, `Page`, `PageSize` (50). 400 si `page` es menor que 1, 404 si el club no existe |

**Validación:** `from` posterior a `to` o un rango de más de 366 días responden 400 con el mensaje en español.

**Rendimiento:** el resumen hace una consulta agrupada por cifra (turnos del período y del anterior, canchas, clientes, dueños, último turno), no turno por turno, así que el costo crece con la cantidad de clubes y no con la de turnos. La serie del detalle se agrupa en memoria, a partir de las horas de inicio de los turnos de un solo club (un año de un club son pocos miles de filas, y la base no devuelve nada más). La lista de clientes se busca, ordena y pagina en la base: el orden por último turno es una subconsulta escalar por cliente, y la página se proyecta al final. Las consultas usan `IgnoreQueryFilters` porque leen todos los clubes: es el único lugar del sistema que lo hace sin un club en sesión, y por eso queda encerrado en los servicios de plataforma.

---

## 6. Estado actual y plan de desarrollo

### 6.1 Lo que ya está en el código (sin commitear)

Hay un primer avance del backend en el árbol de trabajo. La **etapa 1 ya lo adaptó** a lo decidido: login con usuario y hash, *No vino* excluidos, períodos elegibles desde el selector. La **etapa 2** agregó la lista de clientes por club y el conteo de clientes del resumen. **Todas las etapas están hechas.** Estado: 536 tests del backend en verde, recorrido en Chrome en escritorio y celular. Desvíos registrados en [PLAN-DE-ACCION.md](PLAN-DE-ACCION.md), módulo M22.

| Capa | Archivos |
|---|---|
| Domain | `Platform/ClubActivity.cs`, `Platform/ClubActivityRules.cs`, `Platform/UsageCalendar.cs`; `BookingQueries.WhereStandingReservation`; `TimeConstants.DaysPerWeek` |
| Contracts | `Platform/*Dto.cs`, `Platform/PlatformRoles.cs` |
| Application | `IPlatformSignInService`, `IPlatformUsageService`, `PlatformMessages` |
| Infrastructure | `Identity/PlatformOptions`, `PlatformPasswords`, `PlatformCredentials`; `Services/PlatformSignInService`, `PlatformLoginThrottle`, `ClubUsageReader`, `ClubBreakdownReader`, `PlatformUsageService`, `PlatformReporting`; `SessionFactory.CreatePlatformAdmin` |

El login ya está en el esquema de la sección 4 (usuario y hash en variables de entorno, sin Identity). `AccountService` volvió a su versión original, porque `CredentialVerifier` ya no se comparte.
| Api | `PlatformAuthController`, `PlatformController`, `PlatformAdminHandler`, `PlatformAdminRequirement`, `PlatformPolicies`, `PlatformCredentialsWarning`, `Commands/HashPasswordCommand`; `SessionTokenIssuer.IssuePlatformAdmin` |

Las consultas de uso, los DTOs y los controladores de consulta no dependen del login.

### 6.2 Etapas

| # | Etapa | Contenido | Esfuerzo |
|---|---|---|---|
| 1 | **Adaptar el backend a lo decidido** | **Login:** reescribir con usuario y hash en variables de entorno, verificación con `PasswordHasher`, freno por IP en memoria, huella del hash en el token y en la política, comando `hash-password`, falla cerrada sin configuración; deshacer la extracción de `CredentialVerifier`. **Uso:** excluir *No vino* en `WhereStandingReservation` (renombrar a algo como `WhereCountedTurn`). Reemplazar los períodos fijos por un `UsagePeriod` (desde, hasta, período anterior, granularidad) validado en dominio; `from`/`to` en los endpoints; ajustar DTOs (`ClubUsageDto` con turnos del período y del anterior, promedio, online, clientes). Serie agrupada en memoria por día o semana, a partir de las horas de inicio de un solo club. El estado del club sigue calculándose desde hoy. | 1,5–2 d |
| 2 | **Clientes por club** | `GET api/platform/clubs/{id}/customers` con búsqueda y paginación en la base; conteo de clientes en el resumen. | 0,5–1 d |
| 3 | **Tests de backend** | Dominio: `UsagePeriod` (período anterior, granularidad, rango inválido, cruce de mes y de año), `ClubActivityRules` (bordes de 14 días). API: login (usuario incorrecto, contraseña mala, sin configuración → siempre 401, bloqueo de IP tras 5 fallos y levantado a los 15 minutos, éxito), política (token de club → 403, token con huella de un hash anterior → 403 tras cambiar la contraseña), que ni la contraseña ni el hash aparezcan en los logs, resumen y detalle con datos sembrados en dos clubes (cifras exactas; bloqueos, cancelados y *No vino* excluidos; un turno a las 23:30 hora local cae en el día correcto), clientes (paginación, búsqueda, sin mezclar clubes). Sumar los endpoints a `RolePermissionsTests`. | 1,5 d |
| 4 | **Sesión de plataforma en la Web** | `PlatformSession` (como `CurrentSession`, con clave de almacenamiento propia), `ApiClient.Platform.cs`, rutas en `ApiRoutes` y `PageRoutes`, `PlatformLayout` con su barra y redirección a `/plataforma/ingresar` sin sesión o con sesión vencida. | 1 d |
| 5 | **Pantallas** | `PlatformLogin`; componente `PeriodSelector` (atajos, rango personalizado, sincronizado con la URL); `PlatformOverview` (totales, tabla, tarjetas en celular, filtro y buscador); `PlatformClub` (cifras, barras, canchas, lista de clientes con buscador y paginado). Cada una con su `.razor.cs`. | 2–2,5 d |
| 6 | **Configuración y operación** | Variables `Platform__AdminUser` y `Platform__AdminPasswordHash` en Railway o Render (sin valores en `appsettings.json`); sección en [OPERACION.md](OPERACION.md) con el alta, el reinicio de contraseña y la fila del rol en la tabla de permisos. | 0,5 d |
| 7 | **Cierre** | Recorrido en navegador y celular, fila M22 en [PLAN-DE-MODULOS.md](PLAN-DE-MODULOS.md), actualización de [PLAN-DE-ACCION.md](PLAN-DE-ACCION.md). | 0,5 d |

**Total estimado:** 7,5–9 días (con el backend ya empezado).

### 6.3 Listo cuando

- Solo entra quien tenga el usuario y la contraseña; cualquier otro intento recibe el mismo 401, y un token de club recibe 403 en `api/platform/*`.
- Cambiar `Platform__AdminPasswordHash` cierra las sesiones abiertas, y ni la contraseña ni el hash aparecen en los logs.
- Las cifras de un club para "Hoy" coinciden con lo que muestra su propia Agenda ese día (sin contar *No vino*).
- El resumen carga en menos de 1 s con 50 clubes y un rango de un año de turnos de prueba.
- La lista de clientes de un club con 5.000 clientes pagina y busca en menos de 1 s.
- Las tres pantallas se usan bien en celular.

---

## 7. Decisiones tomadas

| # | Tema | Decisión |
|---|---|---|
| 1 | Turnos *No vino* | **No se cuentan.** Se cuentan por día de juego. |
| 2 | Umbrales de estado | Se mantienen: 14 días para "Nuevo", 2 semanas sin turnos para "Inactivo". |
| 3 | Períodos | **Selector de fechas** con atajos y rango personalizado (sección 3.2), en resumen y detalle. |
| 4 | Acceso de admin | **Usuario genérico y contraseña con hash**, en variables de entorno del hosting. Sin cuenta de Identity ni mail. La contraseña se reinicia cambiando la variable y redeployando (sección 4.3). El hash no se escribe en logs. |
| 5 | Segundo factor | **No.** Alcanza con una contraseña larga, el freno de intentos por IP y las variables en Railway o Render. |
| 6 | Datos extra | **Lista sencilla de clientes** por club y cantidad de clientes en el listado. |
| 7 | Exportaciones | **No** por ahora. |

---

## 8. Riesgos

- **Una sola zona horaria:** si se agrega otro país, los días deben calcularse por club; el conteo deja de ser una sola consulta para todos. Hoy el país está fijo en Argentina.
- **Estadísticas de la base para la lista de clientes:** el orden por último turno es una subconsulta por cliente. Con estadísticas actualizadas (el autovacuum de Postgres) responde en unos 0,3 s con 5.000 clientes y 20.000 turnos. Recién cargada, sin `ANALYZE`, el planificador puede elegir mal y tardar varios segundos. Por eso los datos de prueba de rendimiento deben correr `ANALYZE` tras cargarse.
- **Lectura sin filtro de club:** un error en estas consultas mostraría datos cruzados solo a la plataforma, nunca a un club, porque los endpoints exigen la política `PlatformAdmin`. Los tests de la etapa 3 lo cubren.
- **Datos personales:** el panel muestra mail y teléfono de los dueños y de los clientes de cada club. No se exportan ni se loguean, y solo los ve el administrador.
