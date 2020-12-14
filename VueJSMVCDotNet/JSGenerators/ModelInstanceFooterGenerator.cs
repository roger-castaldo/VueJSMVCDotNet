using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    class ModelInstanceFooterGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType)
        {
            builder.AppendLine(@"         if (Vue.version.indexOf('2')==0){
                
                return new Vue({data:function(){return data},methods:methods,computed:computed});
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
                        for(var prop in data){
                            data[prop] = this[prop];
                        }
                        options = options || {};
                        if (options.data!=undefined){
                            delete options.data;
                        }
                        var opts = {
                            data:function(){return data;},
                            methods:extend(extend({},methods),(options.methods==undefined ? {} : options.methods)),
                            computed:extend(extend({},computed),(options.computed==undefined ? {} : options.computed))
                        };
                        return Vue.createApp(extend(opts,options));
                    }
                };
                ret = extend(ret,data);
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
