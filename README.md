# DeadModer

DeadModer is a desktop modification manager and installer designed specifically for Valve's hero shooter Deadlock. It provides automated Source 2 VPK installation, seamless integration with online mod repositories (GameBanana and Deadlocker.net), local addon synchronization, and comprehensive filter controls.

---

## Features

- Automated Source 2 Configuration: Detects the Deadlock Steam installation directory and automatically configures `gameinfo.gi` (`citadel/addons` SearchPaths) so mods load correctly without manual file editing.
- Online Mod Catalog: Browse thousands of modifications directly inside the application via GameBanana API v11 and Deadlocker.net with offline caching.
- Advanced Filtering:
  - Categories: Gameplay, UI, Visual, Sounds, Other.
  - Deadlock Heroes: Filter by character (26 heroes supported) with quick selection for popular picks.
  - Curated & Ready: Quick toggles for featured staff picks and mods ready for one-click installation.
  - Time Periods: Filter by upload date (All time, 24 hours, past week, past month).
  - Age Rating: SFW only or NSFW toggles.
  - Sort Orders: Popularity, newest releases, rating, download count, and alphabetical.
- Mod Management:
  - One-click install with automatic archive extraction and VPK file normalization (`pakXX_dir.vpk` standard).
  - Enable or disable mods instantly via safe state toggling (`.vpk` / `.vpk.disabled`).
  - Delete mods and clean up storage directly from the catalog or the installed list.
  - Open the `game/citadel/addons` folder and synchronize external modifications in real time.
- Bilingual Interface: Fast reactive runtime switching between Russian and English.
- Dark Source 2 Interface: Custom dark aesthetic tailored for Deadlock with official Google Material vector symbols.

---

## System Requirements

- Operating System: Windows 10 or Windows 11 (64-bit)
- Game: Deadlock installed via Steam
- Runtime: .NET 8.0 Runtime (standalone single-file builds include the runtime)

---

## Installation and Quick Start

1. Download the latest `DeadModer-v0.0.1-win-x64.zip` release from the Releases section.
2. Unpack the archive to any convenient directory.
3. Launch `DeadModer.UI.exe`.
4. The application will detect your Deadlock folder automatically. If required, click "Activate mods in 1 click" to configure `gameinfo.gi`.
5. Select any mod from the catalog and click "Install".

---

## Building from Source

### Prerequisites

- .NET 8 SDK or higher: https://dotnet.microsoft.com/download/dotnet/8.0
- Git: https://git-scm.com/

### Build Commands

Clone the repository:
```bash
git clone https://github.com/ticker-01/DeadModer.git
cd DeadModer
```

Restore dependencies:
```bash
dotnet restore
```

Build in Release configuration:
```bash
dotnet build -c Release
```

Run tests:
```bash
dotnet test
```

Publish a standalone executable:
```bash
dotnet publish src/DeadModer.UI/DeadModer.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
```

---

## Project Structure

- `src/DeadModer.Core`: Domain models, interfaces, entities, enums, and business logic abstractions.
- `src/DeadModer.Infrastructure`: Implementations for Steam locator, GameBanana client, Deadlocker scraper, file system operations, and download service.
- `src/DeadModer.UI`: Avalonia UI desktop application, ViewModels, views, localization service, and UI styles.

---

## Development Notice

In certain cases, portions of the codebase, refactoring, and optimizations were assisted or edited by AI.
(Примечание: в частных случаях код и отдельные компоненты редактировались при содействии искусственного интеллекта).

---

## Disclaimer

DeadModer is a third-party open-source project and is not affiliated with, endorsed by, or sponsored by Valve Corporation. Deadlock, Source 2, and the Valve logo are trademarks or registered trademarks of Valve Corporation.

---

## License

This project is licensed under the MIT License.
