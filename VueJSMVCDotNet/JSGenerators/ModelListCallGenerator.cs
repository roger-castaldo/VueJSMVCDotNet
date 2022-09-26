﻿using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ModelListCallGenerator : IJSGenerator
    {
        private static string _CreateJavacriptUrlCode(ModelListMethod mlm, ParameterInfo[] pars, Type modelType,string urlBase)
        {
            Logger.Trace("Creating the javascript url call for the model list method at path {0}",new object[] { mlm.Path });
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
                return "'" + (urlBase==null ? null : urlBase)+string.Format((mlm.Path.StartsWith("/") ? mlm.Path : "/" + mlm.Path).TrimEnd('/'), pNames) + "'";
            }
            else
                return "'"  + (urlBase==null ? null : urlBase)+ (mlm.Path.StartsWith("/") ? mlm.Path : "/" + mlm.Path).TrimEnd('/') + "'";
        }

        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType, string modelNamespace, string urlBase)
        {
            foreach (MethodInfo mi in modelType.GetMethods(Constants.LOAD_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                {
                    Logger.Trace("Adding List Call[{0}] for Model Definition[{1}]", new object[]
                    {
                        mi.Name,
                        modelType.FullName
                    });
                    ModelListMethod mlm = (ModelListMethod)mi.GetCustomAttributes(typeof(ModelListMethod), false)[0];
                    NotNullArguement nna = (mi.GetCustomAttributes(typeof(NotNullArguement), false).Length == 0 ? null : (NotNullArguement)mi.GetCustomAttributes(typeof(NotNullArguement), false)[0]);
                    builder.AppendFormat(@"{0}.{1}=extend({0}.{1},{{
    {2}:function(", new object[] { modelNamespace,modelType.Name, mi.Name });
                    ParameterInfo[] pars = Utility.ExtractStrippedParameters(mi);
                    string url = _CreateJavacriptUrlCode(mlm, pars, modelType,urlBase);
                    if (mlm.Paged)
                        url += string.Format("+'{0}PageStartIndex='+this.currentIndex()+'&PageSize='+this.currentPageSize()", (mlm.Path.Contains("?") ? "&" : "?"));
                    for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                        builder.Append((x > 0 ? "," : "") + pars[x].Name);
                    if (mlm.Paged)
                        builder.Append((pars.Length > 3 ? "," : "") + "pageStartIndex,pageSize");
                    builder.AppendLine(@"){");
                    for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                        builder.AppendLine(string.Format("      {0} = _checkProperty('{0}','{1}',{0},{2});", new object[]
                            {
                                pars[x].Name,
                                Utility.GetTypeString(pars[x].ParameterType,(nna==null ? false : !nna.IsParameterNullable(pars[x]))),
                                Utility.GetEnumList(pars[x].ParameterType)
                            }));
                    if (mlm.Paged) { 
                        builder.AppendLine(@"       pageStartIndex = (pageStartIndex == undefined ? 0 : (pageStartIndex == null ? 0 : pageStartIndex));
        pageSize = (pageSize == undefined ? 10 : (pageSize == null ? 10 : pageSize));
        var ret = secureArray(extend([],{
            currentIndex:function(){return pageStartIndex;},
            currentPageSize:function(){return pageSize;},
            currentPage:function(){return Math.floor(this.currentIndex()/this.currentPageSize());},
            totalPages:function(){return 0;},
            moveToPage:function(pageNumber){
                if (pageNumber>=this.totalPages()){
                    throw 'Unable to move to Page that exceeds current total pages.';
                }else{
                    this.currentIndex=function(){return pageNumber*this.currentPageSize();};
                    return this.reload();
                }
            },
            moveToNextPage:function(){
                if(Math.floor(this.currentIndex()/this.currentPageSize())+1<this.totalPages()){
                    return this.moveToPage(Math.floor(this.currentIndex()/this.currentPageSize())+1);
                }else{
                    throw 'Unable to move to next Page as that will excess current total pages.';
                }
            },
            moveToPreviousPage:function(){
                if(Math.floor(this.currentIndex()/this.currentPageSize())-1>=0){
                    return this.moveToPage(Math.floor(this.currentIndex()/this.currentPageSize())-1);
                }else{
                    throw 'Unable to move to previous Page as that will be before the first page.';
                }
            },
            changePageSize:function(size){
                this.currentPageSize = function(){ return size;};
                return this.reload();
            },");
                    }
                    else
                        builder.AppendLine(@"        var ret = secureArray(extend([],{");
                    builder.Append(string.Format("url:function(){{ return {0};}},", url));
                    builder.Append(Constants._LIST_EVENTS_CODE);
                    builder.Append(Constants.ARRAY_TO_VUE_METHOD);
                    builder.Append(Constants._LIST_RELOAD_CODE.Replace("$url$", "this.url()").Replace("$type$", modelType.Name).Replace("$nspace$",modelNamespace));
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
                        builder.AppendLine(@"){
                if (arguments.length!=0){
                    if (Array.isArray(arguments[0]) && arguments.length==1){
                        var args = arguments[0];");
                        for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                            builder.AppendLine(string.Format("                      {0} = _checkProperty('{0}','{2}',(args.length>{1} ? args[{1}] : undefined),{3});", new object[]{
                                pars[x].Name,
                                x,
                                Utility.GetTypeString(pars[x].ParameterType,(nna==null ? false : !nna.IsParameterNullable(pars[x]))),
                                Utility.GetEnumList(pars[x].ParameterType)
                            }));
                        builder.AppendLine(string.Format(@"                    }}
                }}
                this.url=function(){{ return {0};}};
                this.currentParameters=function(){{
                    return {{", url));
                        for (int x = 0; x < (mlm.Paged ? pars.Length - 3 : pars.Length); x++)
                            builder.AppendLine(string.Format("              {0}:{0}{1}", new object[] { pars[x].Name, (x + 1 == (mlm.Paged ? pars.Length - 3 : pars.Length) ? "" : ",") }));
                        builder.AppendLine(@"                   };
                };
                return this.reload();
            }");
                    }
                    builder.AppendLine(@"        }));
        ret.reload();
        return ret;
    }
});");
                }
            }
        }
    }
}
