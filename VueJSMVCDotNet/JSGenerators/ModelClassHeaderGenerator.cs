using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ModelClassHeaderGenerator : IJSGenerator
    {
        

        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType, string urlBase)
        {
            Logger.Trace("Generating Model Definition javascript for {0}", new object[] { modelType.FullName });
            string urlRoot = Utility.GetModelUrlRoot(modelType,urlBase);
            List<PropertyInfo> props = Utility.GetModelProperties(modelType);
            List<MethodInfo> methods = Utility.GetModelMethods(modelType, false);

            builder.AppendLine(string.Format(@" class {0} {{
        {1}=undefined;
        #isNew(){{ return this.{1}===undefined || this.{1}===null || this.{1}.id===undefined || this.{1}.id===null; }};
        #events=undefined;
        static get #baseURL(){{return '{2}';}};", new object[] { modelType.Name,Constants.INITIAL_DATA_KEY, Utility.GetModelUrlRoot(modelType,urlBase)}));

            foreach (PropertyInfo p in props)
                builder.AppendLine(string.Format("      #{0}=undefined;", p.Name));

            _AppendValidations(props, ref builder);
            _AppendToProxy(ref builder,props, methods, modelType);

            builder.AppendLine(string.Format(@"    constructor(){{
            this.{0} = {{}};
            let data={1};
            for(let prop in data){{
                this['#'+prop]=data[prop];
            }}
            this.#events = new EventHandler(['{2}','{3}','{4}','{5}','{6}']);
            return this.#toProxy();
        }}", new object[] {
                Constants.INITIAL_DATA_KEY, 
                JSON.JsonEncode(modelType.GetConstructor(Type.EmptyTypes).Invoke(null)),
                Constants.Events.MODEL_LOADED,
                Constants.Events.MODEL_UPDATED,
                Constants.Events.MODEL_SAVED,
                Constants.Events.MODEL_DESTROYED,
                Constants.Events.MODEL_PARSED
            }));
        }

        private void _AppendToProxy(ref WrappedStringBuilder builder, List<PropertyInfo> props, List<MethodInfo> methods, Type modelType)
        {
            builder.AppendLine(@"#toProxy(){
    let me = this;
    return new Proxy(this,{
        get: function(target,prop,reciever){
            switch(prop){");
            foreach (PropertyInfo p in props)
                builder.AppendLine(string.Format("                  case '{0}': return (me.#{0}===undefined ? me.{1}.{0} : me.#{0}); break;", new object[] { p.Name, Constants.INITIAL_DATA_KEY }));
            foreach (MethodInfo m in methods)
                builder.AppendLine(string.Format("                  case '{0}': return function(){{ return me.#{0}.apply(me,arguments);}}; break;", m.Name));
            foreach (MethodInfo mi in modelType.GetMethods(Constants.STORE_DATA_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0)
                    builder.AppendLine("                  case 'save': return function(){{ return me.#save.apply(me,arguments);}}; break;");
                else if (mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0)
                    builder.AppendLine("                  case 'update': return function(){{ return me.#update.apply(me,arguments);}}; break;");
                else if (mi.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length > 0)
                    builder.AppendLine("                  case 'destroy': return function(){{ return me.#destroy.apply(me,arguments);}}; break;");
            }
            builder.AppendLine(string.Format(@"              case 'id': return (me.{0}===null || me.{0}===undefined ? null : me.{0}.id); break;", new object[] { Constants.INITIAL_DATA_KEY }));
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
                    builder.AppendLine(string.Format(@"      case '{0}':  
                            me.#{0} = _checkProperty('{0}','{1}',value,{2}); 
                            return true;
                            break;", new object[] {
                        p.Name,
                        Utility.GetTypeString(p.PropertyType,p.GetCustomAttribute(typeof(NotNullProperty),false)!=null),
                        Utility.GetEnumList(p.PropertyType)
                    }));
                }
            foreach (MethodInfo m in methods)
                builder.AppendLine(string.Format("                  case '{0}': return false; break;", m.Name));
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
                builder.AppendFormat(",'{0}'", p.Name);
            foreach (MethodInfo mi in methods)
                builder.AppendFormat(",'{0}'", mi.Name);
            foreach (MethodInfo mi in modelType.GetMethods(Constants.STORE_DATA_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0)
                    builder.Append(",'save'");
                else if (mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0)
                    builder.Append(",'update'");
                else if (mi.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length > 0)
                    builder.Append(",'destroy'");
            }
            builder.AppendLine(@"];
                }
            });
        };");
        }

        private void _AppendValidations(List<PropertyInfo> props, ref WrappedStringBuilder builder)
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
                    Logger.Trace("Appending Required Propert[{0}] for Model Definition[{1}] validations", new object[]{
                        pi.Name,
                        pi.DeclaringType.FullName
                    });
                    builder.AppendLine(string.Format("          ret=ret&&(this.#{0}==undefined||this.#{0}==null ? false : true);", pi.Name));
                }
                builder.AppendLine(@"           return ret;
        };
    #invalidFields(){
            let ret=[];");
                foreach (PropertyInfo pi in requiredProps)
                    builder.AppendLine(string.Format(@"         if (this.#{0}==undefined||this.#{0}==null){{
                ret.push('{0}');
            }}", pi.Name));
                builder.AppendLine(@"           return ret;
    };");
            }
            else
                builder.AppendLine(@"   #isValid(){return true;};
    #invalidFields(){return [];};");
        }
    }
}
