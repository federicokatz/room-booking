FROM node:24-alpine AS frontend-build

WORKDIR /src/frontend/RoomBooking.Web

COPY frontend/RoomBooking.Web/package.json frontend/RoomBooking.Web/package-lock.json ./
RUN npm ci

COPY frontend/RoomBooking.Web/ ./
RUN npm run build


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build

WORKDIR /src

COPY RoomBooking.sln ./
COPY .editorconfig ./
COPY Directory.Build.props Directory.Packages.props ./
COPY src/RoomBooking.Api/RoomBooking.Api.csproj src/RoomBooking.Api/
COPY src/RoomBooking.Application/RoomBooking.Application.csproj src/RoomBooking.Application/
COPY src/RoomBooking.Domain/RoomBooking.Domain.csproj src/RoomBooking.Domain/
COPY src/RoomBooking.Infrastructure/RoomBooking.Infrastructure.csproj src/RoomBooking.Infrastructure/
COPY src/RoomBooking.Api/packages.lock.json src/RoomBooking.Api/
COPY src/RoomBooking.Application/packages.lock.json src/RoomBooking.Application/
COPY src/RoomBooking.Domain/packages.lock.json src/RoomBooking.Domain/
COPY src/RoomBooking.Infrastructure/packages.lock.json src/RoomBooking.Infrastructure/

RUN dotnet restore src/RoomBooking.Api/RoomBooking.Api.csproj --locked-mode

COPY src/ ./src/
RUN dotnet publish src/RoomBooking.Api/RoomBooking.Api.csproj --configuration Release --no-restore --output /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=api-build /app/publish ./
COPY --from=frontend-build /src/frontend/RoomBooking.Web/dist ./wwwroot

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet RoomBooking.Api.dll"]
