using System.Text.RegularExpressions;
using AssetsTools.NET.Extra;
using VRChatContentPublisher.BundleProcessCore.Models;
using VRChatContentPublisher.BundleProcessCore.Services;
using AssetsTools.NET;
using VRChatContentPublisher.BundleProcessCore.Utils;

namespace VRChatContentPublisher.BundleProcessCore.Processers;

public sealed partial class AvatarBundleRenameProcesser : IBundleProcesser
{
    [GeneratedRegex(@"prefab-id-v1_(?<ContentId>.+)_\d{10}\.prefab\.unity3d")]
    private static partial Regex AssetBundleNameRegex();

    private const string Prefix = "prefab-id-v1_";

    public bool ShouldProcess(
        AssetsManager assetsManager,
        BundleFileInstance bundleFileInstance,
        AssetsFileInstance[] assetsFileInstances,
        BundleProcessOptions bundleProcessOptions,
        IProcessProgressReporter? progressReporter
    )
    {
        if (!ShouldProcessCore(assetsManager, bundleFileInstance, assetsFileInstances, bundleProcessOptions,
                progressReporter))
            return false;

        if (!BlueprintOverrideEnabledChecker.IsBlueprintOverrideEnabled(assetsManager, assetsFileInstances))
            throw new InvalidOperationException("Blueprint override is disabled for this bundle.");

        return true;
    }

