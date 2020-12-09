using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ModelDefinitionGenerator : IJSGenerator
    {
        

        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType)
        {
            string urlRoot = Utility.GetModelUrlRoot(modelType);
            List<PropertyInfo> props = Utility.GetModelProperties(modelType);
            _AppendData(modelType, props, ref builder);
            _AppendComputed(props, ref builder);

            builder.AppendLine(string.Format(@"    methods = extend(methods,{{
        isNew:function(){{ return (this.{0}==undefined ? true : (this.id==undefined? true : this.id==undefined||this.id==null));}},",Constants.INITIAL_DATA_KEY));
            _AppendInstanceMethods(modelType,urlRoot, ref builder);
            foreach (MethodInfo mi in modelType.GetMethods(Constants.STORE_DATA_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0)
                    _AppendSave(urlRoot, ref builder);
                else if (mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0)
                    _AppendUpdate(urlRoot, ref builder);
                else if (mi.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length > 0)
                    _AppendDelete(urlRoot, ref builder);
            }
            _AppendReloadMethod(modelType, urlRoot, ref builder);
            builder.AppendLine("    });");
        }

        private void _AppendReloadMethod(Type modelType, string urlRoot, ref WrappedStringBuilder builder)
        {
            builder.AppendLine(string.Format(@"reload:function(options){{
                options = extend((options==undefined || options==null ?{{}}:options),{{
                    async:true,
                    success:function(){{}},
                    failure:function(error){{throw (error==undefined ? 'failed' : error);}}
                }});
                if (this.isNew()){{
                    options.failure('Cannot reload unsaved model.');
                }}else{{
                    var model=this;
                    ajax(
                    {{
                        url:'{0}/'+this.id,
                        type:'GET',
                        async:options.async,
                        fail:function(response){{options.failure(response.text());}},
                        done:function(response){{
                            if (response.ok){{                 
                                model.{1}(response.json());
                                if (model.$emit!=undefined){{ model.$emit('{2}',model); }}
                                options.success(model);
                            }}else{{
                                options.failure(response.text());
                            }}
                        }}
                    }});
                }}
            }}", new object[]{
                urlRoot,
                Constants.PARSE_FUNCTION_NAME,
                Constants.Events.MODEL_LOADED
            }));
        }

        private void _AppendDelete(string urlRoot, ref WrappedStringBuilder builder)
        {
            builder.AppendLine(string.Format(@"         destroy:function(options){{
            options = extend((options==undefined || options==null ?{{}}:options),{{
                async:true,
                success:function(){{}},
                failure:function(error){{throw (error==undefined ? 'failed' : error);}}
            }});
            if (this.isNew()){{
                options.failure('Cannot delete unsaved model.');
            }}else{{
                var model=this;
                ajax(
                {{
                    url:'{0}/'+this.id,
                    type:'{2}',
                    async:options.async,
                    fail:function(response){{options.failure(response.text());}},
                    done:function(response){{
                        if (response.ok){{                 
                            var data = response.json();
                            if (data){{
                                if (model.$emit!=undefined){{model.$emit('{1}',model);}}
                                options.success(model);
                            }}else{{
                                options.failure();
                            }}
                        }}else{{
                            options.failure(response.text());
                        }}
                    }}
                }});
            }}
        }},", new object[]{
                urlRoot,
                Constants.Events.MODEL_DESTROYED,
                RequestHandler.RequestMethods.DELETE
            }));
        }

        private void _AppendUpdate(string urlRoot, ref WrappedStringBuilder builder)
        {
            builder.AppendLine(string.Format(@"         update:function(options){{
            options = extend((options==undefined || options==null ?{{}}:options),{{
                async:true,
                success:function(){{}},
                failure:function(error){{throw (error==undefined ? 'failed' : error);}}
            }});
            if (!this.isValid){{
                options.failure('Invalid model.');
            }}
            else if (this.isNew()){{
                options.failure('Cannot updated unsaved model, please call save instead.');
            }}
            else {{
                var data = this.{1}();
                var model=this;
                if (Object.keys(data).length==0){{
                    options.success(model);
                }}else{{
                    ajax(
                    {{
                        url:'{0}/'+this.id,
                        type:'{4}',   
                        headers: {{
                                'Content-Type': 'application/json',
                            }},
                        data:JSON.stringify(this.{1}()),
                        async:options.async,
                        fail:function(response){{options.failure(response.text());}},
                        done:function(response){{
                            if (response.ok){{                 
                                var data = response.json();
                                if (data){{
                                    data=model.{3};
                                    for(var prop in data){{
                                        if (prop!='id'){{
                                            data[prop]=model[prop];
                                        }}
                                    }}
                                    Object.defineProperty(model,'{3}',{{get:function(){{return data;}},configurable: true}});
                                    if (model.$emit!=undefined){{model.$emit('{2}',model);}}
                                    options.success(model);
                                }}else{{
                                    options.failure();
                                }}
                            }}else{{
                                options.failure(response.text());
                            }}
                        }}
                    }});
                }}
            }}
        }},", new object[]{
                urlRoot,
                Constants.TO_JSON_VARIABLE,
                Constants.Events.MODEL_UPDATED,
                Constants.INITIAL_DATA_KEY,
                RequestHandler.RequestMethods.PATCH
            }));
        }

        private void _AppendSave(string urlRoot, ref WrappedStringBuilder builder)
        {
            builder.AppendLine(string.Format(@"             save:function(options){{
            options = extend((options==undefined || options==null ?{{}}:options),{{
                async:true,
                success:function(){{}},
                failure:function(error){{throw (error==undefined ? 'failed' : error);}}
            }});
            if (!this.isValid){{
                options.failure('Invalid model.');
            }}
            else if (!this.isNew()){{
                options.failure('Cannot save a saved model,please call update instead.');
            }}
            else {{
                var data = this.{1}();
                var model=this;
                ajax(
                {{
                    url:'{0}',
                    type:'{4}',
                    headers: {{
                        'Content-Type': 'application/json',
                    }},
                    data:JSON.stringify(data),
                    async:options.async,
                    fail:function(response){{options.failure(response.text());}},
                    done:function(response){{
                        if (response.ok){{                 
                            data.id=response.json().id;
                            Object.defineProperty(model,'{2}',{{get:function(){{return data;}},configurable: true}});
                            Object.defineProperty(model,'id',{{get:function(){{return this.{2}.id;}},configurable: true}});
                            if (model.$emit!=undefined){{model.$emit('{3}',model);}}
                            options.success(model);
                        }}else{{
                            options.failure(response.text());
                        }}
                    }}
                }});
            }}
        }},", new object[]{
                urlRoot,
                Constants.TO_JSON_VARIABLE,
                Constants.INITIAL_DATA_KEY,
                Constants.Events.MODEL_SAVED,
                RequestHandler.RequestMethods.PUT
            }));
        }

        private void _AppendInstanceMethods(Type modelType,string urlRoot, ref WrappedStringBuilder builder)
        {
            foreach (MethodInfo mi in modelType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                {
                    bool allowNull = ((ExposedMethod)mi.GetCustomAttributes(typeof(ExposedMethod), false)[0]).AllowNullResponse;
                    builder.AppendFormat("          {0}:function(", mi.Name);
                    ParameterInfo[] pars = mi.GetParameters();
                    for (int x = 0; x < pars.Length; x++)
                        builder.Append(pars[x].Name + (x + 1 == pars.Length ? "" : ","));
                    builder.AppendLine(@"){
                var function_data = {};");
                    foreach (ParameterInfo par in pars)
                    {
                        Type propType = par.ParameterType;
                        bool array = false;
                        if (propType.FullName.StartsWith("System.Nullable"))
                        {
                            if (propType.IsGenericType)
                                propType = propType.GetGenericArguments()[0];
                            else
                                propType = propType.GetElementType();
                        }
                        if (propType.IsArray)
                        {
                            array = true;
                            propType = propType.GetElementType();
                        }
                        else if (propType.IsGenericType)
                        {
                            if (propType.GetGenericTypeDefinition() == typeof(List<>))
                            {
                                array = true;
                                propType = propType.GetGenericArguments()[0];
                            }
                        }
                        if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                        {
                            if (array)
                            {
                                builder.AppendLine(string.Format(@"function_data.{0}=[];
for(var x=0;x<{0}.length;x++){{
    function_data.{0}.push({{id:{0}[x].id}});
}}", par.Name));
                            }
                            else
                                builder.AppendLine(string.Format("function_data.{0} = {{ id: {0}.id }};", par.Name));
                        }
                        else
                            builder.AppendLine(string.Format("function_data.{0} = {0};", par.Name));
                    }
                    builder.AppendLine(string.Format(@"             var response = ajax(
                    {{
                        url:'{0}/'+this.id+'/{1}',
                    type:'METHOD',
                    headers: {{
                        'Content-Type': 'application/json',
                    }},
                    data:JSON.stringify(function_data),
                    async:false
                }});
                if (response.ok){{
                    {2}
                }}else{{
                    throw response.text();
                }}", new object[]{
                        urlRoot,
                        mi.Name,
                        (mi.ReturnType == typeof(void) ? "" : @"var ret=response.json();
                    if (ret!=undefined||ret==null)
                        response = ret;")
                    }));
                    if (mi.ReturnType != typeof(void))
                    {
                        Type propType = mi.ReturnType;
                        bool array = false;
                        if (propType.FullName.StartsWith("System.Nullable"))
                        {
                            if (propType.IsGenericType)
                                propType = propType.GetGenericArguments()[0];
                            else
                                propType = propType.GetElementType();
                        }
                        if (propType.IsArray)
                        {
                            array = true;
                            propType = propType.GetElementType();
                        }
                        else if (propType.IsGenericType)
                        {
                            if (propType.GetGenericTypeDefinition() == typeof(List<>))
                            {
                                array = true;
                                propType = propType.GetGenericArguments()[0];
                            }
                        }
                        builder.AppendLine("if (response==null){");
                        if (!allowNull)
                            builder.AppendLine("throw \"A null response was returned by the server which is invalid.\";");
                        else
                            builder.AppendLine("return response;");
                        builder.AppendLine("}else{");
                        if (new List<Type>(propType.GetInterfaces()).Contains(typeof(IModel)))
                        {
                            if (array)
                            {
                                builder.AppendLine(string.Format(@"         ret=[];
            for (var x=0;x<response.length;x++){{
                ret.push(App.Models.{0}.{1}());
                ret[x].{2}(response[x]);
            }}
            response = ret;", new object[]{
                                propType.Name,
                                Constants.CREATE_INSTANCE_FUNCTION_NAME,
                                Constants.PARSE_FUNCTION_NAME
                                    }));
                            }
                            else
                            {
                                builder.AppendLine(string.Format(@"             ret = App.Models.{0}.{1}();
            ret.{2}(response);
            response=ret;", new object[]{
                  propType.Name,
                  Constants.CREATE_INSTANCE_FUNCTION_NAME,
                  Constants.PARSE_FUNCTION_NAME
                      }));
                            }
                        }
                        builder.AppendLine(@"           return response;
        }");
                    }
                    builder.AppendLine("},");
                }
            }
        }

        private void _AppendData(Type modelType, List<PropertyInfo> props, ref WrappedStringBuilder builder)
        {
            IModel m = null;
            if (modelType.GetConstructor(Type.EmptyTypes) != null)
            {
                m = (IModel)modelType.GetConstructor(Type.EmptyTypes).Invoke(new object[] { });
            }
            builder.AppendLine(@"   data = {");
            bool isFirst = true;
            foreach (PropertyInfo pi in props)
            {
                if (pi.CanRead && pi.CanWrite)
                {
                    builder.Append(string.Format(@"{2}
            {0}:{1}", new object[]
                    {
                        pi.Name,
                        (m==null ? "null" : (pi.GetValue(m,new object[0])==null ? "null" : JSON.JsonEncode(pi.GetValue(m,new object[0])))),
                        (isFirst ? "" : ",")
                    }));
                    isFirst = false;
                }
            }
            builder.AppendLine(@"
    };");
        }

        private void _AppendComputed(List<PropertyInfo> props, ref WrappedStringBuilder builder)
        {
            builder.AppendLine("    computed = extend(computed,{");
            foreach (PropertyInfo pi in props)
            {
                if (!pi.CanWrite)
                {
                    builder.AppendLine(string.Format(@"         {0}:{{
                get:function(){{
                    return  (this.{1} == undefined ? undefined : this.{1}.{0});
                }}
            }},", new object[]{
                        pi.Name,
                        Constants.INITIAL_DATA_KEY
                    }));
                }
            }
            _AppendValidations(props, ref builder);
            builder.AppendLine("    });");
        }

        private void _AppendValidations(List<PropertyInfo> props, ref WrappedStringBuilder builder)
        {
            List<PropertyInfo> requiredProps = new List<PropertyInfo>();
            foreach (PropertyInfo pi in props)
            {
                if (pi.GetCustomAttributes(typeof(ModelRequiredField), false).Length > 0)
                    requiredProps.Add(pi);
            }
            if (requiredProps.Count > 0)
            {
                builder.AppendLine(@"        isValid:function(){
            var ret=true;");
                foreach (PropertyInfo pi in requiredProps)
                    builder.AppendLine(string.Format("          ret&=(this.{0}==undefined||this.{0}==null ? false : true);", pi.Name));
                builder.AppendLine(@"           return ret;
        },
        invalidFields:function(){
            var ret=[];");
                foreach (PropertyInfo pi in requiredProps)
                    builder.AppendLine(string.Format(@"          if (this.{0}==undefined||this.{0}==null){{
                ret.push('{0}');
            }}", pi.Name));
                builder.AppendLine(@"            return ret;
        }");
            }
            else
                builder.AppendLine(@"       isValid:function(){return true;},
        invalidFields:function(){return [];}");
        }
    }
}
