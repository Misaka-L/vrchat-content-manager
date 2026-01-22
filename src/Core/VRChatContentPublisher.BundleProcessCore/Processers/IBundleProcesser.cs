using AssetsTools.NET.Extra;
using VRChatContentPublisher.BundleProcessCore.Models;
using VRChatContentPublisher.BundleProcessCore.Services;

namespace VRChatContentPublisher.BundleProcessCore.Processers;

public interface IBundleProcesser
{
    bool ShouldProcess(
        AssetsManager assetsManager,
        BundleFileInstance bundleFileInstance,
        AssetsFileInstance[] assetsFileInstances,
        BundleProcessOptions bundleProcessOptions,
        IProcessProgressReporter? progressReporter
    )
    {
        return true;
    }

    void Process(
        AssetsManager assetsManager,
        BundleFileInstance bundleFileInstance,
        AssetsFileInstance[] assetsFileInstances,
        BundleProcessOptions bundleProcessOptions,
        IProcessProgressReporter? progressReporter
    );
}