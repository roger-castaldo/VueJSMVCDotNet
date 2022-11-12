using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.JSHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    class ModelInstanceFooterGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase)
        {
            Logger.Trace("Appending Model Instance Footer for Model Definition[{0}]", new object[] { modelType.Type.FullName });
            builder.Append(@"
        static createInstance(){
            console.warn(""WARNING! Obsolete function called. Function 'createInstance' has been deprecated, please use new '"+modelType.Type.Name+@"' function instead!"");
            return new "+modelType.Type.Name+@"();
        }
        toVue(options){
            console.warn(""WARNING! Obsolete function called.Function 'toVue' has been deprecated, please use toVueComposition function instead!"");
            if (options.mixins==undefined){options.mixins=[];}
            options.mixins.push(this.toMixins());
            return createApp(options);
        }
        toMixins(){
            console.warn(""WARNING! Obsolete function called.Function 'toMixins' has been deprecated, please use toVueComposition function instead!"");
            let curObj = this;
            let data = {};
            let methods={
                $on:function(event,callback){curObj.$on(event,callback);},
                $off:function(callback){curObj.$off(callback);}
            };");
            foreach (PropertyInfo pi in modelType.Properties)
            {
                if (pi.CanWrite)
                    builder.AppendLine(string.Format("              Object.defineProperty(data,'{0}',{{get:function(){{return curObj.#{0};}},set:function(val){{curObj.{0} = val;}}}});", new object[] { pi.Name }));
                else
                    builder.AppendLine(string.Format("              Object.defineProperty(data,'{0}',{{get:function(){{return curObj.#{0};}}}});", new object[] { pi.Name }));
            }
            builder.AppendLine(@"           Object.defineProperty(data,'id',{get:function(){return curObj.id;}});");
            foreach (MethodInfo mi in modelType.InstanceMethods)
            {
                if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                {
                    ExposedMethod em = (ExposedMethod)mi.GetCustomAttributes(typeof(ExposedMethod), false)[0];
                    Type returnType = (em.ArrayElementType!=null ? Array.CreateInstance(em.ArrayElementType, 0).GetType() : mi.ReturnType);
                    builder.AppendFormat("          methods.{0} = function(", mi.Name);
                    foreach (ParameterInfo pi in mi.GetParameters())
                        builder.AppendFormat("{0},", pi.Name);
                    if (mi.GetParameters().Length > 0)
                        builder.Length = builder.Length-1;
                    builder.AppendFormat("){{ {0} curObj.{1}(", (returnType == typeof(void) ? "" : "return"), mi.Name);
                    foreach (ParameterInfo pi in mi.GetParameters())
                        builder.AppendFormat("{0},", pi.Name);
                    if (mi.GetParameters().Length > 0)
                        builder.Length = builder.Length-1;
                    builder.AppendLine("); };");
                }
            }
            builder.AppendLine(@"       return {
                data:function(){return data;},
                methods:methods,
                created:function(){
                    let view=this;
                    this.$on([");
                builder.AppendFormat("'{0}','{1}','{2}','parsed'",new object[]{
                    Constants.Events.MODEL_LOADED,
                    Constants.Events.MODEL_SAVED,
                    Constants.Events.MODEL_UPDATED
                });
                builder.AppendLine(@"],function(){view.$forceUpdate();});
                            }
                        };
                    }");
        }
    }
}
