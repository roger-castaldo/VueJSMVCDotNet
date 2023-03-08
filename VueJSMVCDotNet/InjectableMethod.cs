using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal class InjectableMethod
    {
        private readonly MethodInfo _method;

        public MethodInfo Method => _method;
        public string Name => _method.Name;
        public bool IsModelUpdateOrSave => _method.GetCustomAttributes().Any(att => att is ModelUpdateMethod || att is ModelSaveMethod);
        public bool IsSlow =>_method.GetCustomAttributes().OfType<ExposedMethod>().Any(em => em.IsSlow);
        public Type ReturnType => _method.ReturnType;

        private readonly int _secureSessionIndex;
        private readonly int _addItemIndex;
        private readonly int _loggerIndex;
        private readonly ParameterInfo[] _parameters;
        private readonly IEnumerable<ASecurityCheck> _securityChecks;
        private readonly NotNullArguement _notNullArguement;
        public NotNullArguement NotNullArguement => _notNullArguement;

        private readonly ParameterInfo[] _strippedParameters;
        public ParameterInfo[] StrippedParameters=> _strippedParameters;

        public InjectableMethod(MethodInfo method)
        {
            _method = method;
            _notNullArguement = (NotNullArguement)method.GetCustomAttribute(typeof(NotNullArguement));
            _parameters = _method.GetParameters();
            List<ParameterInfo> strippedPars = new List<ParameterInfo>();
            _secureSessionIndex=-1;
            _addItemIndex=-1;
            _loggerIndex=-1;
            for(int x = 0; x<_parameters.Length; x++)
            {
                if (Utility.IsISecureSessionType(_parameters[x].ParameterType))
                    _secureSessionIndex=x;
                else if (_parameters[x].ParameterType==typeof(AddItem))
                    _addItemIndex=x;
                else if (_parameters[x].ParameterType==typeof(ILog))
                    _loggerIndex=x;
                if (_secureSessionIndex!=x && _addItemIndex!=x && _loggerIndex!=x)
                    strippedPars.Add(_parameters[x]);
            }
            _strippedParameters= strippedPars.ToArray();
            _securityChecks = method.GetType().GetCustomAttributes().OfType<ASecurityCheck>()
                .Concat(method.GetCustomAttributes().OfType<ASecurityCheck>());
        }

        public bool HasValidAccess(IRequestData data, IModel model, string url, string id)
        {
            return !_securityChecks.Any(sc => !sc.HasValidAccess(data, model, url, id));
        }

        public object Invoke(object obj, object[] pars = null, ISecureSession session = null, AddItem addItem = null)
        {
            object[] mpars = new object[_parameters.Length];
            if (_secureSessionIndex!=-1)
                mpars[_secureSessionIndex] = session;
            if (_addItemIndex!=-1)
                mpars[_addItemIndex] = addItem;
            if(_loggerIndex!=-1)
                mpars[_loggerIndex] = Logger.Instance;
            int index = 0;
            for (int x = 0; x<mpars.Length; x++)
            {
                if (x!=_secureSessionIndex&&x!=_addItemIndex&&x!=_loggerIndex)
                {
                    mpars[x]=pars[index];
                    index++;
                }
            }
            object ret = _method.Invoke(obj, mpars);
            if (_parameters.Any(p => p.IsOut))
            {
                index = 0;
                for (int x = 0; x<_parameters.Length; x++)
                {
                    if (x!=_secureSessionIndex&&x!=_addItemIndex&&x!=_loggerIndex)
                    {
                        if (_parameters[x].IsOut)
                            pars[index]=mpars[x];
                        index++;
                    }
                }
            }
            return ret;
        }
    }
}
