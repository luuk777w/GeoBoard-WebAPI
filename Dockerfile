FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /GeoBoardWebAPI

# Copy csproj and restore as distinct layers
COPY GeoBoardWebAPI.sln ./
COPY ./GeoBoardWebAPI/*.csproj ./GeoBoardWebAPI/
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /GeoBoardWebAPI

ENV ASPNETCORE_ENVIRONMENT = Production
ENV ASPNETCORE_URLS = http://*:5000

EXPOSE 5000
COPY --from=build-env /GeoBoardWebAPI/out .
ENTRYPOINT ["dotnet", "GeoBoardWebAPI.dll"]
