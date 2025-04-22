@echo off
cls

ECHO:** DOTNET BUILD
dotnet run --property WarningLevel=0 --project ./utils/pbir-cli/pbir-cli.csproj -- %*