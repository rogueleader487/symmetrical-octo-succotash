using Kx.Availability.Data.Mongo.Models;
using Kx.Availability.Data.Mongo.StoredModels;

namespace Kx.Availability.Data.Interfaces
{
    public interface ILocationService
    {
        Task InsertLocationsAsync();

        IEnumerable<LocationModel> AddLocationModels(BedroomsDataStoreModel room);
    }
}
