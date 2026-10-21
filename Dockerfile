FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ConCancha.slnx ./
COPY src/ConCancha.Domain/*.csproj src/ConCancha.Domain/
COPY src/ConCancha.Infrastructure/*.csproj src/ConCancha.Infrastructure/
COPY src/ConCancha.Api/*.csproj src/ConCancha.Api/
COPY src/ConCancha.Web/*.csproj src/ConCancha.Web/
COPY tests/ConCancha.Tests/*.csproj tests/ConCancha.Tests/
RUN dotnet restore
COPY . .
RUN dotnet publish src/ConCancha.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Railway inyecta PORT; ConnectionStrings__Default viene de las variables del servicio.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ConCancha.Api.dll"]
