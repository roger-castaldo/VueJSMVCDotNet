namespace VueJSMVCDotNet.Interfaces
{
    /// <summary>
    /// This interface is used to define the handler for a given model.  Inside this class you can implement all the desired callbacks, both the standard update/save/delete as 
    /// well as exposed methods, streams, and list calls.
    /// </summary>
    /// <typeparam name="T">The type of model that this class handles</typeparam>
    public interface IModelHandler<T>
        where T : IModel
    {
        /// <summary>
        /// Called to load a model instance with the given id
        /// </summary>
        /// <param name="id">The id of the model that was defined in the IModel</param>
        /// <returns>The particular instance of a given model</returns>
        ValueTask<T?> LoadAsync(string id);
    }
}
