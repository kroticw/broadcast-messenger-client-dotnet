using System.Text.Json.Serialization;

namespace connect;

public class ClientServerMessage {
    [JsonPropertyName("from")]
    public string from="";
    [JsonPropertyName("to")]
    public string to="";
    [JsonPropertyName("service_type")]
    public string serviceType="";
    [JsonPropertyName("service_data")]
    public string serviceData="";
    [JsonPropertyName("message")]
    public string message="";
    [JsonIgnore]
    public static int lengthFile = 0;
    [JsonPropertyName("file")]
    public byte[] file = new byte[lengthFile];
    
}