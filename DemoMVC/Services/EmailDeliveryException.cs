namespace DemoMVC.Services;

public sealed class EmailDeliveryException : Exception
{
    public EmailDeliveryException(string message) : base(message)
    {
    }
}
