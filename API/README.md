# ProjectX API

The backend follows the layer direction used by
[jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture):
`Domain <- Application <- Infrastructure <- API`.

## Development setup

Restore the repository-local .NET tools:

```powershell
dotnet tool restore
```

Create a local JWT signing key. The value is stored by .NET User Secrets and is
not written to the repository:

```powershell
$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
dotnet user-secrets set "JwtSettings:SecurityKey" ([Convert]::ToBase64String($bytes)) --project src/API/API.csproj
```

Development startup intentionally deletes, recreates, and seeds the configured
database. Database initialization does not run outside the Development
environment.

## Verification

```powershell
dotnet format ProjectX.slnx --no-restore --verify-no-changes
dotnet build ProjectX.slnx --no-restore
dotnet test ProjectX.slnx --no-restore
```
