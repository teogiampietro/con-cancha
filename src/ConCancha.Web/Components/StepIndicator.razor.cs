using Microsoft.AspNetCore.Components;

namespace ConCancha.Web.Components;

public partial class StepIndicator
{
    private const int TotalSteps = 4;

    [Parameter] public int Current { get; set; }

    private string GetStepClass(int step)
    {
        if (step == Current) return "on";
        return step < Current ? "done" : string.Empty;
    }
}
