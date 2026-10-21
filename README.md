# ConCancha

Gestión de turnos para clubes de canchas. Ver [ANALISIS-REFERENCIA.md](ANALISIS-REFERENCIA.md) y [PLAN-DE-ACCION.md](PLAN-DE-ACCION.md). La maqueta navegable está en [maqueta/index.html](maqueta/index.html).

## Estructura

- `src/ConCancha.Domain`: entidades y reglas de negocio, sin dependencias.
- `src/ConCancha.Infrastructure`: EF Core + PostgreSQL.
- `src/ConCancha.Api`: API web con Swagger.
- `src/ConCancha.Web`: Blazor Web App que consume la API.
- `tests/ConCancha.Tests`: pruebas.

## Desarrollo local

```sh
docker compose up -d                 # PostgreSQL en localhost:5432
dotnet run --project src/ConCancha.Api
```

- Swagger: `/swagger`
- Salud: `/health` (la app vive) y `/health/ready` (también responde la base)
- Tests: `dotnet test ConCancha.slnx`

La cadena de conexión está en `ConnectionStrings:Default`. En Railway se define con la variable `ConnectionStrings__Default`.
