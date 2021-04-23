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
            }},",new object[]{
                                                                            Events.LIST_MODEL_LOADED,
                                                                            Events.LIST_MODEL_DESTROYED,
                                                                            Events.LIST_MODEL_UPDATED,
                                                                            Events.LIST_LOADED
        });
        public static readonly string _LIST_RELOAD_CODE = string.Format(@"            reload:function(){{
                var tmp = this;
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
                                tmp.totalPages=function(){{return pages;}};
                                data=data.response;
                            }}
                        }}else if (tmp.totalPages!=undefined){{
                            tmp.totalPages=function(){{return 0;}};
                        }}
                        if (data!=null){{
                            for(var x=0;x<data.length;x++){{ 
                                var mtmp = App.Models.$type$.{8}();
                                mtmp.{0}(data[x]);
                                data[x] = mtmp;
                                data[x].$on('{1}',function(model){{
                                    for(var x=0;x<tmp.length;x++){{
                                        if (tmp[x].id==model.id){{
                                            tmp._trigger('{2}',model);
                                            tmp.splice(x,1);
                                            break;
                                        }}
                                    }}
                                }});
                                data[x].$on('{3}',function(model){{
                                    for(var x=0;x<tmp.length;x++){{
                                        if (tmp[x].id==model.id){{
                                            tmp._trigger('{4}',model);
                                            if ({9}){{
                                                tmp['splice'](x,1,model);
                                            }}else{{
                                                Vue.set(tmp,x,model);
                                            }}
                                            break;
                                        }}
                                    }}
                                }});
                                data[x].$on('{5}',function(model){{
                                    for(var x=0;x<tmp.length;x++){{
                                        if (tmp[x].id==model.id){{
                                            tmp._trigger('{6}',model);
                                            if ({9}){{
                                                tmp['splice'](x,1,model);
                                            }}else{{
                                                Vue.set(tmp,x,model);
                                            }}
                                            break;
                                        }}
                                    }}
                                }});
                            }}
                            Array.prototype.push.apply(tmp,data);
                            if (tmp.length-data.length>0){{
                                tmp.splice(0,tmp.length-data.length);
                            }}else{{
                                tmp.push(App.Models.$type$.{8}());
                                tmp.splice(tmp.length-1,tmp.length);
                            }}
                        }}else{{
                            if (tmp.length>0){{
                                tmp.splice(0,tmp.length);
                            }}else{{
                                tmp.push(App.Models.$type$.{8}());
                                tmp.splice(tmp.length-1,tmp.length);
                            }}
                        }}
                        tmp._trigger('{7}');
                    }}else{{
                        throw data;
                    }}
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
        public static readonly string ARRAY_TO_VUE_METHOD = string.Format(@"            toVue:function(options){{
                options = options || {{}};
                var items = this;
                var computed = {{
                    Items:{{
                        get:function(){{return items;}}
                    }}
                }};
                var methods = {{
                    reload:function(){{
                        this.Items.reload();
                    }}
                }};
                if (this.currentIndex!=undefined){{
                    methods= extend(methods,{{
                        currentIndex:function(){{ return this.Items.currentIndex();}},
                        currentPageSize:function(){{ return this.Items.currentPageSize();}},
                        totalPages:function(){{ return this.Items.totalPages();}},
                        moveToPage:function(pageNumber){{ return this.Items.moveToPage(pageNumber);}},
                        moveToNextPage:function(){{ return this.Items.moveToNextPage();}},
                        moveToPreviousPage:function(){{ return this.Items.moveToPreviousPage();}},
                        changePageSize:function(){{ return this.Items.changePageSize();}},
                        currentParameters:function(){{ return this.Items.currentParameters();}},
                        changeParameters:function(){{
                            if (arguments.length==0){{
                                this.Items.changeParameters.call(this.Items);
                            }}else{{
                                var args=[];
                                for(var x=0;x<arguments.length;x++){{
                                    args.push(arguments[x]);
                                }}
                                this.Items.changeParameters.call(this.Items,args);
                            }}
                        }}
                    }});
                }}
                var _created = options.created;
                options.created = function(){{
                    var view=this;
                    this.Items.on('{1}',function(){{view.$forceUpdate();}});
                    this.Items.on('{2}',function(){{view.$forceUpdate();}});
                    this.Items.on('{3}',function(){{view.$forceUpdate();}});
                    this.Items.on('{4}',function(){{view.$forceUpdate();}});
                    if (_created!=undefined&&_created!=null){{
                        _created.call(this);
                    }}
                }};
                options.computed = extend(computed,options.computed);
                options.methods = extend(methods,options.methods);
                if ({0}){{
                    return Vue.createApp(options);
                }}else{{
                    return new Vue(options);
                }}
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
