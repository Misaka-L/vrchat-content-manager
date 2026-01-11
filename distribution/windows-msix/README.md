# MSIS Package for Windows

## 1. Copy bundle files to `./workspace`

## 2. Generate `.pri`

```powershell
makepri.exe new /pr .\Images\ /cf .\priconfig.xml /in Misaka-L.VRChatContentPublisher
```

## 3. Make MSIX Package

```powershell
makeappx.exe pack /d . /p app.msix
```

## 4. Publish to Store or sign

TODO
