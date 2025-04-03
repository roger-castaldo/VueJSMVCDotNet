using System.Text.Json.Serialization;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model
{
    public record PagedResult<M>(
        [property:JsonPropertyName("data")]
        IEnumerable<M> Data,
        [property:JsonPropertyName("totalPages")]
        int TotalPages
    )
        where M : IModel
    {
    }
}
