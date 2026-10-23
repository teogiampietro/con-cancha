# Plan de desarrollo: nuevos planes y su gestión

Reemplaza el alcance del módulo M20 de [PLAN-DE-MODULOS.md](PLAN-DE-MODULOS.md) ("Planes y cobro") en lo que respecta a los tipos de plan y a cómo los administra ConCancha desde [/plataforma](PLAN-PANEL-PLATAFORMA.md). Se rige por [ESTANDARES-DE-CODIGO.md](ESTANDARES-DE-CODIGO.md).

Este documento cubre solo dos cosas: **qué incluye cada plan** y **cómo se gestiona desde el panel de plataforma**. Las funciones del plan Pro (asistente, torneos y buffet) tienen cada una su propio módulo y no se describen acá.

---

## 1. Planes

| Plan | Precio | Vigencia | Canchas activas | Qué incluye |
|---|---|---|---|---|
| **Demo** | Gratis | 14 días desde que termina el alta | Hasta **4** | Todo lo del plan Base |
| **Base** | **$40.000** por mes | 30 días por pago, se renueva solo | Hasta **6** | Gestión de canchas, clientes, reservas, recordatorios por WhatsApp y caja |
| **Pro** | *Próximamente* | — | Hasta **10** (más de 10: contactar) | Todo lo del plan Base, más asistente de chat, torneos y buffet |

### 1.1 Reglas de los planes

- **El límite cuenta canchas activas.** Una cancha desactivada no suma. Así un club puede tener más canchas cargadas que su límite, siempre que las demás estén desactivadas.
- **Demo es la prueba de siempre.** La vigencia, el inicio y el vencimiento siguen las reglas actuales de `SubscriptionRules` (14 días desde que termina el alta). Cuando vence, el panel se cierra hasta que el club tenga un plan pago, igual que hoy.
- **Pro se muestra pero no se vende.** Mientras esté *Próximamente* no se puede asignar a ningún club desde la plataforma (ver decisión 3).
- **Subir de plan** aplica de inmediato. **Bajar de plan** solo se permite si el club tiene activas no más canchas que el límite del plan nuevo; si no, la plataforma lo rechaza y muestra cuántas canchas hay que desactivar. No se borra ninguna cancha ni ningún turno.
- **Un plan no cambia por sí solo.** Salvo la renovación de Base, todo cambio lo hace el administrador de la plataforma, después de confirmar el pago fuera de la app.

### 1.2 Precios y cobro

El cobro sigue siendo manual, como hoy: el club paga por transferencia o en efectivo y el administrador lo registra en `/plataforma`. No hay pasarela de pago en este módulo.

Cada pago registrado guarda el monto, el método y el plan cubierto. Así el precio puede cambiar sin tocar los pagos ya hechos.

---

## 2. Gestión desde `/plataforma`

Todo lo de esta sección lo hace el administrador de la plataforma. Un club no ve ni cambia su plan desde esas pantallas. Las URLs siguen las del panel existente.

### 2.1 Resumen de clubes — `/plataforma`

- Nueva columna **Plan** con un chip (*Demo*, *Base*, *Pro*). Los clubes sin plan vigente (prueba vencida) muestran *Vencido* en rojo.
- Nueva columna **Canchas** que muestra `activas / límite`, por ejemplo `5 / 6`. Si el club supera el límite (caso de clubes anteriores, ver decisión 2) el número se marca.
- Filtro por plan, además de los filtros actuales por estado.

### 2.2 Detalle de club — `/plataforma/clubes/{id}`

Nueva tarjeta **Plan y suscripción** arriba de las cifras de uso, con:

- Plan actual, estado (*En prueba*, *Vencido*, *Al día*, *Pago pendiente*) y fecha de vencimiento.
- Canchas activas frente al límite del plan.
- Último pago registrado: fecha, monto y método.

Desde la tarjeta, cuatro acciones:

| Acción | Qué hace | Cuándo está disponible |
|---|---|---|
| **Cambiar plan** | Elige Base (o Pro cuando esté disponible). Pide confirmar. Si baja y hay más canchas activas que el límite nuevo, lo rechaza con el número de canchas a desactivar | Siempre, salvo Pro mientras sea *Próximamente* |
| **Registrar pago** | Pide plan (por defecto el actual), monto (por defecto el precio vigente), método (transferencia o efectivo) y fecha del pago. Cubre 30 días desde la fecha del pago o desde el vencimiento de lo ya pagado, lo que sea posterior | Siempre |
| **Extender prueba** | Suma días a la prueba (1 a 365, como hoy) | Solo con plan Demo |
| **Enviar aviso de regularización** | Manda el mail al dueño por prueba o pago vencido (como hoy) | Solo si el estado es *Vencido* o *Pago pendiente* |

