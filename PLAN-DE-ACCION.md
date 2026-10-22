# Plan de acción: de la maqueta al MVP

Base: [ANALISIS-REFERENCIA.md](ANALISIS-REFERENCIA.md) y [maqueta/index.html](maqueta/index.html).

## 1. Decisiones tomadas

| Tema | Decisión |
|---|---|
| Alcance MVP | Canchas, precios, agenda, turnos; reserva pública anónima; Clientes, Reservas, Caja, Recordatorios |
| Arquitectura | API Web (Swagger) + Blazor Web App que consume la API |
| Auth / multi-club | ASP.NET Identity, una BD, `ClubId` con filtro global de EF, roles por membresía (dueño / encargado) |
| Avisos | Enlaces `wa.me` con mensaje armado, envío manual |
| Grilla | Horario de apertura/cierre **por club** (default 8:00–00:00) con excepción por cancha (opcional); duración del turno por cancha; los slots libres se calculan, no se guardan |
| Doble reserva | Constraint de exclusión en PostgreSQL (cancha + rango de tiempo); cliente identificado por WhatsApp normalizado |
| Antifraude | Límite de turnos activos por teléfono + ventana de cancelación (default 12 h) |
| Nombre | **ConCancha** (proyectos `ConCancha.*`); dominio disponible |
| Precios | En el alta se piden **por deporte** (duración, precio y variante "desde las 18 hs") y se copian a cada cancha de ese deporte. Después se ajustan **por cancha** con franjas por grupo de días y horario (ej. vie–dom más caro). No hay reglas "para todas las canchas". Gana la franja con días específicos y, si empatan, la de horario más angosto. El precio de un turno puede modificarse al cargarlo o desde su detalle |
| Límite de turnos activos | 2 por teléfono |
| Turnos y medianoche | Terminan siempre antes de las 00:00 |
| Cancelación anónima | Link de gestión con token enviado al reservar |
| Caja | "Cerrar" solo informa, no bloquea el día |
| Alta de club | Autoservicio guiado, como en la app real: cuenta (mail + contraseña ≥10) → nombre y link (slug auto, editable) → teléfono; luego asistente de configuración: deportes → canchas por deporte → horario del club → duración y precio por deporte → resumen con "Cambiar". Slugs reservados y límite de registros. El progreso se guarda y se retoma. Seña, buffet y torneos no entran en el MVP; se activarán desde Configuración. Sin encuesta ni referido por ahora. País fijo Argentina |
| Entorno | Railway, Argentina, ARS, hora guardada en UTC + zona del club |

## 2. Fuera del MVP (después)

Señas con comprobante, lista de espera, turnos fijos, cuenta de jugador, puntos y premios, "busco jugadores", buffet, torneos, asistente IA, planes y cobro, push, verificación de mail, multi-país. Del alta de la app real no se toma la encuesta "¿cómo tomás los turnos hoy?" ni el campo de referido; las preguntas de seña, buffet y torneos pasan a activaciones posteriores.

## 3. Decisiones que siguen abiertas

| # | Pregunta | Fase |
|---|---|---|
| 1 | Comprar dominio y reservar usuarios de redes de ConCancha | 6 |
| 2 | Verificación de mail al registrarse (la maqueta no la simula) | 1 |
| 3 | Límite de registros por IP/dispositivo contra abuso del autoservicio | 1 |
| 4 | Encuesta "¿cómo tomás los turnos hoy?" y campo de referido del alta: evaluar si aportan para marketing | 6 |
| 5 | Cómo se ajustan en la app real los precios por cancha y por día después del alta (no se recorrió). Resuelto en Fase 2 con la decisión propia de la sección 1 (franjas por cancha); validar con el club piloto | 2 |

## 4. Fases

