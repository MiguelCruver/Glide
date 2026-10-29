param(
    [string]$CommitMessage = "chore: atualizacao automatizada do Glide"
)

$ErrorActionPreference = "Stop"
$rootDir = (Get-Item $PSScriptRoot).Parent.FullName
Set-Location $rootDir

Write-Host "=== GLIDE: Pipeline de Deploy Automatizado ===" -ForegroundColor Cyan

Write-Host "[1/4] Executando testes automatizados..." -ForegroundColor Yellow
& "C:\Users\raposo\.dotnet\dotnet.exe" test "$rootDir\Glide.sln" --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw "Testes falharam." }

Write-Host "[2/4] Compilando executavel Standalone..." -ForegroundColor Yellow
& "C:\Users\raposo\.dotnet\dotnet.exe" publish "$rootDir\src\Glide.App\Glide.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$rootDir\release_standalone"
if ($LASTEXITCODE -ne 0) { throw "Compilacao falhou." }

Write-Host "[3/4] Gerando instalador Inno Setup..." -ForegroundColor Yellow
$iscc = "C:\Users\raposo\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (Test-Path $iscc) {
    & $iscc "$rootDir\installer\GlideSetup.iss"
    if ($LASTEXITCODE -ne 0) { throw "Falha no instalador." }
    Write-Host "Instalador gerado com sucesso em installer_output!" -ForegroundColor Green
}

Write-Host "[4/4] Enviando alteracoes para o GitHub..." -ForegroundColor Yellow
git add .
$changes = git status --porcelain
if ($changes) {
    git commit -m $CommitMessage
    git pull --rebase origin master
    git push origin master
    Write-Host "Sincronizado com o GitHub com sucesso!" -ForegroundColor Green
} else {
    Write-Host "Sem alteracoes de codigo para commit." -ForegroundColor Cyan
}

Write-Host "Deploy finalizado com exito!" -ForegroundColor Green
