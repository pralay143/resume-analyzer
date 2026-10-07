# Build context: the backend/ folder.
#   docker build -t resume-analyzer-api backend
#   docker run -p 8080:8080 --env-file <your-env-file> resume-analyzer-api

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, in its own layer, so code changes don't re-download packages.
COPY src/ResumeAnalyzer.Api/ResumeAnalyzer.Api.csproj src/ResumeAnalyzer.Api/
RUN dotnet restore src/ResumeAnalyzer.Api/ResumeAnalyzer.Api.csproj

COPY src/ResumeAnalyzer.Api/ src/ResumeAnalyzer.Api/
RUN dotnet publish src/ResumeAnalyzer.Api/ResumeAnalyzer.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
# Listens on 8080 by default; on Render the app switches to the PORT it is given.
EXPOSE 8080

COPY --from=build /app/publish .

# The image's built-in non-root user.
USER $APP_UID

ENTRYPOINT ["dotnet", "ResumeAnalyzer.Api.dll"]
