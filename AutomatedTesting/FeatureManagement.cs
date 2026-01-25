using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutomatedTesting
{
    [TestClass]
    public class FeatureManagement
    {
        [TestMethod]
        [DataRow(Constants.Features.Feature1, true)]
        [DataRow(Constants.Features.Feature2, true)]
        [DataRow(null, false)]
        public async Task TestAnyRequirementType(string? enabledFeature, bool success)
        {
            //Arrange
            Dictionary<string, string?> configuration = [];
            if (!string.IsNullOrWhiteSpace(enabledFeature))
                configuration.Add($"FeatureManagement:{enabledFeature}", "true");
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, configuration: configuration);

            //Act
            var (_, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.FeaturesModelRoute}.js", webApplicationFactory);

            //Assert
            Assert.AreEqual((success ? 200 : 404), responseStatus);
        }

        [TestMethod]
        [DataRow(false, Constants.Features.Feature1)]
        [DataRow(false, Constants.Features.Feature2)]
        [DataRow(true, Constants.Features.Feature1, Constants.Features.Feature2)]
        public async Task TestAllRequirementType(bool success, params string[] enabledFeatures)
        {
            //Arrange
            Dictionary<string, string?> configuration = [];
            foreach (var feature in enabledFeatures)
                configuration.Add($"FeatureManagement:{feature}", "true");
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, configuration: configuration);

            //Act
            var (_, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.FeaturesModelRoute}/RequiresAll", webApplicationFactory);

            //Assert
            Assert.AreEqual((success ? 200 : 404), responseStatus);
        }

        [TestMethod]
        [DataRow(false, Constants.Features.Feature1, Constants.Features.Feature3)]
        [DataRow(false, Constants.Features.Feature2, Constants.Features.Feature4)]
        [DataRow(true, Constants.Features.Feature1)]
        [DataRow(true, Constants.Features.Feature2)]
        public async Task TestAnyRequirementTypeNegated(bool success, params string[] enabledFeatures)
        {
            //Arrange
            Dictionary<string, string?> configuration = [];
            foreach (var feature in enabledFeatures)
                configuration.Add($"FeatureManagement:{feature}", "true");
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, configuration: configuration);

            //Act
            var (_, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.FeaturesModelRoute}/RequiresAnyNegated", webApplicationFactory);

            //Assert
            Assert.AreEqual((success ? 200 : 404), responseStatus);
        }

        [TestMethod]
        [DataRow(false, Constants.Features.Feature1, Constants.Features.Feature3)]
        [DataRow(false, Constants.Features.Feature2, Constants.Features.Feature4)]
        [DataRow(false, Constants.Features.Feature2, Constants.Features.Feature3, Constants.Features.Feature4)]
        [DataRow(true, Constants.Features.Feature1)]
        [DataRow(true, Constants.Features.Feature2)]
        public async Task TestAllRequirementTypeNegated(bool success, params string[] enabledFeatures)
        {
            //Arrange
            Dictionary<string, string?> configuration = [];
            foreach (var feature in enabledFeatures)
                configuration.Add($"FeatureManagement:{feature}", "true");
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true, configuration: configuration);

            //Act
            var (_, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Post, $"{Constants.FeaturesModelRoute}/RequiresAllNegated", webApplicationFactory);

            //Assert
            Assert.AreEqual((success ? 200 : 404), responseStatus);
        }
    }
}
