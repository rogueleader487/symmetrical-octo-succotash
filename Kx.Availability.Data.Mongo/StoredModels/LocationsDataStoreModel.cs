using Kx.Core.Common.Interfaces;

namespace Kx.Availability.Data.Mongo.StoredModels;

/*
 * This model would be better stored in a generic Data project as opposed to Mongo specific
 * to allow reuse across differing data access implementations. 
 * Potentially as with BedroomsDataStoreModel, refactor into a Base class with implemntation specifis where needed
 */
public class LocationsDataStoreModel : IDataStoreModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ExternalId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? ParentId { get; set; } = null;
    public List<LocationsDataStoreModel> Locations { get; set; } = new ();
    public bool IsDirectLocation { get; set; } = false;
}
