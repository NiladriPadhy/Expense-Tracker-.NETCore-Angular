using ExpenseTracker.Domain.Abstractions;
using ExpenseTracker.Domain.Entities;
using SixLabors.ImageSharp;

namespace ExpenseTracker.Infrastructure.Storage;

public sealed class DatabasePhotoStorage : IPhotoStorage
{
    private static readonly HashSet<string> AllowedMime = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp",
    };

    private const long MaxBytes = 2 * 1024 * 1024; // 2 MB
    private const int MaxDimension = 2048;

    private readonly IClock _clock;
    public DatabasePhotoStorage(IClock clock) => _clock = clock;

    public async Task<UserProfilePhoto> StoreAsync(Guid userId, Stream content, string contentType, CancellationToken ct)
    {
        if (!AllowedMime.Contains(contentType))
        {
            throw new InvalidOperationException("photo_unsupported_mime");
        }

        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct).ConfigureAwait(false);
        var bytes = ms.ToArray();

        if (bytes.LongLength > MaxBytes)
        {
            throw new InvalidOperationException("photo_too_large");
        }

        using var img = Image.Load(bytes);
        if (img.Width > MaxDimension || img.Height > MaxDimension)
        {
            throw new InvalidOperationException("photo_dimensions_exceeded");
        }

        return UserProfilePhoto.Create(userId, contentType, img.Width, img.Height, bytes, _clock.UtcNow);
    }
}
