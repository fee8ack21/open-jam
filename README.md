# Open Jam

Taiwanese digital marketplace for creators.

## Services

| Service | Path |
|---------|------|
| Auth | `src/Auth/` |
| Docs | `docs/` |

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

### Docs

Run from `docs/`:

```bash
# Dev server (http://localhost:5173)
pnpm dev

# Build
pnpm build

# Preview build output
pnpm preview
```

Docker (run from `docs/`):

```bash
docker build -t open-jam-docs .
docker run -p 8080:80 open-jam-docs
```
