FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the solution file
COPY *.sln .

# Create the Proj_backend directory and copy the .csproj file
COPY /Proj_backend/*.csproj ./Proj_backend/Proj_backend.csproj
COPY /Proj_backend/ ./Proj_backend/

# Restore dependencies
RUN dotnet restore

# Set the working directory to the project folder
WORKDIR /app/Proj_backend

# Publish the project
RUN dotnet publish -c Release -o publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y libfontconfig1

COPY --from=build /app/Proj_backend/publish ./
ENTRYPOINT ["dotnet", "Proj_backend.dll"]
