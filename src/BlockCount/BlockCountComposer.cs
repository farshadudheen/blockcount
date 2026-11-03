using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;

namespace BlockCount;

public sealed class BlockCountComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.DataEditors()
            .Exclude<BlockListPropertyEditor>()
            .Exclude<BlockGridPropertyEditor>();
        builder.Services.AddTransient<BlockCountContentValidator>();
        builder.AddNotificationAsyncHandler<ContentPublishingNotification, BlockCountPublishingHandler>();
    }
}
