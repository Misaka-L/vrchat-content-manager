using System.Diagnostics;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VRChatContentManager.Core.Extensions;
using VRChatContentManager.Core.Services;
using VRChatContentManager.Core.Services.PublishTask;
using VRChatContentManager.Core.Services.UserSession;

var stopWatch = new Stopwatch();
stopWatch.Start();

var bundleFileStream = File.OpenRead(@"C:\Data\Project\aaas-workspace\Assets\StreamingAssets\.raw.vrcw");
var bundleFile = new AssetBundleFile();
var bundleReader = new AssetsFileReader(bundleFileStream);

bundleFile.Read(bundleReader);

if (bundleFile.DataIsCompressed)
{
    bundleFile = BundleHelper.UnpackBundle(bundleFile);
}

using var writer = new AssetsFileWriter(@"C:\Data\Project\aaas-workspace\Assets\StreamingAssets\.result.vrcw");

bundleFile.Pack(writer, AssetBundleCompressionType.LZMA);

stopWatch.Stop();
Console.WriteLine($"Completed in {stopWatch.ElapsedMilliseconds} ms");