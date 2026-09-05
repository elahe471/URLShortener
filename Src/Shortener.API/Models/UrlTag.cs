

using MongoDB.Bson;

namespace Shortener.API.Models
{
    [Collection("UrlTags")]
    public class UrlTag
    {
        public ObjectId Id { get; set; }
        public string ShortenedCode { get; set; } = default!;
        public required string DestinationURL { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ExpirationDate { get; set; }
        
    }
}