### 2.3 Historial del club

Debajo de la tarjeta, una lista cronológica de **todo lo que se hizo con la suscripción**: cambios de plan, pagos registrados, pruebas extendidas y avisos enviados. Cada fila tiene fecha, tipo, plan, monto si corresponde y el usuario de la plataforma que lo hizo (hoy es uno solo, pero queda registrado).

El historial es la única fuente para responder "¿cuánto pagó este club y cuándo?". No se edita ni se borra: si un pago se cargó mal, se registra un ajuste.

### 2.4 Configuración de precios

El precio de Base y los límites de cada plan viven como constantes en `ClubPlanRules` (Domain). Cambiar un precio o un límite es un cambio de código y un deploy. Se decide así porque hoy hay un solo precio y un solo administrador; si se necesitan promociones o precios por club, se revisa (decisión 7).

---

## 3. Efecto en el panel del club

Lo que ve el club, pensado para que nadie tenga que escribirle a ConCancha para entender su límite.

| Pantalla | Cambio |
|---|---|
| **Suscripción** (`/panel/suscripcion`) | Muestra el plan actual, el límite de canchas y cuántas tiene activas. Para subir de plan dice "Escribinos y te pasamos los datos" (sin pasarela). Pro aparece como *Próximamente* |
| **Canchas** (`/panel/canchas`) | Muestra `activas / límite` arriba. Al crear o reactivar una cancha por encima del límite, el sistema responde con un mensaje claro, por ejemplo: "Tu plan Demo permite hasta 4 canchas activas. Para sumar más, pasá al plan Base." |
| **Canchas, al bajar de plan** | No aplica: el club no baja de plan desde la app. Si el administrador lo intenta con más canchas activas, la plataforma lo rechaza (ver 1.1) |

Las validaciones de límite van en Application, no en la pantalla: la API es la que responde, y la Web solo muestra el mensaje.

---

## 4. Modelo y API

### 4.1 Dominio

- `ClubPlan` (enum): `Demo`, `Base`, `Pro`.
- `ClubPlanRules`: límite de canchas por plan, precio vigente de Base, y si un plan puede asignarse hoy (Pro, mientras esté *Próximamente*, no).
- `SubscriptionRules` sigue igual para vigencias y estados; el plan se suma a lo que ya guarda la suscripción.
- Regla de bajada: `CanChangeTo(ClubPlan target, int activeCourts)`.

### 4.2 Persistencia

- Columna `Plan` en la suscripción del club. Los clubes con prueba arrancan en `Demo`.
- Tabla `SubscriptionEvent`: tipo (`PlanChanged`, `PaymentRecorded`, `TrialExtended`, `RegularizationSent`), fecha, plan, monto (si aplica), método (si aplica) y usuario de la plataforma. Es el historial de la sección 2.3.
- Migración: los clubes con el "Plan Completo" actual pasan a `Base`. Ver decisión 1.

### 4.3 API

Todo bajo `api/platform/clubs/{clubId}` con la política `PlatformAdmin`, igual que las rutas de suscripción existentes:

| Método y ruta | Qué hace |
|---|---|
| `PUT plan` | Cambia el plan. 409 si la bajada supera el límite (con las canchas activas en el mensaje). 422 si el plan es Pro mientras esté *Próximamente* |
| `POST subscription/payment` | Ya existe: se amplía con `Plan`, `Amount`, `Method` y `PaidOn` |
| `POST trial/extension` | Ya existe: sin cambios, solo con plan Demo |
| `GET events` | Historial paginado |

`ClubUsageDto` suma `Plan`, `ActiveCourts` y `CourtLimit`. El resumen sigue en una sola consulta agrupada.

Club-side, la validación de cancha nueva o reactivada se agrega al servicio de canchas; la API responde 409 con el mensaje en español.

---

## 5. Plan de desarrollo

Las etapas son independientes entre sí salvo donde se indica. Estimaciones en días de una persona, siguiendo las convenciones de [PLAN-DE-MODULOS.md](PLAN-DE-MODULOS.md).

