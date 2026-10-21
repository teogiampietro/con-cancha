# ConCancha

Gestión de turnos para clubes de canchas. Ver [ANALISIS-REFERENCIA.md](ANALISIS-REFERENCIA.md) y [PLAN-DE-ACCION.md](PLAN-DE-ACCION.md). La maqueta navegable está en [maqueta/index.html](maqueta/index.html).

Antes de contribuir, lee [ESTANDARES-DE-CODIGO.md](ESTANDARES-DE-CODIGO.md).

## Estructura

- `src/ConCancha.Domain`: entidades y reglas de negocio, sin dependencias.
- `src/ConCancha.Contracts`: DTOs de request/response (records inmutables).
- `src/ConCancha.Application`: casos de uso (interfaces), resultados y puertos.
- `src/ConCancha.Infrastructure`: implementa los casos de uso con EF Core + PostgreSQL + Identity.
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
