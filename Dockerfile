# Use the SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy files for restoring dependencies
COPY WhereAreThey.sln ./
COPY WhereAreThey.csproj ./
COPY WhereAreThey.Tests/WhereAreThey.Tests.csproj WhereAreThey.Tests/
COPY package*.json ./

# Install Node.js for JavaScript tests
RUN apt-get update && apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

# Restore dependencies
RUN dotnet restore
RUN npm install

# Copy the rest of the application code
COPY . ./

# Run JavaScript tests
RUN npm run test:run

# Run .NET tests (including component tests)
RUN dotnet test WhereAreThey.Tests/WhereAreThey.Tests.csproj -c Release

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
