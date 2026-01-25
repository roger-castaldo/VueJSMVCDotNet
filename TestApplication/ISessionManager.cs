using System;
using VueJSMVCDotNet.Interfaces;

namespace TestApplication
{
    public interface ISessionManager : ISecureSession, ISecureSessionFactory
    {
        DateTime Start { get; }
    }
}
