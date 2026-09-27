<#
.SYNOPSIS
  Copies the database dumps from the Docker "backups" volume to a normal folder on this PC,
  for example on a second disk. Windows counterpart of offsite-backup.sh.

.DESCRIPTION
  The backup container writes a dump every night into a Docker volume that lives on the same disk as the
  database. This copies them to another disk so one failing drive can't take both. A second disk in the
  same machine does not protect against theft, fire or a power surge: keep a copy somewhere else as well.

  Existing copies are never overwritten. Copies older than -KeepDays are deleted from the destination
  (use -KeepDays 0 to keep everything).

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File deploy\backup-to-folder.ps1 -Destination D:\gla-backups
#>
param(
    [Parameter(Mandatory = $true)][string]$Destination,
    [string]$VolumeName = "genericlearningapp_backups",
    [int]$KeepDays = 60
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker isn't available. Start Docker Desktop and try again."
}

$volumes = @(docker volume ls --quiet)
if ($volumes -notcontains $VolumeName) {
    throw "There is no Docker volume called '$VolumeName'. Run 'docker volume ls' to find it, then pass -VolumeName."
}

New-Item -ItemType Directory -Force -Path $Destination | Out-Null
$dest = (Resolve-Path -LiteralPath $Destination).Path

Write-Host "[backup-to-folder] $(Get-Date -Format s) copying dumps from volume '$VolumeName' to $dest"

# Only the dated dumps (latest.dump is just a link to one of them), and only ones young enough to be kept, so
# an expired dump is never copied just to be deleted again. cp -p keeps the dump's own timestamp, which is what
# the age-based cleanup below goes by. -KeepDays 0 means keep everything (a 100-year window).
# The shell text contains no double quotes on purpose: Windows PowerShell mangles them on the way to docker.
$window = if ($KeepDays -gt 0) { $KeepDays } else { 36500 }
docker run --rm -e WINDOW=$window -v "${VolumeName}:/backups:ro" -v "${dest}:/dest" alpine sh -c `
    'for f in $(find /backups -maxdepth 1 -name ''gla-*.dump'' -mtime -$WINDOW); do b=${f##*/}; [ -e /dest/$b ] || { cp -p $f /dest/$b && echo copied $b; }; done'
if ($LASTEXITCODE -ne 0) { throw "Copying failed (docker exited with $LASTEXITCODE)." }

if ($KeepDays -gt 0) {
    $cutoff = (Get-Date).AddDays(-$KeepDays)
    Get-ChildItem -LiteralPath $dest -Filter "gla-*.dump" |
        Where-Object { $_.LastWriteTime -lt $cutoff } |
        ForEach-Object { Write-Host "removing old copy $($_.Name)"; Remove-Item -LiteralPath $_.FullName -Force }
}

# The file names carry the time (gla-YYYYMMDDTHHMMSSZ), so sorting by name is sorting by age.
$kept = Get-ChildItem -LiteralPath $dest -Filter "gla-*.dump" | Sort-Object Name
Write-Host "[backup-to-folder] done. $($kept.Count) dump(s) in $dest, newest: $(if ($kept) { $kept[-1].Name } else { 'none' })"
