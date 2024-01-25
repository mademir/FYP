using System.Text.Json.Serialization;

namespace Networking
{
    public class Client : COM.Client
    {
        [JsonIgnore] public string IP;
    }
}