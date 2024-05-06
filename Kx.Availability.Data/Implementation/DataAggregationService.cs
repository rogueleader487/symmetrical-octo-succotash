using Kx.Availability.Data.Interfaces;
using Kx.Availability.Data.Mongo.Models;
using Kx.Core.Common.Data;
using Kx.Core.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Serilog;
using System.Data;
using System.Net;
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable PossibleMultipleEnumeration

namespace Kx.Availability.Data.Implementation;

/*
 * This is a large class that has multiple responsibilities across multiple object types.
 * Can be refacored out into multiple smaller discrete services. 
 * These independent elements can then be tested in isolation where appropriate
 * Data Access needs would need thought to ensure consistency across services (without using a singleton approach with multiple tenancy)
*/
public class DataAggregationService : IDataAggregationService
{
    private readonly ITenant _tenant;   
    private readonly IDataAccessAggregation _aggregateData;
    private readonly string? _mongoId;
    private readonly ITenantService _tenantService;
    private readonly IBedroomDataService _bedroomDataService;
    private readonly ILocationService _locationService;

    public DataAggregationService(IDataAccessFactory dataAccessFactory, ITenant tenant, IConfiguration config, ITenantService tenantService, IBedroomDataService bedroomDataService,
        ILocationService locationService)
    {
        _tenant = tenant;                

        var dbAccessAggregate = dataAccessFactory.GetDataAccess(KxDataType.AvailabilityAggregation);
       _aggregateData = DataAccessHelper.ParseAggregationDataAccess(dbAccessAggregate);
                     
        _mongoId = config.GetSection("MongoID").Value ?? null;

        _locationService = locationService;  
        _tenantService = tenantService;
        _bedroomDataService = bedroomDataService;
    }

    public async Task<(HttpStatusCode statusCode, string result)> ReloadOneTenantsDataAsync()
    {
        try
        {
            _aggregateData.StartStateRecord();

            Log.Information("Cleaning tmp table");
            await _tenantService.CleanTenantTempTablesAsync();

            await _tenantService.CreateIndexes();

            //1. Get Locations and store them
            var locationsTask = _locationService.InsertLocationsAsync();

            //2. Get the rooms and store them
            var roomsTask = _bedroomDataService.InsertRoomsAsync();

            await Task.WhenAll(locationsTask, roomsTask);

            //3. Combine them together
            //make the main table from all imported tables            
            await CombineTempTablesIntoTheAvailabilityModelAsync();

            //4. save tenantAvailabilityModel.            
            await MoveTempTenantToLive();
            await _tenantService.CleanTenantTempTablesAsync();

            return (HttpStatusCode.NoContent, string.Empty);

        }
        catch (Exception ex)
        {
            return (HttpStatusCode.ExpectationFailed, ex.Message);
        }
    }

    public async Task InsertStateAsync(ITenantDataModel item)
    {
        await _aggregateData.InsertStateAsync(item);
    }

    //Renaming, personal preference/opinion but 'Mash' feels incorrect for concise method naming
    private async Task CombineTempTablesIntoTheAvailabilityModelAsync()
    {
        try
        {
            var aggregatedAvailabilityModel = GetAggregatedDataStoreModel();

            var rooms = _bedroomDataService.GetRooms();

            if (rooms is null || !rooms.Any()) throw new DataException();

            foreach (var room in rooms)
            {
                var availabilityModel = CreateAvailabilityMongoModel();
                availabilityModel.ID = _mongoId;
                if (_mongoId is null)
                {
                    availabilityModel.ID = Convert.ToString(availabilityModel.GenerateNewID());
                }

                availabilityModel.TenantId = _tenant.TenantId;
                availabilityModel.RoomId = room.RoomId;

                var addLocations = _locationService.AddLocationModels(room);
                availabilityModel.Locations.AddRange(addLocations);

                aggregatedAvailabilityModel.Availability.Add(availabilityModel);
            }

            await _aggregateData.InsertAsync(aggregatedAvailabilityModel);

        }
        catch (Exception ex)
        {
            Log.Error($"Failed to combine data together: {ex}");
            throw;
        }
    }

    private async Task MoveTempTenantToLive()
    {
        await _aggregateData.UpdateAsync();
    }

    private AvailabilityMongoModel CreateAvailabilityMongoModel()
    {
        return new AvailabilityMongoModel
        {
            TenantId = _tenant.TenantId,
            RoomId = string.Empty,
            Locations = new List<LocationModel>()
        };
    }

    private AggregatedAvailabilityModel GetAggregatedDataStoreModel()
    {
        var data = new AggregatedAvailabilityModel
        {
            TenantId = _tenant.TenantId
        };
        return data;
    }

    
}
