using Microsoft.ClearScript.V8;
using System.IO;
using System.Text.Json;

namespace VueJSMVCDotNet.Javascript
{
    internal class JSEngine : IDisposable
    {
        private const string sfcCompilerModule = "vue-compiler-sfc-esm-browser";
        private const string processFilesCall = "processFiles";
        private const string compressCodeCall = "compressCode";

        private const string invokableCode = @$"
const minifySettings = {{
  ecma: 2020,           // Support modern syntax (adjust if needed)
  module: true,         // REQUIRED for ESM
  compress: {{
    passes: 2,          // Run multiple compression passes
    pure_getters: true, // Assume object property access has no side-effects
    unsafe: true,       // Enable some aggressive optimizations (see below)
    unsafe_arrows: true,
    unsafe_methods: true,
    toplevel: true,     // Drop unused top-level bindings
    hoist_funs: true,
    hoist_vars: true,
  }},
  mangle: {{
    toplevel: true,     // Mangle top-level names
    module: true,       // Mangle ESM export names (if allowed)
  }},
  format: {{
    comments: false,    // Strip comments
    beautify: false     // Minified output
  }}
}};

async function {compressCodeCall}(code,setResult,setError){{
    try{{
        const result = await Terser.minify(code,minifySettings);
        if (setResult!==undefined && setResult!==null){{
            setResult(result.code);
        }}
        return result.code;
    }}catch(e){{
        setError(e.message??JSON.stringify(e));
    }}
}};

async function {processFilesCall}(files,setResult,setError,loadContent,produceResult){{
    try{{
        const {{ parse, compileScript, compileTemplate, compileStyle }} = await import('{sfcCompilerModule}');
        
        let loadedFiles = [];
        let tfiles = JSON.parse(files);

        for(let x=0;x<tfiles.length;x++){{
            loadedFiles.push({{
                id:tfiles[x].id,
                name:tfiles[x].name,
                content: loadContent(tfiles[x].name)
            }});
        }}

        const result = loadedFiles.map(file=>{{
            const {{ descriptor }} = parse(file.content,{{ id: file.id,filename:file.name }});

            const script = compileScript(descriptor, {{id: file.id,
                filename: file.name,
                isProd: true,
                sourceMap: false,
                inlineTemplate: false,
                genDefaultAs:file.id
            }}).content;

            let template = compileTemplate({{source: descriptor.template.content,
                    filename: file.name,
                    id: file.id,
                    isProd: true,
                    scoped: true
                }}).code;
            if (descriptor.scriptSetup !== null) {{
                template = template.replaceAll('_ctx.','$setup.');
            }}
            let style = '';


            if (descriptor.styles.length > 0) {{
                style = compileStyle({{source: descriptor.styles[0].content,
                    filename: file.name,
                    id: file.id,
                    scoped: descriptor.styles[0].scoped,
                }}).code;
            }}
            
            return produceResult(script,template,style);
        }});

        setResult(result);
    }}catch(e){{
        setError(e.message??JSON.stringify(e));
    }}
}};";

        private static readonly TimeSpan callTimeout = TimeSpan.FromMinutes(1);

        private readonly V8ScriptEngine v8Engine;
        private bool disposedValue;

        public record CompileResult(string Script, string TemplateScript, string StyleScript);

        public JSEngine()
        {
            v8Engine = new V8ScriptEngine(
                //"V8Test",
                V8ScriptEngineFlags.EnableTaskPromiseConversion|V8ScriptEngineFlags.EnableDynamicModuleImports
            //|V8ScriptEngineFlags.EnableDebugging | V8ScriptEngineFlags.AwaitDebuggerAndPauseOnStart,
            //9222
            );


            using var srCompressor = new StreamReader(typeof(JSEngine).Assembly.GetManifestResourceStream("VueJSMVCDotNet.Javascript.compressor.min.js")!);
            var compressorCode = srCompressor.ReadToEnd();
            srCompressor.Close();

            v8Engine.DocumentSettings.Loader = new EmbeddedDocumentLoader(new Dictionary<string, string>
            {
                {sfcCompilerModule,"VueJSMVCDotNet.Javascript.compiler-sfc.esm-browser.js" }
            });
            v8Engine.Execute(compressorCode);
            v8Engine.Execute(invokableCode);
        }

        private static async Task<T> AwaitTaskWithTimeout<T>(Task<T> task, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(task, timeoutTask);
            if (completedTask==timeoutTask)
                throw new TimeoutException();
            return (task.IsCompletedSuccessfully ? task.Result : throw task.Exception!);
        }

        public async ValueTask<string> CompressCodeAsync(string code)
        {
            var result = new TaskCompletionSource<string>();
            v8Engine.Invoke(compressCodeCall, code,
                new Action<string>((content) => result.TrySetResult(content)),
                new Action<string>((content) => result.TrySetException(new Exception(content)))
            );
            return await AwaitTaskWithTimeout<string>(result.Task, callTimeout);
        }

        public async ValueTask<IEnumerable<CompileResult>> CompileVueFilesAsync(IEnumerable<VueFile> files)
        {
            var result = new TaskCompletionSource<IEnumerable<CompileResult>>();
            v8Engine.Invoke(processFilesCall, JsonSerializer.Serialize(files),
                new Action<IEnumerable<object>>((content) => result.TrySetResult(content.Select(o => (CompileResult)o))),
                new Action<string>((content) => result.TrySetException(new Exception(content))),
                new Func<string, string>((name) => files.FirstOrDefault(f => string.Equals(f.Name, name))?.Content??throw new FileNotFoundException()),
                new Func<string, string, string, CompileResult>((script, template, style) => new(script, template, style))
            );
            return await AwaitTaskWithTimeout<IEnumerable<CompileResult>>(result.Task, callTimeout);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    v8Engine.Dispose();
                }
                disposedValue=true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
