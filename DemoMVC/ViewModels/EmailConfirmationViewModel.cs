namespace DemoMVC.ViewModels;

public sealed class EmailConfirmationViewModel
{
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
}
