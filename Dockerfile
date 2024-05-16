ARG PARENT_VERSION=dotnet8.0

# Development
FROM defradigital/dotnetcore-development:${PARENT_VERSION} AS development
ARG PARENT_VERSION
LABEL uk.gov.defra.ffc.parent-image=defradigital/dotnetcore-development:${PARENT_VERSION}

COPY --chown=dotnet:dotnet ./Directory.Build.props ./Directory.Build.props
RUN mkdir -p /home/dotnet/ncea-harvester/ /home/dotnet/ncea-harvester.tests/
COPY --chown=dotnet:dotnet ./src/ncea-harvester.tests/*.csproj ./ncea-harvester.tests/
RUN dotnet restore ./ncea-harvester.tests/ncea-harvester.tests.csproj
COPY --chown=dotnet:dotnet ./src/ncea-harvester/*.csproj ./ncea-harvester/
RUN dotnet restore ./ncea-harvester/ncea-harvester.csproj
COPY --chown=dotnet:dotnet ./src/ncea-harvester.tests/ ./ncea-harvester.tests/
# some CI builds fail with back to back COPY statements, eg Azure DevOps
RUN true
COPY --chown=dotnet:dotnet ./src/ncea-harvester/ ./ncea-harvester/
RUN dotnet publish ./ncea-harvester/ -c Release -o /home/dotnet/out

ARG PORT=8080
ENV PORT ${PORT}
EXPOSE ${PORT}
# Override entrypoint using shell form so that environment variables are picked up
ENTRYPOINT dotnet watch --project ./ncea-harvester run --urls "http://*:${PORT}"

# Production
FROM defradigital/dotnetcore:${PARENT_VERSION} AS production
ARG PARENT_VERSION
LABEL uk.gov.defra.ffc.parent-image=defradigital/dotnetcore:${PARENT_VERSION}
COPY --from=development /home/dotnet/out/ ./
ARG PORT=8080
ENV ASPNETCORE_URLS http://*:${PORT}
EXPOSE ${PORT}
# Override entrypoint using shell form so that environment variables are picked up
ENTRYPOINT dotnet ncea-harvester.dll
