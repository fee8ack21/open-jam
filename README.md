# Open Jam

Taiwanese digital marketplace for creators.

## Services

| Service | Path |
|---------|------|
| Auth | `src/Auth/` |

## Commands

Run from repo root:

```bash
# Dev server (http://localhost:5169 or https://localhost:7280)
dotnet run --project src/Auth

# Build
dotnet build src/Auth

# Publish (release)
dotnet publish src/Auth -c Release
```

Docker (run from `src/`):

```bash
docker build -f Auth/Dockerfile -t open-jam-auth .
```
