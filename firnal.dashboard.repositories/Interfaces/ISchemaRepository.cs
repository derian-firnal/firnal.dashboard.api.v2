namespace firnal.dashboard.repositories.Interfaces
{
    public interface ISchemaRepository
    {
        Task<List<string>> GetAll();
    }
}
