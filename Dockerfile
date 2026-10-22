# Una sola imagen para los dos servicios. Se elige cuál con el argumento PROJECT:
#   docker build --build-arg PROJECT=ConCancha.Api .   (por defecto)
#   docker build --build-arg PROJECT=ConCancha.Web .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT=ConCancha.Api
WORKDIR /src
COPY ConCancha.slnx ./
COPY src/ConCancha.Domain/*.csproj src/ConCancha.Domain/
COPY src/ConCancha.Contracts/*.csproj src/ConCancha.Contracts/
COPY src/ConCancha.Application/*.csproj src/ConCancha.Application/
COPY src/ConCancha.Infrastructure/*.csproj src/ConCancha.Infrastructure/
COPY src/ConCancha.Api/*.csproj src/ConCancha.Api/
COPY src/ConCancha.Web/*.csproj src/ConCancha.Web/
COPY tests/ConCancha.Tests/*.csproj tests/ConCancha.Tests/
RUN dotnet restore
COPY . .
RUN dotnet publish src/${PROJECT} -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG PROJECT=ConCancha.Api
ENV PROJECT=${PROJECT}
WORKDIR /app
COPY --from=build /app .
# En Render el puerto se fija con PORT=8080 (ver render.yaml); las demás variables
# (ConnectionStrings__Default, Jwt__Key, Api__BaseUrl, Web__BaseUrl, Database__MigrateOnStartup) vienen del servicio.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet ${PROJECT}.dll"]
