# Room Booking

Meeting-room booking assistant built with .NET 8. The functional implementation
will be added incrementally in subsequent pull requests.

## Prerequisites

- .NET SDK 8.0.401 or a compatible later patch release. The expected version is
  defined in [global.json](global.json).

## Run locally

```powershell
dotnet restore RoomBooking.sln --locked-mode
dotnet run --project src/RoomBooking.Api
```

The API exposes `GET /health` as a readiness endpoint.

## Verify the solution

```powershell
dotnet build RoomBooking.sln --configuration Release --no-restore
dotnet test RoomBooking.sln --configuration Release --no-build --no-restore
```

The GitHub Actions workflow runs the same restore, build, and test sequence for
pull requests and pushes targeting `main`.

