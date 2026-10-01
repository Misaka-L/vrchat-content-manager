using VRChatContentPublisher.App.Models.Update;

namespace VRChatContentPublisher.App.Services.Update;

public abstract class AppUpdateServiceState
{
    public sealed class Idle : AppUpdateServiceState;

    public sealed class Downloading : AppUpdateServiceState
    {
        public required AppUpdateInformation UpdateInformation { get; init; }
        public required string PathToDownloadedFile { get; init; }

        public double BytesPerSecondSpeed { get; set; }
        public long TotalFileSize { get; set; }
        public long DownloadedFileSize { get; set; }

        public required CancellationTokenSource DownloadCancellationTokenSource { get; init; }
    }

    public sealed class DownloadError : AppUpdateServiceState
    {
        public required AppUpdateInformation UpdateInformation { get; init; }
        public required Exception DownloadErrorException { get; init; }
    }

    public sealed class IntegrityCheckFailed : AppUpdateServiceState
    {
        public required AppUpdateInformation UpdateInformation { get; init; }

        public required string DownloadedFileSha256 { get; init; }
        public required string ExceptedFileSha256 { get; init; }
    }

    public sealed class WaitingForInstall : AppUpdateServiceState
    {
        public required AppUpdateInformation UpdateInformation { get; init; }

        public required string PathToDownloadedFile { get; init; }
    }

    public sealed class InstallError : AppUpdateServiceState
    {
        public required AppUpdateInformation UpdateInformation { get; init; }

        public required Exception InstallErrorException { get; init; }
        public required string PathToDownloadedFile { get; init; }
    }
}