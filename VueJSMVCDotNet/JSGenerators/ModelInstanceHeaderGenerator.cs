using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    class ModelInstanceHeaderGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType)
        {
            builder.AppendLine(@"            var methods = {};
            var data = {};
            var computed = {};
            var map = new WeakMap();
            const setMap=function(obj,val){
                var t = obj;
                if (Vue!=undefined && Vue.isProxy!=undefined && Vue.toRaw!=undefined){
                    if (Vue.isProxy(obj)){
                        t = Vue.toRaw(obj);
                    }
                }
                map.set(t,val);
            };
            const getMap=function(obj){
                var t = obj;
                if (Vue!=undefined && Vue.isProxy!=undefined && Vue.toRaw!=undefined){
                    if (Vue.isProxy(obj)){
                        t = Vue.toRaw(obj);
                    }
                }
                return map.get(t);
            };");
        }
    }
}
