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
        public void GeneratorJS(ref WrappedStringBuilder builder, Type modelType, string urlBase)
        {
            builder.AppendLine(@"
            const generateUUID = function() { // Public Domain/MIT
                let d = new Date().getTime();//Timestamp
                let d2 = ((typeof performance !== 'undefined') && performance.now && (performance.now()*1000)) || 0;//Time in microseconds since page-load or 0 if unsupported
                return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
                    let r = Math.random() * 16;//random number between 0 and 16
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
            let arrMap = new WeakMap();
            const getArrayMap = function(arr){
                let t = arr;
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
                let t = arr;
                if (Vue!=undefined && Vue.isProxy!=undefined && Vue.toRaw!=undefined){
                    if (Vue.isProxy(arr)){
                        t = Vue.toRaw(arr);
                    }
                }
                arrMap.set(t,arr);
            };
        
            let arrSecMap = new WeakMap();
            const unlockArray=function(arr){
                let t = arr;
                if (Vue!=undefined && Vue.isProxy!=undefined && Vue.toRaw!=undefined){
                    if (Vue.isProxy(arr)){
                        t = Vue.toRaw(arr);
                    }
                }
                if (arrSecMap.get(t)==undefined){
                    arrSecMap.set(t,[]);
                }
                let id = generateUUID();
                arrSecMap.set(t,arrSecMap.get(t).concat([id]));
                return id;
            };
            const lockArray=function(arr,id){
                let t = arr;
                if (Vue!=undefined && Vue.isProxy!=undefined && Vue.toRaw!=undefined){
                    if (Vue.isProxy(arr)){
                        t = Vue.toRaw(arr);
                    }
                }
                if (arrSecMap.get(t)!=undefined){
                    let index=arrSecMap.get(t).indexOf(id);
                    if (index>-1){
                        let tmp = arrSecMap.get(t);
                        tmp.splice(index,1);
                        arrSecMap.set(t,tmp);
                    }
                }
            };
            const isArrayLocked=function(arr){
                let t = arr;
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
                                            let ret = target.splice(start,deleteCount,item1);
                                        }else if (deleteCount!=undefined){
                                            let ret = target.splice(start,deleteCount);
                                        }else{
                                            let ret = target.splice(start);
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
