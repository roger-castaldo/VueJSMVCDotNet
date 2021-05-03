using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    class ModelInstanceFooterGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType)
        {
            builder.AppendLine(string.Format(@"     App.Models.{0} = App.Models.{0}||{{}};
        App.Models.{0}.{1} = function(){{ ", modelType.Name, Constants.CREATE_INSTANCE_FUNCTION_NAME));
            builder.AppendLine(@"         if (Vue.version.indexOf('2')==0){
                
                return new Vue({data:function(){return data;},methods:methods,computed:computed});
            }else if (Vue.version.indexOf('3')==0){
                var ret = {
                    $on:function(event,callback){
                        if (this._$events==undefined){this._$events={};}
                        if (this._$events[event]==undefined){this._$events[event]=[];}
                        this._$events[event].push(callback);
                    },
                    $off:function(callback){
                        if (this._$events!=undefined){
                            for(var evt in this._$events){
                                for(var x=0;x<this._$events[evt].length;x++){
                                    if (this._$events[evt][x]==callback){
                                        this._$events[evt].splice(x,1);
                                        break;
                                    }
                                }
                            }
                        }
                    },
                    $emit:function(event,data){
                        if (this._$events!=undefined){
                            if (this._$events[event]!=undefined){
                                for(var x=0;x<this._$events[event].length;x++){
                                    this._$events[event][x]((data==undefined ? this : data));
                                }
                            }
                        }
                    },
                    toVue:function(options){
                        if (options.mixins==undefined){options.mixins=[];}
                        options.mixins.push(this.toMixin());
                        return Vue.createApp(options);
                    },
                    toMixin:function(){
                        for(var prop in data){
                            data[prop] = this[prop];
                        }
                        return {
                            data:function(){return data;},
                            methods:extend(extend({},methods),(options.methods==undefined ? {} : options.methods)),
                            computed:extend(extend({},computed),(options.computed==undefined ? {} : options.computed))
                        };
                    }
                };
                var tmp = extend({_hashCode:null},data);");
                foreach (PropertyInfo pi in Utility.GetModelProperties(modelType))
                {
                    if (pi.CanRead && pi.CanWrite)
                    {
                    builder.AppendLine(string.Format(@"                Object.defineProperty(ret,'{0}',{{
                    get:function(){{return tmp.{0};}},
                    set:function(value){{
                        tmp.{0}=value;
                        H(JSON.stringify(tmp)).then(hash=>{{tmp._hashCode=hash;}});
                    }},
                    enumerable: true,
                    configurable: true
                }});", pi.Name));
                    }
                }
            builder.AppendLine(@"Object.defineProperty(ret,'_hashCode',{ get:function(){return tmp._hashCode;}});
                ret = extend(ret,methods);
                for(var prop in computed){
                    Object.defineProperty(ret,prop,computed[prop]);
                }
                return ret;
            }else{
                throw 'unsupported version of VueJS found.';
            }
        };");
        }
    }
}
