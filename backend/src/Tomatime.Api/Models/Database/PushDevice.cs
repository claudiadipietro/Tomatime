namespace Tomatime.Api.Models;

public sealed class PushDevice
{
    /// <summary>
    /// Gets or sets the unique identifier for the push device.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the endpoint URL for the push device.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the P-256 public key used for Web Push encryption.
    /// </summary>
    public string P256dh { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the authentication key for the push device.
    /// </summary>
    public string Auth { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the language preference for the push device.
    /// </summary>
    public string Language { get; set; } = "en";
}
