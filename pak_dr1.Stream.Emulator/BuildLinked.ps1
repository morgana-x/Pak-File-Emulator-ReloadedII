# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/PAK_DR1.Stream.Emulator/*" -Force -Recurse
dotnet publish "./PAK_DR1.Stream.Emulator.csproj" -c Release -o "$env:RELOADEDIIMODS/PAK_DR1.Stream.Emulator" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location