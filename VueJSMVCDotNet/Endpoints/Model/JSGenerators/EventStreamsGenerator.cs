using System.Threading;
using System.Threading.Channels;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Extensions;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    internal class EventStreamsGenerator : IJSGenerator
    {
        void IJSGenerator.GeneratorJS(StringBuilder builder, ModelType modelType, string baseURL, ILogger? log)
        {
            modelType.HandlerType.GetMethods(Constants.METHOD_FLAGS)
                .Where(m => m.GetCustomAttribute<EventStreamMethodAttribute>(false)!=null)
                .ForEach(m =>
                {
                    var method = new InjectableMethod(m, []);
                    builder.Append($@"          {(!method.RequiresModel ? "static " : "#")}{method.Name}(");
                    var pars = method.StrippedParameters
                        .Where(par => !Equals(par.ParameterType, typeof(ChannelWriter<object>)) && !Equals(par.ParameterType, typeof(CancellationToken)));
                    builder.AppendLine($@"{string.Join(',', pars.Select(p => p.Name))}){{
                    let pars = {{}};");
                    pars.ForEach(par => builder.AppendLine($"                   pars.{par.Name} = checkProperty('{par.Name}','{Utility.GetTypeString(par.ParameterType, false)}',{par.Name},{Utility.GetEnumList(par.ParameterType)});"));
                    builder.AppendLine($@"                   let url = `${{{modelType.Type.Name}.#baseURL}}/{(!method.RequiresModel ? method.Name : $"${{this.{Constants.INITIAL_DATA_KEY}.id}}/{method.Name}")}?_=${{parseInt((new Date().getTime() / 1000).toFixed(0))}}`;
                    Object.keys(pars).forEach(prop => {{
                        url += `&${{prop}}=${{encodeURIComponent(pars[prop]??'{EventStreamHelper.NullKeyword}')}}`;
                    }});
                    const eventSource = new EventSource(url);
                    eventSource.addEventListener(""close"",(e)=>eventSource.close());
                    
                    return {{
                        onOpen : (callback) => eventSource.onopen = callback,
                        onMessageReceived : (callback) => {{
                            eventSource.addEventListener(""{EventStreamHelper.MessageEvent}"",(e)=>{{
                                callback(JSON.parse(e.data));
                            }});
                        }},
                        onClose : (callback) => {{
                            eventSource.addEventListener(""{EventStreamHelper.CloseEvent}"",(e)=>callback());
                        }}
                    }};
                }};");
                });
        }
    }
}
