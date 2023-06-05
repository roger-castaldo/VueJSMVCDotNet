using VueJSMVCDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelClassHeaderGenerator : IJSGenerator
    {
        

        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase, ILog log)
        {
            log.Trace("Generating Model Definition javascript for {0}", new object[] { modelType.Type.FullName });

            builder.AppendLine(@$" class {modelType.Type.Name} {{
        {Constants.INITIAL_DATA_KEY}=undefined;
        #isNew(){{ return this.{Constants.INITIAL_DATA_KEY}===undefined || this.{Constants.INITIAL_DATA_KEY}===null || this.{Constants.INITIAL_DATA_KEY}.id===undefined || this.{Constants.INITIAL_DATA_KEY}.id===null; }};
        #events=undefined;
        static get #baseURL(){{return '{Utility.GetModelUrlRoot(modelType.Type, urlBase)}';}};");

            foreach (PropertyInfo p in modelType.Properties)
                builder.AppendLine($"      #{p.Name}=undefined;");

            _AppendValidations(modelType.Properties, ref builder,log);
            _AppendToProxy(ref builder,modelType.Properties, modelType.InstanceMethods, modelType);

            builder.AppendLine(@$"    constructor(){{
            this.{Constants.INITIAL_DATA_KEY} = {{}};
            let data={Utility.JsonEncode(modelType.Type.GetConstructor(Type.EmptyTypes).Invoke(null),log)};
            for(let prop in data){{
                this['#'+prop]=data[prop];
            }}
            this.#events = new EventHandler(['{Constants.Events.MODEL_LOADED}','{Constants.Events.MODEL_UPDATED}','{Constants.Events.MODEL_SAVED}','{Constants.Events.MODEL_DESTROYED}','{Constants.Events.MODEL_PARSED}']);
            return this.#toProxy();
        }}");
        }

        private void _AppendToProxy(ref WrappedStringBuilder builder, IEnumerable<PropertyInfo> props, IEnumerable<MethodInfo> methods, sModelType modelType)
        {
            builder.AppendLine(@"#toProxy(){
    let me = this;
    return new Proxy(this,{
        get: function(target,prop,reciever){
            switch(prop){");
            foreach (PropertyInfo p in props)
                builder.AppendLine($"                  case '{p.Name}': return (me.#{p.Name}===undefined ? me.{Constants.INITIAL_DATA_KEY}.{p.Name} : me.#{p.Name}); break;");
            foreach (MethodInfo m in methods)
                builder.AppendLine($"                  case '{m.Name}': return function(){{ return me.#{m.Name}.apply(me,arguments);}}; break;");
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
            foreach (PropertyInfo p in props)
                if (p.CanWrite)
                {
                    builder.AppendLine($@"      case '{p.Name}':  
                            me.#{p.Name} = checkProperty('{p.Name}','{Utility.GetTypeString(p.PropertyType, p.GetCustomAttribute(typeof(NotNullProperty), false)!=null)}',value,{Utility.GetEnumList(p.PropertyType)}); 
                            return true;
                            break;");
                }
            foreach (MethodInfo m in methods)
                builder.AppendLine($"                  case '{m.Name}': return false; break;");
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
                    return ['id','isNew','isValid','invalidFields','reload','$on','$off'");
            foreach (PropertyInfo p in props)
                builder.Append($",'{p.Name}'");
            foreach (MethodInfo mi in methods)
                builder.Append($",'{mi.Name}'");
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

        private void _AppendValidations(IEnumerable<PropertyInfo> props, ref WrappedStringBuilder builder, ILog log)
        {
            List<PropertyInfo> requiredProps = new List<PropertyInfo>();
            foreach (PropertyInfo pi in props)
            {
                if (pi.GetCustomAttributes(typeof(ModelRequiredField), false).Length > 0)
                    requiredProps.Add(pi);
            }
            if (requiredProps.Count > 0)
            {
                builder.AppendLine(@"   #isValid(){
                    let ret=true;");
                foreach (PropertyInfo pi in requiredProps)
                {
                    log.Trace("Appending Required Propert[{0}] for Model Definition[{1}] validations", new object[]{
                        pi.Name,
                        pi.DeclaringType.FullName
                    });
                    builder.AppendLine($"          ret=ret&&(this.#{pi.Name}==undefined||this.#{pi.Name}==null ? false : true);");
                }
                builder.AppendLine(@"           return ret;
        };
    #invalidFields(){
            let ret=[];");
                foreach (PropertyInfo pi in requiredProps)
                    builder.AppendLine(@$"         if (this.#{pi.Name}==undefined||this.#{pi.Name}==null){{
                ret.push('{pi.Name}');
            }}");
                builder.AppendLine(@"           return ret;
    };");
            }
            else
                builder.AppendLine(@"   #isValid(){return true;};
    #invalidFields(){return [];};");
        }
    }
}
