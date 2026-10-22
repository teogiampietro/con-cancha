using ConCancha.Contracts.Public;
using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class MyBookingsCard
{
    private Guid? _confirmingId;

    [Parameter, EditorRequired] public PublicMyBookingsDto Bookings { get; set; } = null!;
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<Guid> OnCancel { get; set; }

    private void StopConfirming() => _confirmingId = null;

    private async Task ConfirmCancelAsync(Guid bookingId)
    {
        _confirmingId = null;
        await OnCancel.InvokeAsync(bookingId);
    }
}
