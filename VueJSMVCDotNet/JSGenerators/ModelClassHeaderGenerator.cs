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

            builder.AppendLine(string.Format(@" class {0} {{
        {1}=undefined;
        #events=undefined;", new object[] { modelType.Name,Constants.INITIAL_DATA_KEY }));

            foreach (PropertyInfo p in props)
                builder.AppendLine(string.Format("      #{0}=undefined;", p.Name));

            builder.AppendLine(@"       #hashCode=undefined;
        async #setHash(){
            this.#hashCode = await H(JSON.stringify(_stripBigInt({");
            bool isFirst = true;
            foreach (PropertyInfo p in props)
            {
                builder.AppendLine(string.Format("      {1}{0}:this.#{0}", p.Name,(isFirst?"":",")));
                isFirst = false;
            }
            builder.AppendLine(@"})));
        }");

            builder.AppendLine(string.Format(@"    constructor(){{
            this.{0} = null;
            let data={1};
            for(let prop in data){{
                this['#'+prop]=data[prop];
            }}
            let me = this;
            return new Proxy(this,{{
                get: function(target,prop,reciever){{
                    switch(prop){{", new object[] { Constants.INITIAL_DATA_KEY, JSON.JsonEncode(modelType.GetConstructor(Type.EmptyTypes).Invoke(null)) }));
            foreach (PropertyInfo p in props)
                builder.AppendLine(string.Format("                  case '{0}': return me.{0}; break;", p.Name));
            builder.AppendLine(@"                   default: 
                            if (me[prop]!==undefined && isFunction(me[prop]))
                                return function(){ me[prop].apply(me,arguments); }
                            return me[prop]; 
                        break;
                    }
                },
                set: function(target,prop,value){
                    switch(prop) {");
            foreach (PropertyInfo p in props)
                builder.AppendLine(string.Format("                  case '{0}': me.{0} = value; break;", p.Name));
            builder.AppendLine(@"                   default: me[prop]=value; break;
                    }
                }
            });
        }");
            foreach (PropertyInfo p in props)
            {
                builder.AppendLine(string.Format("     get {0}(){{ return (this.#{0}===undefined ? this.{1}.{0} : this.#{0}); }}", new object[] { p.Name,Constants.INITIAL_DATA_KEY }));
                if (p.CanWrite)
                {
                    builder.AppendLine(string.Format("      set {0}(val){{ this.#{0} = _checkProperty('{0}','{1}',val,{2}); this.#setHash(); }}", new object[] {
                        p.Name,
                        Utility.GetTypeString(p.PropertyType,false),
                        Utility.GetEnumList(p.PropertyType)
                    }));
                }
            }

            builder.AppendLine(string.Format(@"      get id(){{ return (this.{0}===null || this.{0}===undefined ? null : this.{0}.id); }}
      isNew(){{ return this.{0}===null || this.{0}.id===undefined || this.{0}.id===null;}}", Constants.INITIAL_DATA_KEY));

            _AppendValidations(props, ref builder);
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
                builder.AppendLine(@"       get isValid(){
            let ret=true;");
                foreach (PropertyInfo pi in requiredProps)
                {
                    Logger.Trace("Appending Required Propert[{0}] for Model Definition[{1}] validations", new object[]{
                        pi.Name,
                        pi.DeclaringType.FullName
                    });
                    builder.AppendLine(string.Format("          ret=ret&&(this.{0}==undefined||this.{0}==null ? false : true);", pi.Name));
                }
                builder.AppendLine(@"           return ret;
        }
        get invalidFields(){
            let ret=[];");
                foreach (PropertyInfo pi in requiredProps)
                    builder.AppendLine(string.Format(@"         if (this.{0}==undefined||this.{0}==null){{
                ret.push('{0}');
            }}", pi.Name));
                builder.AppendLine(@"           return ret;
        }");
            }
            else
                builder.AppendLine(@"       get isValid(){return true;}
        get invalidFields(){return [];}");
        }
    }
}
