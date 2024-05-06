namespace Kx.Availability.Data.Interfaces
{
    public interface ITenantService
    {
        Task CleanTenantTempTablesAsync();

        Task CreateIndexes();
    }
}
