# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/WeaponFOV/*" -Force -Recurse
dotnet publish "./WeaponFOV.csproj" -c Release -o "$env:RELOADEDIIMODS/WeaponFOV" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location