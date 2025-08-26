FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder
WORKDIR /

COPY ./.editorconfig ./
COPY ./global.json ./
COPY ./Directory.Build.props ./

# Setup working directory for the project
COPY ./src/BuildingBlocks/BuildingBlocks.csproj ./src/BuildingBlocks/
COPY ./src/Services/Basket/src/Basket.csproj ./src/Services/Basket/src/


# Restore nuget packages
RUN dotnet restore ./src/Services/Basket/src/Basket.csproj

# Copy project files
COPY ./src/BuildingBlocks ./src/BuildingBlocks/
COPY ./src/Services/Basket/src/  ./src/Services/Basket/src/

# Build project with Release configuration
# and no restore, as we did it already

RUN ls
RUN dotnet build  -c Release --no-restore ./src/Services/Basket/src/Basket.csproj

WORKDIR /src/Services/Basket/src

# Publish project to output folder
# and no build, as we did it already
RUN dotnet publish -c Release --no-build -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0

# Setup working directory for the project
WORKDIR /
COPY --from=builder /src/Services/Basket/src/out  .


ENV ASPNETCORE_URLS https://*:443, http://*:80
ENV ASPNETCORE_ENVIRONMENT docker

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "Basket.dll"]

