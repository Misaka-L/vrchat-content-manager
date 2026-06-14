using System.Diagnostics;

namespace VRChatContentPublisher.Core.Extensions;

public static class ActivityExtension
{
    extension(Activity activity)
    {
        public Activity SetContentMetadata(
            string contentId,
            string? contentName = null,
            string? contentType = null,
            string? platform = null,
            string? unityVersion = null,
            string? authorId = null)
        {
            activity.SetTag("content.id", contentId);

            if (contentName != null)
                activity.SetTag("content.name", contentName);
            if (contentType != null)
                activity.SetTag("content.type", contentType);
            if (platform != null)
                activity.SetTag("content.platform", platform);
            if (unityVersion != null)
                activity.SetTag("content.unity_version", unityVersion);
            if (authorId != null)
                activity.SetTag("content.author_id", authorId);

            return activity;
        }
    }
}