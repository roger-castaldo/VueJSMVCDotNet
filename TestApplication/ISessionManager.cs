﻿using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestApplication
{
    public interface ISessionManager : ISecureSession
    {
        DateTime Start { get; }
    }
}
