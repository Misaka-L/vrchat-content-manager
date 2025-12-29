using Microsoft.Extensions.Options;

namespace VRChatContentPublisher.Core.Settings;

// https://stackoverflow.com/a/42705862
public interface IWritableOptions<out T> : IOptions<T> where T : class, new()
{
    Task UpdateAsync(Action<T> applyChanges);
}