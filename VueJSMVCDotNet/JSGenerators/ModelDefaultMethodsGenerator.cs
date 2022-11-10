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
                    if (model.#isNew()){{
                        reject('Cannot reload unsaved model.');
                    }}else{{
                        ajax({{
                            url:'{0}/'+model.{3}.id,
                            method:'GET'
                        }}).then(
                            response=>{{
                                if (response.ok){{
                                    let data = response.json();
                                    if (data==null){{
                                        reject(null);
                                    }}else{{
                                        model.{1}(data);
                                        model.#events.trigger('{2}',model);
                                        resolve(model);
                                    }}
                                }}else{{
                                    reject(response.text());
                                }}
                            }},
                            response=>{{ reject(response.text()); }}
                        );
                    }}
                }});
            }}", new object[]{
                urlRoot,
                Constants.PARSE_FUNCTION_NAME,
                Constants.Events.MODEL_LOADED,
                Constants.INITIAL_DATA_KEY
            }));
        }

        private void _AppendDelete(string urlRoot, ref WrappedStringBuilder builder)
        {
            builder.AppendLine(string.Format(@"         #destroy(){{
                let model = this;
                return new Promise((resolve,reject)=>{{
                    if (model.#isNew()){{
                        reject('Cannot delete unsaved model.');
                    }}else{{
                        ajax(
                        {{
                            url:'{0}/'+model.{3}.id,
                            method:'{2}'
                        }}).then(
                            response=>{{
                                if (response.ok){{                 
                                    let data = response.json();
                                    if (data){{
                                        model.#events.trigger('{1}',model);
                                        resolve(model);
                                    }}else{{
                                        reject();
                                    }}
                                }}else{{
                                    reject(response.text());
                                }}
                            }},
                            response=>{{reject(response.text());}}
                        );    
                    }}
                }});
        }}", new object[]{
                urlRoot,
                Constants.Events.MODEL_DESTROYED,
                ModelRequestHandler.RequestMethods.DELETE,
                Constants.INITIAL_DATA_KEY
            }));
        }

        private void _AppendUpdate(string urlRoot, ref WrappedStringBuilder builder,bool useJSON)
        {
            builder.AppendLine(string.Format(@"         #update(){{
                let model=this;
                return new Promise((resolve,reject)=>{{
                    if (!model.#isValid()){{
                        reject('Invalid model.');
                    }}else if (model.#isNew()){{
                        reject('Cannot update unsaved model, please call save instead.');
                    }}else{{
                        let data = model.{1}();
                        if (JSON.stringify(data)===JSON.stringify({{}})){{
                            resolve(model);
                        }}else{{
                            ajax(
                            {{
                                url:'{0}/'+model.{6}.id,
                                method:'{4}',
                                useJSON:{5},
                                data:data
                            }}).then(response=>{{
                                if (response.ok){{                 
                                    let data = response.json();
                                    if (data){{
                                        data=model.{3};
                                        for(let prop in data){{
                                            if (prop!='id'){{
                                                data[prop]=model[prop];
                                            }}
                                        }}
                                        model.{3}=data;
                                        model.#events.trigger('{2}',model);
                                        resolve(model);
                                    }}else{{
                                        reject();
                                    }}
                                }}else{{
                                    reject(response.text());
                                }}
                            }},response=>{{reject(response.text());}});
                        }}
                    }}
                }});
        }}", new object[]{
                urlRoot,
                Constants.TO_JSON_VARIABLE,
                Constants.Events.MODEL_UPDATED,
                Constants.INITIAL_DATA_KEY,
                ModelRequestHandler.RequestMethods.PATCH,
                useJSON.ToString().ToLower(),
                Constants.INITIAL_DATA_KEY
            }));
        }

        private void _AppendSave(string urlRoot, ref WrappedStringBuilder builder,bool useJSON)
        {
            builder.AppendLine(string.Format(@"             #save(){{
                let model=this;
                return new Promise((resolve,reject)=>{{
                    if (!model.#isValid()){{
                        reject('Invalid model.');
                    }}else if (!model.isNew()){{
                        reject('Cannot save a saved model, please call update instead.');
                    }}else{{
                        let data = model.{1}();
                        ajax(
                        {{
                            url:'{0}',
                            method:'{4}',
                            useJSON:{5},
                            data:data
                        }}).then(response=>{{
                            if (response.ok){{                 
                                model.{2}=extend(data,response.json());
                                model.#events.trigger('{3}',model);
                                resolve(model);
                            }}else{{
                                reject(response.text());
                            }}
                        }},response=>{{reject(response.text());}});    
                    }}
                }});
        }}", new object[]{
                urlRoot,
                Constants.TO_JSON_VARIABLE,
                Constants.INITIAL_DATA_KEY,
                Constants.Events.MODEL_SAVED,
                ModelRequestHandler.RequestMethods.PUT,
                useJSON.ToString().ToLower()
            }));
        }
    }
}
