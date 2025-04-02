namespace VueJSMVCDotNet.Interfaces
{
    public interface IModelHandler<T>
        where T : IModel
    {
        ValueTask<T?> LoadAsync(string id);
    }
}
