using Kx.Availability.Data.Mongo.StoredModels;

namespace Kx.Availability.Data.Interfaces
{
    public interface IBedroomDataService
    {
        IQueryable<BedroomsDataStoreModel>? GetRooms();

        Task InsertRoomsAsync();
    }
}
