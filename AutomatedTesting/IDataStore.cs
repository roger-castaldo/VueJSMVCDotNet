namespace AutomatedTesting
{
    public interface IDataStore
    {
        object this[string key] { get; set; }
    }
}
