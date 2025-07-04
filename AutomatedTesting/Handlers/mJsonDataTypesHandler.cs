using AutomatedTesting.Models;
using System;
using System.Net;
using System.Threading.Tasks;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Handlers
{
    [ModelRoute(Constants.JsonDataTypesModelRoute)]
    public class mJsonDataTypesHandler : IModelHandler<mJsonDataTypes>
    {
        public ValueTask<mJsonDataTypes> LoadAsync(string id)
            => ValueTask.FromResult<mJsonDataTypes>(null);

        [ExposedMethod]
        public IPAddress CheckIPAddress(IPAddress input)
            => input;

        [ExposedMethod]
        public Guid CheckGuid(Guid input)
            => input;

        [ExposedMethod]
        public Decimal CheckDecimal(Decimal input)
            => input;

        [ExposedMethod]
        public string CheckModel(mPerson person)
            => person.id;

        [ExposedMethod]
        public async ValueTask<mPerson?> CheckNullModelAsync(string id)
            => await ((IModelHandler<mPerson>)new mPersonHandler(new DataStore())).LoadAsync(id);

        [ExposedMethod()]
        public async ValueTask<bool> CheckExceptionAsync(bool input)
            => (input ? throw new ArgumentException(nameof(input)) : false);
    }
}
