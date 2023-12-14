using Newtonsoft.Json;

namespace CloudFiles.Dto;

public class FileDto
{
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("name")]
    public string Name { get; set; } = null!;

    [JsonProperty("extension")]
    public string Extension { get; set; } = null!;

    [JsonProperty("size")]
    public long Size { get; set; }
}