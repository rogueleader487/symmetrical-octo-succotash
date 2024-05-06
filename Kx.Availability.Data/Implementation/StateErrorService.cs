using Kx.Availability.Data.Common;
using Kx.Availability.Data.Interfaces;
using Kx.Core.Common.Data;
using Kx.Core.Common.Interfaces;
using Serilog;

namespace Kx.Availability.Data.Implementation
{
    /*
     * This is an example of how the State Error specific calls could be broken out from the DataAggregationService.
     * Potentially this would be in a Core service if relevant - more application/domain experience knowledge would confirm or deny this
    */
    public class StateErrorService : IStateErrorService
    {
        private readonly IDataAccessAggregation _dataAccessAggregation;

        public StateErrorService(IDataAccessFactory dataAccessFactory)
        {
            var dbAccessAggregate = dataAccessFactory.GetDataAccess(KxDataType.AvailabilityAggregation);
            _dataAccessAggregation = DataAccessHelper.ParseAggregationDataAccess(dbAccessAggregate);
        }

        public async Task LogStateErrorsAsync(LocationType changeTableType, Exception ex)
        {
            await _dataAccessAggregation.UpdateStateAsync(
            StateEventType.CycleError,
            true,
            ex.ToString());

            Log.Logger.Error(
                "Error inserting {S}{FullMessage}",
                changeTableType.ToString(),
                ex.ToString());
        }
    }
}
