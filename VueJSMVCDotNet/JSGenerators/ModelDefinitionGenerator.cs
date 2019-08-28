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
        

        public void GeneratorJS(ref WrappedStringBuilder builder,bool minimize, Type modelType)
        {
            string urlRoot = Utility.GetModelUrlRoot(modelType);
            builder.AppendLine(string.Format(@"App.Models.{0}=Vue.extend({{", modelType.Name));
            List<PropertyInfo> props = Utility.GetModelProperties(modelType);
            IModel m = (IModel)modelType.GetConstructor(new Type[] { }).Invoke(new object[] { });
            _AppendData(m, props, ref builder);
            _AppendComputed(m, props, ref builder);

            builder.AppendLine("    methods:{");
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
            builder.AppendLine("    }");
            builder.AppendLine("});");
        }

        private void _AppendReloadMethod(Type modelType, string urlRoot, ref WrappedStringBuilder builder)
        {
            builder.AppendLine(string.Format(@"reload:function(options){{
                options = $.extend((options==undefined || options==null ?{{}}:options),{{
                    async:true,
                    success:function(){{}},
                    failure:function(error){{throw (error==undefined ? 'failed' : error);}}
                }});
                var model=this;
                $.ajax({{
                    type:'GET',
                    url:'{1}/'+this.id(),
                    dataType:'text',
                    async:options.async,
                    cache:false
                }}).fail(function(jqXHR,testStatus,errorThrown){{
                    options.error(errorThrown);
                }}).done(function(data,textStatus,jqXHR){{
                    if (jqXHR.status==200){{                 
                        {2}['{0}'](JSON.parse(data),model);
                        model.$emit('{3}',model);
                        options.success(model);
                    }}else{{
                        options.failure(data);
                    }}
                }});
            }}", new object[]{
                modelType.Name,
                urlRoot,
                Constants.PARSERS_VARIABLE,
                Constants.Events.MODEL_LOADED
            }));
        }

        private void _AppendDelete(string urlRoot, ref WrappedStringBuilder builder)
        {
            builder.AppendLine(string.Format(@"         destroy:function(options){{
            options = $.extend({{
                async:true,
                success:function(){{}},
                failure:function(error){{throw (error==undefined ? 'failed' : error);}}
            }},(options==undefined || options==null ?{{}}:options));
            var model=this;
            $.ajax({{
                type:'DELETE',
                url:'{0}/'+this.id(),
                content_type:'application/json; charset=utf-8',
                dataType:'text',
                async:options.async,
                cache:false
            }}).fail(function(jqXHR,testStatus,errorThrown){{
                options.error(errorThrown);
            }}).done(function(data,textStatus,jqXHR){{
                if (jqXHR.status==200){{                 
                    data = JSON.parse(data);
                    if (data){{
                        model.$emit('{1}',model);
                        options.success(model);
                    }}else{{
                        options.failure();
                    }}
                }}else{{
                    options.failure(data);
                }}
            }});
        }},", new object[]{
                urlRoot,
                Constants.Events.MODEL_DESTROYED
            }));
        }

        private void _AppendUpdate(string urlRoot, ref WrappedStringBuilder builder)
        {
            builder.AppendLine(string.Format(@"         update:function(options){{
            options = $.extend({{
                async:true,
                success:function(){{}},
                failure:function(error){{throw (error==undefined ? 'failed' : error);}}
            }},(options==undefined || options==null ?{{}}:options));
            if (!this.isValid){{
                options.failure();
            }}
            var model=this;
            $.ajax({{
               type:'PATCH',
               url:'{0}/'+this.id(),
               content_type:'application/json; charset=utf-8',
               data:JSON.stringify({1}(this)),
               dataType:'text',
               async:options.async,
               cache:false
            }}).fail(function(jqXHR,testStatus,errorThrown){{
                options.error(errorThrown);
            }}).done(function(data,textStatus,jqXHR){{
                if (jqXHR.status==200){{                 
                    data = JSON.parse(data);
                    if (data){{
                        model.$emit('{2}',model);
                        options.success(model);
                    }}else{{
                        options.failure();
                    }}
                }}else{{
                    options.failure(data);
                }}
           }});
        }},", new object[]{
                urlRoot,
                Constants.TO_JSON_VARIABLE,
                Constants.Events.MODEL_UPDATED
            }));
        }

        private void _AppendSave(string urlRoot, ref WrappedStringBuilder builder)
        {
            builder.AppendLine(string.Format(@"             save:function(options){{
            options = $.extend({{
                async:true,
                success:function(){{}},
                failure:function(error){{throw (error==undefined ? 'failed' : error);}}
            }},(options==undefined || options==null ?{{}}:options));
            if (!this.isValid){{
                options.failure();
            }}
            var data = {1}(this);
            var model=this;
            $.ajax({{
                type:'PUT',
                url:'{0}',
                content_type:'application/json; charset=utf-8',
                data:JSON.stringify(data),
                dataType:'text',
                async:options.async,
                cache:false
            }}).fail(function(jqXHR,testStatus,errorThrown){{
                options.error(errorThrown);
            }}).done(function(data,textStatus,jqXHR){{
                if (jqXHR.status==200){{                 
                    data=JSON.parse(data).id;
                    model.{2}= function(){{return data;}};
                    model.$emit('{3}',model);
                    options.success(model);
                }}else{{
                    options.failure(data);
                }}
           }});
        }},", new object[]{
                urlRoot,
                Constants.TO_JSON_VARIABLE,
                Constants.INITIAL_DATA_KEY,
                Constants.Events.MODEL_SAVED
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
    function_data.{0}.push({{id:{0}[x].id()}});
}}", par.Name));
                            }
                            else
                                builder.AppendLine(string.Format("function_data.{0} = {{ id: {0}.id() }};", par.Name));
                        }
                        else
                            builder.AppendLine(string.Format("function_data.{0} = {0};", par.Name));
                    }
                    builder.AppendLine(string.Format(@"             var response = $.ajax({{
                    type:'METHOD',
                    url:'{0}/'+this.id()+'/{1}',
                    data:JSON.stringify(function_data),
                    content_type:'application/json; charset=utf-8',
                    dataType:'json',
                    async:false,
                    cache:false
                }});
                if (response.status==200){{
                    {2}
                }}else{{
                    throw response.responseText;
                }}", new object[]{
                        urlRoot,
                        mi.Name,
                        (mi.ReturnType == typeof(void) ? "" : @"var ret=response.responseText;
                    if (ret!=undefined)
                        var response = JSON.parse(ret);")
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
                ret.push({1}(response[x],new App.Models.{0}()));
            }}
            response = ret;", new object[]{
                                propType.Name,
                                Constants.PARSERS_VARIABLE
                                    }));
                            }
                            else
                            {
                                builder.AppendLine(string.Format(@"             ret = {1}(response,new App.Models.{0}());
response=ret;", new object[]{
                  propType.Name,
                  Constants.PARSERS_VARIABLE
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

        private void _AppendData(IModel m, List<PropertyInfo> props, ref WrappedStringBuilder builder)
        {
            builder.AppendLine(@"   data:function(){
        return {");
            bool isFirst = true;
            foreach (PropertyInfo pi in props)
            {
                if (pi.CanRead && pi.CanWrite && pi.GetCustomAttributes(typeof(ReadOnlyModelProperty), true).Length == 0)
                {
                    builder.Append(string.Format(@"{2}
            {0}:{1}", new object[]
                    {
                        pi.Name,
                        (pi.GetValue(m,new object[0])==null ? "null" : JSON.JsonEncode(pi.GetValue(m,new object[0]))),
                        (isFirst ? "" : ",")
                    }));
                    isFirst = false;
                }
            }
            builder.AppendLine(@"
        };
    },");
        }

        private void _AppendComputed(IModel m, List<PropertyInfo> props, ref WrappedStringBuilder builder)
        {
            builder.AppendLine("    computed:{");
            foreach (PropertyInfo pi in props)
            {
                if (!pi.CanRead)
                {
                    builder.AppendLine(string.Format(@"         {0}:{{
                get:function(){{
                    return  (this.{1} == undefined ? undefined : this.{1}().{0});
                }},
                set:function(value){{}}
            }},",new object[]{
                        pi.Name,
                        Constants.INITIAL_DATA_KEY
                    }));
                }
            }
            _AppendValidations(props, ref builder);
            builder.AppendLine("    },");
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
