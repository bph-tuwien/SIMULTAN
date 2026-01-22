# Use the official .NET 7 SDK image
#FROM mcr.microsoft.com/dotnet/sdk:7.0-bookworm-slim
FROM mcr.microsoft.com/dotnet/sdk:7.0-jammy-amd64

# Set the working directory in the container
WORKDIR /src

# Copy the rest of the application files
COPY . .

# Restore the project dependencies
RUN dotnet restore SIMULTAN.sln

# Build the application
RUN dotnet build -c Release SIMULTAN.sln

# Test
RUN dotnet test --verbosity detailed --consoleLoggerParameters:ErrorsOnly --no-restore SIMULTAN.sln

# Set the entry point for the container
ENTRYPOINT ["bash"]