### Fase 0: Esqueleto (1–2 días)
- Solución .NET 10 con proyectos Domain, Infrastructure (EF Core + Npgsql), Api, Web, Tests.
- PostgreSQL local con Docker Compose; Swagger activo; health check.
- CI básico (build + tests) y despliegue vacío en Railway.
- **Listo cuando:** `dotnet test` verde y la API responde en Railway.

### Fase 1: Identidad y multi-club (3–4 días)
- Entidades: Club (slug único), MiembroDelClub (rol), usuario Identity.
- Login/logout, recuperar contraseña, mínimo 10 caracteres.
- Filtro global por `ClubId` y resolución del club por usuario (panel) o por slug (público).
- Alta de club por autoservicio, igual a la maqueta, tramo cuenta y club (3 pasos): registro (mail + contraseña ≥10, rechazo de contraseñas filtradas) → nombre y link (slug auto y editable, validado: 3–30 caracteres, sin reservados, único, disponibilidad en vivo) → teléfono. País fijo Argentina.
- **Listo cuando:** un dueño entra y solo ve datos de su club; test que prueba que otro club no se filtra.

### Fase 2: Canchas, precios y agenda (1 semana)
- Entidades: Club (apertura y cierre por defecto), Cancha (deporte, duración, apertura/cierre opcionales como excepción), ReglaDePrecio (CanchaId obligatorio, días de la semana opcionales, desde, hasta, precio).
- Asistente de configuración inicial, igual a la maqueta (progreso "Paso N de M", persistente): deportes → canchas por deporte → horario del club → duración y precio por deporte (con variante desde las 18 hs) → resumen con "Cambiar". Al confirmar crea las canchas ("Pádel 1", "Pádel 2"…) y copia el precio del deporte a cada una como regla base y, si corresponde, regla nocturna.
- Servicio de generación de slots y resolución de precio.
- Turno (reserva | bloqueo, estado, pago, método, no-show, motivo), con constraint de exclusión `tstzrange` por cancha para turnos no cancelados.
- Endpoints + pantallas: Canchas, Precios, Agenda (cargar, bloquear, detalle, pagar, jugado, no vino, cancelar).
- **Listo cuando:** dos cargas simultáneas al mismo horario dejan una sola reserva (test de concurrencia) y la agenda replica la maqueta.
- **Estado:** backend y tests hechos (constraint de exclusión, concurrencia, aislamiento por club, roles). Pantallas Blazor (asistente, Canchas con precios, Agenda) implementadas; falta recorrerlas en navegador y celular contra la maqueta.

### Fase 3: Reserva pública (1 semana)
- Sitio `/{slug}`: selector de 7 días, selector de cancha, lista de turnos, hoja de reserva (nombre, WhatsApp, mail opcional).
- Cliente (Clientes) creado/unido por teléfono normalizado.
- Límite de turnos activos, link de gestión con token, cancelación dentro de la ventana.
- Limitación de tasa en endpoints públicos.
- **Listo cuando:** un jugador reserva y cancela desde el celular, y el turno aparece en la agenda del club.
- **Estado:** backend y tests hechos (reserva anónima, cliente por teléfono normalizado, límite de 2 turnos activos, token de gestión guardado como hash, ventana de cancelación configurable por club, límite de tasa). Sitio `/{slug}` y `/{slug}/turno/{token}` implementados; falta recorrerlos en celular real. La ventana de cancelación se edita desde Configuración (módulo M3).

### Fase 4: Gestión diaria (1 semana)
- Reservas (filtros Hoy/7/30/Historial, A cobrar, Online, búsqueda).
- Clientes (visitas, gasto, deuda, faltas; filtros; exportar Excel/PDF).
- Caja (cierre del día por método, imprimir).
- Recordatorios (Mañana/Hoy con `wa.me`).
- Agenda: indicadores Ocupación / Entró / Se escapa con las fórmulas de la maqueta.
- **Listo cuando:** un día de prueba se carga, se cobra y la Caja cuadra con lo cargado.
- **Estado:** backend, tests y pantallas hechos (Reservas, Clientes, Caja, Recordar, indicadores de la agenda). El test de la Caja valida que efectivo + transferencia + falta cobrar cuadran con lo cargado. "Excel" exporta un CSV que abre en Excel y "PDF" usa la impresión del navegador (guardar como PDF); un `.xlsx` y un PDF generados en servidor quedan fuera hasta que el piloto los pida. Falta recorrer las pantallas en navegador y celular.

