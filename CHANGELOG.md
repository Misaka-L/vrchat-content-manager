# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Changed

- Show Build Datetime in local time zone. [`#129`](https://github.com/project-vrcz/content-publisher/pull/129)

### Added

- Show app build info (version, git commit, build date) in error report window. [`#140`](https://github.com/project-vrcz/content-publisher/pull/140)
- Check is account valid before enter account repair page. [`#138`](https://github.com/project-vrcz/content-publisher/pull/138)
  - If account is valid, the account will be mark as repaired. No further operation requested.
- Acknowledgement for early adopters and open source softwares in Settings Page. [`#129`](https://github.com/project-vrcz/content-publisher/pull/129)
  - Also the software license.
- Logging when create publish task failed. [`#128`](https://github.com/project-vrcz/content-publisher/pull/128)

### Fixed

- App crash when any error occurred during account repair process. [`#138`](https://github.com/project-vrcz/content-publisher/pull/138)

## [2.0.2] - 2026-01-02

### Fixed

- Content Publish will always failed due to forget to remove test code. [`#127`](https://github.com/project-vrcz/content-publisher/pull/127)

## [2.0.1] - 2026-01-02

### Fixed

- Won't retry when connect timeout error occurred. [`#125`](https://github.com/project-vrcz/content-publisher/pull/125)

### Changed

- Will give more detail information when upload process found file version with same md5. [`#126`](https://github.com/project-vrcz/content-publisher/pull/126)

## [2.0.0] - 2025-12-29

### Added

- Custom Http Proxy. [`#113`](https://github.com/project-vrcz/content-publisher/pull/113)
- Report create publish task error to rpc client. [`#115`](https://github.com/project-vrcz/content-publisher/pull/115)
- New Error Report Window for debug publish task failed. [`#122`](https://github.com/project-vrcz/content-publisher/pull/122)
- Allow open logs folder in tray icon context menu. [`#122`](https://github.com/project-vrcz/content-publisher/pull/122)

### Fixed

- Unable to remove invalid user session in settings. [`#116`](https://github.com/project-vrcz/content-publisher/pull/116)
- VRChat Api HttpClient won't retry in some case. [`#118`](https://github.com/project-vrcz/content-publisher/pull/118)

### Changed

- HttpClient no longer follow `Retry-After` header. [`#118`](https://github.com/project-vrcz/content-publisher/pull/118)
- Rename to `VRChat Content Publisher`. [`#119`](https://github.com/project-vrcz/content-publisher/pull/119)
  - You must uninstall old version to install new version. (You can keep your user data)

## [1.3.0] - 2025-12-23

### Added

- Add Unity Setup Guide [`#110`](https://github.com/project-vrcz/content-publisher/pull/110)
  - Include install connect package, connect unity to app.
  - You can directly jump to home page if you connect unity to app during guide.

## [1.2.0] - 2025-12-18

### Added

- Support `ready-for-publish` health check endpoint for RPC. [`104`](https://github.com/project-vrcz/content-publisher/pull/105)
- Launch App by URL protocol `vrchat-content-manager://launch`. (Windows-only for now) [`#104`](https://github.com/project-vrcz/content-publisher/pull/104)
- Windows Installer (NSIS). [`#101`](https://github.com/project-vrcz/content-publisher/pull/101)
- Single Instance. [`#103`](https://github.com/project-vrcz/content-publisher/pull/103)
  - Prevent launch new instance when another intance already exist.
  - Bring up existing instance's main window.

## [1.1.0] - 2025-12-11

### Changed

- Insert new task to the beginning of the task list. [`#89`](https://github.com/project-vrcz/content-publisher/pull/89)
- Challenge Code will Always uppercase. [`#93`](https://github.com/project-vrcz/content-publisher/pull/93)
- Allow copy challenge code in request challenge dialog. [`#94`](https://github.com/project-vrcz/content-publisher/pull/94)

## [1.0.0] - 2025-12-08

### Added

- Show App version, commit hash and build date in App settings page [`#70`](https://github.com/project-vrcz/content-publisher/pull/70).
- Basic Linux Support [`#76`](https://github.com/project-vrcz/content-publisher/pull/76)

### Changed

- Use `Path.Combine(Path.GetTempPath(), "vrchat-content-manager-81b7bca3")` as temp path:
  - Windows:
    - If App running as SYSTEM, it will use `C:\Windows\SystemTemp\vrchat-content-manager-81b7bca3` (DON'T DO TAHT)
    - If not, App will check environment variables in the following order and uses the first path found:
      - The path specified by the `TMP` environment variable. (usually `C:\Users\{UserName}\AppData\Local\Temp\vrchat-content-manager-81b7bca3`)
      - The path specified by the `TEMP` environment variable. (usually `C:\Users\{UserName}\AppData\Local\Temp\vrchat-content-manager-81b7bca3`)
      - The path specified by the `USERPROFILE` environment variable. (usually `C:\Users\{UserName}\vrchat-content-manager-81b7bca3`)
      - The Windows directory. (MAYBE `C:\Windows\Temp\vrchat-content-manager-81b7bca3`, and you will run into trouble as App MAY don't have premission to access this folder) 
  - Linux:
    - Use environment variable `TMPDIR` if exist.
    - If not, use `/tmp/vrchat-content-manager-81b7bca3`
  - see [Path.GetTempPath()](https://learn.microsoft.com/en-us/dotnet/api/System.IO.Path.GetTempPath?view=net-10.0) for more information.
- Adjust http rqeuest pipeline [`#80`](https://github.com/project-vrcz/content-publisher/pull/80)
  - Use DecorrelatedJitterV2 as http request retry strategy
  - Increase retry delay
  - Increase MaxConnectionsPerServer to 256 from 10 for AWS S3 HttpClient

## [1.0.0-rc.1] - 2025-12-07

### Added

- Show App version, commit hash and build date in App settings page [`#70`](https://github.com/project-vrcz/content-publisher/pull/70).
- Basic Linux Support [`#76`](https://github.com/project-vrcz/content-publisher/pull/76)

### Changed

- Adjust http rqeuest pipeline [`#80`](https://github.com/project-vrcz/content-publisher/pull/80)
  - Use DecorrelatedJitterV2 as http request retry strategy
  - Increase retry delay
  - Increase MaxConnectionsPerServer to 256 from 10 for AWS S3 HttpClient

[unreleased]: https://github.com/project-vrcz/content-publisher/compare/v2.0.2...HEAD
[2.0.2]: https://github.com/project-vrcz/content-publisher/compare/v2.0.1...v2.0.2
[2.0.1]: https://github.com/project-vrcz/content-publisher/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/project-vrcz/content-publisher/compare/v1.3.0...v2.0.0
[1.3.0]: https://github.com/project-vrcz/content-publisher/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/project-vrcz/content-publisher/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/project-vrcz/content-publisher/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/project-vrcz/content-publisher/compare/v1.0.0-rc.1...v1.0.0
[1.0.0-rc.1]: https://github.com/project-vrcz/content-publisher/releases/tag/v1.0.0-rc.1
