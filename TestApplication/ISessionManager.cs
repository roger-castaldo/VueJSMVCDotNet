using VueJSMVCDotNet.Interfaces;
using System;

namespace TestApplication
{
    public interface ISessionManager : ISecureSession, ISecureSessionFactory
    {
        DateTime Start { get; }
    }
}
