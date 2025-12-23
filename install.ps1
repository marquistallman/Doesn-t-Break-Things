# Este script instala el comando 'dbt' en tu sistema Windows

# 1. Definir rutas
$scriptDir = $PSScriptRoot
$targetDir = Join-Path $scriptDir "DBT\bin\Debug\net8.0"
$configFile = Join-Path $scriptDir "properties.json"
$targetConfig = Join-Path $targetDir "properties.json"

# 2. Verificar que existe el ejecutable
if (-not (Test-Path $targetDir)) {
    Write-Host "Error: No se encuentra la carpeta de compilación." -ForegroundColor Red
    Write-Host "Ejecuta 'dotnet build' antes de instalar."
    exit
}

# 3. Copiar properties.json junto al .exe para que la configuración viaje con el comando
if (Test-Path $configFile) {
    Copy-Item -Path $configFile -Destination $targetConfig -Force
    Write-Host "Configuración (properties.json) copiada al directorio del ejecutable." -ForegroundColor Gray
}

# 4. Añadir al PATH de usuario (Permanente)
$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")

if ($currentPath -notlike "*$targetDir*") {
    [Environment]::SetEnvironmentVariable("Path", "$currentPath;$targetDir", "User")
    Write-Host "¡Éxito! 'dbt' ha sido añadido a tu PATH." -ForegroundColor Green
    Write-Host "Cierra esta terminal y abre una nueva para empezar a usarlo." -ForegroundColor Yellow
} else {
    Write-Host "El comando 'dbt' ya estaba configurado en tu sistema." -ForegroundColor Cyan
}

Read-Host "Presiona Enter para salir..."
