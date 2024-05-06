using Kx.Availability.Data.Common;

namespace Kx.Availability.Data.Interfaces
{
    public interface IStateErrorService
    {
        Task LogStateErrorsAsync(LocationType changeTableType, Exception ex);
    }
}
