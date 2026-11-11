namespace WaveMP.Data.Entities;

/// <summary>
/// A secret of the server that must survive restarts, e.g. the key that signs the tokens.
/// </summary>
public class ServerSecret
{
    public required string Name { get; set; }

    public required byte[] Value { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }
}