    public bool ShouldProcessCore(
        AssetsManager assetsManager,
        BundleFileInstance bundleFileInstance,
        AssetsFileInstance[] assetsFileInstances,
        BundleProcessOptions bundleProcessOptions,
        IProcessProgressReporter? progressReporter
    )
    {
        var blockName = bundleFileInstance.file.BlockAndDirInfo.DirectoryInfos[0].Name;
        if (!blockName.StartsWith("CAB-"))
            throw new InvalidOperationException("Bundle block name is not starting with CAB- prefix.");
        var oldAssetBundleHash = blockName.Substring(4);

        foreach (var assetsFileInstance in assetsFileInstances)
        {
            var assetsBundleMetadata =
                assetsFileInstance.file.GetAssetsOfType(AssetClassID.AssetBundle).FirstOrDefault();

            if (assetsBundleMetadata is null)
                throw new InvalidOperationException("No AssetBundle metadata found in the main assets file.");

            var bundleBase = assetsManager.GetBaseField(assetsFileInstance, assetsBundleMetadata);
            if (bundleBase["m_Name"] is not
                { IsDummy: false, Value.ValueType: AssetValueType.String } nameField)
                throw new InvalidOperationException("AssetBundle metadata is missing m_Name field.");

            if (bundleBase["m_AssetBundleName"] is not
                { IsDummy: false, Value.ValueType: AssetValueType.String } bundleNameField)
                throw new InvalidOperationException("AssetBundle metadata is missing m_AssetBundleName field.");

            if (nameField.AsString != bundleNameField.AsString)
                return true;

            var assetBundleName = nameField.AsString;
            var bundleRegexMatch = AssetBundleNameRegex().Match(assetBundleName);
            if (!bundleRegexMatch.Success ||
                bundleRegexMatch.Groups["ContentId"].Value != bundleProcessOptions.ContentId)
                return true;

            var assetBundleNameMd4Hash = new MD4().GetHexHashFromString(assetBundleName).ToLowerInvariant();
            if (oldAssetBundleHash != assetBundleNameMd4Hash)
                return true;
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
        progressReporter?.Report("Calculating asset bundle name...");

        var randomPostfix = GetRandomPostfix();
        var assetGameObjectName = $"{Prefix}{bundleProcessOptions.ContentId}_{randomPostfix}";
        var assetPrefabName = $"{assetGameObjectName}.prefab";
        var assetBundleName = $"{assetPrefabName}.unity3d";
        var fullAssetPrefabPath = $"assets/{assetPrefabName}";

        var assetBundleNameMd4Hash = new MD4().GetHexHashFromString(assetBundleName).ToLowerInvariant();
        var oldBlockName =
            bundleFileInstance.file.BlockAndDirInfo.DirectoryInfos[0].Name;
        var newBlockName = "CAB-" + assetBundleNameMd4Hash;

        progressReporter?.Report("Renaming asset file block to " + assetBundleName);
        foreach (var blockInfo in bundleFileInstance.file.BlockAndDirInfo.DirectoryInfos)
        {
            blockInfo.Name = blockInfo.Name.Replace(oldBlockName, newBlockName);
        }

        foreach (var assetsFileInstance in assetsFileInstances)
        {
            progressReporter?.Report("Updating asset bundle metadata...");
            RenameAssetsBundleMetadata(assetsManager, assetsFileInstance, assetBundleName, fullAssetPrefabPath);

            progressReporter?.Report("Renaming avatar prefab...");
            RenameAvatarPrefab(assetsManager, assetsFileInstance, assetGameObjectName);

            progressReporter?.Report("Updating Texture2D asset references...");
            UpdateTexture2dReference(assetsManager, assetsFileInstance, oldBlockName, newBlockName);

            progressReporter?.Report("Updating Texture2DArray asset references...");
            UpdateTexture2dArrayReference(assetsManager, assetsFileInstance, oldBlockName, newBlockName);

            progressReporter?.Report("Updating AudioClip asset references...");
            UpdateAudioClipReference(assetsManager, assetsFileInstance, oldBlockName, newBlockName);

            progressReporter?.Report("Updating Cubemap asset references...");
            UpdateCubeMapReference(assetsManager, assetsFileInstance, oldBlockName, newBlockName);

            progressReporter?.Report("Updating CubemapArray asset references...");
            UpdateCubemapArrayReference(assetsManager, assetsFileInstance, oldBlockName, newBlockName);
        }
    }

    private void RenameAssetsBundleMetadata(
        AssetsManager assetsManager,
        AssetsFileInstance assetsFileInstance,
        string newAssetBundleName,
        string fullAssetPrefabPath
    )
    {
        var bundleMetadataInfo = assetsFileInstance.file.GetAssetsOfType(AssetClassID.AssetBundle).FirstOrDefault() ??
                                 throw new InvalidOperationException(
                                     "No AssetBundle metadata found in the main assets file.");

        var bundleBase = assetsManager.GetBaseField(assetsFileInstance, bundleMetadataInfo);
        if (bundleBase["m_Name"] is { IsDummy: false, Value.ValueType: AssetValueType.String } nameField)
        {
            nameField.AsString = newAssetBundleName;
        }

        if (bundleBase["m_AssetBundleName"] is
            { IsDummy: false, Value.ValueType: AssetValueType.String } bundleNameField)
        {
            bundleNameField.AsString = newAssetBundleName;
        }

        if (bundleBase["m_Container.Array"] is
            { IsDummy: false, Value.ValueType: AssetValueType.Array } containerField)
        {
            foreach (var containerMap in containerField)
            {
                if (containerMap[0] is
                        { IsDummy: false, Value.ValueType: AssetValueType.String } keyField &&
                    keyField.AsString.StartsWith("assets/" + Prefix))
                {
                    keyField.AsString = fullAssetPrefabPath;
                }
            }
        }

        bundleMetadataInfo.SetNewData(bundleBase);
    }

    private void RenameAvatarPrefab(
        AssetsManager assetsManager,
        AssetsFileInstance assetsFileInstance,
        string newPrefabName
    )
    {
        foreach (var assetFileInfo in assetsFileInstance.file.GetAssetsOfType(AssetClassID.GameObject))
        {
            var gameObjectBase = assetsManager.GetBaseField(assetsFileInstance, assetFileInfo);
            if (gameObjectBase["m_Name"] is
                    { IsDummy: false, Value.ValueType: AssetValueType.String } nameField &&
                nameField.AsString.StartsWith(Prefix))
            {
                nameField.AsString = newPrefabName;

                assetFileInfo.SetNewData(gameObjectBase);
            }
        }
    }

    private void UpdateTexture2dReference(
        AssetsManager assetsManager,
        AssetsFileInstance assetsFileInstance,
        string oldBlockName,
        string newBlockName
    )
    {
        foreach (var textureInfo in assetsFileInstance.file.GetAssetsOfType(AssetClassID.Texture2D))
        {
            var textureBase = assetsManager.GetBaseField(assetsFileInstance, textureInfo);
            if (textureBase["m_StreamData"] is
                    { IsDummy: false, TypeName: "StreamingInfo" } streamDataField &&
                streamDataField["path"] is { IsDummy: false, Value.ValueType: AssetValueType.String } pathField)
            {
                pathField.AsString =
                    pathField.AsString.Replace(oldBlockName, newBlockName);

                textureInfo.SetNewData(textureBase);
            }
        }
    }

    private void UpdateTexture2dArrayReference(
        AssetsManager assetsManager,
        AssetsFileInstance assetsFileInstance,
        string oldBlockName,
        string newBlockName
    )
    {
        foreach (var textureInfo in assetsFileInstance.file.GetAssetsOfType(AssetClassID.Texture2DArray))
        {
            var textureBase = assetsManager.GetBaseField(assetsFileInstance, textureInfo);
            if (textureBase["m_StreamData"] is
                    { IsDummy: false, TypeName: "StreamingInfo" } streamDataField &&
                streamDataField["path"] is { IsDummy: false, Value.ValueType: AssetValueType.String } pathField)
            {
                pathField.AsString =
                    pathField.AsString.Replace(oldBlockName, newBlockName);

                textureInfo.SetNewData(textureBase);
            }
        }
    }

    private void UpdateAudioClipReference(
        AssetsManager assetsManager,
        AssetsFileInstance assetsFileInstance,
        string oldBlockName,
        string newBlockName
    )
    {
        foreach (var audioClipInfo in assetsFileInstance.file.GetAssetsOfType(AssetClassID.AudioClip))
        {
            var audioClipBase = assetsManager.GetBaseField(assetsFileInstance, audioClipInfo);
            if (audioClipBase["m_Resource"] is
                    { IsDummy: false, TypeName: "StreamedResource" } resourceField &&
                resourceField["m_Source"] is { IsDummy: false, Value.ValueType: AssetValueType.String } pathField)
            {
                pathField.AsString =
                    pathField.AsString.Replace(oldBlockName, newBlockName);

                audioClipInfo.SetNewData(audioClipBase);
            }
        }
    }

    private void UpdateCubeMapReference(
        AssetsManager assetsManager,
        AssetsFileInstance mainAssetsFileInstance,
        string oldBlockName,
        string newBlockName
    )
    {
        foreach (var cubeMapInfo in mainAssetsFileInstance.file.GetAssetsOfType(AssetClassID.Cubemap))
        {
            var cubeMapBase = assetsManager.GetBaseField(mainAssetsFileInstance, cubeMapInfo);
            if (cubeMapBase["m_StreamData"] is
                    { IsDummy: false, TypeName: "StreamingInfo" } streamDataField &&
                streamDataField["path"] is { IsDummy: false, Value.ValueType: AssetValueType.String } pathField)
            {
                pathField.AsString =
                    pathField.AsString.Replace(oldBlockName, newBlockName);

                cubeMapInfo.SetNewData(cubeMapBase);
            }
        }
    }

    private void UpdateCubemapArrayReference(
        AssetsManager assetsManager,
        AssetsFileInstance mainAssetsFileInstance,
        string oldBlockName,
        string newBlockName
    )
    {
        foreach (var cubeMapInfo in mainAssetsFileInstance.file.GetAssetsOfType(AssetClassID.CubemapArray))
        {
            var cubeMapBase = assetsManager.GetBaseField(mainAssetsFileInstance, cubeMapInfo);
            if (cubeMapBase["m_StreamData"] is
                    { IsDummy: false, TypeName: "StreamingInfo" } streamDataField &&
                streamDataField["path"] is { IsDummy: false, Value.ValueType: AssetValueType.String } pathField)
            {
                pathField.AsString =
                    pathField.AsString.Replace(oldBlockName, newBlockName);

                cubeMapInfo.SetNewData(cubeMapBase);
            }
        }
    }

    private string GetRandomPostfix()
    {
        // 10 random digits
        var rand = new Random();
        var postfix = string.Empty;
        for (var i = 0; i < 10; i++)
        {
            postfix += rand.Next(0, 10).ToString();
        }

        return postfix;
    }
}