### Fase 5: Calidad y endurecimiento (3–4 días)
- Revisión de permisos por rol (encargado no ve configuración).
- Tests de reglas (precio, slots, ventana de cancelación, aislamiento por club).
- Logs, manejo de errores, backups de BD en Railway.
- Pruebas en celular.
- **Estado:** hecho en código: matriz de permisos por rol probada endpoint por endpoint (dueño / encargado / sin club / anónimo) y un test que exige declarar el acceso de cada acción (encontró cinco endpoints públicos anónimos solo por omisión; ahora son `[AllowAnonymous]` explícitos); manejador global de errores (500 genérico en español, detalle solo en logs); logs de eventos de negocio sin datos personales; tests extra de reglas (turnos sin cruzar medianoche, ventana de cancelación por club); script de backup y guía en [docs/OPERACION.md](docs/OPERACION.md). Pendiente de hacer a mano: activar backups programados en Railway, probar una restauración y recorrer el checklist en un celular real.

### Módulo M1: Navegación y estructura del panel (ver [PLAN-DE-MODULOS.md](PLAN-DE-MODULOS.md))
- **Estado:** hecho en código: `PanelLayout` con menú lateral agrupado (Hoy / El club), barra inferior móvil con hoja "Todo el panel", marca del club, "Ver mi página", Cerrar sesión y `ShareLinkButton` (menú, hoja y Agenda); catálogo `PanelNavItems` por rol (el encargado no ve Canchas) con tests; `PanelHeader` eliminado. Recorrido en navegador a 1280 y 390 px como dueño. Sin recorrer: copiar el link (portapapeles), cerrar sesión, vista de encargado en navegador, celular real. Los ítems de módulos futuros (Espera, Asistente, Crecer) no aparecen hasta que existan.

### Módulo M3: Configuración del club (ver [PLAN-DE-MODULOS.md](PLAN-DE-MODULOS.md))
- **Estado:** hecho en código: `/panel/configuracion` (solo dueño) con pestañas Marca (nombre y teléfono; el slug no se edita, decisión confirmada), Cancelación (ventana de 0 a 168 horas) y Tu cuenta (cambiar contraseña con el rechazo de contraseñas filtradas); `PUT api/club/profile`, `PUT api/club/cancellation` y `POST api/auth/change-password` en la matriz de permisos; `ClubDto` suma `CancellationWindowHours`; ítem "Configuración" en el menú del dueño. Sin migración (no hay entidades nuevas). Tests de API, dominio y navegación en verde. Recorrido en navegador a 1280 y 390 px como dueño, y como encargado (no ve el ítem y la URL directa lo manda a la Agenda). Sin recorrer: celular real, el rechazo de rango en pantalla (lo frena el navegador antes de enviar) y la contraseña filtrada contra el servicio real (el test usa uno simulado).

### Fase 6: Piloto (continuo)
- Un club real, feedback semanal, ajustar reglas abiertas (sección 3).
- Recién después elegir el siguiente módulo (señas es el candidato natural).

## 5. Riesgos

- **Reglas no confirmadas** del análisis marcadas [Maq]: validarlas con el club piloto antes de endurecerlas.
- **Zona horaria**: todo en UTC, convertir a la zona del club solo en la capa de presentación.
- **Fuga entre clubes**: el filtro global es la defensa principal; hay que testearlo desde la fase 1.
- **Blazor Server en celular con mala red**: medir en piloto; la API permite cambiar el cliente después.
