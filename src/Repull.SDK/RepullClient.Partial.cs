using Microsoft.Kiota.Abstractions;

namespace Repull.SDK;

/// <summary>
/// Hand-written extensions to the Kiota-generated <see cref="RepullClient"/>.
/// Exposes the otherwise-protected <see cref="IRequestAdapter"/> so SDK
/// extension methods can run typed requests that bypass the auto-generated
/// (sometimes too-loose) response models.
/// </summary>
public partial class RepullClient
{
    /// <summary>
    /// Underlying Kiota request adapter. Useful for advanced flows that need
    /// to call <c>SendAsync&lt;T&gt;</c> directly with a custom parsable.
    /// </summary>
    public IRequestAdapter Adapter => RequestAdapter;
}
