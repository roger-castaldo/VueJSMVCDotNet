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
            Logger.Trace("Appending Model Instance Footer for Model Definition[{0}]", new object[] { modelType.FullName });
            builder.AppendLine(string.Format(@"     App.Models.{0} = App.Models.{0}||{{}};
        App.Models.{0}.{1} = function(){{ ", modelType.Name, Constants.CREATE_INSTANCE_FUNCTION_NAME));
            builder.AppendLine(@"         if (Vue.version.indexOf('2')==0){
                return new Vue({data:function(){return data;},methods:methods,computed:computed});
            }else if (Vue.version.indexOf('3')==0){
                var ret = {
                    $on:function(event,callback){
                        if (this._$events==undefined){this._$events={};}
                        if (Array.isArray(event)){
                            for(var x=0;x<event.length;x++){
                                if (this._$events[event[x]]==undefined){this._$events[event[x]]=[];}
                                this._$events[event[x]].push(callback);
                            }
                        }else{
                            if (this._$events[event]==undefined){this._$events[event]=[];}
                            this._$events[event].push(callback);
                        }
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
                        var tmp = {};
                        for(var prop in data){
                            tmp[prop] = this[prop];
                        }
                        if (tmp.id!=undefined)
                            delete tmp.id;
                        var og = this;
                        Object.defineProperty(tmp,'id',{get:function(){return (getMap(this)==undefined ? undefined : getMap(this).id);}});
                        return {
                            data:function(){return tmp;},
                            methods:extend({$on:ret.$on,$off:ret.$off},methods),
                            computed:computed,
                            created:function(){
                                var view=this;
                                this.$on([");
                builder.AppendFormat("'{0}','{1}','{2}','parsed'",new object[]{
                    Constants.Events.MODEL_LOADED,
                    Constants.Events.MODEL_SAVED,
                    Constants.Events.MODEL_UPDATED
                });
                builder.AppendLine(@"],function(){view.$forceUpdate();});
                            }
                        };
                    }
                };
                var tmp = extend({_hashCode:null},data);
                Object.defineProperty(ret,'_hashCode',{ get:function(){return tmp._hashCode;},set:function(hash){tmp._hashCode=null;H(JSON.stringify(ret)).then(hash=>{tmp._hashCode=hash;});}});");
                foreach (PropertyInfo pi in Utility.GetModelProperties(modelType))
                {
                    if (pi.CanRead && pi.CanWrite)
                    {
                    builder.AppendLine(string.Format(@"                Object.defineProperty(ret,'{0}',{{
                    get:function(){{return tmp.{0};}},
                    set:function(value){{
                        tmp.{0}=value;
                        ret._hashCode='';
                    }},
                    enumerable: true,
                    configurable: false
                }});", pi.Name));
                    }
                }
            builder.AppendLine(@"                ret = extend(ret,methods);
                for(var prop in computed){
                    Object.defineProperty(ret,prop,computed[prop]);
                }
                setMap(ret,getMap(this));
                return ret;
            }else{
                throw 'unsupported version of VueJS found.';
            }
        };");
        }
    }
}
