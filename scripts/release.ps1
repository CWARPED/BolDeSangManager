# Build & push d'une release CalVer (YYYY.MM.DD) de BolDeSangManager.
#
# Tags appliques sur Docker Hub a chaque push :
#   - cwarp/boldesangmanager:YYYY.MM.DD[-N]   (immutable, version exacte)
#   - cwarp/boldesangmanager:YYYY.MM          (rolling sur le mois)
#   - cwarp/boldesangmanager:latest           (dernier en date)
#
# Cree aussi un git tag local v<version>. Push du tag en option (-PushGitTag).
#
# Usage :
#   ./scripts/release.ps1                 # build + push Docker (interactif)
#   ./scripts/release.ps1 -PushGitTag     # idem + git push du tag
#   ./scripts/release.ps1 -DryRun         # affiche les actions sans rien faire

[CmdletBinding()]
param(
    [string]$Image = 'cwarp/boldesangmanager',
    [string]$Builder = 'multiarch-bds',
    [string]$Platforms = 'linux/amd64,linux/arm64',
    [switch]$PushGitTag,
    [switch]$DryRun,
    [switch]$Yes
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Push-Location $repoRoot
try {
    $today = Get-Date -Format 'yyyy.MM.dd'
    $month = Get-Date -Format 'yyyy.MM'

    # Compteur -N si un tag git v<today>[-N] existe deja
    $existing = git tag -l "v$today*" | Where-Object { $_ -match "^v$([regex]::Escape($today))(-(\d+))?$" }
    if (-not $existing) {
        $version = $today
    } else {
        $maxSuffix = 0
        foreach ($t in $existing) {
            if ($t -match "^v$([regex]::Escape($today))-(\d+)$") {
                $n = [int]$matches[1]
                if ($n -gt $maxSuffix) { $maxSuffix = $n }
            } elseif ($t -eq "v$today" -and $maxSuffix -lt 1) {
                $maxSuffix = 1
            }
        }
        $version = "$today-$($maxSuffix + 1)"
    }

    $gitTag = "v$version"

    Write-Host ""
    Write-Host "=== BolDeSangManager release ===" -ForegroundColor Cyan
    Write-Host "Version    : $version"
    Write-Host "Git tag    : $gitTag"
    Write-Host "Image      : $Image"
    Write-Host "Tags Docker:"
    Write-Host "  - ${Image}:$version"
    Write-Host "  - ${Image}:$month"
    Write-Host "  - ${Image}:latest"
    Write-Host "Platforms  : $Platforms"
    Write-Host "Push git   : $($PushGitTag.IsPresent)"
    Write-Host ""

    if ($DryRun) {
        Write-Host "DryRun: aucune action effectuee." -ForegroundColor Yellow
        return
    }

    if (-not $Yes) {
        $confirmation = Read-Host "Continuer ? [o/N]"
        if ($confirmation -notmatch '^[oOyY]') {
            Write-Host "Annule." -ForegroundColor Yellow
            return
        }
    }

    # 1) Git tag local
    Write-Host "`n[1/3] Creation du tag git $gitTag..." -ForegroundColor Cyan
    git tag -a $gitTag -m "Release $version"

    # 2) Build & push multi-arch
    Write-Host "`n[2/3] Build multi-arch + push..." -ForegroundColor Cyan
    $buildArgs = @(
        'buildx', 'build',
        '--builder', $Builder,
        '--platform', $Platforms,
        '--build-arg', "APP_VERSION=$version",
        '-t', "${Image}:$version",
        '-t', "${Image}:$month",
        '-t', "${Image}:latest",
        '--push', '.'
    )
    & docker @buildArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build/push Docker echoue. Le tag git $gitTag a ete cree mais l'image n'est pas publiee." -ForegroundColor Red
        Write-Host "Pour supprimer le tag local: git tag -d $gitTag" -ForegroundColor Yellow
        exit 1
    }

    # 3) Push git tag (optionnel)
    Write-Host "`n[3/3] Push git tag..." -ForegroundColor Cyan
    if ($PushGitTag) {
        git push origin $gitTag
    } else {
        Write-Host "Skip (utiliser -PushGitTag pour pousser le tag $gitTag sur origin)." -ForegroundColor Yellow
    }

    Write-Host "`nRelease $version publiee." -ForegroundColor Green
    Write-Host "Deploiement Freebox : docker compose pull ; docker compose up -d"
}
finally {
    Pop-Location
}
