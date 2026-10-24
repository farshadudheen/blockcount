using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace BlockCount;

public sealed class BlockCountComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddTransient<BlockCountContentValidator>();
        builder.AddNotificationAsyncHandler<ContentPublishingNotification, BlockCountPublishingHandler>();
    }
}
