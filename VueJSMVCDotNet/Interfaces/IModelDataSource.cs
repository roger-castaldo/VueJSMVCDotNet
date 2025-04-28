using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VueJSMVCDotNet.Interfaces
{
    public interface IModelDataSource
    {
        void AssemblyAdded();
        void AsssemblyLoadContextAdded(string? contextName);
        void AsssemblyLoadContextAdded(AssemblyLoadContext alc, bool triggerChange = true);
        void UnloadAssemblyContext(string? contextName);
        void UnloadAssemblyContext(AssemblyLoadContext alc);

    }
}
