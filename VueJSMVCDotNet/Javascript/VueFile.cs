using System.Text.Json.Serialization;

namespace VueJSMVCDotNet.Javascript
{
    internal record VueFile(
        [property:JsonPropertyName("id")]
        string ID,
        [property:JsonPropertyName("name")]
        string Name,
        [property:JsonIgnore()]
        string Content
    ) 
    {}
}
