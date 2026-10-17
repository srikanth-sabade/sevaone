FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Build.props
COPY Directory.Packages.props Directory.Packages.props
COPY global.json global.json
COPY SevaOne.Domain/ SevaOne.Domain/
COPY SevaOne.Infrastructure/ SevaOne.Infrastructure/
COPY SevaOne.Business/ SevaOne.Business/
COPY SevaOne.Services/ SevaOne.Services/
RUN dotnet restore SevaOne.Services/SevaOne.Services.csproj
RUN dotnet publish SevaOne.Services/SevaOne.Services.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker
EXPOSE 8080
ENTRYPOINT ["dotnet", "SevaOne.Services.dll"]
