# Build context is the REPOSITORY ROOT, not this directory — the build needs Directory.Build.props,
# Directory.Packages.props and the library project, all of which live above here.
#   docker build -f MarqSpec.Client.ProjectX.FakeGateway/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the manifests first so a source-only change does not re-resolve every package.
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY MarqSpec.Client.ProjectX/MarqSpec.Client.ProjectX.csproj MarqSpec.Client.ProjectX/
COPY MarqSpec.Client.ProjectX.FakeGateway/MarqSpec.Client.ProjectX.FakeGateway.csproj MarqSpec.Client.ProjectX.FakeGateway/

# MinVer derives the version from git history, which is not present in the build context. Pinning it here keeps
# restore deterministic and stops the fake picking up a meaningless 0.0.0-alpha.0 (ADR-0006).
ARG VERSION=0.0.0-fake
RUN dotnet restore MarqSpec.Client.ProjectX.FakeGateway/MarqSpec.Client.ProjectX.FakeGateway.csproj

COPY MarqSpec.Client.ProjectX/ MarqSpec.Client.ProjectX/
COPY MarqSpec.Client.ProjectX.FakeGateway/ MarqSpec.Client.ProjectX.FakeGateway/

RUN dotnet publish MarqSpec.Client.ProjectX.FakeGateway/MarqSpec.Client.ProjectX.FakeGateway.csproj \
    -c Release -o /app --no-restore \
    -p:MinVerVersionOverride=$VERSION

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# No credentials, no secrets, nothing to protect: this image impersonates a venue, it does not talk to one.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=5s --timeout=3s --start-period=5s --retries=10 \
    CMD ["dotnet", "--info"]

ENTRYPOINT ["dotnet", "MarqSpec.Client.ProjectX.FakeGateway.dll"]
