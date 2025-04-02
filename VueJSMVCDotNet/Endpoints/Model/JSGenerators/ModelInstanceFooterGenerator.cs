using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Extensions;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    class ModelInstanceFooterGenerator : IJSGenerator
    {
        void IJSGenerator.GeneratorJS(WrappedStringBuilder builder, ModelType modelType, string baseURL, ILogger? log)
        {
            log?.LogTrace("Appending Model Instance Footer for Model Definition[{TypeName}]", modelType.Type.FullName);
            builder.Append(@$"
        static createInstance(){{
            console.warn(""WARNING! Obsolete function called. Function 'createInstance' has been deprecated, please use new '{modelType.Type.Name}' function instead!"");
            return new {modelType.Type.Name}();
        }}
        toVue(options){{
            console.warn(""WARNING! Obsolete function called.Function 'toVue' has been deprecated, please use toVueComposition function instead!"");
            if (options.mixins==undefined){{options.mixins=[];}}
            options.mixins.push(this.toMixins());
            return createApp(options);
        }}
        toMixins(){{
            console.warn(""WARNING! Obsolete function called.Function 'toMixins' has been deprecated, please use toVueComposition function instead!"");
            let curObj = this;
            let data = {{}};
            let methods={{
                $on:function(event,callback){{curObj.$on(event,callback);}},
                $off:function(callback){{curObj.$off(callback);}}
            }};");
            modelType.Properties.ForEach(pi => builder.AppendLine($"              Object.defineProperty(data,'{pi.Name}',{{get:function(){{return curObj.#{pi.Name};}}{(pi.CanWrite ? $",set:function(val){{curObj.{pi.Name} = val;}}" : "")}}});"));
            builder.AppendLine(@"           Object.defineProperty(data,'id',{get:function(){return curObj.id;}});");
            modelType.InstanceMethods
                .Select(mi => new { Method = mi, ExposedAttribute = mi.GetCustomAttribute<ExposedMethodAttribute>() })
                .Where(gm => gm.ExposedAttribute!=null)
                .ForEach(gm =>
                {
                    Type returnType = (gm.ExposedAttribute?.ArrayElementType!=null ? Array.CreateInstance(gm.ExposedAttribute.ArrayElementType, 0).GetType() : gm.Method.ReturnType);
                    builder.AppendLine($@"          methods.{gm.Method.Name} = function({string.Join(",", gm.Method.GetParameters().Select(p => p.Name))}){{
            {(returnType == typeof(void) ? "" : "return")} curObj.{gm.Method.Name}({string.Join(",", gm.Method.GetParameters().Select(p => p.Name))}); 
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
            if (modelType.SaveMethod!=null)
                builder.AppendLine("            save:function(){ return me.save.apply(me,arguments); },");
            if (modelType.DeleteMethod != null)
                builder.AppendLine("            destroy:function(){ return me.destroy.apply(me,arguments); },");
            if (modelType.UpdateMethod!=null)
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
