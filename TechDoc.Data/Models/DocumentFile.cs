using System.Text.Json.Serialization;

namespace TechDoc.Data.Models
{
    internal class DocumentFile : Document
    {
        /// <summary>
        /// Internal document full path
        /// </summary>
        [JsonPropertyOrder(99)]
        public string FullPath { get; set; }
    }
}
