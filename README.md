# Dragon Shelter PT-BR Installer

Source code for the Windows installer included with the Brazilian Portuguese translation of Dragon Shelter.

The installer verifies the supported game files before making any changes, creates backups of the original files, installs the PT-BR localization bundle and allows the user to restore the original files.

## Source files

- `Program.cs` - Windows Forms user interface and application entry point.
- `Installer.cs` - installation, validation, backup and restore logic.
- `Catalog.cs` - Addressables catalog handling.
- `DragonShelterPTBR.csproj` - .NET project configuration.
- `compilar.ps1` - PowerShell build script.

## Requirements

- Windows x64
- .NET 10 SDK

## Building

The distributed installer is built as a self-contained Windows x64 single-file executable.

The project expects the translated localization payload at:

`payload/localization-string-tables-english(en)_assets_all_PTBR_PRESERVADO.bundle`

The payload is embedded into the executable at build time as the resource:

`PTBR.bundle`

The translated bundle is not included in this source-code repository because it contains modified game localization data.

To reproduce a complete build, place the PT-BR localization bundle at the path above and run:

`compilar.ps1`

The build script uses `dotnet publish` with the `win-x64` runtime and self-contained deployment.

## Safety

Before installation, the program validates the SHA-256 hashes of the supported original game files and validates the embedded PT-BR payload.

The installer modifies only the required Dragon Shelter localization bundle and the corresponding Addressables catalog metadata.

Backups of the original files are created before installation. The Restore Original option restores and verifies those backups.

If the installed game files do not match the supported version, the installer refuses to install the translation.

## Nexus Mods review

This repository is provided so the source code of the compiled installer distributed with the Dragon Shelter Brazilian Portuguese translation can be reviewed.

The executable distributed through Nexus Mods is unsigned and self-contained, which may cause automated security systems to flag or quarantine it for manual review.

## Credits

Brazilian Portuguese translation, revision and installer project: LanaLym.

Dragon Shelter and its original game assets belong to their respective developers and rights holders.