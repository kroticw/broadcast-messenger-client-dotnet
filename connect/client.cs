using System.Text.Json.Serialization;

namespace connect;

public class Client {
    [JsonPropertyName("client_ip")]
    public string ClientIp {get; set;}
    [JsonPropertyName("client_port")]
    public string ClientPort {get; set;}
    [JsonPropertyName("username")]
    public static string Username = "";
}