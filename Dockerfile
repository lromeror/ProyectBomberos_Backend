# Build de emergencia para el despliegue temporal en Hetzner (Linux + Docker).
# El despliegue "real" 24/7 sigue siendo la máquina Windows + IIS documentada en
# DEPLOY.md — este Dockerfile es solo para tener la API arriba rápido mientras tanto.

# ---- Etapa 1: build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BomberosAPI.slnx ./
COPY Directory.Build.props ./
COPY src/BomberosAPI.Domain/BomberosAPI.Domain.csproj src/BomberosAPI.Domain/
COPY src/BomberosAPI.Application/BomberosAPI.Application.csproj src/BomberosAPI.Application/
COPY src/BomberosAPI.Infrastructure/BomberosAPI.Infrastructure.csproj src/BomberosAPI.Infrastructure/
COPY src/BomberosAPI.API/BomberosAPI.API.csproj src/BomberosAPI.API/
RUN dotnet restore src/BomberosAPI.API/BomberosAPI.API.csproj

COPY src/ src/
RUN dotnet publish src/BomberosAPI.API/BomberosAPI.API.csproj -c Release -o /app/publish --no-restore

# ---- Etapa 2: runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# No correr como root dentro del contenedor. La imagen base ya trae el usuario/grupo
# "app" (uid/gid 64198) sin crearlo a mano.
RUN chown -R app:app /app
USER app

# HTTP plano puertas adentro del contenedor (sin certificado configurado, así que
# UseHttpsRedirection queda como no-op — ver DEPLOY-DOCKER.md). No hace falta tocar
# Program.cs: sin ASPNETCORE_HTTPS_PORT ni endpoint HTTPS configurado, ese middleware
# simplemente registra una advertencia y deja pasar la petición.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BomberosAPI.API.dll"]
