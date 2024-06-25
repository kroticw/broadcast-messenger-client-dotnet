using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace connect;

public class ClientServerMessage {
    [JsonProperty("from")]
    public string from {get; set;}

    [JsonProperty("to")]
    public string to {get; set;}

    [JsonProperty("servtype")]
    public string serviceType {get; set;}

    [JsonProperty("servdata")]
    public string serviceData {get; set;}

    [JsonProperty("message")]
    public string message {get; set;}
}