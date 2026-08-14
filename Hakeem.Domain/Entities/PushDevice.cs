namespace Hakeem.Domain.Entities;

public sealed class PushDevice : BaseEntity
{
    public Guid UserId { get; set; }
    public string ExpoPushToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = null!;
}
