using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.JSHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class EventClassGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, string urlBase, sModelType[] models)
        {
            builder.AppendLine(@"class EventHandler{
	#events;
	constructor(events){
		this.#events={};
		for(let i in events){
			this.#events[events[i]] = [];
		}
	}
	
	on(event,callback){
		if (this.#events[event]===undefined){throw 'undefined event';}
		this.#events[event].push(callback);
	}
	
	off(callback){
		for(let prop in this.#events){
			for(let x=0;x<this.#events[prop].length;x++){
				if (this.#events[prop][x]===callback){
					this.#events[prop].splice(x,1);
					break;
				}
			}
		}
	}
	
	trigger(event,data){
		if (this.#events[event]===undefined){throw 'undefined event';}
		for(let i in this.#events[event]){
			this.#events[event][i](data);
		}
	}
};");
        }
    }
}
