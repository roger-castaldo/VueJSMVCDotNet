using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal static class Constants
    {
        public static readonly DateTime UTC = new DateTime(1970, 1, 1, 00, 00, 00, DateTimeKind.Utc);
        public const string PARSERS_VARIABLE = "parsers";
        public const string INITIAL_DATA_KEY = "_initialData";
        public const string TO_JSON_VARIABLE = "ModelToJSON";
        public const string STATICS_VARAIBLE = "staticCalls";
        public static readonly BindingFlags STORE_DATA_METHOD_FLAGS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        public static readonly BindingFlags LOAD_METHOD_FLAGS = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
        public static readonly string _LIST_EVENTS_CODE = string.Format(@"          _events:{{
                {0}:[],
                {1}:[],
                {2}:[],
                {3}:[]
            }},
            on:function(event,callback){{
                if (this._events[event]==undefined){{throw 'undefined event';}}
                this._events[event].push(callback);
            }},
            off:function(callback){{
                for(var prop in this._events){{
                    for(var x=0;x<this._events[prop].length;x++){{
                        if (this._events[prop][x]==callback){{
                            this._events[prop].splice(x,1);
                            break;
                        }}
                    }}
                }}
            }},
            _trigger:function(event,model){{
                if (this._events[event]==undefined){{throw 'undefined event';}}
                for(var x=0;x<this._events[event].length;x++){{
                    this._events[event][x]((model==undefined ? this : model));
                }}
            }},",new object[]{
                                                                            Events.LIST_MODEL_LOADED,
                                                                            Events.LIST_MODEL_DESTROYED,
                                                                            Events.LIST_MODEL_UPDATED,
                                                                            Events.LIST_LOADED
        });
        public static readonly string _LIST_RELOAD_CODE = string.Format(@"            reload:function(async){{
                var tmp = this;
                async = (async==undefined ? true : async);
                $.ajax({{
                    type:'GET',
                    url:$url$,
                    dataType:'text',
                    async:async,
                    cache:false
                }}).fail(function(jqXHR,testStatus,errorThrown){{
                    throw errorThrown;
                }}).done(function(data,textStatus,jqXHR){{
                    if (jqXHR.status==200){{                 
                        data = JSON.parse(data);
                        if (data!=null){{
                            if (data.TotalPages!=undefined){{
                                var pages = data.TotalPages;
                                tmp.totalPages=function(){{return pages;}};
                                data=data.response;
                            }}
                        }}else if (tmp.totalPages!=undefined){{
                            tmp.totalPages=function(){{return 0;}};
                        }}
                        if (data!=null){{
                            for(var x=0;x<data.length;x++){{ 
                                data[x] = {0}['$type$'](data[x],new App.Models.$type$()); 
                                data[x].$on('{1}',function(model){{
                                    for(var x=0;x<tmp.length;x++){{
                                        if (tmp[x].id()==model.id()){{
                                            tmp._trigger('{2}',model);
                                            tmp.splice(x,1);
                                            break;
                                        }}
                                    }}
                                }});
                                data[x].$on('{3}',function(model){{
                                    for(var x=0;x<tmp.length;x++){{
                                        if (tmp[x].id()==model.id()){{
                                            tmp._trigger('{4}',model);
                                            Vue.set(tmp,x,model);
                                            break;
                                        }}
                                    }}
                                }});
                                data[x].$on('{5}',function(model){{
                                    for(var x=0;x<tmp.length;x++){{
                                        if (tmp[x].id()==model.id()){{
                                            tmp._trigger('{6}',model);
                                            Vue.set(tmp,x,model);
                                            break;
                                        }}
                                    }}
                                }});
                            }}
                            Array.prototype.push.apply(tmp,data);
                            if (tmp.length-data.length>0){{
                                tmp.splice(0,tmp.length-data.length);
                            }}else{{
                                tmp.push('');
                                tmp.splice(tmp.length-1,tmp.length);
                            }}
                            /*for(var x=0;x<data.length;x++){{
                                tmp.push(data[x]);
                            }}*/
                        }}else{{
                            if (tmp.length>0){{
                                tmp.splice(0,tmp.length);
                            }}else{{
                                tmp.push('');
                                tmp.splice(tmp.length-1,tmp.length);
                            }}
                        }}
                        tmp._trigger('{7}');
                    }}else{{
                        throw data;
                    }}
                }});
            }}", new object[]{
                                                                            PARSERS_VARIABLE,
                                                                            Events.MODEL_DESTROYED,
                                                                            Events.LIST_MODEL_DESTROYED,
                                                                            Events.MODEL_UPDATED,
                                                                            Events.LIST_MODEL_UPDATED,
                                                                            Events.MODEL_LOADED,
                                                                            Events.LIST_MODEL_LOADED,
                                                                            Events.LIST_LOADED
        });
        public static class Events
        {
            public const string MODEL_LOADED = "loaded";
            public const string MODEL_DESTROYED = "destroyed";
            public const string MODEL_UPDATED = "updated";
            public const string MODEL_SAVED = "saved";
            public const string LIST_MODEL_LOADED = "model_loaded";
            public const string LIST_MODEL_DESTROYED = "model_destroyed";
            public const string LIST_MODEL_UPDATED = "model_updated";
            public const string LIST_LOADED = "loaded";
        }
    }
}
