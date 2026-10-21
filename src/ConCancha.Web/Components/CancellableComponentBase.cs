using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

/// <summary>Base for components that start async work: <see cref="ComponentToken"/> is cancelled when the component is disposed.</summary>
public abstract class CancellableComponentBase : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource _componentCancellation = new();

    protected CancellationToken ComponentToken => _componentCancellation.Token;

    public void Dispose()
    {
        _componentCancellation.Cancel();
        _componentCancellation.Dispose();
        OnDisposing();
    }

    protected virtual void OnDisposing()
    {
    }
}
