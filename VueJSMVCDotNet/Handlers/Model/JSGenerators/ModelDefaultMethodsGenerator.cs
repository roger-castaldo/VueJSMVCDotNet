using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using VueJSMVCDotNet.Interfaces;
using static VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelDefaultMethodsGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase, ILog log)
        {
            log.Trace("Generating Model Default Methods Definition javascript for {0}", new object[] { modelType.Type.FullName });
            if (modelType.HasSave)
            {
                log.Trace("Adding save method for Model Definition[{0}]", new object[] { modelType.Type.FullName });
                _AppendSave(modelType,ref builder, (modelType.SaveMethod.GetCustomAttributes(typeof(UseFormData), false).Length == 0));
            }
            if (modelType.HasUpdate)
            {
                log.Trace("Adding update method for Model Definition[{0}]", new object[] { modelType.Type.FullName });
                _AppendUpdate(modelType, ref builder, (modelType.UpdateMethod.GetCustomAttributes(typeof(UseFormData), false).Length == 0));
            }
            if(modelType.HasDelete)
            {
                log.Trace("Adding delete method for Model Definition[{0}]", new object[] { modelType.Type.FullName });
                _AppendDelete(modelType, ref builder);
            }
            _AppendReloadMethod(modelType, ref builder, log);
        }

        private void _AppendReloadMethod(sModelType modelType, ref WrappedStringBuilder builder, ILog log)
        {
            log.Trace("Adding reload method for Model Definition[{0}]", new object[] { modelType.Type.FullName });
            builder.AppendLine(@$"     async #reload(){{
                let response = await ModelMethods.reload({modelType.Type.Name}.#baseURL,this.{Constants.INITIAL_DATA_KEY},this.#isNew());
                this.{Constants.PARSE_FUNCTION_NAME}(response);
                let proxy = this.#toProxy();
                this.#events.trigger('{Constants.Events.MODEL_LOADED}',proxy);
                return proxy;
            }}");
        }

        private void _AppendDelete(sModelType modelType,ref WrappedStringBuilder builder)
        {
            builder.AppendLine(@$"         async #destroy(){{
                let response = await ModelMethods.destroy({modelType.Type.Name}.#baseURL,this.{Constants.INITIAL_DATA_KEY}.id,this.#isNew());
                let proxy = this.#toProxy();
                this.#events.trigger('{Constants.Events.MODEL_DESTROYED}',proxy);
                return proxy;
        }}");
        }

        private void _AppendUpdate(sModelType modelType,ref WrappedStringBuilder builder,bool useJSON)
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

        private void _AppendSave(sModelType modelType,ref WrappedStringBuilder builder,bool useJSON)
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
