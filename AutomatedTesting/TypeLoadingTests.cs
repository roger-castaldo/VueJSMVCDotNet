using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
#if NET
using System.Runtime.Loader;
#endif

namespace AutomatedTesting
{
    [TestClass]
    public class TypeLoadingTests
    {
        [TestMethod]
        public void AssemblyAdded()
        {
            var middleware = Utility.CreateMiddleware(true);
            Exception error=null;
            try
            {
                middleware.Options.AssemblyAdded();
            }catch(Exception ex)
            {
                error = ex;
            }
            Assert.IsNull(error);
        }

#if NET
        [TestMethod]
        public void ReloadingAssemblyContext(){
            var middleware = Utility.CreateMiddleware(true);
            Exception error=null;
            try
            {
                middleware.Options.UnloadAssemblyContext(AssemblyLoadContext.Default);
            }catch(Exception ex)
            {
                error = ex;
            }
            Assert.IsNull(error);
            try
            {
                middleware.Options.AsssemblyLoadContextAdded(AssemblyLoadContext.Default);
            }catch(Exception ex)
            {
                error = ex;
            }
            Assert.IsNull(error);
        }

#endif
    }
}
