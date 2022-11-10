using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet.JSGenerators
{
    internal class DefaultMethodsGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, string urlBase, Type[] models)
        {
            builder.AppendLine(@"const ModelMethods={
    reload:function(url,id,isNew){
        return new Promise((resolve,reject)=>{
            if (isNew){
                reject('Cannot reload unsaved model.');
            }else{
                ajax({
                    url:url+'/'+id,
                    method:'GET'
                }).then(
                    response=>{
                        if (response.ok){
                            let data = response.json();
                            if (data==null){
                                reject(null);
                            }else{
                                resolve(data);
                            }
                        }else{
                            reject(response.text());
                        }
                    },
                    response=>{ reject(response.text()); }
                );
            }
        });
    },
    destroy:function(url,id,isNew){
        return new Promise((resolve,reject)=>{
            if (isNew){
                reject('Cannot delete unsaved model.');
            }else{
                ajax(
                {
                    url: url+'/'+id,
                    method:'"+ModelRequestHandler.RequestMethods.DELETE+@"'
                }).then(
                    response=>{
                        if (response.ok){                 
                            let data = response.json();
                            if (data){
                                resolve();
                            }else{
                                reject();
                            }
                        }else{
                            reject(response.text());
                        }
                    },
                    response=>{reject(response.text());}
                );    
            }
        });
    },
    update:function(url,id,isNew,isValid,data,useJSON){
        return new Promise((resolve,reject)=>{
            if (!isValid){
                reject('Invalid model.');
            }else if (isNew){
                reject('Cannot update unsaved model, please call save instead.');
            }else{
                if (JSON.stringify(data)===JSON.stringify({})){
                    resolve(data);
                }else{
                    ajax(
                    {
                        url:url+'/'+id,
                        method:'"+ModelRequestHandler.RequestMethods.PATCH+@"',
                        useJSON:useJSON,
                        data:data
                    }).then(response=>{
                        if (response.ok){                 
                            let data = response.json();
                            if (data){
                                resolve();
                            }else{
                                reject();
                            }
                        }else{
                            reject(response.text());
                        }
                    },response=>{reject(response.text());});
                }
            }
        });
    },
    save:function(url,isNew,isValid,data,useJSON){
        return new Promise((resolve,reject)=>{
            if (!isValid){
                reject('Invalid model.');
            }else if (!isNew){
                reject('Cannot save a saved model, please call update instead.');
            }else{
                ajax(
                {
                    url:url,
                    method:'"+ModelRequestHandler.RequestMethods.PUT+@"',
                    useJSON:useJSON,
                    data:data
                }).then(response=>{
                    if (response.ok){                 
                        resolve(response.json());
                    }else{
                        reject(response.text());
                    }
                },
                response=>{reject(response.text());});    
            }
        });
    }
};");
        }
    }
}
