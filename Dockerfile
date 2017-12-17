FROM microsoft/aspnetcore-build:2.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY src/Backup/BackupNetworkLibrary.csproj ./src/Backup/
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet src/Backup publish -c Release -o out

# Build runtime image
FROM microsoft/aspnetcore:2.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Backup.dll"]
