using WaveMP.Core.Streaming.Signatures;

namespace WaveMP.Service.Signatures;

/// <summary>
/// Marks an endpoint that delivers media and accepts signed media URLs (see
/// <see cref="MediaSignatureAuthenticationHandler"/>) besides the session token. A signature is only valid for the
/// kind of media named here and the track in the route value <c>id</c>. Use it together with
/// <c>[Authorize(Policy = AuthorizationPolicies.Media)]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SignedMediaAttribute(MediaResource resource) : Attribute
{
    public MediaResource Resource { get; } = resource;
}
