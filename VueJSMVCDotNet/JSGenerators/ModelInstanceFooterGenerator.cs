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
                var ret = {};
                for(var prop in data){ Object.defineProperty(ret,prop,data[prop]);}
                for(var method in methods){ Object.defineProperty(ret,method,methods[method]);}
                for(var compute in computed){ Object.defineProperty(ret,compute,computed[compute]);}
                Object.defineProperty(ret,'toVue',function(){
                    for(var prop in data){
                        data[prop] = this[prop];
                    }
                    return Vue.createApp({data:function(){return data},methods:methods,computed:computed});
                });
                return ret;
            }else{
                throw 'unsupported version of VueJS found.';
            }
        };");
        }
    }
}
