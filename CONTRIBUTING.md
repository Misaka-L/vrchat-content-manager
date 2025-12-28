# Contributing guide

## Project struct

```plaintext
- CHANGELOG.md # You should record your changes in this file under `Unreleased` section.
- distribution/windows-installer # NSIS Installer source (For windows installer)
- src
    - App
        - VRChatContentPublisher.App # Entrypoint, all ViewModels, UI.
    - Core
        - VRChatContentPublisher.ConnectCore # Handle HTTP RPC (for communication with VRChat Content Publisher Connect).
        - VRChatContentPublisher.Core # Stuffs like Publish Task, Storage, Settings, Api and so on.
        - VRChatContentPublisher.IpcCore # Mutex and IPC named pipe server / client for single instance mode (for now).
```

## Requirement

- Git (with git submodule)
- .NET 10 SDK
- Any code editor or IDE you like (e.g Rider, Visual Studio)

## Steps

1. [Fork](https://docs.github.com/pull-requests/collaborating-with-pull-requests/working-with-forks/fork-a-repo) this repo.
2. Clone your fork by `git clone --recurse-submodules {your-repo-git-url-here}`.
3. Push your changes.
4. Describe your changes in `CHANGELOG.md` following [Keep a changelog guide](https://keepachangelog.com/en/1.1.0/).
5. Make a pull request.
