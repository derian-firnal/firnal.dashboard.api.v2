namespace firnal.dashboard.services.Interfaces
{
    public interface ITwilioService
    {
        Task SendSmsAsync(string user);
    }
}
