using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    class ModelInstanceHeaderGenerator : IJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType, string modelNamespace, string urlBase)
        {
            string curBase = "window";
            string[] parts = modelNamespace.Split('.');
            foreach (string part in parts)
            {
                builder.AppendLine(string.Format("{0}.{1}={0}.{1}||{{}};", new object[] { curBase, part }));
                curBase+="."+part;
            }
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
            };
            const generateUUID = function() { // Public Domain/MIT
                var d = new Date().getTime();//Timestamp
                var d2 = ((typeof performance !== 'undefined') && performance.now && (performance.now()*1000)) || 0;//Time in microseconds since page-load or 0 if unsupported
                return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
                    var r = Math.random() * 16;//random number between 0 and 16
                    if(d > 0){//Use timestamp until depleted
                        r = (d + r)%16 | 0;
                        d = Math.floor(d/16);
                    } else {//Use microseconds since page-load if supported
                        r = (d2 + r)%16 | 0;
                        d2 = Math.floor(d2/16);
                    }
                    return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
                });
            }
            var arrMap = new WeakMap();
            const getArrayMap = function(arr){
                var t = arr;
                if (Vue!=undefined && Vue.isProxy!=undefined && Vue.toRaw!=undefined){
                    if (Vue.isProxy(arr)){
                        t = Vue.toRaw(arr);
                    }
                }
                if (arrMap.get(t)!=undefined){
                    return arrMap.get(t);
                }
                return arr;
            };
            const setArrayMap=function(arr){
                var t = arr;
                if (Vue!=undefined && Vue.isProxy!=undefined && Vue.toRaw!=undefined){
                    if (Vue.isProxy(arr)){
                        t = Vue.toRaw(arr);
                    }
                }
                arrMap.set(t,arr);
            };
        
            var arrSecMap = new WeakMap();
            const unlockArray=function(arr){
                var t = arr;
                if (Vue!=undefined && Vue.isProxy!=undefined && Vue.toRaw!=undefined){
                    if (Vue.isProxy(arr)){
                        t = Vue.toRaw(arr);
                    }
                }
                if (arrSecMap.get(t)==undefined){
                    arrSecMap.set(t,[]);
                }
                var id = generateUUID();
                arrSecMap.set(t,arrSecMap.get(t).concat([id]));
                return id;
            };
            const lockArray=function(arr,id){
                var t = arr;
                if (Vue!=undefined && Vue.isProxy!=undefined && Vue.toRaw!=undefined){
                    if (Vue.isProxy(arr)){
                        t = Vue.toRaw(arr);
                    }
                }
                if (arrSecMap.get(t)!=undefined){
                    var index=arrSecMap.get(t).indexOf(id);
                    if (index>-1){
                        var tmp = arrSecMap.get(t);
                        tmp.splice(index,1);
                        arrSecMap.set(t,tmp);
                    }
                }
            };
            const isArrayLocked=function(arr){
                var t = arr;
                if (Vue!=undefined && Vue.isProxy!=undefined && Vue.toRaw!=undefined){
                    if (Vue.isProxy(arr)){
                        t = Vue.toRaw(arr);
                    }
                }
                return (arrSecMap.get(t)==undefined ? [] : arrSecMap.get(t)).length==0;
            };
            const secureArray=function(arr){
                return new Proxy(arr,
                    {
                        get:function(target,prop,reciever){
                            switch(prop){
                                case 'push':
                                    return function(item){
                                        if(isArrayLocked(reciever)){
                                            throw 'Array is ReadOnly';
                                        }
                                        length++;
                                        return target.push(item);
                                    };
                                    break;
                                case 'splice':
                                    return function(start,deleteCount,item1){
                                        if(isArrayLocked(reciever)){
                                            throw 'Array is ReadOnly';
                                        }
                                        if (item1!=undefined){
                                            var ret = target.splice(start,deleteCount,item1);
                                        }else if (deleteCount!=undefined){
                                            var ret = target.splice(start,deleteCount);
                                        }else{
                                            var ret = target.splice(start);
                                        }
                                        return ret;
                                    };
                                    break;
                                case 'sort':
                                case 'reverse':
                                    throw 'Array is ReadOnly';
                                    break;
                                case '__proto__':
                                    return Array.__proto__;
                                    break;
                                case 'length':
                                    return target.length;
                                    break;
                                default:
                                    return target[prop];
                                    break;
                            }
                        },
                        set:function(target,prop,val,reciever){
                            if (!isNaN(prop)){
                                if(isArrayLocked(reciever)){
                                    throw 'Array is ReadOnly';
                                }
                                if (val==undefined){
                                    target.splice(prop*1,1);
                                }
                                if (prop*1==target.length){
                                    target.push(val);
                                }else{
                                    target[prop] = val;
                                }
                                return true;
                            }else{
                                target[prop]=val;
                                return true;
                            }
                        },
                        defineProperty(target,key,descriptor){
                            return Object.defineProperty(target,key,descriptor);
                        }
                    }
                );
            };");
        }
    }
}
