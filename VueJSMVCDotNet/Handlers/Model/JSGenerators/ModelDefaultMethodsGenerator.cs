using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ModelDefaultMethodsGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, sModelType modelType, string urlBase)
        {
            Logger.Trace("Generating Model Default Methods Definition javascript for {0}", new object[] { modelType.Type.FullName });
            if (modelType.HasSave)
            {
                Logger.Trace("Adding save method for Model Definition[{0}]", new object[] { modelType.Type.FullName });
                _AppendSave(modelType,ref builder, (modelType.SaveMethod.GetCustomAttributes(typeof(UseFormData), false).Length == 0));
            }
            if (modelType.HasUpdate)
            {
                Logger.Trace("Adding update method for Model Definition[{0}]", new object[] { modelType.Type.FullName });
                _AppendUpdate(modelType, ref builder, (modelType.UpdateMethod.GetCustomAttributes(typeof(UseFormData), false).Length == 0));
            }
            if(modelType.HasDelete)
            {
                Logger.Trace("Adding delete method for Model Definition[{0}]", new object[] { modelType.Type.FullName });
                _AppendDelete(modelType, ref builder);
            }
            _AppendReloadMethod(modelType, ref builder);
        }

        private void _AppendReloadMethod(sModelType modelType, ref WrappedStringBuilder builder)
        {
            Logger.Trace("Adding reload method for Model Definition[{0}]", new object[] { modelType.Type.FullName });
            builder.AppendLine(string.Format(@"     #reload(){{
                let model=this;
                return new Promise((resolve,reject)=>{{
                    ModelMethods.reload({3}.#baseURL,model.{0}.id,model.#isNew()).then(
                        resolved=>{{
                            model.{1}(data);
                            let proxy = model.#toProxy();
                            model.#events.trigger('{2}',proxy);
                            resolve(proxy);
                        }},
                        rejected=>{{
                            reject(rejected);
                        }}
                    );
                }});
            }}", new object[]{
                Constants.INITIAL_DATA_KEY,
                Constants.PARSE_FUNCTION_NAME,
                Constants.Events.MODEL_LOADED,
                modelType.Type.Name
            }));
        }

        private void _AppendDelete(sModelType modelType,ref WrappedStringBuilder builder)
        {
            builder.AppendLine(string.Format(@"         #destroy(){{
                let model = this;
                return new Promise((resolve,reject)=>{{
                    ModelMethods.destroy({2}.#baseURL,model.{0}.id,model.#isNew()).then(
                        resolved=>{{
                            let proxy = model.#toProxy();
                            model.#events.trigger('{1}',proxy);
                            resolve(proxy);
                        }},
                        rejected=>{{
                            reject(rejected);
                        }}
                    );
                }});
        }}", new object[]{
                Constants.INITIAL_DATA_KEY,
                Constants.Events.MODEL_DESTROYED,
                modelType.Type.Name
            }));
        }

        private void _AppendUpdate(sModelType modelType,ref WrappedStringBuilder builder,bool useJSON)
        {
            builder.AppendLine(string.Format(@"         #update(){{
                let model=this;
                return new Promise((resolve,reject)=>{{
                    ModelMethods.update({4}.#baseURL,model.{0}.id,model.#isNew(),model.#isValid(),model.{1}(),{2}).then(
                        resolved=>{{
                            let data=model.{1}();
                            for(let prop in data){{
                                if (prop!=='id'){{
                                    model.{0}[prop]=data[prop];
                                }}
                            }}
                            let proxy = model.#toProxy();
                            model.#events.trigger('{3}',proxy);
                            resolve(proxy);
                        }},
                        rejected=>{{
                            reject(rejected);
                        }}
                    );
                }});
        }}", new object[]{
                Constants.INITIAL_DATA_KEY,
                Constants.TO_JSON_VARIABLE,
                useJSON.ToString().ToLower(),
                Constants.Events.MODEL_UPDATED,
                modelType.Type.Name
            }));
        }

        private void _AppendSave(sModelType modelType,ref WrappedStringBuilder builder,bool useJSON)
        {
            builder.AppendLine(string.Format(@"             #save(){{
                let model=this;
                return new Promise((resolve,reject)=>{{
                    ModelMethods.save({4}.#baseURL,model.#isNew(),model.#isValid(),model.{0}(),{1}).then(
                        resolved=>{{
                            model.{2}=Object.assign({{}},model.{0}(),resolved);
                            let proxy = model.#toProxy();
                            model.#events.trigger('{3}',proxy);
                            resolve(proxy);
                        }},
                        rejected=>{{
                            reject(rejected);
                        }}
                    );
                }});
        }}", new object[]{
                Constants.TO_JSON_VARIABLE,
                useJSON.ToString().ToLower(),
                Constants.INITIAL_DATA_KEY,
                Constants.Events.MODEL_SAVED,
                modelType.Type.Name
            }));
        }
    }
}
