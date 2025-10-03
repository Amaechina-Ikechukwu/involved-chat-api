# Use .NET SDK image to build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY Involved-Chat.csproj ./
RUN dotnet restore Involved-Chat.csproj

# Copy everything else
COPY . ./
RUN dotnet publish Involved-Chat.csproj -c Release -o /app

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Involved-Chat.dll"]
