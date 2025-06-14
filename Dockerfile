# Stage 1: Build stage with Playwright + dependencies
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install system dependencies and Playwright (including Chromium)
RUN apt-get update && apt-get install -y \
    wget gnupg ca-certificates curl \
    libnss3 libatk-bridge2.0-0 libxss1 libasound2 libgbm1 libgtk-3-0 \
    libxshmfence1 libxcomposite1 libxrandr2 libxdamage1 libxtst6 libdrm2 \
    libglib2.0-0 libglu1-mesa libpango-1.0-0 libpangocairo-1.0-0 \
    fonts-liberation libappindicator3-1 xdg-utils \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && npm install -g playwright \
    && npx playwright install chromium \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Restore and build your .NET project
COPY ["firnal.dashboard.api.csproj", "."]
RUN dotnet restore "./firnal.dashboard.api.v2.csproj"
COPY . .
RUN dotnet build "./firnal.dashboard.api.v2.csproj" -c Release -o /app/build
RUN dotnet publish "./firnal.dashboard.api.v2.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Only re-install minimal required Chromium dependencies
RUN apt-get update && apt-get install -y \
    libnss3 libatk-bridge2.0-0 libxss1 libasound2 libgbm1 libgtk-3-0 \
    libxshmfence1 libxcomposite1 libxrandr2 libxdamage1 libxtst6 libdrm2 \
    libglib2.0-0 libglu1-mesa libpango-1.0-0 libpangocairo-1.0-0 \
    fonts-liberation libappindicator3-1 xdg-utils \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy binaries only   keep image lean
COPY --from=build /app/publish /app
COPY --from=build /root/.cache /root/.cache

# Environment
ENV PLAYWRIGHT_BROWSERS_PATH=/root/.cache/ms-playwright

ENTRYPOINT ["dotnet", "firnal.dashboard.api.v2.dll"]
