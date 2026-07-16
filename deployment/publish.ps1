param(
    [string]$Output = "publish/ECommerce.Web"
)

$ErrorActionPreference = "Stop"

dotnet restore ECommerce.sln
dotnet test ECommerce.sln
dotnet publish src/ECommerce.Web/ECommerce.Web.csproj -c Release -o $Output

Write-Host "Published ECommerce.Web to $Output"
