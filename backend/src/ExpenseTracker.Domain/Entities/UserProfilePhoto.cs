namespace ExpenseTracker.Domain.Entities;

public sealed class UserProfilePhoto
{
    private UserProfilePhoto() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public long SizeBytes { get; private set; }
    public byte[] Data { get; private set; } = Array.Empty<byte>();
    public DateTime CreatedAtUtc { get; private set; }

    public static UserProfilePhoto Create(
        Guid userId,
        string contentType,
        int width,
        int height,
        byte[] data,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("ContentType required.", nameof(contentType));
        }
        if (data is null || data.Length == 0)
        {
            throw new ArgumentException("Photo data required.", nameof(data));
        }
        return new UserProfilePhoto
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ContentType = contentType,
            Width = width,
            Height = height,
            SizeBytes = data.LongLength,
            Data = data,
            CreatedAtUtc = nowUtc,
        };
    }
}
