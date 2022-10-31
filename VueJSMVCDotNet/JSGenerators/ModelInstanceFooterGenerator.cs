using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    class ModelInstanceFooterGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType, string urlBase)
        {
            Logger.Trace("Appending Model Instance Footer for Model Definition[{0}]", new object[] { modelType.FullName });
            //    builder.AppendLine(string.Format(@"     class {0} {{
            //static {1}(){{ ", modelType.Name, Constants.CREATE_INSTANCE_FUNCTION_NAME));
            builder.Append(@"
        $on(event,callback){
            if (this.#events===undefined){this.#events={};}
            if (Array.isArray(event)){
                for(let x=0;x<event.length;x++){
                    if (this.#events[event[x]]===undefined){this.#events[event[x]]=[];}
                    this.#events[event[x]].push(callback);
                }
            }else{
                if (this.#events[event]===undefined){this.#events[event]=[];}
                this.#events[event].push(callback);
            }
        }
        $off(callback){
            if (this.#events!=undefined){
                for(let evt in this.#events){
                    for(let x=0;x<this.#events[evt].length;x++){
                        if (this.#events[evt][x]==callback){
                            this.#events[evt].splice(x,1);
                            break;
                        }
                    }
                }
            }
        }
        $emit(event,data){
            if (this.#events!=undefined){
                if (this.#events[event]!=undefined){
                    for(let x=0;x<this.#events[event].length;x++){
                        this.#events[event][x]((data==undefined ? this : data));
                    }
                }
            }
        }
        static createInstance(){
            console.warn(""WARNING! Obsolete function called. Function 'createInstance' has been deprecated, please use new '"+modelType.Name+@"' function instead!"");
        }
        toVue(options){
            if (options.mixins==undefined){options.mixins=[];}
            options.mixins.push(this.toMixins());
            return Vue.createApp(options);
        }
        toMixins(){
            if (Vue===undefined || Vue.version.indexOf('3')!==0){ throw 'Unable to operate without Vue version 3.0'; }
            let curObj = this;
            let data = {};
            let methods={
                $on:function(event,callback){curObj.$on(event,callback);},
                $off:function(callback){curObj.$off(callback);}
            };");
            foreach (PropertyInfo pi in Utility.GetModelProperties(modelType))
            {
                if (pi.CanWrite)
                    builder.AppendLine(string.Format("              Object.defineProperty(data,'{0}',{{get:function(){{return curObj.#{0};}},set:function(val){{curObj.{0} = val;}}}});", new object[] { pi.Name }));
                else
                    builder.AppendLine(string.Format("              Object.defineProperty(data,'{0}',{{get:function(){{return curObj.#{0};}}}});", new object[] { pi.Name }));
            }
            builder.AppendLine(@"           Object.defineProperty(data,'id',{get:function(){return curObj.id;}});");
            foreach (MethodInfo mi in modelType.GetMethods(Constants.INSTANCE_METHOD_FLAGS))
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
