FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY GymTracker.sln ./
COPY backend/GymTracker.Api/GymTracker.Api.csproj backend/GymTracker.Api/
RUN dotnet restore GymTracker.sln
COPY . .
RUN dotnet publish backend/GymTracker.Api/GymTracker.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "GymTracker.Api.dll"]
