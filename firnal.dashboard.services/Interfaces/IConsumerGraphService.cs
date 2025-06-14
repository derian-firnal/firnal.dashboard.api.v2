using firnal.dashboard.data;

namespace firnal.dashboard.services.Interfaces
{
    public interface IConsumerGraphService
    {
        public Task GetSearchResults(SolomonSearchRequest filters, string userEmail);
    }
}
