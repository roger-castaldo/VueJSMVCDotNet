using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelDefaultMethodsGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, SModelType modelType, string urlBase, ILogger log)
        {
            log?.LogTrace("Generating Model Default Methods Definition javascript for {}",  modelType.Type.FullName);
            if (modelType.HasSave)
            {
                log?.LogTrace("Adding save method for Model Definition[{}]", modelType.Type.FullName);
                ModelDefaultMethodsGenerator.AppendSave(modelType,ref builder, (modelType.SaveMethod.GetCustomAttributes(typeof(UseFormData), false).Length == 0));
            }
            if (modelType.HasUpdate)
            {
                log?.LogTrace("Adding update method for Model Definition[{}]", modelType.Type.FullName);
                ModelDefaultMethodsGenerator.AppendUpdate(modelType, ref builder, (modelType.UpdateMethod.GetCustomAttributes(typeof(UseFormData), false).Length == 0));
            }
            if(modelType.HasDelete)
            {
                log?.LogTrace("Adding delete method for Model Definition[{}]", modelType.Type.FullName);
                ModelDefaultMethodsGenerator.AppendDelete(modelType, ref builder);
            }
            ModelDefaultMethodsGenerator.AppendReloadMethod(modelType, ref builder, log);
        }

        private static void AppendReloadMethod(SModelType modelType, ref WrappedStringBuilder builder, ILogger log)
        {
            log?.LogTrace("Adding reload method for Model Definition[{}]", modelType.Type.FullName);
            builder.AppendLine(@$"     async #reload(){{
                let response = await ModelMethods.reload({modelType.Type.Name}.#baseURL,this.{Constants.INITIAL_DATA_KEY},this.#isNew());
                this.{Constants.PARSE_FUNCTION_NAME}(response);
                let proxy = this.#toProxy();
                this.#events.trigger('{Constants.Events.MODEL_LOADED}',proxy);
                return proxy;
            }}");
        }

        private static void AppendDelete(SModelType modelType,ref WrappedStringBuilder builder)
        {
            builder.AppendLine(@$"         async #destroy(){{
                let response = await ModelMethods.destroy({modelType.Type.Name}.#baseURL,this.{Constants.INITIAL_DATA_KEY}.id,this.#isNew());
                let proxy = this.#toProxy();
                this.#events.trigger('{Constants.Events.MODEL_DESTROYED}',proxy);
                return proxy;
        }}");
        }

        private static void AppendUpdate(SModelType modelType,ref WrappedStringBuilder builder,bool useJSON)
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

        private static void AppendSave(SModelType modelType,ref WrappedStringBuilder builder,bool useJSON)
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
