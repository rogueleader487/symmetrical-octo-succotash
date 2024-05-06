using Kx.Core.Common.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities;

namespace Kx.Availability.Data.Mongo.StoredModels;

/* 
 * This model is tied to Mongo, if we were to implement another data storage system
 * this wouldn't work - potentially refactoring out into a base class that can be shared
 * across all options, with Mongo specific overrides applied here
*/
[BsonIgnoreExtraElements]

public class BedroomsDataStoreModel : IEntity, IDataStoreModel
{
    
    [BsonId] public string? ID { get; set; } = ObjectId.GenerateNewId().ToString();
   
    public object GenerateNewID() => ObjectId.GenerateNewId().ToString();

    public bool HasDefaultID()
    {
        return false;
    }

    public string Name { get; set; } = string.Empty;
    public string LocationID { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
}
