using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ModelLoadAllGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, bool minimize, Type modelType)
        {
            string urlRoot = Utility.GetModelUrlRoot(modelType);
            foreach (MethodInfo mi in modelType.GetMethods(Constants.LOAD_METHOD_FLAGS))
            {
                if (mi.GetCustomAttributes(typeof(ModelLoadAllMethod), false).Length > 0)
                {
                    builder.AppendLine(string.Format(@"{0}=$.extend({0},{{LoadAll:function(){{
        var ret = $.extend([],{{
            reload:function(async){{
                async = (async==undefined ? true : async);
                $.ajax({{
                    type:'GET',
                    url:'{2}',
                    dataType:'text',
                    async:async,
                    cache:false
                }}).fail(function(jqXHR,testStatus,errorThrown){{
                    throw errorThrown;
                }}).done(function(data,textStatus,jqXHR){{
                    if (jqXHR.status==200){{                 
                        data = JSON.parse(data);
                        while(ret.length>0){{ret.pop();}}
                        for(var x=0;x<data.length;x++){{
                            ret.push({3}['{1}'](data[x],new App.Models.{1}()));
                        }}
                        for(var x=0;x<ret.length;x++){{
                            ret[x].$on('{4}',function(model){{
                                for(var x=0;x<ret.length;x++){{
                                    if (ret[x].id()==model.id()){{
                                        ret.splice(x,1);
                                        break;
                                    }}
                                }}
                            }});
                            ret[x].$on('{5}',function(model){{
                                for(var x=0;x<ret.length;x++){{
                                    if (ret[x].id()==model.id()){{
                                        Vue.set(ret,x,model);
                                        break;
                                    }}
                                }}
                            }});
                            ret[x].$on('{6}',function(model){{
                                for(var x=0;x<ret.length;x++){{
                                    if (ret[x].id()==model.id()){{
                                        Vue.set(ret,x,model);
                                        break;
                                    }}
                                }}
                            }});
                        }}
                    }}else{{
                        throw data;
                    }}
                }});
            }}
        }});
        ret.reload(false);
        return ret;
    }}
}});", new object[] {
                        Constants.STATICS_VARAIBLE,
                        modelType.Name,
                        urlRoot,
                        Constants.PARSERS_VARIABLE,
                        Constants.Events.MODEL_DESTROYED,
                        Constants.Events.MODEL_UPDATED,
                        Constants.Events.MODEL_LOADED
                    }));
                    break;
                }
            }
        }
    }
}
