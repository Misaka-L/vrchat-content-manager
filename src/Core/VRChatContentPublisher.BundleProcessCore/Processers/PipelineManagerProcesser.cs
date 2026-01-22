using AssetsTools.NET;
using AssetsTools.NET.Extra;
using VRChatContentPublisher.BundleProcessCore.Models;
using VRChatContentPublisher.BundleProcessCore.Services;

namespace VRChatContentPublisher.BundleProcessCore.Processers;

public sealed class PipelineManagerProcesser : IBundleProcesser
{
    public bool ShouldProcess(
        AssetsManager assetsManager,
        BundleFileInstance bundleFileInstance,
        AssetsFileInstance[] assetsFileInstances,
        BundleProcessOptions bundleProcessOptions,
        IProcessProgressReporter? progressReporter
    )
    {
        foreach (var assetsFileInstance in assetsFileInstances)
        {
            foreach (var assetFileInfo in assetsFileInstance.file.GetAssetsOfType(AssetClassID.MonoBehaviour))
            {
                var behaviourBase = assetsManager.GetBaseField(assetsFileInstance, assetFileInfo);
                if (behaviourBase["blueprintId"] is not
                    { IsDummy: false, Value.ValueType: AssetValueType.String } blueprintIdField) continue;

                if (blueprintIdField.AsString != bundleProcessOptions.ContentId)
                    return true;
            }
        }

        return false;
    }

    public void Process(
        AssetsManager assetsManager,
        BundleFileInstance bundleFileInstance,
        AssetsFileInstance[] assetsFileInstances,
        BundleProcessOptions bundleProcessOptions,
        IProcessProgressReporter? progressReporter
    )
    {
        foreach (var assetsFileInstance in assetsFileInstances)
        {
            foreach (var assetFileInfo in assetsFileInstance.file.GetAssetsOfType(AssetClassID.MonoBehaviour))
            {
                var behaviourBase = assetsManager.GetBaseField(assetsFileInstance, assetFileInfo);
                if (behaviourBase["blueprintId"] is not
                    { IsDummy: false, Value.ValueType: AssetValueType.String } blueprintIdField) continue;

                blueprintIdField.AsString = bundleProcessOptions.ContentId;
                assetFileInfo.SetNewData(behaviourBase);
            }
        }
    }
}