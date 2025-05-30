using NUglify;
using NUglify.JavaScript;

namespace VueJSMVCDotNet
{
    internal static class JSMinifier
    {
        public static string Minify(string js, bool ignoreComments = false)
        {
            var result = Uglify.Js(js, new()
            {
                PreserveFunctionNames = true,
                ScriptVersion = ScriptVersion.None,
                PreserveImportantComments = ignoreComments,
                SourceMode = JavaScriptSourceMode.Module
            });
            return (result.HasErrors ? throw new JavascriptMinificationException(result.Errors) : result.Code);
        }

    }
}
