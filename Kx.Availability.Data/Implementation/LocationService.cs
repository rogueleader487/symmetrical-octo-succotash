using Kx.Availability.Data.Common;
using Kx.Availability.Data.Exceptions;
using Kx.Availability.Data.Interfaces;
using Kx.Availability.Data.Mongo.Models;
using Kx.Availability.Data.Mongo.StoredModels;
using Kx.Core.Common.HelperClasses;
using Kx.Core.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Net.Http.Json;

namespace Kx.Availability.Data.Implementation
{
    /*
     * This is an example of how the Location specific calls could be broken out from the DataAggregationService.
     * Methods in here can now be tested in isolation where needed as opposed to the single method in DataAggregationService needing 
     * many dependencies to be injected
    */
    public class LocationService : ILocationService
    {
        private readonly IStateErrorService _stateErrorService;
        private readonly ITenant _tenant;
        private readonly string? _coreLocationsUrl;
        private readonly int _pageSize;
        private readonly IDataAggregationStoreAccess<LocationsDataStoreModel> _locationsData;
        private readonly IHttpClientFactory _httpClientFactory;

        public LocationService(IStateErrorService stateErrorService, IDataAccessFactory dataAccessFactory, IConfiguration config, ITenant tenant,
            IHttpClientFactory httpClientFactory)
        {
            _stateErrorService = stateErrorService;
            _locationsData = dataAccessFactory.GetDataStoreAccess<LocationsDataStoreModel>();
            _coreLocationsUrl = config.GetSection("LOCATIONS_URL").Value;
            _tenant = tenant;

            _pageSize = 1000;
            if (int.TryParse(config.GetSection("DEFAULT_PAGE_SIZE").Value, out var pageSize))
            {
                _pageSize = pageSize;
            }

            _httpClientFactory = httpClientFactory;
        }

        public async Task InsertLocationsAsync()
        {
            try
            {
                //paginate
                var pageOfLocations = await GetLocationsAsync(pageNo: 1);
                await _locationsData.InsertPageAsync<PaginatedStoreModel<LocationsDataStoreModel>>(pageOfLocations);

                if (pageOfLocations.TotalPages > 1)
                {
                    for (var i = 2; i <= pageOfLocations.TotalPages; i++)
                    {
                        var page = await GetLocationsAsync(pageNo: i);
                        await _locationsData.InsertPageAsync<PaginatedStoreModel<LocationsDataStoreModel>>(page);
                    }
                }
            }
            catch (Exception ex)
            {
                await _stateErrorService.LogStateErrorsAsync(LocationType.Locations, ex);
                throw;
            }
        }        

        public IEnumerable<LocationModel> AddLocationModels(BedroomsDataStoreModel room)
        {
            try
            {
                var locationsQuery = _locationsData.QueryFreely();

                /* Add the direct parent area */
                var tempLocations =
                    locationsQuery?
                        .Where(l => l.Id == room.LocationID
                                    && (l.Type.ToLower() != "area" && l.Type.ToLower() != "site"))
                        .Select(loc => new LocationModel
                        {
                            Id = loc.Id,
                            Name = loc.Name,
                            ParentId = loc.ParentId,
                            IsDirectLocation = true
                        }).ToList();


                if (!(tempLocations?.Count > 0))
                    return tempLocations as IEnumerable<LocationModel> ?? new List<LocationModel>();


                var currentTopLevelAreaIndex = 0;

                while (!tempLocations.Exists(x => x.ParentId == null))
                {
                    var parentLocation = tempLocations[currentTopLevelAreaIndex].ParentId;

                    var nextParentLocation =
                        locationsQuery?
                            .Where(l => l.Id == parentLocation)
                            .Select(loc => new LocationModel
                            {
                                Id = loc.Id,
                                Name = loc.Name,
                                ParentId = loc.ParentId,
                                IsDirectLocation = true
                            });

                    if (nextParentLocation != null && nextParentLocation.Any())
                    {
                        tempLocations.AddRange(nextParentLocation.ToList());
                        currentTopLevelAreaIndex++;
                    }
                    else
                    {
                        Log.Error(
                            $"The location has a parent Id where the location does not exist ParentId: {parentLocation}");
                        break;
                    }

                    if (currentTopLevelAreaIndex >= tempLocations.Count) break;

                }

                return tempLocations as IEnumerable<LocationModel> ?? new List<LocationModel>();

            }
            catch (Exception ex)
            {
                Task.FromResult(async () => await _stateErrorService.LogStateErrorsAsync(LocationType.Locations, ex));
                throw;
            }
        }

        private async Task<IPaginatedModel<LocationsDataStoreModel>> GetLocationsAsync(int pageNo = 1)
        {
            var uriBuilder = new UriBuilder(_coreLocationsUrl!)
            {
                Path = $"production/v1/{_tenant.TenantId}/locations",
                Query = $"pageSize={_pageSize}&page={pageNo}"
            };
            var httpClient = _httpClientFactory.CreateClient(nameof(LocationsDataStoreModel));

            var response = await httpClient.GetAsync(uriBuilder.ToString());

            return await response.Content.ReadFromJsonAsync<PaginatedStoreModel<LocationsDataStoreModel>>() ??
                   throw new UnprocessableEntityException();
        }
    }
}
