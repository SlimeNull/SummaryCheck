using System.Text.Json.Serialization;

namespace SummaryCheck.Models
{
    public struct Md5Info
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("md5")]
        public string Md5 { get; set; }
    }
}
