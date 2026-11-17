# One image serving the front end and the API over plain HTTP on 8080; HTTPS ends at the reverse proxy.

FROM node:lts-alpine AS web
WORKDIR /src/web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
# Writes into ../src/Chores.Api/wwwroot, the API's web root
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY Directory.Build.props ./
COPY src/ src/
RUN dotnet restore src/Chores.Api
# Before publish, so the front end ships as the web root
COPY --from=web /src/src/Chores.Api/wwwroot src/Chores.Api/wwwroot
RUN dotnet publish src/Chores.Api --no-restore -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /app ./
# The database and the cookie keys next to it; mount a volume here. Owned by the image's non-root user,
# which a fresh named volume inherits.
ENV Database__Path=/data/chores.db
RUN mkdir /data && chown $APP_UID /data
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Chores.Api.dll"]
