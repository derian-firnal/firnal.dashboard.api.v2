using firnal.dashboard.repositories.Interfaces;
using firnal.dashboard.services.Interfaces;

namespace firnal.dashboard.services
{
    public class SchemaService : ISchemaService
    {
        private readonly ISchemaRepository _schemaRepository;
        private readonly IUserRepository _userRepository;

        public SchemaService(ISchemaRepository schemaRepository, IUserRepository userRepository)
        {
            _schemaRepository = schemaRepository;
            _userRepository = userRepository;
        }

        public async Task<List<string>> GetAll()
        {
            return await _schemaRepository.GetAll();
        }

        public async Task<List<string>> GetSchemaForUserId(string? userEmail)
        {
            var user = await _userRepository.GetUserByEmail(userEmail);
            
            if (user != null && user?.RoleName?.ToLower() == "admin")
                return await _schemaRepository.GetAll();
            else
                return user.Schemas.Select(s => s?.SchemaName).ToList();
        }
    }
}
