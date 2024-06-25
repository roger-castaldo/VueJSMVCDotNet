namespace VueJSMVCDotNet.Attributes
{
    /// <summary>
    /// Used to specify the js path that the javascript code for this model will be written to
    /// </summary>
    /// <param name="path">The url path to identify what url to provide the javascript definition of this model to.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModelJSFilePathAttribute(string path) : Attribute
    {
        internal string Path => path;
        internal string MinPath => Path.EndsWith(".min.js", StringComparison.InvariantCultureIgnoreCase) ? Path : $"{Path[..^2]}min.js";
        internal string ModulePath => $"{(Path.EndsWith(".min.js", StringComparison.InvariantCultureIgnoreCase) ? Path[..^6] : Path[..^2])}mjs";

        internal bool IsMatch(string url)
            => Path.Equals(url, StringComparison.InvariantCultureIgnoreCase)
                || MinPath.Equals(url, StringComparison.InvariantCultureIgnoreCase)
                || ModulePath.Equals(url, StringComparison.InvariantCultureIgnoreCase);
    }
}
