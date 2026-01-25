using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.Loader;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting
{
    [TestClass]
    public class TypeLoadingTests
    {
        [TestMethod]
        public void AssemblyAdded()
        {
            var (webApplicationFactory, _, _)= Utility.CreateApplication(true);
            var dataSource = webApplicationFactory.Services.GetRequiredService<IModelDataSource>();
            Exception error = null;
            try
            {
                dataSource.AssemblyAdded();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            Assert.IsNull(error);
        }

        [TestMethod]
        public void ReloadingAssemblyContext()
        {
            var (webApplicationFactory, _, _)= Utility.CreateApplication(true);
            Exception error = null;
            var dataSource = webApplicationFactory.Services.GetRequiredService<IModelDataSource>();
            try
            {
                dataSource.UnloadAssemblyContext(AssemblyLoadContext.Default);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            Assert.IsNull(error);
            try
            {
                dataSource.AsssemblyLoadContextAdded(AssemblyLoadContext.Default);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            Assert.IsNull(error);
            try
            {
                dataSource.UnloadAssemblyContext(AssemblyLoadContext.Default);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            Assert.IsNull(error);
            try
            {
                dataSource.AsssemblyLoadContextAdded(AssemblyLoadContext.Default.Name);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            Assert.IsNull(error);
        }
    }
}
