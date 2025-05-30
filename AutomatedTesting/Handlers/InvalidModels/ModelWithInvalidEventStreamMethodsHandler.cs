using AutomatedTesting.Models.InvalidModels;
using Castle.Core.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Handlers.InvalidModels
{
    [ModelRouteAttribute("/models/ModelWithInvalidEventStreamMethods")]
    internal class ModelWithInvalidEventStreamMethodsHandler : IModelHandler<ModelWithInvalidEventStreamMethods>
    {
        ValueTask<ModelWithInvalidEventStreamMethods> IModelHandler<ModelWithInvalidEventStreamMethods>.LoadAsync(string id)
            => ValueTask.FromResult<ModelWithInvalidEventStreamMethods>(null);

        [EventStreamMethodAttribute()]
        public void DuplicateStaticStreamMethod(ChannelWriter<object> writer) { }

        [EventStreamMethodAttribute()]
        public void DuplicateStaticStreamMethod(ChannelWriter<object> writer, CancellationToken cancellationToken) { }

        [EventStreamMethodAttribute()]
        public void DuplicateInstanceStreamMethod([ModelIDParameter]string id, ChannelWriter<object> writer) { }

        [EventStreamMethodAttribute()]
        public void DuplicateInstanceStreamMethod([ModelIDParameter] string id, ChannelWriter<object> writer, CancellationToken cancellationToken) { }

        [EventStreamMethodAttribute()]
        public void StreamMethodWithInvalidParameter([FromServices]ILogger logger,object parameter1, ChannelWriter<object> writer, CancellationToken cancellationToken) { }
    }
}
