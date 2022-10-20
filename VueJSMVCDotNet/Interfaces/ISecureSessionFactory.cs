using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Reddragonit.VueJSMVCDotNet.Interfaces
{
    /// <summary>
    /// An interface that is required to be defined in order to extract a secure session from a given request
    /// </summary>
    public interface ISecureSessionFactory
    {
        /// <summary>
        /// Called to produce a Secure Session from the given context
        /// </summary>
        /// <param name="context">the current context request</param>
        /// <returns>An instance of the ISecureSession extracted from the current request</returns>
        ISecureSession ProduceFromContext(HttpContext context);
    }
}
