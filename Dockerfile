# syntax=docker/dockerfile:1
# Architectures : linux/amd64 (serveurs x86) et linux/arm64 (Raspberry Pi 4/5, Freebox Ultra VM)
#
# Build mono-plateforme :
#   docker build -t boldesangmanager:latest .
#
# Build multi-arch (push vers un registry) :
#   docker buildx build --platform linux/amd64,linux/arm64 \
#     -t votre-registre/boldesangmanager:latest --push .

# ── Étape build (SDK, sur l'architecture hôte pour la vitesse) ─────────────────
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH
WORKDIR /src

# Restore séparé pour le cache Docker
COPY src/BolDeSangManager/BolDeSangManager.csproj ./
RUN dotnet restore -a $TARGETARCH

# Compilation et publication
COPY src/BolDeSangManager/ ./
RUN dotnet publish -c Release -o /app/publish -a $TARGETARCH --no-restore

# ── Image finale (runtime uniquement, ~200 Mo) ─────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Polices Noto pour le rendu PDF (QuestPDF/SkiaSharp utilise les polices système sur Linux)
RUN apt-get update \
 && apt-get install -y --no-install-recommends fonts-noto-core fonts-noto-extra \
 && rm -rf /var/lib/apt/lists/*

# Volume pour la base SQLite (monter un volume nommé ou un répertoire hôte)
VOLUME ["/data"]

# Variables d'environnement par défaut — toutes surchargeable dans docker-compose ou via -e
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__DefaultConnection="Data Source=/data/boldesang.db;Cache=Shared" \
    DataProtection__KeysPath=/data/DataProtection-Keys \
    BolDeSang__AdminEmail=commissaire@boldesang.fr \
    BolDeSang__AdminPassword=Commissaire123! \
    BolDeSang__AdminPseudo="Grand Commissaire"

EXPOSE 8080

ENTRYPOINT ["dotnet", "BolDeSangManager.dll"]
