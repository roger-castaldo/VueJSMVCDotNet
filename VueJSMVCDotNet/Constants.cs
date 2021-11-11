using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal static class Constants
    {
        public static readonly DateTime UTC = new DateTime(1970, 1, 1, 00, 00, 00, DateTimeKind.Utc);
        public const string INITIAL_DATA_KEY = "_initialData";
        public const string TO_JSON_VARIABLE = "_toJSON";
        public const string PARSE_FUNCTION_NAME = "_parse";
        public const string CREATE_INSTANCE_FUNCTION_NAME = "createInstance";
        public const string IS_VUE_3 = "(Vue.version.indexOf('3')==0)";
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
            }},", new object[]{
                                                                            Events.LIST_MODEL_LOADED,
                                                                            Events.LIST_MODEL_DESTROYED,
                                                                            Events.LIST_MODEL_UPDATED,
                                                                            Events.LIST_LOADED
        });
        public static readonly string _LIST_RELOAD_CODE = string.Format(@"            reload:function(){{
                var tmp = this;
                return new Promise((resolve,reject)=>{{
                    ajax({{
                        url:$url$,
                        type:'GET',
                        credentials: 'include'
                    }}).then(response=>{{
                        if (response.ok){{                 
                            var data = response.json();
                            if (data!=null){{
                                if (data.TotalPages!=undefined){{
                                    var pages = data.TotalPages;
                                    Object.defineProperty(tmp,'totalPages',{{value:function(){{return pages;}},writeable:true}});
                                    data=data.response;
                                }}
                            }}else if (tmp.totalPages!=undefined){{
                                Object.defineProperty(tmp,'totalPages',{{value:function(){{return 0;}},writeable:true}});
                            }}
                            if (data!=null){{
                                for(var x=0;x<data.length;x++){{ 
                                    var mtmp = App.Models.$type$.{8}();
                                    mtmp.{0}(data[x]);
                                    data[x] = mtmp;
                                    data[x].$on('{1}',function(model){{
                                        var arr = getArrayMap(tmp);
                                        for(var x=0;x<arr.length;x++){{
                                            if (arr[x].id==model.id){{
                                                arr._trigger('{2}',model);
                                                var tid = unlockArray(arr);
                                                Array.prototype.splice.apply(arr,[x,1]);
                                                lockArray(arr,tid);
                                                break;
                                            }}
                                        }}
                                    }});
                                    data[x].$on('{3}',function(model){{
                                        var arr = getArrayMap(tmp);
                                        for(var x=0;x<arr.length;x++){{
                                            if (arr[x].id==model.id){{
                                                arr._trigger('{4}',model);
                                                var tid = unlockArray(arr);
                                                if ({9}){{
                                                    Array.prototype.splice.apply(arr,[x,1,model]);
                                                }}else{{
                                                    Vue.set(arr,x,model);
                                                }}
                                                lockArray(arr,tid);
                                                break;
                                            }}
                                        }}
                                    }});
                                    data[x].$on('{5}',function(model){{
                                        var arr = getArrayMap(tmp);
                                        for(var x=0;x<arr.length;x++){{
                                            if (arr[x].id==model.id){{
                                                arr._trigger('{6}',model);
                                                var tid = unlockArray(arr);
                                                if ({9}){{
                                                    Array.prototype.splice.apply(arr,[x,1,model]);
                                                }}else{{
                                                    Vue.set(arr,x,model);
                                                }}
                                                lockArray(arr,tid);
                                                break;
                                            }}
                                        }}
                                    }});
                                }}
                                var tid = unlockArray(tmp);
                                Array.prototype.push.apply(getArrayMap(tmp),data);
                                if (tmp.length-data.length>0){{
                                    Array.prototype.splice.apply(tmp,[0,tmp.length-data.length]);
                                }}
                                lockArray(tmp,tid);
                            }}else{{
                                var tid = unlockArray(tmp);
                                if (tmp.length>0){{
                                    Array.prototype.splice.apply(tmp,[0,tmp.length]);
                                }}
                                lockArray(tmp,tid);
                            }}
                            tmp._trigger('{7}');
                            resolve(tmp);
                        }}else{{
                            reject(data);
                        }}
                    }});
                }});
            }}", new object[]{
                                                                            PARSE_FUNCTION_NAME,
                                                                            Events.MODEL_DESTROYED,
                                                                            Events.LIST_MODEL_DESTROYED,
                                                                            Events.MODEL_UPDATED,
                                                                            Events.LIST_MODEL_UPDATED,
                                                                            Events.MODEL_LOADED,
                                                                            Events.LIST_MODEL_LOADED,
                                                                            Events.LIST_LOADED,
                                                                            CREATE_INSTANCE_FUNCTION_NAME,
                                                                            IS_VUE_3
        });
        public static readonly string ARRAY_TO_VUE_METHOD = string.Format(@"
        toVue:function(options){{
            var opts = this.toMixins();
            if (options==undefined || options==null){{
                options=opts;
            }}else{{
                if (options.mixins==undefined){{
                    options.mixins=[];
                }}
                options.mixins.push(opts);
            }}
            if ({0}){{
                return Vue.createApp(options);
            }}else{{
                return new Vue(options);
            }}
        }},
        toMixins:function(){{
                var items = this;
                var options={{
                    data : function(){{
                        return {{
                            Items:items
                        }};
                    }},
                    methods:{{
                        reload:function(){{
                            return this.Items.reload();
                        }}
                    }},
                    created:function(){{
                        var view=this;
                        setArrayMap(this.Items);
                        if ({0}){{
                            this.Items.on('{1}',function(){{view.$forceUpdate();}});
                            this.Items.on('{2}',function(){{view.$forceUpdate();}});
                            this.Items.on('{3}',function(){{view.$forceUpdate();}});
                            this.Items.on('{4}',function(){{view.$forceUpdate();}});
                        }}
                    }}
                }};
                if (this.currentIndex!=undefined){{
                    options.methods= extend(options.methods,{{
                        currentIndex:function(){{ return this.Items.currentIndex();}},
                        currentPage:function(){{ return this.Items.currentPage();}},
                        currentPageSize:function(){{ return this.Items.currentPageSize();}},
                        totalPages:function(){{ return this.Items.totalPages();}},
                        moveToPage:function(pageNumber){{ return this.Items.moveToPage(pageNumber);}},
                        moveToNextPage:function(){{ return this.Items.moveToNextPage();}},
                        moveToPreviousPage:function(){{ return this.Items.moveToPreviousPage();}},
                        changePageSize:function(){{ return this.Items.changePageSize();}}
                    }});
                }}
                if (this.changeParameters!=undefined){{
                    options.methods= extend(options.methods,{{
                        currentParameters:function(){{ return this.Items.currentParameters();}},
                        changeParameters:function(){{
                            if (arguments.length==0){{
                                return this.Items.changeParameters.call(this.Items);
                            }}else{{
                                var args=[];
                                for(var x=0;x<arguments.length;x++){{
                                    args.push(arguments[x]);
                                }}
                                return this.Items.changeParameters.call(this.Items,args);
                            }}
                        }}
                    }});
                }}
                return options;
            }},", new object[]{
                                                                              IS_VUE_3,
                                                                               Events.LIST_MODEL_LOADED,
                                                                               Events.LIST_MODEL_UPDATED,
                                                                               Events.LIST_MODEL_DESTROYED,
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
