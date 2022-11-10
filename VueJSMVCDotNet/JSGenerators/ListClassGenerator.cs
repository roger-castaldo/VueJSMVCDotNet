using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class ListClassGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, string urlBase, Type[] models)
        {
            builder.AppendLine(@"class ModelList{
	#events;
	#data;
	#isPaged;
	#totalPages=undefined;
	#constructModel;
	#constructURL;
	#params=undefined;
	#setParameters=undefined;
	#currentIndex=undefined;
	#currentPageSize=undefined;

	get #currentPage(){
		return (!this.#isPaged ? undefined : Math.floor(this.#currentIndex/this.#currentPageSize));
	};


	#moveToPage(pageNumber){
		if (pageNumber>=this.#totalPages || pageNumber<0){
			throw 'Unable to move to Page that is outside the page range.';
		}else{
			this.#currentIndex = pageNumber*this.#currentPageSize;
			return this.#reload();
		}
	};

	#moveToNextPage(){
		if((this.#currentPage+1)<this.#totalPages){
			return this.#moveToPage(this.#currentPage+1);
		}else{
			throw 'Unable to move to next Page as that will excess current total pages.';
		}
	};

	#moveToPreviousPage(){
		if((this.#currentPage)>=0){
			return this.moveToPage(this.#currentPage-1);
		}else{
			throw 'Unable to move to previous Page as that will be before the first page.';
		}
	};

	#changePageSize(size){
		this.#currentPageSize = size;
		return this.#reload();
	};

	constructor(constructModel,constructURL,isPaged,setParameters,currentParams,currentIndex,currentPageSize){");
			builder.AppendLine(string.Format("		this.#events = new EventHandler(['{0}','{1}','{2}','{3}']);",new object[]{
				Constants.Events.LIST_MODEL_LOADED,
				Constants.Events.LIST_MODEL_DESTROYED,
				Constants.Events.LIST_MODEL_UPDATED,
				Constants.Events.LIST_LOADED	
			}));
			builder.AppendLine(@"		this.#constructModel=constructModel;
		this.#constructURL = constructURL;
		this.#isPaged=isPaged;
		this.#setParameters=setParameters;
		this.#params=currentParams;
		this.#data = reactive([]);
		if (isPaged){
			this.#currentIndex=(currentIndex===undefined ? 0 : currentIndex);
			this.#currentPageSize=(currentPageSize===undefined ? 10 : currentPageSize);
		}
		return this.#toProxy();
	};

	#toProxy(){
		let me = this;
		return new Proxy(this,{
			get(target,prop,reciever){
				let ret=undefined;
				switch(prop){
					case 'concat':
					case 'copyWithin':
					case 'fill':
					case 'pop':
					case 'push':
					case 'reverse':
					case 'shift':
					case 'sort':
					case 'splice':
					case 'unshift':
						throw 'Arrray is readonly';
						break;
					case 'length':
						ret = me.#data.length;
						break;
					case 'totalPages': 
						ret = (me.#isPaged ? me.#totalPages : undefined); 
						break;
					case 'currentPageSize':
						ret = (me.#isPaged ? me.#currentPageSize : undefined); 
						break;
					case 'currentPage':
						ret = (me.#isPaged ? me.#currentPage : undefined); 
						break;
					case 'moveToPage':
						ret = function(number){ return me.#moveToPage(number); };
						break;
					case 'moveToNextPage':
						ret = function(){ return me.#moveToNextPage(); };
						break;
					case 'moveToPreviousPage':
						ret = function(){ return me.#moveToPreviousPage(); };
						break;
					case 'changePageSize':
						ret = function(size){ return me.#changePageSize(size); };
						break;
					case '__proto__':
						ret = Array.__proto__;
						break;
					case 'reload':
						ret = function(){ return me.#reload(); };
						break;
					case 'currentParameters':
						ret = new Proxy(me.#params,{
							get(target,prop,reciever){ return target[prop]; },
							set(target,prop,value){throw 'The values are readonly'; }
						});
						break;
					case 'changeParameters':
						ret = function(){ 
							me.#setParameters.apply(me.#params,arguments);
							return me.#reload();
						};
						break;
					case 'toVueComposition':
						ret = function(){
							return me.#toVueComposition();
						};
						break;
					case '$on': ret = function(event,callback) { me.#events.on(event,callback); }; break;
                	case '$off': ret =  function(callback) { me.#events.off(callback); }; break;
					default:
						if (!isNaN(prop))
							ret = me.#data[prop];
						else if (me.#data[prop]!=undefined)
							ret = function(){ return me.#data[prop].apply(me.#data,arguments); };
						break;
				}
				return ret;
			},
			set(target,prop,value){
				throw 'Arrray is readonly';
			},
			ownKeys(target){return ['length','$on','$off','reload','toVueComposition'].concat((me.#isPaged ? ['totalPages','currentPageSize','currentPage','moveToPage','moveToNextPage','moveToPreviousPage','changePageSize'] : []).concat((me.#setParameters!==undefined ? ['currentParameters','changeParameters'] : [])));}
		});
	};");
			_appendReload(ref builder);
			_appendToVueComposition(ref builder);
			builder.AppendLine("};");
        }

        private void _appendReload(ref WrappedStringBuilder builder){
			builder.AppendLine(@"		#reload(){
			let tmp = this;
			return new Promise((resolve,reject)=>{
				ajax({
					url:tmp.#constructURL(tmp.#params,tmp.#currentIndex,tmp.#currentPageSize),
					method:'GET',
					credentials:'include'
				}).then(
					response=>{
						if (response.ok){
							let data = response.json();
							if (data===null){
								tmp.#totalPages = 0;
								Array.prototype.splice.apply(tmp.#data,[0,tmp.#data.length]);
							}else{
								if (data.TotalPages!==undefined){
									tmp.#totalPages = data.TotalPages;
									data=data.response;
								}
								for(let i in data){
									let mtmp = tmp.#constructModel();");
			builder.AppendLine(string.Format(@"									mtmp.{0}(data[i]);
									data[i]=mtmp;",new object[]{Constants.PARSE_FUNCTION_NAME}));
			//append delete trigger
			builder.AppendLine(string.Format(@"data[i].$on('{0}',function(model){{
                                        for(let x=0;x<tmp.#data.length;x++){{
                                            if (tmp.#data[x].id===model.id){{
                                                tmp.#events.trigger('{1}',model);
                                                Array.prototype.splice.apply(tmp.#data,[x,1]);
                                                break;
                                            }}
                                        }}
                                    }});",new object[]{
										Constants.Events.MODEL_DESTROYED,
										Constants.Events.LIST_MODEL_DESTROYED
									}));
									//append update trigger
			builder.AppendLine(string.Format(@"data[i].$on('{0}',function(model){{
                                        for(let x=0;x<tmp.#data.length;x++){{
                                            if (tmp.#data[x].id===model.id){{
												tmp.#events.trigger('{1}',model);
                                                Array.prototype.splice.apply(tmp.#data,[x,0,model]);
                                                Array.prototype.splice.apply(tmp.#data,[x+1,1]);
                                                break;
                                            }}
                                        }}
                                    }});",new object[]{
										Constants.Events.MODEL_UPDATED,
										Constants.Events.LIST_MODEL_UPDATED
									}));
			//append reload trigger
			builder.AppendLine(string.Format(@"data[i].$on('{0}',function(model){{
                                        for(let x=0;x<tmp.#data.length;x++){{
                                            if (tmp.#data[x].id===model.id){{
                                                tmp.#events.trigger('{1}',model);
                                                Array.prototype.splice.apply(tmp.#data,[x,0,model]);
                                                Array.prototype.splice.apply(tmp.#data,[x+1,1]);
                                                break;
                                            }}
                                        }}
                                    }});",new object[]{
				Constants.Events.MODEL_LOADED,
				Constants.Events.LIST_MODEL_LOADED
			}));
			builder.AppendLine(@"Array.prototype.push.apply(tmp.#data,data);
                                if (tmp.#data.length-data.length>0){{
                                    Array.prototype.splice.apply(tmp.#data,[0,tmp.#data.length-data.length]);
                                }}
						}
							}
							let proxy = tmp.#toProxy();
							tmp.#events.trigger('"+Constants.Events.LIST_LOADED+@"',proxy);
							resolve(proxy);
						}else{
							reject(response.text());
						}
					},
					rejected=>{
						reject(rejected);
					}
				);
			});
		}");
		}

		private void _appendToVueComposition(ref WrappedStringBuilder builder)
        {
            builder.AppendLine(@"		#toVueComposition(){
		let me = this;
		let ret = {
			Items:readonly(me.#data),
			reload:function(){ return me.#reload(); },
			getEditableItem:function(index){ return me.#data[index]; }
		};
		if (this.#isPaged){
			ret = extend(ret,{
				currentIndex:readonly(me.#currentIndex),
				currentPage:readonly(me.#currentPage),
				currentPageSize:readonly(me.#currentPageSize),
				totalPages:readonly(me.#totalPages),
				moveToPage:function(pageNumber){return me.#moveToPage(pageNumber);},
				moveToNextPage:function(){return me.#moveToNextPage();},
				moveToPreviousPage:function(){return me.#moveToPreviousPage();},
				changePageSize:function(size){return me.#changePageSize(size);}
			});
		}
		if (this.#setParameters!==undefined){
			ret = extend(ret,{
				currentParameters:readonly(me.#params),
				changeParameters:function(){
					me.#setParameters.apply(me.#params,arguments);
					return me.#reload();
				}
			});
		}
		return ret;
	};");
        }
    }
}
