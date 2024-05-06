using Kx.Availability.Data.Common;
using Kx.Availability.Data.Exceptions;
using Kx.Availability.Data.Interfaces;
using Kx.Availability.Data.Mongo.StoredModels;
using Kx.Core.Common.HelperClasses;
using Kx.Core.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace Kx.Availability.Data.Implementation
{
    /*
     * This is an example of how the Bedroom specific calls could be broken out from the DataAggregationService.
     * Methods in here can now be tested in isolation where needed as opposed to the single method in DataAggregationService needing 
     * many dependencies to be injected
    */
    public class BedroomDataService : IBedroomDataService
    {
        private readonly ITenant _tenant;
        private readonly IStateErrorService _stateErrorService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDataAggregationStoreAccess<BedroomsDataStoreModel> _roomsData;
        private readonly int _pageSize;

        private readonly string? _coreBedroomsUrl;

        public BedroomDataService(IStateErrorService stateErrorService, IDataAccessFactory dataAccessFactory, IConfiguration config, 
            ITenant tenant, IHttpClientFactory httpClientFactory)
        {
            _stateErrorService = stateErrorService;
            _httpClientFactory = httpClientFactory;
            _tenant = tenant;
            _roomsData = dataAccessFactory.GetDataStoreAccess<BedroomsDataStoreModel>();
            _coreBedroomsUrl = config.GetSection("BEDROOMS_URL").Value;
            _pageSize = 1000;
            if (int.TryParse(config.GetSection("DEFAULT_PAGE_SIZE").Value, out var pageSize))
            {
                _pageSize = pageSize;
            }            
        }

        public IQueryable<BedroomsDataStoreModel>? GetRooms()
        {
            return _roomsData.QueryFreely();
        }

        public async Task InsertRoomsAsync()
        {
            try
            {
                var pageOfRooms = await GetRoomsFromBedroomsApiAsync();
                await _roomsData.InsertPageAsync<PaginatedStoreModel<BedroomsDataStoreModel>>(pageOfRooms);

                if (pageOfRooms.TotalPages > 1)
                {

                    for (var i = 2; i <= pageOfRooms.TotalPages; i++)
                    {

                        var page = await GetRoomsFromBedroomsApiAsync(i);
                        await _roomsData.InsertPageAsync<PaginatedStoreModel<BedroomsDataStoreModel>>(page);
                    }
                }
            }
            catch (Exception ex)
            {
                await _stateErrorService.LogStateErrorsAsync(LocationType.Rooms, ex);
                throw;
            }
        }

        private async Task<IPaginatedModel<BedroomsDataStoreModel>> GetRoomsFromBedroomsApiAsync(int pageNo = 1)
        {
            var uriBuilder = new UriBuilder(_coreBedroomsUrl!)
            {
                Path = $"production/v1/{_tenant.TenantId}/bedrooms/rooms",
                Query = $"pageSize={_pageSize}&page={pageNo}"
            };

            var httpClient = _httpClientFactory.CreateClient(nameof(BedroomsDataStoreModel));

            var response = await httpClient.GetAsync(uriBuilder.ToString());

            return await response.Content.ReadFromJsonAsync<PaginatedStoreModel<BedroomsDataStoreModel>>() ??
                   throw new UnprocessableEntityException();
        }
    }
}
