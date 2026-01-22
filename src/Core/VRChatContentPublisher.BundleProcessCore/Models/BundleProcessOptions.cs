using VRChatContentPublisher.BundleProcessCore.Processers;

namespace VRChatContentPublisher.BundleProcessCore.Models;

public record BundleProcessOptions(
    string ContentId,
    IBundleProcesser[] Processers
);

public record AvatarBundleProcessOptions(
    string ContentId,
    IBundleProcesser[] Processers
) : BundleProcessOptions(ContentId, [new PipelineManagerProcesser(), new AvatarBundleRenameProcesser(), ..Processers]);

public record WorldBundleProcessOptions(
    string ContentId,
    IBundleProcesser[] Processers
) : BundleProcessOptions(ContentId, [new PipelineManagerProcesser(), ..Processers]);