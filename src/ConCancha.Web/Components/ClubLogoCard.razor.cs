using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using ConCancha.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ConCancha.Web.Components;

public partial class ClubLogoCard : CancellableComponentBase
{
    private const string AcceptedTypes = "image/png,image/jpeg";
    private const string FallbackContentType = "application/octet-stream";

    private string? _errorMessage;
    private bool _isBusy;
    private bool _isSaved;

    [Inject] private ApiClient Api { get; set; } = null!;

    [Parameter, EditorRequired] public ClubDto Club { get; set; } = null!;
    [Parameter] public EventCallback<ClubDto> OnSaved { get; set; }

    private async Task UploadAsync(InputFileChangeEventArgs args)
    {
        Begin();
        var result = await TryUploadAsync(args.File, ComponentToken);
        await FinishAsync(result);
    }

    private async Task RemoveAsync(CancellationToken cancellationToken)
    {
        Begin();
        var result = await Api.RemoveClubLogoAsync(cancellationToken);
        await FinishAsync(result);
    }

    private async Task<ApiResult<ClubDto>> TryUploadAsync(IBrowserFile file, CancellationToken cancellationToken)
    {
        try
        {
            await using var content = file.OpenReadStream(LogoRules.MaxBytes + 1, cancellationToken);
            var contentType = string.IsNullOrEmpty(file.ContentType) ? FallbackContentType : file.ContentType;
            return await Api.UploadClubLogoAsync(content, file.Name, contentType, cancellationToken);
        }
        catch (IOException)
        {
            return ApiResult<ClubDto>.Failure(LogoRules.FileTooLargeMessage, StatusCodes.Status400BadRequest);
        }
    }

    private void Begin()
    {
        _errorMessage = null;
        _isSaved = false;
        _isBusy = true;
    }

    private async Task FinishAsync(ApiResult<ClubDto> result)
    {
        _isBusy = false;
        _errorMessage = result.IsSuccess ? null : result.GetFirstFieldError(ApiFieldNames.Logo) ?? result.GeneralMessage;
        _isSaved = result.IsSuccess;
        if (result.IsSuccess) await OnSaved.InvokeAsync(result.Value);
    }
}
