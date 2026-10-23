# Operación

## Logs

- La API escribe a la salida estándar; en Railway se ven en la pestaña *Logs* de cada servicio.
- Eventos de negocio (nivel Information): turno creado, turno cancelado (panel u online), horario ocupado en una carrera, club creado.
- Seguridad (nivel Warning): contraseña incorrecta y login rechazado por bloqueo. Solo se registra el id de usuario, nunca mail ni teléfono.
- Panel de plataforma: ingreso correcto (Information) y rechazos o bloqueos por IP (Warning). Nunca aparecen el usuario, la contraseña ni el hash.
- Cualquier excepción no controlada la captura `UnhandledExceptionHandler`: queda en el log como Error con método, ruta y club, y la persona recibe un mensaje genérico en español (HTTP 500).
- Para más detalle en producción: variable `Logging__LogLevel__Default=Debug`.

## Copias de seguridad de la base

1. **Railway:** en el servicio PostgreSQL, pestaña *Backups*, activar copias programadas (diaria) y definir retención. Hacerlo antes de la Fase 6.
2. **Copia propia (antes de migraciones grandes o por semana):**
   ```sh
   DATABASE_URL="<URL pública de Railway>" scripts/backup-db.sh
   ```
   Requiere `pg_dump` 17 o superior. Los `.dump` están en `backups/` (ignorado por git).
3. **Restaurar** en una base vacía:
   ```sh
   pg_restore --no-owner --dbname "$DATABASE_URL" backups/concancha-AAAAMMDDTHHMMSSZ.dump
   ```
4. **Ensayo:** restaurar una copia en la base local (`docker compose up -d db`) y abrir el panel al menos una vez antes del piloto.

## Panel de plataforma (ConCancha)

Panel de uso de los clubes, para el administrador de ConCancha. Ver [PLAN-PANEL-PLATAFORMA.md](PLAN-PANEL-PLATAFORMA.md). Tiene un único administrador, con usuario y contraseña en variables de entorno; no usa cuentas de club ni mail.

**Variables** en el servicio de la API (Railway o Render), nunca en el repo:

| Variable | Valor |
|---|---|
| `Platform__AdminUser` | Usuario, por ejemplo `admin` |
| `Platform__AdminPasswordHash` | Hash de la contraseña (ver alta) |

Sin las dos variables el panel queda cerrado y la API avisa con un Warning al iniciar.

**Alta o cambio de contraseña:**

1. En la computadora propia, desde la raíz del repo:
   ```sh
   dotnet run --project src/ConCancha.Api -- hash-password
   ```
   Pide la contraseña (mínimo 16 caracteres) sin mostrarla y escribe la línea `Platform__AdminPasswordHash=...` solo en la terminal. No toca la base ni los logs.
2. Pegar esa línea en las variables del servicio de la API y redeployar.
3. Entrar por `/plataforma/ingresar`.

Cambiar el hash cierra al instante todas las sesiones de plataforma abiertas. La contraseña anterior deja de servir.

**Bloqueo por intentos:** 5 fallos desde una misma IP en 15 minutos la bloquean 15 minutos (HTTP 429). El bloqueo vive en memoria: un reinicio del servicio lo levanta.

**Si se olvida la contraseña:** se reinicia desde el hosting. No hay mail de recuperación. Repetir el paso 1 con una contraseña nueva, reemplazar `Platform__AdminPasswordHash` y redeployar.

**Datos personales:** el panel muestra mail y teléfono de dueños y clientes. No se exportan ni se registran en logs.

## Revisión de permisos

| Rol | Puede | No puede |
|---|---|---|
| Dueño | Todo el panel, Canchas y precios, asistente de configuración, horario del club, equipo | — |
| Encargado | Agenda, Reservas, Recordar, Caja, Clientes, ver canchas y precios | Crear/editar canchas, precios, horario, asistente, equipo |
| Administrador de la plataforma (usuario de ConCancha) | Panel de plataforma: clubes, canchas, turnos por período y clientes de cada club | Cualquier endpoint de club; un token de club no abre la plataforma |

`RolePermissionsTests` recorre cada endpoint con dueño, encargado, cuenta sin club y sin sesión, y verifica por reflexión que toda acción declare `[Authorize]` o `[AllowAnonymous]`.

## Pruebas en celular (lista de control)

Con `docker compose up -d --build`, abrir `http://<IP de la computadora>:5090` desde el celular en la misma red.

- [ ] Registro, alta del club y asistente de configuración hasta el resumen.
- [ ] Agenda: cargar, bloquear, cobrar, jugado, no vino y cancelar desde la hoja inferior.
- [ ] Reservas, Clientes (exportar CSV), Caja (imprimir) y Recordar (abre WhatsApp).
- [ ] Sitio público `/{slug}`: elegir día y cancha, reservar con nombre y WhatsApp.
- [ ] Link de gestión `/{slug}/turno/{token}`: ver y cancelar dentro y fuera de la ventana.
- [ ] Con red lenta (modo 3G del navegador): el aviso de reconexión aparece y la pantalla se recupera.
- [ ] Encargado: no ve "Canchas" y las rutas de dueño lo devuelven a la Agenda.
