using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Attributes.Models;
using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Extensions;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    internal class ModelClassHeaderGenerator : IJSGenerator
    {
        private static readonly IEnumerable<string> ModelKeys = ["id", "isNew", "isValid", "invalidFields", "reload", "$on", "$off"];
        void IJSGenerator.GeneratorJS(StringBuilder builder, ModelType modelType, string baseURL, ILogger? log)
        {
            log?.LogTrace("Generating Model Definition javascript for {TypeName}", modelType.Type.FullName);
            builder.Append(@$" class {modelType.Type.Name} {{
        {Constants.INITIAL_DATA_KEY}=undefined;
        #isNew(){{ return this.{Constants.INITIAL_DATA_KEY}===undefined || this.{Constants.INITIAL_DATA_KEY}===null || this.{Constants.INITIAL_DATA_KEY}.id===undefined || this.{Constants.INITIAL_DATA_KEY}.id===null; }};
        #events=undefined;
        static get #baseURL(){{return `{(baseURL.StartsWith('/') ? "${hosturl.origin}" : "")}{baseURL}`;}};
        static get #constructorBaseData(){{return {{}};}};");

            modelType.Properties.ForEach(p => builder.AppendLine($"      #{p.Name}=undefined;"));

            ModelClassHeaderGenerator.AppendValidations(modelType.Properties, builder);
            ModelClassHeaderGenerator.AppendToProxy(builder, modelType.Properties, modelType.InstanceMethods
                .Concat(
                    modelType.HandlerType.GetMethods(Constants.METHOD_FLAGS)
                    .Where(m => m.GetCustomAttribute<EventStreamMethodAttribute>(false)!=null && Helper.IsExposedMethodInstance(m))
                ).DistinctBy(m => m.Name)
                , modelType);

            builder.AppendLine(@$"    constructor(){{
            this.{Constants.INITIAL_DATA_KEY} = {{}};
            {string.Join('\n', modelType.Properties.Select(p => $"         this.#{p.Name} = {modelType.Type.Name}.#constructorBaseData.{p.Name};"))}
            this.#events = new EventHandler(['{Constants.Events.MODEL_LOADED}','{Constants.Events.MODEL_UPDATED}','{Constants.Events.MODEL_SAVED}','{Constants.Events.MODEL_DESTROYED}','{Constants.Events.MODEL_PARSED}']);
            return this.#toProxy();
        }}");
        }

        private static void AppendToProxy(StringBuilder builder, IEnumerable<PropertyInfo> props, IEnumerable<MethodInfo> methods, ModelType modelType)
        {
            builder.AppendLine(@"#toProxy(){
    let me = this;
    return new Proxy(this,{
        get: function(target,prop,reciever){
            switch(prop){");
            props.ForEach(p => builder.AppendLine($"                  case '{p.Name}': return (me.#{p.Name}===undefined ? me.{Constants.INITIAL_DATA_KEY}.{p.Name} : me.#{p.Name}); break;"));
            methods.ForEach(m => builder.AppendLine($"                  case '{m.Name}': return function(){{ return me.#{m.Name}.apply(me,arguments);}}; break;"));
            if (modelType.SaveMethod!=null)
                builder.AppendLine("                  case 'save': return function(){{ return me.#save.apply(me,arguments);}}; break;");
            if (modelType.UpdateMethod!=null)
                builder.AppendLine("                  case 'update': return function(){{ return me.#update.apply(me,arguments);}}; break;");
            if (modelType.DeleteMethod!=null)
                builder.AppendLine("                  case 'destroy': return function(){{ return me.#destroy.apply(me,arguments);}}; break;");
            builder.AppendLine($"              case 'id': return (me.{Constants.INITIAL_DATA_KEY}===null || me.{Constants.INITIAL_DATA_KEY}===undefined ? null : me.{Constants.INITIAL_DATA_KEY}.id); break;");
            builder.AppendLine(@"                        case 'isNew': return function(){return me.#isNew();}; break;
                        case 'isValid': return function(){return me.#isValid();}; break;
                        case 'invalidFields': return function(){return me.#invalidFields();}; break;
                        case 'reload': return function(){return me.#reload();}; break;
                        case '$on': return function(event,callback) { me.#events.on(event,callback); }; break;
                        case '$off': return function(callback) { me.#events.off(callback); }; break;
                        default: 
                            if (me[prop]!==undefined && isFunction(me[prop]))
                                return function(){ return me[prop].apply(me,arguments); }
                            return me[prop]; 
                        break;
                    }
                },
                set: function(target,prop,value){
                    switch(prop) {");
            props.Where(p => p.CanWrite).ForEach(p =>
            {
                builder.AppendLine($@"      case '{p.Name}':  
                            me.#{p.Name} = checkProperty('{p.Name}','{Utility.GetTypeString(p.PropertyType, p.GetCustomAttribute(typeof(NotNullPropertyAttribute), false)!=null)}',value,{Utility.GetEnumList(p.PropertyType)}); 
                            return true;
                            break;");
            });
            methods.ForEach(m => builder.AppendLine($"                  case '{m.Name}': return false; break;"));
            builder.Append(@"                       case 'isNew': 
                        case 'isValid': 
                        case 'invalidFields':
                        case '$on':
                        case '$off': 
                            return false; 
                        break;
                    }
                    return Reflect.set(...arguments);
                },
                ownKeys:function(target){
                    return ");

            var keys = ModelKeys.Concat(props.Select(p => p.Name))
                .Concat(methods.Select(m => m.Name));

            if (modelType.SaveMethod != null)
                keys = keys.Append("save");
            if (modelType.UpdateMethod!=null)
                keys = keys.Append("update");
            if (modelType.DeleteMethod!=null)
                keys = keys.Append("destroy");

            builder.AppendLine(@$"['{string.Join("','", keys.Distinct())}'];
                }}
            }});
        }};");
        }

        private static void AppendValidations(IEnumerable<PropertyInfo> props, StringBuilder builder)
        {
            var requiredProps = props.Where(pi => pi.GetCustomAttributes(typeof(ModelRequiredFieldAttribute), false).Length > 0);
            if (requiredProps.Any())
            {
                builder.AppendLine(@$"   #isValid(){{ return {string.Join("&&", requiredProps.Select(p => $"this.#{p.Name}!==undefined&&this.#{p.Name}!==null"))};
        }};
    #invalidFields(){{
            let ret=[];");
                requiredProps.ForEach(pi =>
                {
                    builder.AppendLine(@$"         if (this.#{pi.Name}===undefined||this.#{pi.Name}===null){{
                ret.push('{pi.Name}');
            }}");
                });
                builder.AppendLine(@"           return ret;
    };");
            }
            else
                builder.AppendLine(@"   #isValid(){return true;};
    #invalidFields(){return [];};");
        }

    }
}
