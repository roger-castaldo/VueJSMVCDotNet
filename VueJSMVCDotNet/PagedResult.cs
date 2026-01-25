using System.Text.Json.Serialization;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model
{
    /// <summary>
    /// Used as the returned result for a paged call for a given model
    /// </summary>
    /// <typeparam name="M">The type of model being returned</typeparam>
    /// <param name="Data">Will house the page of the results</param>
    /// <param name="TotalPages">Houses the total number of pages available</param>
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
