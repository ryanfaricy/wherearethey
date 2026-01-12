# Use the SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy the project file and restore dependencies
COPY WhereAreThey.csproj ./
RUN dotnet restore

# Copy the rest of the application code
COPY . ./

# Publish the application
RUN dotnet publish WhereAreThey.csproj -c Release -o out

# Use the ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .

# The application listens on the port specified by the PORT environment variable (set by Railway)
# We handle this in Program.cs with serverOptions.ListenAnyIP(portNumber)
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WhereAreThey.dll"]
