using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ModelDefaultMethodsGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType, string urlBase)
        {
            Logger.Trace("Generating Model Default Methods Definition javascript for {0}", new object[] { modelType.FullName });
            string urlRoot = Utility.GetModelUrlRoot(modelType,urlBase);
            foreach (MethodInfo mi in modelType.GetMethods(Constants.STORE_DATA_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0)
                {
                    Logger.Trace("Adding save method for Model Definition[{0}]", new object[] { modelType.FullName });
                    _AppendSave(urlRoot, ref builder, (mi.GetCustomAttributes(typeof(UseFormData), false).Length == 0));
                }
                else if (mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0)
                {
                    Logger.Trace("Adding update method for Model Definition[{0}]", new object[] { modelType.FullName });
                    _AppendUpdate(urlRoot, ref builder, (mi.GetCustomAttributes(typeof(UseFormData), false).Length == 0));
                }
                else if (mi.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length > 0)
                {
                    Logger.Trace("Adding delete method for Model Definition[{0}]", new object[] { modelType.FullName });
                    _AppendDelete(urlRoot, ref builder);
                }
            }
            _AppendReloadMethod(modelType, urlRoot, ref builder);
        }

        private void _AppendReloadMethod(Type modelType, string urlRoot, ref WrappedStringBuilder builder)
        {
            Logger.Trace("Adding reload method for Model Definition[{0}]", new object[] { modelType.FullName });
            builder.AppendLine(string.Format(@"     #reload(){{
                let model=this;
                return new Promise((resolve,reject)=>{{
                    ModelMethods.reload('{0}',model.{1}.id,model.#isNew()).then(
                        resolved=>{{
                            model.{2}(data);
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
                urlRoot,
                Constants.INITIAL_DATA_KEY,
                Constants.PARSE_FUNCTION_NAME,
                Constants.Events.MODEL_LOADED
            }));
        }

        private void _AppendDelete(string urlRoot, ref WrappedStringBuilder builder)
        {
            builder.AppendLine(string.Format(@"         #destroy(){{
                let model = this;
                return new Promise((resolve,reject)=>{{
                    ModelMethods.destroy('{0}',model.{1}.id,model.#isNew()).then(
                        resolved=>{{
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
                urlRoot,
                Constants.INITIAL_DATA_KEY,
                Constants.Events.MODEL_DESTROYED
            }));
        }

        private void _AppendUpdate(string urlRoot, ref WrappedStringBuilder builder,bool useJSON)
        {
            builder.AppendLine(string.Format(@"         #update(){{
                let model=this;
                return new Promise((resolve,reject)=>{{
                    ModelMethods.update('{0}',model.{1}.id,model.#isNew(),model.#isValid(),model.{2}(),{3}).then(
                        resolved=>{{
                            let data=model.{2}();
                            for(let prop in data){{
                                if (prop!=='id'){{
                                    model.{1}[prop]=data[prop];
                                }}
                            }}
                            let proxy = model.#toProxy();
                            model.#events.trigger('{4}',proxy);
                            resolve(proxy);
                        }},
                        rejected=>{{
                            reject(rejected);
                        }}
                    );
                }});
        }}", new object[]{
                urlRoot,
                Constants.INITIAL_DATA_KEY,
                Constants.TO_JSON_VARIABLE,
                useJSON.ToString().ToLower(),
                Constants.Events.MODEL_UPDATED
            }));
        }

        private void _AppendSave(string urlRoot, ref WrappedStringBuilder builder,bool useJSON)
        {
            builder.AppendLine(string.Format(@"             #save(){{
                let model=this;
                return new Promise((resolve,reject)=>{{
                    ModelMethods.save('{0}',model.#isNew(),model.#isValid(),model.{1}(),{2}).then(
                        resolved=>{{
                            model.{1}=extend(model.{1}(),resolved);
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
                urlRoot,
                Constants.TO_JSON_VARIABLE,
                useJSON.ToString().ToLower(),
                Constants.INITIAL_DATA_KEY,
                Constants.Events.MODEL_SAVED
            }));
        }
    }
}
