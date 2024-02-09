using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    class ModelInstanceFooterGenerator : IJSGenerator
    {
        public void GeneratorJS(WrappedStringBuilder builder, SModelType modelType, string urlBase, ILogger log)
        {
            log?.LogTrace("Appending Model Instance Footer for Model Definition[{}]",  modelType.Type.FullName);
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
            modelType.Properties.ForEach(pi => builder.AppendLine($"              Object.defineProperty(data,'{pi.Name}',{{get:function(){{return curObj.#{pi.Name};}}{(pi.CanWrite?$",set:function(val){{curObj.{pi.Name} = val;}}":"")}}});"));
            builder.AppendLine(@"           Object.defineProperty(data,'id',{get:function(){return curObj.id;}});");
            modelType.InstanceMethods
                .Where(mi => mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                .ForEach(mi =>
                {
                    ExposedMethod em = (ExposedMethod)mi.GetCustomAttributes(typeof(ExposedMethod), false)[0];
                    Type returnType = (em.ArrayElementType!=null ? Array.CreateInstance(em.ArrayElementType, 0).GetType() : mi.ReturnType);
                    builder.AppendLine($@"          methods.{mi.Name} = function({string.Join(",",mi.GetParameters().Select(p=>p.Name))}){{
            {(returnType == typeof(void) ? "" : "return")} curObj.{mi.Name}({string.Join(",", mi.GetParameters().Select(p => p.Name))}); 
        }};");
                });
            builder.AppendLine(@$"       return {{
                data:function(){{return data;}},
                methods:methods,
                created:function(){{
                    let view=this;
                    this.$on(['{Constants.Events.MODEL_LOADED}','{Constants.Events.MODEL_SAVED}','{Constants.Events.MODEL_UPDATED}','parsed'],function(){{view.$forceUpdate();}});
                            }}
                        }};
                    }}");

            //composition code
            builder.AppendLine(@"   toVueComposition(){
        let me = this.#toProxy();
        return {");
            modelType.Properties.ForEach(p => builder.AppendLine($"          {p.Name}:{(p.CanWrite ? "readonly" : "ref")}(me.{p.Name}),"));
            modelType.InstanceMethods.ForEach(m => builder.AppendLine($"          {m.Name}:function(){{ return me.{m.Name}.apply(me,arguments); }},"));
            if (modelType.HasSave)
                builder.AppendLine("            save:function(){ return me.save.apply(me,arguments); },");
            if (modelType.HasDelete)
                builder.AppendLine("            destroy:function(){ return me.destroy.apply(me,arguments); },");
            if(modelType.HasUpdate)
                builder.AppendLine("            update:function(){ return me.update.apply(me,arguments); },");
            builder.AppendLine(@"           isNew: function(){return me.isNew(); },
            isValid: function(){return me.isValid();},
            invalidFields: function(){return me.invalidFields();},
            reload: function(){return me.reload();},
            $on: function(event,callback) { me.$on(event,callback); },
            $off: function(callback) { me.$off(callback); }
        };
    }");
        }
    }
}
