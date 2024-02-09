using Microsoft.AspNetCore.Authentication;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelClassHeaderGenerator : IJSGenerator
    {
        

        public void GeneratorJS(WrappedStringBuilder builder, SModelType modelType, string urlBase, ILogger log)
        {
            log?.LogTrace("Generating Model Definition javascript for {}", modelType.Type.FullName);

            builder.AppendLine(@$" class {modelType.Type.Name} {{
        {Constants.INITIAL_DATA_KEY}=undefined;
        #isNew(){{ return this.{Constants.INITIAL_DATA_KEY}===undefined || this.{Constants.INITIAL_DATA_KEY}===null || this.{Constants.INITIAL_DATA_KEY}.id===undefined || this.{Constants.INITIAL_DATA_KEY}.id===null; }};
        #events=undefined;
        static get #baseURL(){{return '{Utility.GetModelUrlRoot(modelType.Type, urlBase)}';}};");

            modelType.Properties.ForEach(p => builder.AppendLine($"      #{p.Name}=undefined;"));

            ModelClassHeaderGenerator.AppendValidations(modelType.Properties, builder,log);
            ModelClassHeaderGenerator.AppendToProxy(builder,modelType.Properties, modelType.InstanceMethods, modelType);

            builder.AppendLine(@$"    constructor(){{
            this.{Constants.INITIAL_DATA_KEY} = {{}};
            let data={Utility.JsonEncode(modelType.Type.GetConstructor(Type.EmptyTypes).Invoke(null),log)};
            Object.keys(data).forEach((prop)=>this['#'+prop]=data[prop]);
            this.#events = new EventHandler(['{Constants.Events.MODEL_LOADED}','{Constants.Events.MODEL_UPDATED}','{Constants.Events.MODEL_SAVED}','{Constants.Events.MODEL_DESTROYED}','{Constants.Events.MODEL_PARSED}']);
            return this.#toProxy();
        }}");
        }

        private static void AppendToProxy(WrappedStringBuilder builder, IEnumerable<PropertyInfo> props, IEnumerable<MethodInfo> methods, SModelType modelType)
        {
            builder.AppendLine(@"#toProxy(){
    let me = this;
    return new Proxy(this,{
        get: function(target,prop,reciever){
            switch(prop){");
            props.ForEach(p => builder.AppendLine($"                  case '{p.Name}': return (me.#{p.Name}===undefined ? me.{Constants.INITIAL_DATA_KEY}.{p.Name} : me.#{p.Name}); break;"));
            methods.ForEach(m => builder.AppendLine($"                  case '{m.Name}': return function(){{ return me.#{m.Name}.apply(me,arguments);}}; break;"));
            if (modelType.HasSave)
                builder.AppendLine("                  case 'save': return function(){{ return me.#save.apply(me,arguments);}}; break;");
            if (modelType.HasUpdate)
                builder.AppendLine("                  case 'update': return function(){{ return me.#update.apply(me,arguments);}}; break;");
            if(modelType.HasDelete)
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
                            me.#{p.Name} = checkProperty('{p.Name}','{Utility.GetTypeString(p.PropertyType, p.GetCustomAttribute(typeof(NotNullProperty), false)!=null)}',value,{Utility.GetEnumList(p.PropertyType)}); 
                            return true;
                            break;");
            });
            methods.ForEach(m=>builder.AppendLine($"                  case '{m.Name}': return false; break;"));
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
                    return ['id','isNew','isValid','invalidFields','reload','$on','$off',");

            builder.Append(string.Join(',', props.Select(p => $"'{p.Name}'").Concat(methods.Select(m => $"'{m.Name}'"))));
            if (!props.Any()&&!methods.Any())
                builder.Length--;
            if (modelType.HasSave)
                builder.Append(",'save'");
            if (modelType.HasUpdate)
                builder.Append(",'update'");
            if (modelType.HasDelete)
                builder.Append(",'destroy'");
            builder.AppendLine(@"];
                }
            });
        };");
        }

        private static void AppendValidations(IEnumerable<PropertyInfo> props, WrappedStringBuilder builder, ILogger log)
        {
            var requiredProps = props.Where(pi => pi.GetCustomAttributes(typeof(ModelRequiredField), false).Length > 0);
            if (requiredProps.Any())
            {
                builder.AppendLine(@$"   #isValid(){{ return {string.Join("&&",requiredProps.Select(p=>$"this.#{p.Name}!==undefined&&this.#{p.Name}!==null"))};
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
