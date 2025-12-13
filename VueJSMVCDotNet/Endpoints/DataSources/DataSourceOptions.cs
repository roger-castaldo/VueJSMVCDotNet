namespace VueJSMVCDotNet.Endpoints.DataSources
{
    internal readonly record struct DataSourceOptions(
        string VueImportPath,
            string CoreJSURL,
            string CoreJSImport,
            bool IgnoreInvalidModels,
            bool CompressJS
    ){}
}
