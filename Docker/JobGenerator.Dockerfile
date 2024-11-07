# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy the .csproj and .sln files and restore as distinct layers
COPY Sahadeva.Dossier.Common/*.csproj Sahadeva.Dossier.Common/
COPY Sahadeva.Dossier.DAL/*.csproj Sahadeva.Dossier.DAL/
COPY Sahadeva.Dossier.Entities/*.csproj Sahadeva.Dossier.Entities/
COPY Sahadeva.Dossier.JobGenerator/*.csproj Sahadeva.Dossier.JobGenerator/

# Restore as distinct layers
RUN dotnet restore ./Sahadeva.Dossier.Common/Sahadeva.Dossier.Common.csproj
RUN dotnet restore ./Sahadeva.Dossier.DAL/Sahadeva.Dossier.DAL.csproj
RUN dotnet restore ./Sahadeva.Dossier.Entities/Sahadeva.Dossier.Entities.csproj
RUN dotnet restore ./Sahadeva.Dossier.JobGenerator/Sahadeva.Dossier.JobGenerator.csproj

# Copy the remaining project files
COPY . ./

# Build and publish a release
RUN dotnet publish Sahadeva.Dossier.JobGenerator/Sahadeva.Dossier.JobGenerator.csproj -c Release -o out

# Use the official .NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Specify the entry point for the app
ENTRYPOINT ["dotnet", "Sahadeva.Dossier.JobGenerator.dll"]
