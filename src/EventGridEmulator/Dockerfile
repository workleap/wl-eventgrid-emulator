FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 6500
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true
ENV DOTNET_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:6500

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS publish
WORKDIR /src
COPY . .
WORKDIR "/src/EventGridEmulator"
RUN dotnet publish "EventGridEmulator.csproj" -c Release -r linux-musl-x64 --no-self-contained --nologo -p:RunAnalyzers=false -o /app/publish

FROM base AS final
RUN chown -R app /app
USER app
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventGridEmulator.dll"]
