using Kx.Availability.Data.Interfaces;
using Kx.Availability.Data.Mongo.StoredModels;
using Kx.Core.Common.Interfaces;
using MongoDB.Driver;

/*
 * This isn't an ideal implementation but a WIP representation of how things could start to be broken out
 * Any index related calls would ideally be in a Mongo specific library, abstracted, so that other data stores
 * can utilise the same approach (SQL, Elasticsearch etc) without being tied to Mongo
*/
namespace Kx.Availability.Data.Implementation
{
    public class TenantService : ITenantService
    {
        private readonly IDataAggregationStoreAccess<BedroomsDataStoreModel> _roomsData;
        private readonly IDataAggregationStoreAccess<LocationsDataStoreModel> _locationsData;

        public TenantService(
            IDataAccessFactory dataAccessFactory, IBedroomDataService bedroomDataService, ITenant tenant
            )
        {
            _locationsData = dataAccessFactory.GetDataStoreAccess<LocationsDataStoreModel>();
            _roomsData = dataAccessFactory.GetDataStoreAccess<BedroomsDataStoreModel>();
        }

        public async Task CleanTenantTempTablesAsync()
        {
            await _locationsData.DeleteAsync();
            await _roomsData.DeleteAsync();
        }

        public async Task CreateIndexes()
        {
            await CreateLocationsIndexes();
            await CreateRoomsIndexes();
        }

        private async Task CreateRoomsIndexes()
        {
            var indexBuilder = Builders<BedroomsDataStoreModel>.IndexKeys;
            var indexModel = new CreateIndexModel<BedroomsDataStoreModel>(indexBuilder.Ascending(x => x.RoomId));
            await _roomsData.AddIndex(indexModel);
        }

        private async Task CreateLocationsIndexes()
        {
            var indexBuilder = Builders<LocationsDataStoreModel>.IndexKeys;
            var indexModel = new CreateIndexModel<LocationsDataStoreModel>(indexBuilder
                .Ascending(x => x.ExternalId)
                .Ascending(x => x.Type)
                .Ascending(x => x.Id)
                .Ascending(p => p.ParentId));
            await _locationsData.AddIndex(indexModel);
        }        
    }
}
