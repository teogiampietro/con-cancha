# Operación

## Logs

- La API escribe a la salida estándar; en Railway se ven en la pestaña *Logs* de cada servicio.
- Eventos de negocio (nivel Information): turno creado, turno cancelado (panel u online), horario ocupado en una carrera, club creado.
- Seguridad (nivel Warning): contraseña incorrecta y login rechazado por bloqueo. Solo se registra el id de usuario, nunca mail ni teléfono.
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

## Revisión de permisos

| Rol | Puede | No puede |
|---|---|---|
| Dueño | Todo el panel, Canchas y precios, asistente de configuración, horario del club, equipo | — |
| Encargado | Agenda, Reservas, Recordar, Caja, Clientes, ver canchas y precios | Crear/editar canchas, precios, horario, asistente, equipo |

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
