using Microsoft.AspNetCore.Http;

namespace VueJSMVCDotNet.Interfaces
{
    internal interface IRequestHandler : IDisposable
    {
        IHandlesRequestResponse HandlesRequest(HttpContext context);
    }
}
