using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators
{
    internal class ModelDefaultMethodsGenerator : IJSGenerator
    {
        void IJSGenerator.GeneratorJS(WrappedStringBuilder builder, ModelType modelType, string baseURL, ILogger? log)
        {
            log?.LogTrace("Generating Model Default Methods Definition javascript for {TypeName}", modelType.Type.FullName);
            if (modelType.SaveMethod != null)
            {
                log?.LogTrace("Adding save method for Model Definition[{TypeName}]", modelType.Type.FullName);
                ModelDefaultMethodsGenerator.AppendSave(modelType, builder, modelType.SaveMethod!.GetCustomAttribute<UseFormDataAttribute>(false)==null);
            }
            if (modelType.UpdateMethod != null)
            {
                log?.LogTrace("Adding update method for Model Definition[{TypeName}]", modelType.Type.FullName);
                ModelDefaultMethodsGenerator.AppendUpdate(modelType, builder, modelType.UpdateMethod!.GetCustomAttribute<UseFormDataAttribute>(false) == null);
            }
            if (modelType.DeleteMethod!=null)
            {
                log?.LogTrace("Adding delete method for Model Definition[{TypeName}]", modelType.Type.FullName);
                ModelDefaultMethodsGenerator.AppendDelete(modelType, builder);
            }
            ModelDefaultMethodsGenerator.AppendReloadMethod(modelType, builder, log);
        }

        private static void AppendReloadMethod(ModelType modelType, WrappedStringBuilder builder, ILogger? log)
        {
            log?.LogTrace("Adding reload method for Model Definition[{TypeName}]", modelType.Type.FullName);
            builder.AppendLine(@$"     async #reload(){{
                let response = await ModelMethods.reload({modelType.Type.Name}.#baseURL,this.{Constants.INITIAL_DATA_KEY},this.#isNew());
                this.{Constants.PARSE_FUNCTION_NAME}(response);
                let proxy = this.#toProxy();
                this.#events.trigger('{Constants.Events.MODEL_LOADED}',proxy);
                return proxy;
            }}");
        }

        private static void AppendDelete(ModelType modelType, WrappedStringBuilder builder)
        {
            builder.AppendLine(@$"         async #destroy(){{
                let response = await ModelMethods.destroy({modelType.Type.Name}.#baseURL,this.{Constants.INITIAL_DATA_KEY}.id,this.#isNew());
                let proxy = this.#toProxy();
                this.#events.trigger('{Constants.Events.MODEL_DESTROYED}',proxy);
                return proxy;
        }}");
        }

        private static void AppendUpdate(ModelType modelType, WrappedStringBuilder builder, bool useJSON)
        {
            builder.AppendLine(@$"         async #update(){{
                let response = ModelMethods.update({modelType.Type.Name}.#baseURL,this.{Constants.INITIAL_DATA_KEY}.id,this.#isNew(),this.#isValid(),this.{Constants.TO_JSON_VARIABLE}(),{useJSON.ToString().ToLower()});
                let data = this.{Constants.TO_JSON_VARIABLE}();
                for(let prop in data){{
                    if (prop!=='id'){{
                        this.{Constants.INITIAL_DATA_KEY}[prop]=data[prop];
                    }}
                }}
                let proxy = this.#toProxy();
                this.#events.trigger('{Constants.Events.MODEL_UPDATED}',proxy);
                return proxy;
        }}");
        }

        private static void AppendSave(ModelType modelType, WrappedStringBuilder builder, bool useJSON)
        {
            builder.AppendLine(@$"             async #save(){{
                let response = ModelMethods.save({modelType.Type.Name}.#baseURL,this.#isNew(),this.#isValid(),this.{Constants.TO_JSON_VARIABLE}(),{useJSON.ToString().ToLower()});
                this.{Constants.INITIAL_DATA_KEY} = Object.assign({{}},this.{Constants.TO_JSON_VARIABLE}(),response);
                let proxy = this.#toProxy();
                this.#events.trigger('{Constants.Events.MODEL_SAVED}',proxy);
                return proxy;
        }}");
        }
    }
}
