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
            Logger.Trace("Generating Model Definition javascript for {0}", new object[] { modelType.FullName });
            string urlRoot = Utility.GetModelUrlRoot(modelType);
            List<PropertyInfo> props = Utility.GetModelProperties(modelType);
            _AppendData(modelType, props, ref builder);
            Logger.Trace("Adding computed properties for Model Definition[{0}]", new object[] { modelType.FullName });
            _AppendComputed(props, ref builder);

            builder.AppendLine(string.Format(@"    methods = extend(methods,{{
        isNew:function(){{ return (getMap(this)==undefined ? true : (getMap(this).{0} == undefined ? true : (this.id==undefined? true : this.id==undefined||this.id==null)));}},",Constants.INITIAL_DATA_KEY));
            _AppendInstanceMethods(modelType,urlRoot, ref builder);
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
            builder.AppendLine("    });");
        }

        private void _AppendReloadMethod(Type modelType, string urlRoot, ref WrappedStringBuilder builder)
        {
            Logger.Trace("Adding reload method for Model Definition[{0}]", new object[] { modelType.FullName });
            builder.AppendLine(string.Format(@"reload:function(){{
                var model=this;
                return new Promise((resolve,reject)=>{{
                    if (model.isNew()){{
                        reject('Cannot reload unsaved model.');
                    }}else{{
                        ajax({{
                            url:'{0}/'+model.id,
                            type:'GET'
                        }}).then(
                            response=>{{
                                if (response.ok){{                 
                                    model.{1}(response.json());
                                    if (model.$emit!=undefined){{ model.$emit('{2}',model); }}
                                    resolve(model);
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
                Constants.Events.MODEL_LOADED
            }));
        }

        private void _AppendDelete(string urlRoot, ref WrappedStringBuilder builder)
        {
            builder.AppendLine(string.Format(@"         destroy:function(){{
                var model = this;
                return new Promise((resolve,reject)=>{{
                    if (model.isNew()){{
                        reject('Cannot delete unsaved model.');
                    }}else{{
                        ajax(
                        {{
                            url:'{0}/'+model.id,
                            type:'{2}'
                        }}).then(
                            response=>{{
                                if (response.ok){{                 
                                    var data = response.json();
                                    if (data){{
                                        if (model.$emit!=undefined){{model.$emit('{1}',model);}}
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
        }},", new object[]{
                urlRoot,
                Constants.Events.MODEL_DESTROYED,
                RequestHandler.RequestMethods.DELETE
            }));
        }

        private void _AppendUpdate(string urlRoot, ref WrappedStringBuilder builder,bool useJSON)
        {
            builder.AppendLine(string.Format(@"         update:function(){{
                var model=this;
                return new Promise((resolve,reject)=>{{
                    if (!model.isValid){{
                        reject('Invalid model.');
                    }}else if (model.isNew()){{
                        reject('Cannot update unsaved model, please call save instead.');
                    }}else{{
                        var data = model.{1}();
                        if (JSON.stringify(data)===JSON.stringify({{}})){{
                            resolve(model);
                        }}else{{
                            ajax(
                            {{
                                url:'{0}/'+model.id,
                                type:'{4}',
                                useJSON:{5},
                                data:data
                            }}).then(response=>{{
                                if (response.ok){{                 
                                    var data = response.json();
                                    if (data){{
                                        data=getMap(model).{3};
                                        for(var prop in data){{
                                            if (prop!='id'){{
                                                data[prop]=model[prop];
                                            }}
                                        }}
                                        setMap(model,{{{3}:data}});
                                        if (model.$emit!=undefined){{model.$emit('{2}',model);}}
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
        }},", new object[]{
                urlRoot,
                Constants.TO_JSON_VARIABLE,
                Constants.Events.MODEL_UPDATED,
                Constants.INITIAL_DATA_KEY,
                RequestHandler.RequestMethods.PATCH,
                useJSON.ToString().ToLower()
            }));
        }

        private void _AppendSave(string urlRoot, ref WrappedStringBuilder builder,bool useJSON)
        {
            builder.AppendLine(string.Format(@"             save:function(){{
                var model=this;
                return new Promise((resolve,reject)=>{{
                    if (!model.isValid){{
                        reject('Invalid model.');
                    }}else if (!model.isNew()){{
                        reject('Cannot save a saved model, please call update instead.');
                    }}else{{
                        var data = model.{1}();
                        ajax(
                        {{
                            url:'{0}',
                            type:'{4}',
                            useJSON:{5},
                            data:data
                        }}).then(response=>{{
                            if (response.ok){{                 
                                setMap(model,{{{2}:data}});
                                if (model.$emit!=undefined){{model.$emit('{3}',model);}}
                                resolve(model);
                            }}else{{
                                reject(response.text());
                            }}
                        }},response=>{{reject(response.text());}});    
                    }}
                }});
        }},", new object[]{
                urlRoot,
                Constants.TO_JSON_VARIABLE,
                Constants.INITIAL_DATA_KEY,
                Constants.Events.MODEL_SAVED,
                RequestHandler.RequestMethods.PUT,
                useJSON.ToString().ToLower()
            }));
        }

        private void _AppendInstanceMethods(Type modelType,string urlRoot, ref WrappedStringBuilder builder)
        {
            foreach (MethodInfo mi in modelType.GetMethods(Constants.INSTANCE_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                {
                    Logger.Trace("Adding Exposed Method[{1}] for Model Definition[{0}]", new object[] { modelType.FullName,mi.Name });
                    ExposedMethod em = (ExposedMethod)mi.GetCustomAttributes(typeof(ExposedMethod), false)[0];
                    Type returnType = (em.ArrayElementType!=null ? Array.CreateInstance(em.ArrayElementType, 0).GetType() : mi.ReturnType);
                    builder.AppendFormat("          {0}:function(", mi.Name);
                    ParameterInfo[] pars = Utility.ExtractStrippedParameters(mi);
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
                    builder.AppendLine(string.Format(@"             var model = this;
                return new Promise((resolve,reject)=>{{
                    ajax(
                    {{
                        url:'{0}/'+model.id+'/{1}',
                        type:'METHOD',
                        useJSON:{2},
                        data:function_data
                    }}).then(response=>{{
                        {3}", new object[]{
                        urlRoot,
                        mi.Name,
                        (mi.GetCustomAttributes(typeof(UseFormData),false).Length==0).ToString().ToLower(),
                        (returnType == typeof(void) ? "" : @"var ret=response.json();
                    if (ret!=undefined||ret==null)
                        response = ret;")
                    }));
                    if (em.IsSlow)
                    {
                        builder.AppendLine(@"               ret=[];
                var pullCall = function(){
                    ajax(
                    {
                        url:response,
                        type:'PULL',
                        useJSON:true
                    }).then(
                        res=>{
                            res = res.json();
                            if (res.Data.length>0){
                                Array.prototype.push.apply(ret,res.Data);
                            }
                            if (res.HasMore){
                                pullCall();
                            }else if (res.IsFinished){
                                response = ret;");
                    }
                    if (returnType != typeof(void))
                    {
                        bool array = false;
                        if (returnType.FullName.StartsWith("System.Nullable"))
                        {
                            if (returnType.IsGenericType)
                                returnType = returnType.GetGenericArguments()[0];
                            else
                                returnType = returnType.GetElementType();
                        }
                        if (returnType.IsArray)
                        {
                            array = true;
                            returnType = returnType.GetElementType();
                        }
                        else if (returnType.IsGenericType)
                        {
                            if (returnType.GetGenericTypeDefinition() == typeof(List<>))
                            {
                                array = true;
                                returnType = returnType.GetGenericArguments()[0];
                            }
                        }
                        if (!array && em.IsSlow)
                            builder.AppendLine("response = (ret.length==1 ? ret[0] : null);");
                        builder.AppendLine("if (response==null){");
                        if (!em.AllowNullResponse)
                            builder.AppendLine("reject(\"A null response was returned by the server which is invalid.\");");
                        else
                            builder.AppendLine("resolve(response);");
                        builder.AppendLine("}else{");
                        if (new List<Type>(returnType.GetInterfaces()).Contains(typeof(IModel)))
                        {
                            if (array)
                            {
                                builder.AppendLine(string.Format(@"         ret=[];
            for (var x=0;x<response.length;x++){{
                ret.push(_{0}(response[x]));
            }}
            response = ret;", new object[]{
                                returnType.Name
                                    }));
                            }
                            else
                            {
                                builder.AppendLine(string.Format(@"             ret = _{0}(response);
            response=ret;", new object[]{
                  returnType.Name
                      }));
                            }
                        }
                        builder.AppendLine(@"           resolve(response);
        }");
                    }else
                        builder.AppendLine("           resolve();");
                    if (em.IsSlow)
                    {
                        builder.AppendLine(@"                   resolve(ret);
                            }else{
                                setTimeout(pullCall,200);
                            }
                        },
                        err=>{
                            reject(err);
                        }
                    );
                };
                pullCall();");
                    }
                    builder.AppendLine(@"},
                    response=>{
                        reject(response);
                    });
    });
},");
                }
            }
        }

        private void _AppendData(Type modelType, List<PropertyInfo> props, ref WrappedStringBuilder builder)
        {
            Logger.Trace("Adding data method for Model Definition[{0}]", new object[] { modelType.FullName });
            IModel m = null;
            if (modelType.GetConstructor(Type.EmptyTypes) != null)
            {
                m = (IModel)modelType.GetConstructor(Type.EmptyTypes).Invoke(new object[] { });
            }
            builder.AppendLine(@"    data = {");
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
                    Logger.Trace("Appending Computed Property[{0}] for Model Definition[{1}]", new object[]{
                        pi.Name,
                        pi.DeclaringType.FullName
                    });
                    builder.AppendLine(string.Format(@"         {0}:{{
                get:function(){{
                    return  (getMap(this) == undefined ? undefined : getMap(this).{1}.{0});
                }},
                set:function(val){{}}
            }},", new object[]{
                        pi.Name,
                        Constants.INITIAL_DATA_KEY
                    }));
                }
            }
            builder.AppendLine(string.Format(@"     id:{{
            get:function(){{ 
                return (getMap(this)==undefined ? undefined : (getMap(this).{0}==undefined ? undefined : getMap(this).{0}.id));
            }}
        }},",Constants.INITIAL_DATA_KEY));
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
                builder.AppendLine(@"        isValid:{ 
            get:function(){
                var ret=true;");
                foreach (PropertyInfo pi in requiredProps)
                {
                    Logger.Trace("Appending Required Propert[{0}] for Model Definition[{1}] validations", new object[]{
                        pi.Name,
                        pi.DeclaringType.FullName
                    });
                    builder.AppendLine(string.Format("              ret=ret&&(this.{0}==undefined||this.{0}==null ? false : true);", pi.Name));
                }
                builder.AppendLine(@"               return ret;
            }
        },
        invalidFields:{
            get:function(){
                var ret=[];");
                foreach (PropertyInfo pi in requiredProps)
                    builder.AppendLine(string.Format(@"             if (this.{0}==undefined||this.{0}==null){{
                    ret.push('{0}');
                }}", pi.Name));
                builder.AppendLine(@"               return ret;
            }
        }");
            }
            else
                builder.AppendLine(@"       isValid:{get:function(){return true;}},
        invalidFields:{get:function(){return [];}}");
        }
    }
}
