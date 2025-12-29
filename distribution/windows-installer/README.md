# NSIS Installer for VRChat Content Publisher for Windows

## Make

```powershell
$Env:INSTALLER_DISPLAY_VERSION = "0.1.2-alpha.1"; $Env:INSTALLER_OLD_CLASSIC_VERSION = "0.1.2.0"; $Env:INSTALLER_PATH_TO_BUNDLE = "/path/to/bundle"; makensis installer.nsi
```

Output: `vrchat-content-publisher-installer.exe`