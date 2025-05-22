# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy the .csproj file and restore dependencies
# This helps with Docker layer caching, only re-running dotnet restore if .csproj changes
COPY ["auth.csproj", "./"]
RUN dotnet restore "auth.csproj"

# Copy the rest of the application code
COPY . .

# Build the application
RUN dotnet build "auth.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "auth.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Create the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose the port your API runs on (default for ASP.NET Core is 80)
EXPOSE 8080


ENV ASPNETCORE_URLS="http://+:8080"
# Set the entry point for the application
# Assuming your output assembly name is auth.dll based on auth.csproj
ENTRYPOINT ["dotnet", "auth.dll"]