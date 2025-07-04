namespace VueJSMVCDotNet.Interfaces
{
    /// <summary>
    /// This interface is injected into the services collection when adding in the VueJS Models support to allow for updating based on additional 
    /// libraries being loaded during runtime
    /// </summary>
    public interface IModelDataSource
    {
        /// <summary>
        /// Called when you add a new assembly to reload the available models
        /// </summary>
        void AssemblyAdded();
        /// <summary>
        /// Called when you wish to load available models from a given assembly load context
        /// </summary>
        /// <param name="contextName">The name of the context</param>
        void AsssemblyLoadContextAdded(string? contextName);
        /// <summary>
        /// Called when you wish to load available models from a given assembly load context
        /// </summary>
        /// <param name="alc">The assembly load context to process</param>
        /// <param name="triggerChange">Indicates if this should trigger a cache clear and full load instead of partial</param>
        void AsssemblyLoadContextAdded(AssemblyLoadContext alc, bool triggerChange = true);
        /// <summary>
        /// Called when you wish to unload the models from a given assembly load context
        /// </summary>
        /// <param name="contextName">The name of the context</param>
        void UnloadAssemblyContext(string? contextName);
        /// <summary>
        /// Called when you wish to unload the models from a given assembly load context
        /// </summary>
        /// <param name="alc">The assembly load context to process</param>
        void UnloadAssemblyContext(AssemblyLoadContext alc);

    }
}
