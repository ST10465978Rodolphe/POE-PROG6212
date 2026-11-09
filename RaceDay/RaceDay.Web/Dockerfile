# =====================================================
# RaceDay.Web (ASP.NET Core MVC) - Docker image
#
# Build it from the RaceDay folder (the one that contains RaceDay.slnx):
#   docker build -f RaceDay.Web/Dockerfile -t raceday-web .
#
# Change DOTNET_VERSION so it matches <TargetFramework> in RaceDay.Web.csproj
# (net8.0 -> 8.0, net9.0 -> 9.0, net10.0 -> 10.0).
# =====================================================
ARG DOTNET_VERSION=10.0

# ---------- Stage 1: build and publish the application ----------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Copy only the project files first, so the restored packages are cached
# and are not downloaded again unless a project file changes.
COPY ["RaceDay.Web/RaceDay.Web.csproj", "RaceDay.Web/"]
COPY ["RaceDay.Contracts/RaceDay.Contracts.csproj", "RaceDay.Contracts/"]
RUN dotnet restore "RaceDay.Web/RaceDay.Web.csproj"

# Now copy the source code and publish
COPY RaceDay.Web/ RaceDay.Web/
COPY RaceDay.Contracts/ RaceDay.Contracts/
RUN dotnet publish "RaceDay.Web/RaceDay.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------- Stage 2: run the published application ----------
# The runtime image is much smaller than the SDK image used above.
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app
COPY --from=build /app/publish .

# The site listens on port 8080 inside the container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Where the RaceDay API is. This is only a default: "docker run" overrides it
# with -e Api__BaseUrl=... (see the README).
ENV Api__BaseUrl=http://host.docker.internal:5123/

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]