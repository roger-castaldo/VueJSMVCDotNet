using System;
using System.Collections.Generic;
using System.Text;

namespace AutomatedTesting
{
    internal static class Constants
    {
        public static class Rights
        {
            public const string CAN_ACCESS = "CanAccess";
            public const string DELETE = "Delete";
            public const string LOAD = "Load";
            public const string LOAD_ALL = "LoadAll";
            public const string UPDATE = "Update";
            public const string SAVE = "Save";
            public const string SEARCH = "Search";
            public const string METHOD = "Method";
        }

        public const string JAVASCRIPT_BASE = @"
    var window={App:{}};
    var App = window.App;
    var Vue = {
        version:'3.0'
    };

    function WeakMap(){
        return {
            _data:{},
            set:function(key,value){
                this._data[key]=value;
            },
            get:function(key){
                return this._data[key];
            }
        }
    };

    function TextEncoder(){
        return {
            encode:function(data){
                data;
            }
        }
    }

    var crypto = {};

    function atob(data){return data;}

    var mockResults = [];
    
    function defineMockResult(url,callback){
        mockResults.push({
            url:url,
            callback:callback
        });
    }

    /*class XMLHttpRequest {
        constructor() {
            this.withCredentials=false;
            this.responseHeaders=[];
            this.requestHeaders=[];
            this.url=null;
            this.method=null;
            this.onreadystatechange=null;
            this.readyState=null;
            this.status=null;
            this.responseText=null;
            this.getResponseHeader = function(name){
                for(var x=0;x<this.responseHeaders.length;x++){
                    if (this.responseHeaders[x].name===name){
                        return this.responseHeaders[x].value;
                    }
                }
                return '';
            };

            this.setRequestHeader = function(name,value){
                this.requestHeaders.push({name:name,value:value});
            };

            this.open = function(method,url){
                this.method=method;
                this.url=url;
            };

            this.send = function(data){
                for(var x=0;x<mockResults.length;x++){
                    if (mockResults[x].url===this.url){
                        mockResults[x].callback(this);
                        break;
                    }
                }
                if (this.status==null){
                    this.status=400;
                    this.responseText='Not Found';
                }
                this.readyState=XMLHttpRequest.DONE;
                this.onreadystatechange();
            };
        };
        
        static DONE=1;
    }*/
";
    }
}