| # | Etapa | Contenido | Esfuerzo | Depende de |
|---|---|---|---|---|
| 0 | **Home y textos** | Sección de planes del home con Demo, Base y Pro (*Próximamente*, sin compra). FAQ de canchas actualizada | 0,5 d | — |
| 1 | **Dominio de planes** | `ClubPlan`, `ClubPlanRules`, regla de bajada. Tests de límites por plan, bajada permitida y rechazada, Pro *Próximamente* | 1 d | — |
| 2 | **Límite de canchas** | Validación en crear y reactivar cancha (Application y API), mensaje en `CourtMessages`, `activas / límite` en la lista de canchas. Tests: límite exacto, cancha inactiva no cuenta, club que ya supera el límite conserva sus canchas | 1–1,5 d | 1 |
| 3 | **Persistencia** | Columna `Plan`, tabla `SubscriptionEvent`, migración de clubes actuales a Base. Tests de migración y de historial | 1 d | 1 |
| 4 | **API de plataforma** | `PUT plan`, `POST payment` ampliado, `GET events`, DTOs. Tests de API: 401 y 403 según política, aislamiento entre clubes, 409 y 422. Sumar a la matriz de permisos | 1,5 d | 3 |
| 5 | **Pantallas de plataforma** | Chips y columnas en el resumen, tarjeta **Plan y suscripción**, diálogos de cambio de plan y de pago, historial. Cada una con su `.razor.cs` | 2 d | 4 |
| 6 | **Panel del club** | Plan y límite en Suscripción y Canchas, mensaje de límite, Pro *Próximamente* | 0,5–1 d | 2, 4 |
| 7 | **Cierre** | Recorrido en navegador y celular (home, plataforma, panel del club), sección en [OPERACION.md](OPERACION.md) para registrar pagos y cambiar planes, actualización de M20 en [PLAN-DE-MODULOS.md](PLAN-DE-MODULOS.md) y de [PLAN-DE-ACCION.md](PLAN-DE-ACCION.md) | 0,5 d | todas |

**Total estimado:** 7,5–9 días. La etapa 0 puede salir sola, antes que el resto, porque no depende de nada.

### 5.1 Listo cuando

- El home muestra Demo, Base y Pro; Pro dice *Próximamente* y no tiene botón de compra.
- Un club Demo no puede tener más de 4 canchas activas, ni reactivar una cuando ya tiene 4; un club Base, no más de 6.
- El administrador cambia el plan de un club y el cambio aparece en el historial con fecha y monto.
- Bajar de plan con más canchas activas que el límite nuevo es rechazado y el mensaje dice cuántas desactivar.
- Un club que ya tenía más canchas que su límite las conserva y no puede sumar más.
- Ningún cambio de plan ni de pago deja sin historial.

---

## 6. Decisiones

Se tomaron las recomendaciones salvo donde se indica que hace falta confirmar. Las marcadas como **pendiente** tienen que resolverse antes de la etapa que las usa.

| # | Tema | Decisión | Estado |
|---|---|---|---|
| 1 | Clubes que hoy pagan el "Plan Completo" de $50.000 | Pasan a **Base** y pagan $40.000 desde su **próximo período** (no a mitad de mes). Se respeta el precio que ya pagaron hasta esa fecha | **Pendiente** de confirmar |
| 2 | Clubes que ya tienen más de 6 canchas activas | Conservan sus canchas y **no pueden sumar** más hasta bajar por debajo del límite | Recomendado; **pendiente** |
| 3 | Pro asignable a mano antes de salir | **No.** Mientras esté *Próximamente*, el administrador no puede asignarlo | Recomendado; **pendiente** |
| 4 | "Atención personalizada por WhatsApp" (estaba en el plan de $50.000) | **Sale de los planes nuevos**: no figura en Base ni en Pro, porque no estaba en la lista de funciones | **Pendiente**: confirmar si se mantiene como beneficio |
| 5 | Qué incluye Pro | **Todo lo de Base**, más asistente, torneos y buffet | Recomendado; **pendiente** |
| 6 | Cambio de plan a mitad de período | Aplica de inmediato, **sin prorrateo**. El cobro de la diferencia se registra a mano | Recomendado |
| 7 | Dónde viven los precios | Constantes en `ClubPlanRules`. Cambiar precio es un deploy | Recomendado; se revisa si aparecen precios por club |
| 8 | Qué cuenta como cancha para el límite | Solo las **activas** | Confirmado |
| 9 | Plan Demo al vencer | Igual que hoy: el panel se cierra hasta elegir un plan pago | Confirmado |

---

## 7. Riesgos

- **Precio duplicado:** el precio aparece en el home (`LandingContent`) y en `ClubPlanRules`. Si cambia uno y no el otro, el home miente. Mientras sean constantes en el código, conviene que el home lea el precio de `ClubPlanRules`. Se corrige en la etapa 0 o en la 1.
- **Límites y canchas en carrera:** dos altas simultáneas pueden pasar el límite si cada una cuenta antes de guardar. La validación tiene que hacerse dentro de la misma transacción que el alta, igual que el resto de las reglas de concurrencia de la agenda.
- **Pagos mal cargados:** como no hay pasarela, un error de monto o de fecha queda solo en el historial. Por eso el historial no se edita y las correcciones son eventos nuevos.
