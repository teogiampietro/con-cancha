# Estándares de código

Reglas obligatorias para todo el código del proyecto. Una revisión de código no se aprueba si las incumple.

## Idioma

1. Todo el código va en inglés: tipos, miembros, variables, nombres de archivos, rutas de API, mensajes de log, excepciones y claves de configuración.
2. La UI va en español: textos de páginas, mensajes de validación y errores que ve la persona usuaria, y las URLs de las páginas Blazor (`/panel/registro`). Las rutas de la API van en inglés.
3. Los mensajes de commit van en inglés.

## Diseño

4. Código limpio y principios SOLID. Una clase, una razón para cambiar; dependencias hacia abstracciones; sin lógica de negocio en controladores ni en componentes Blazor.
5. Nombres explícitos que revelan la intención: `GetActiveProductsByBranch`, no `GetProducts`.
6. Métodos cortos (menos de 20 líneas). Guard clauses en vez de anidamiento.
7. Sin números ni strings mágicos: usar constantes o enums.
8. Sin comentarios que expliquen el "qué". Solo el "por qué" cuando no es obvio.

## Asincronía

9. Todo método `async` recibe `CancellationToken` y lo propaga hasta el final de la cadena.
10. `ConfigureAwait(false)` en Application e Infrastructure. No se usa en código Blazor.
11. Prohibido `async void`.

## Tipos y datos

12. DTOs de request y response: `record` inmutables.
13. Colecciones de solo lectura: `IReadOnlyList<T>`. Nunca `List<T>` en una firma pública.
14. Timestamps: siempre `DateTimeOffset.UtcNow`. Nunca `DateTime.Now`.

## Blazor

15. Nunca `@code { ... }` embebido en un `.razor`.
16. Toda la lógica C# vive en `ComponentName.razor.cs`, como `partial class` en el namespace que refleja la carpeta (por ejemplo, `ConCancha.Web.Components.Pages` para `Components/Pages/Login.razor.cs`).
17. El `.razor` solo tiene markup y directivas (`@page`, `@inject`, `@using`).

## Commits

- Inglés, modo imperativo, primera línea de hasta 72 caracteres: `Add club slug validation`.
