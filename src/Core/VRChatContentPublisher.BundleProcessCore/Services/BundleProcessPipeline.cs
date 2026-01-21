using AssetsTools.NET;
using AssetsTools.NET.Extra;
using VRChatContentPublisher.BundleProcessCore.Models;

namespace VRChatContentPublisher.BundleProcessCore.Services;

internal sealed class BundleProcessPipeline(BundleProcessOptions options)
{
    public async ValueTask<Stream> ProcessAsync(
        Stream bundleStream,
        IProcessProgressReporter? progressReporter,
        bool leaveOpen = true,
        CancellationToken cancellationToken = default)
    {
        return await Task.Factory.StartNew(() =>
                ProcessCore(bundleStream, progressReporter, leaveOpen, cancellationToken),
            cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }

    private Stream ProcessCore(
        Stream bundleStream,
        IProcessProgressReporter? progressReporter,
        bool leaveOpen,
        CancellationToken cancellationToken)
    {
        var avatarId = options.AvatarId;
        CopyToTempFile(bundleStream, "source");

        var manager = new AssetsManager();
        try
        {
            manager.LoadClassPackage("Assets/type-tree.tpk");

            var bundle = manager.LoadBundleFile(bundleStream, "bundle", false);
            if (bundle.file.DataIsCompressed)
                throw new InvalidOperationException("Bundle must be decompressed before processing.");

            var assetsFile = manager.LoadAssetsFileFromBundle(bundle, 0);
            manager.LoadClassDatabaseFromPackage(assetsFile.file.Metadata.UnityVersion);

            foreach (var assetFileInfo in assetsFile.file.GetAssetsOfType(AssetClassID.MonoBehaviour))
            {
                var behaviourBase = manager.GetBaseField(assetsFile, assetFileInfo);
                if (behaviourBase["blueprintId"] is not
                    { IsDummy: false, Value.ValueType: AssetValueType.String } blueprintIdField) continue;

                blueprintIdField.AsString = avatarId;
                assetFileInfo.SetNewData(behaviourBase);
            }

            var randomPostfix = GetRandomPostfix();
            var prefix = "prefab-id-v1_";
            var assetGameObjectName = $"{prefix}{avatarId}_{randomPostfix}";
            var assetPrefabName = $"{assetGameObjectName}.prefab";
            var assetBundleName = $"{assetPrefabName}.unity3d";
            var fullAssetPrefabPath = $"assets/{assetPrefabName}";

            var assetBundleNameMd4Hash = new MD4().GetHexHashFromString(assetBundleName).ToLowerInvariant();
            var originalAssetBundleBlockName =
                bundle.file.BlockAndDirInfo.DirectoryInfos[0].Name;
            var newAssetBundleBlockName = "CAB-" + assetBundleNameMd4Hash;

            foreach (var blockInfo in bundle.file.BlockAndDirInfo.DirectoryInfos)
            {
                blockInfo.Name = blockInfo.Name.Replace(originalAssetBundleBlockName, newAssetBundleBlockName);
            }

            foreach (var assetFileInfo in assetsFile.file.GetAssetsOfType(AssetClassID.AssetBundle))
            {
                var bundleBase = manager.GetBaseField(assetsFile, assetFileInfo);
                if (bundleBase["m_Name"] is { IsDummy: false, Value.ValueType: AssetValueType.String } nameField)
                {
                    nameField.AsString = assetBundleName;
                }

                if (bundleBase["m_AssetBundleName"] is
                    { IsDummy: false, Value.ValueType: AssetValueType.String } bundleNameField)
                {
                    bundleNameField.AsString = assetBundleName;
                }

                if (bundleBase["m_Container.Array"] is
                    { IsDummy: false, Value.ValueType: AssetValueType.Array } containerField)
                {
                    foreach (var containerMap in containerField)
                    {
                        if (containerMap[0] is
                                { IsDummy: false, Value.ValueType: AssetValueType.String } keyField &&
                            keyField.AsString.StartsWith("assets/" + prefix))
                        {
                            keyField.AsString = fullAssetPrefabPath;
                        }
                    }
                }

                assetFileInfo.SetNewData(bundleBase);
            }

            foreach (var assetFileInfo in assetsFile.file.GetAssetsOfType(AssetClassID.GameObject))
            {
                var gameObjectBase = manager.GetBaseField(assetsFile, assetFileInfo);
                if (gameObjectBase["m_Name"] is
                        { IsDummy: false, Value.ValueType: AssetValueType.String } nameField &&
                    nameField.AsString.StartsWith(prefix))
                {
                    nameField.AsString = assetGameObjectName;

                    assetFileInfo.SetNewData(gameObjectBase);
                }
            }

            foreach (var textureInfo in assetsFile.file.GetAssetsOfType(AssetClassID.Texture2D))
            {
                var textureBase = manager.GetBaseField(assetsFile, textureInfo);
                if (textureBase["m_StreamData"] is
                        { IsDummy: false, TypeName: "StreamingInfo" } streamDataField &&
                    streamDataField["path"] is { IsDummy: false, Value.ValueType: AssetValueType.String } pathField)
                {
                    pathField.AsString =
                        pathField.AsString.Replace(originalAssetBundleBlockName, newAssetBundleBlockName);

                    textureInfo.SetNewData(textureBase);
                }
            }

            foreach (var textureInfo in assetsFile.file.GetAssetsOfType(AssetClassID.Texture2DArray))
            {
                var textureBase = manager.GetBaseField(assetsFile, textureInfo);
                if (textureBase["m_StreamData"] is
                        { IsDummy: false, TypeName: "StreamingInfo" } streamDataField &&
                    streamDataField["path"] is { IsDummy: false, Value.ValueType: AssetValueType.String } pathField)
                {
                    pathField.AsString =
                        pathField.AsString.Replace(originalAssetBundleBlockName, newAssetBundleBlockName);

                    textureInfo.SetNewData(textureBase);
                }
            }

            foreach (var audioClipInfo in assetsFile.file.GetAssetsOfType(AssetClassID.AudioClip))
            {
                var audioClipBase = manager.GetBaseField(assetsFile, audioClipInfo);
                if (audioClipBase["m_Resource"] is
                        { IsDummy: false, TypeName: "StreamedResource" } resourceField &&
                    resourceField["m_Source"] is { IsDummy: false, Value.ValueType: AssetValueType.String } pathField)
                {
                    pathField.AsString =
                        pathField.AsString.Replace(originalAssetBundleBlockName, newAssetBundleBlockName);

                    audioClipInfo.SetNewData(audioClipBase);
                }
            }

            foreach (var cubeMapInfo in assetsFile.file.GetAssetsOfType(AssetClassID.Cubemap))
            {
                var cubeMapBase = manager.GetBaseField(assetsFile, cubeMapInfo);
                if (cubeMapBase["m_StreamData"] is
                        { IsDummy: false, TypeName: "StreamingInfo" } streamDataField &&
                    streamDataField["path"] is { IsDummy: false, Value.ValueType: AssetValueType.String } pathField)
                {
                    pathField.AsString =
                        pathField.AsString.Replace(originalAssetBundleBlockName, newAssetBundleBlockName);

                    cubeMapInfo.SetNewData(cubeMapBase);
                }
            }

            foreach (var cubeMapInfo in assetsFile.file.GetAssetsOfType(AssetClassID.CubemapArray))
            {
                var cubeMapBase = manager.GetBaseField(assetsFile, cubeMapInfo);
                if (cubeMapBase["m_StreamData"] is
                        { IsDummy: false, TypeName: "StreamingInfo" } streamDataField &&
                    streamDataField["path"] is { IsDummy: false, Value.ValueType: AssetValueType.String } pathField)
                {
                    pathField.AsString =
                        pathField.AsString.Replace(originalAssetBundleBlockName, newAssetBundleBlockName);

                    cubeMapInfo.SetNewData(cubeMapBase);
                }
            }

            bundle.file.BlockAndDirInfo.DirectoryInfos[0].SetNewData(assetsFile.file);

            var newBundleStream = CreateTempStream();
            using var writer = new AssetsFileWriter(newBundleStream, true);
            bundle.file.Write(writer);

            if (!leaveOpen)
                bundleStream.Close();

            CopyToTempFile(newBundleStream, "processed");

            newBundleStream.Position = 0;
            return newBundleStream;
        }
        finally
        {
            manager.UnloadAll();
        }
    }

    private Stream CreateTempStream()
    {
        var tempFolderPath = options.TempFolderPath ??
                             Path.Combine(Path.GetTempPath(), "vrchat-content-publisher-bundle-process");

        if (!Directory.Exists(tempFolderPath))
            Directory.CreateDirectory(tempFolderPath);

        var tempFilePath = Path.Combine(
            tempFolderPath,
            Guid.NewGuid().ToString("N") + "-bundle.tmp"
        );

        return File.Create(tempFilePath, 4096,
            FileOptions.DeleteOnClose | FileOptions.SequentialScan | FileOptions.Asynchronous);
    }

    private void CopyToTempFile(Stream source, string name)
    {
        source.Position = 0;
        var tempFolderPath = options.TempFolderPath ??
                             Path.Combine(Path.GetTempPath(), "vrchat-content-publisher-bundle-process");

        if (!Directory.Exists(tempFolderPath))
            Directory.CreateDirectory(tempFolderPath);

        var tempFilePath = Path.Combine(
            tempFolderPath,
            name + "-debug.tmp"
        );

        using var stream = File.Create(tempFilePath, 4096, FileOptions.SequentialScan | FileOptions.Asynchronous);
        source.CopyTo(stream);
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