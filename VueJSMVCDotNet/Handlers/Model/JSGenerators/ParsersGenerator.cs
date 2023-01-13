using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSGenerators.Interfaces;
using static Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSHandler;

namespace Org.Reddragonit.VueJSMVCDotNet.Handlers.Model.JSGenerators
{
    internal class ParsersGenerator : IBasicJSGenerator
    {
        public void GeneratorJS(ref WrappedStringBuilder builder, string urlBase, sModelType[] models)
        {
            List<object> types = new List<object>();
            foreach (sModelType modelType in models)
            {
                builder.AppendLine(string.Format(@"     const _{0} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = new {0}();
                ret.{1}(data);
            }}
            return ret;
        }};", new object[]{
                                                 modelType.Type.Name,
                                                 Constants.PARSE_FUNCTION_NAME
            }));
                types.Add(modelType);
            }
            foreach (sModelType modelType in models)
                _RecurLocateLinkedTypes(ref types, modelType);
            foreach (sModelType modelType in models)
                types.Remove(modelType.Type);
            foreach (Type type in types)
            {
                if (type.GetCustomAttributes(typeof(ModelJSFilePath), false).Length>0)
                    builder.AppendLine(string.Format("        import {{ {0} }} from '{1}';", new object[] { type.Name, ((ModelJSFilePath)type.GetCustomAttributes(typeof(ModelJSFilePath), false)[0]).Path }));
            }
            foreach (sModelType type in types)
            {
                Logger.Trace("Appending Parser Call for Linked Type[{0}]", new object[]
                {
                    type.Type.FullName
                });
                if (type.Type.GetCustomAttributes(typeof(ModelJSFilePath), false).Length>0)
                {
                    builder.AppendLine(string.Format(@"     const _{0} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = new {0}();
                ret.{1}(data);
            }}
            return ret;
        }};", new object[]{
                        type.Type.Name,
                        Constants.PARSE_FUNCTION_NAME
                    }));
                }
                else
                {
                    builder.AppendLine(string.Format(@"     const _{0} = function(data){{
            let ret=null;
            if (data!=null){{
                ret = {{}};
                Object.defineProperty(ret,'id',{{get:function(){{return data.id}}}});", new object[] {
                    type.Type.Name,
                    Constants.PARSE_FUNCTION_NAME }));
                    foreach (PropertyInfo pi in type.Properties)
                    {
                        Type t = pi.PropertyType;
                        if (t.IsArray)
                            t = t.GetElementType();
                        else if (t.IsGenericType)
                            t = t.GetGenericArguments()[0];
                        if (new List<Type>(t.GetInterfaces()).Contains(typeof(IModel)))
                        {
                            if (Utility.IsArrayType(pi.PropertyType))
                            {
                                builder.AppendLine(string.Format(@"      if (data.{0}!=null){{
                let tmp = [];
                for(let x=0;x<data.{0}.length;x++){{", new object[] { pi.Name }));
                                if (t.GetCustomAttributes(typeof(ModelJSFilePath), false).Length>0)
                                    builder.AppendLine(string.Format(@"                 tmp.push(new {0}());
                    tmp[x].{1}(data.{2}[x]);", new object[]
                                    {
                                        t.Name,
                                        Constants.PARSE_FUNCTION_NAME,
                                        pi.Name
                                    }));
                                else
                                    builder.AppendLine(string.Format("                    tmp.push(_{0}(data.{1}[x]);", new object[] { t.Name, pi.Name }));
                                builder.AppendLine(string.Format(@"               }}
                ret.{0}=tmp;
            }}else{{
                ret.{0}=null;
            }}", new object[]{
                                pi.Name
                            }));
                            }
                            else
                            {
                                builder.AppendLine(string.Format("          if (data.{0}!=null){{", new object[] { pi.Name }));
                                if (t.GetCustomAttributes(typeof(ModelJSFilePath), false).Length>0)
                                    builder.AppendLine(string.Format(@"              ret.{0} = new {1}();
                ret.{0}.{1}(data{0});", new object[] { pi.Name, t.Name }));
                                else
                                    builder.AppendLine(string.Format("              ret.{0} = data.{0}", new object[] { pi.Name }));
                                builder.AppendLine(string.Format(@"          }}else{{
                ret.{0}=null;
            }}", new object[] { pi.Name }));
                            }
                        }
                        else
                            builder.AppendLine(string.Format("          ret.{0} = data.{0};", pi.Name));
                        builder.AppendLine(@"           }
            }
            return ret;
        };");
                    }
                }
            }
        }

        private void _RecurLocateLinkedTypes(ref List<object> types,sModelType modelType)
        {
            if (!types.Contains(modelType))
            {
                types.Add(modelType);
                foreach (sModelType linked in modelType.LinkedTypes)
                    _RecurLocateLinkedTypes(ref types, linked);
            }
        }
    }
}
