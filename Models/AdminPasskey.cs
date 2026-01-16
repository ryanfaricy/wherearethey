using System;

namespace WhereAreThey.Models;

public class AdminPasskey
{
    public int Id { get; set; }
    public string Name { get; set; } = "Passkey";
    public byte[] CredentialId { get; set; } = Array.Empty<byte>();
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();
    public uint Counter { get; set; }
    public string? CredType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Aaguid { get; set; }
}
