using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace BlockCount;

public sealed class BlockCountPublishingHandler : INotificationAsyncHandler<ContentPublishingNotification>
{
    private readonly BlockCountContentValidator _validator;

    public BlockCountPublishingHandler(BlockCountContentValidator validator)
    {
        _validator = validator;
    }

    public async Task HandleAsync(ContentPublishingNotification notification, CancellationToken cancellationToken)
    {
        foreach (var content in notification.PublishedEntities)
        {
            var messages = await _validator.ValidateAsync(
                content,
                culture => notification.IsPublishingCulture(content, culture),
                cancellationToken).ConfigureAwait(false);

            foreach (var message in messages)
            {
                notification.CancelOperation(new EventMessage("Block count", message, EventMessageType.Error));
            }
        }
    }
}
