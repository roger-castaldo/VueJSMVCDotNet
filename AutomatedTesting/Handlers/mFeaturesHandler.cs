using AutomatedTesting.Models;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using System.Threading.Tasks;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Handlers
{
    [ModelRouteAttribute(Constants.FeaturesModelRoute)]
    [FeatureGate(RequirementType.Any, Constants.Features.Feature1, Constants.Features.Feature2)]
    internal class mFeaturesHandler : IModelHandler<mFeatures>
    {
        ValueTask<mFeatures> IModelHandler<mFeatures>.LoadAsync(string id)
        {
            return ValueTask.FromResult<mFeatures?>(null);
        }

        [ExposedMethod()]
        [FeatureGate(RequirementType.All, Constants.Features.Feature1, Constants.Features.Feature2)]
        public bool RequiresAll()
            => true;

        [ExposedMethod()]
        [FeatureGate(RequirementType.Any, true, Constants.Features.Feature3, Constants.Features.Feature4)]
        public bool RequiresAnyNegated()
            => true;

        [ExposedMethod()]
        [FeatureGate(RequirementType.All, true, Constants.Features.Feature3, Constants.Features.Feature4)]
        public bool RequiresAllNegated()
            => true;
    }
}
