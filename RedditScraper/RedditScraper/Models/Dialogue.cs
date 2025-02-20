using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace RedditScraper.Models
{
    public class Dialogue
    {
        [BsonId]
        public ObjectId _id { get; set; }
        [BsonElement("conversation")]
        public List<Conversation> Conversation { get; set; } = new List<Conversation>();
    }

    public class Conversation
    {
        [BsonElement("speaker")]
        public string Speaker { get; set; } = string.Empty;
        [BsonElement("text")]
        public string Text { get; set; } = string.Empty;
    }
}
