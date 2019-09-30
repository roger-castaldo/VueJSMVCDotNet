using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ModelListCallGenerator : IJSGenerator
    {
        private static string _CreateJavacriptUrlCode(ModelListMethod mlm, MethodInfo mi, Type modelType)
        {
            Logger.Debug("Creating the javascript url call for the model list method at path " + mlm.Path);
            ParameterInfo[] pars = mi.GetParameters();
            if (pars.Length > 0)
            {
                string[] pNames = new string[pars.Length];
                for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("'+(");
                    if (pars[x].ParameterType == typeof(bool))
                        sb.AppendFormat("{0}==undefined ? 'false' : ({0}==null ? 'false' : ({0} ? 'true' : 'false')))+'", pars[x].Name);
                    else if (pars[x].ParameterType == typeof(DateTime))
                        sb.AppendFormat("{0}==undefined ? 'NULL' : ({0}==null ? 'NULL' : extractUTCDate({0})))+'", pars[x].Name);
                    else
                        sb.AppendFormat("{0}==undefined ? 'NULL' : ({0} == null ? 'NULL' : encodeURI({0})))+'", pars[x].Name);
                    pNames[x] = sb.ToString();
                }
                return "'" + string.Format((mlm.Path.StartsWith("/") ? mlm.Path : "/" + mlm.Path).TrimEnd('/'), pNames) + "'";
            }
            else
                return "'" + (mlm.Path.StartsWith("/") ? mlm.Path : "/" + mlm.Path).TrimEnd('/') + "'";
        }

        public void GeneratorJS(ref WrappedStringBuilder builder, bool minimize, Type modelType)
        {
            foreach (MethodInfo mi in modelType.GetMethods(Constants.LOAD_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                {
                    ModelListMethod mlm = (ModelListMethod)mi.GetCustomAttributes(typeof(ModelListMethod), false)[0];
                    builder.AppendFormat(@"{0}=$.extend({0},{{
    {1}:function(", new object[] { Constants.STATICS_VARAIBLE, mi.Name });
                    ParameterInfo[] pars = mi.GetParameters();
                    string url = _CreateJavacriptUrlCode(mlm, mi, modelType);
                    if (mlm.Paged)
                        url += string.Format("+'{0}PageStartIndex='+this.currentIndex()+'&PageSize='+this.currentPageSize()", (mlm.Path.Contains("?") ? "&" : "?"));
                    for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                        builder.Append((x > 0 ? "," : "") + pars[x].Name);
                    if (mlm.Paged)
                    {
                        builder.Append((pars.Length > 3 ? "," : "") + "pageStartIndex,pageSize");
                        builder.AppendLine(@"){
        pageStartIndex = (pageStartIndex == undefined ? 0 : (pageStartIndex == null ? 0 : pageStartIndex));
        pageSize = (pageSize == undefined ? 10 : (pageSize == null ? 10 : pageSize));
        var ret = $.extend([],{
            currentIndex:function(){return pageStartIndex;},
            currentPageSize:function(){return pageSize;},
            currentPage:function(){return Math.floor(this.currentIndex()/this.currentPageSize());},
            totalPages:function(){return 0;},
            moveToPage:function(pageNumber){
                if (pageNumber>=this.totalPages()){
                    throw 'Unable to move to Page that exceeds current total pages.';
                }else{
                    this.currentIndex=function(){return pageNumber*this.currentPageSize();};
                    this.reload();
                }
            },
            moveToNextPage:function(){
                if(Math.floor(this.currentIndex()/this.currentPageSize())+1<this.totalPages()){
                    this.moveToPage(Math.floor(this.currentIndex()/this.currentPageSize())+1);
                }else{
                    throw 'Unable to move to next Page as that will excess current total pages.';
                }
            },
            moveToPreviousPage:function(){
                if(Math.floor(this.currentIndex()/this.currentPageSize())-1>=0){
                    this.moveToPage(Math.floor(this.currentIndex()/this.currentPageSize())-1);
                }else{
                    throw 'Unable to move to previous Page as that will be before the first page.';
                }
            },
            changePageSize:function(size){
                this.currentPageSize = function(){ return size;};
                this.reload();
            },");
                    }
                    else
                    {
                        builder.AppendLine(@"){
        var ret = $.extend([],{");
                    }
                    builder.AppendLine(@"reload:function(){
                var tmp = this;
                var response = $.ajax({
                    type:'GET',
                    url:this.url(),
                    dataType:'text',
                    async:false,
                    cache:false
                }).fail(function(jqXHR,testStatus,errorThrown){
                    throw errorThrown;
                }).done(function(data,textStatus,jqXHR){
                    if (jqXHR.status==200){
                        data = JSON.parse(data);
                        while(tmp.length>0){ret.pop();}");
                    if (mlm.Paged)
                        builder.AppendLine("tmp.totalPages=function(){return data.TotalPages;};");
                    builder.AppendLine(string.Format(@"                 if (data{2}!=null){{
                            for(var x=0;x<data{2}.length;x++){{
                                tmp.push({1}['{0}'](data{2}[x],new App.Models.{0}()));
                            }}
                        }}
                        for(var x=0;x<tmp.length;x++){{
                            tmp[x].$on('{4}',function(model){{
                                tmp.reload();
                            }});
                            tmp[x].$on('{5}',function(model){{
                                for(var x=0;x<tmp.length;x++){{
                                    if (tmp[x].id()==model.id()){{
                                        Vue.set(tmp,x,model);
                                        break;
                                    }}
                                }}
                            }});
                            tmp[x].$on('{6}',function(model){{
                                for(var x=0;x<tmp.length;x++){{
                                    if (tmp[x].id()==model.id()){{
                                        Vue.set(tmp,x,model);
                                        break;
                                    }}
                                }}
                            }});
                        }}
                    }}else{{
                        throw data;
                    }}
                }});
            }},
            url:function(){{ return {3};}}", new object[] {
                        modelType.Name,
                        Constants.PARSERS_VARIABLE,
                        (mlm.Paged ? ".response" : ""),
                        url,
                        Constants.Events.MODEL_DESTROYED,
                        Constants.Events.MODEL_UPDATED,
                        Constants.Events.MODEL_LOADED
                    }));
                    if ((mlm.Paged&&pars.Length > 3)||(!mlm.Paged&&pars.Length>0))
                    {
                        builder.AppendLine(@",
            currentParameters:function(){
                return {");
                        for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                            builder.AppendLine(string.Format("              {0}:{0}{1}",new object[] { pars[x].Name, (x + 1 == (mlm.Paged ? pars.Length - 3 : pars.Length) ? "" : ",") }));
                        builder.AppendLine(@"                };
            },");
                        builder.Append("            changeParameters:function(");
                        for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                            builder.Append((x > 0 ? "," : "") + pars[x].Name);
                        builder.AppendLine(string.Format(@"){{
                this.url=function(){{ return {0};}};
                this.currentParameters=function(){{
                    return {{",url));
                        for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                            builder.AppendLine(string.Format("              {0}:{0}{1}", new object[] { pars[x].Name, (x + 1 == (mlm.Paged ? pars.Length - 3 : pars.Length) ? "" : ",") }));
                        builder.AppendLine(@"                   };
                };
                this.reload();
            }");
                    }
                    builder.AppendLine(@"        });
        ret.reload();
        return ret;
    }
});");
                }
            }
        }
    }
}
