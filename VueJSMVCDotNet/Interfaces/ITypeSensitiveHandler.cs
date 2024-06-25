namespace VueJSMVCDotNet.Interfaces
{
    internal interface ITypeSensitiveHandler : IRequestHandler
    {
        void ClearTypes();
        void LoadTypes(IEnumerable<Type> types);
        void UnloadTypes(IEnumerable<Type> types);
    }
}
