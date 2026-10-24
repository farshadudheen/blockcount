using System.Text.Json.Nodes;
using Moq;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace BlockCount.Tests;

public class BlockCountValidatorTests
{
    private static readonly Guid Image = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Card = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ParentDataType = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid NestedDataType = Guid.Parse("66666666-6666-6666-6666-666666666666");

    [Test]
    public async Task Variant_properties_are_checked_only_for_cultures_being_published()
    {
        var propertyType = PropertyType("blocks", "Blocks", BlockEditors.BlockList, ParentDataType, varies: true);
        var property = new Mock<IProperty>();
        property.SetupGet(item => item.PropertyType).Returns(propertyType);
        property.Setup(item => item.GetValue(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .Returns((string? culture, string? _, bool _) => culture == "da-DK" ? BlockList(Image, 4) : BlockList(Image, 1));

        var content = new Mock<IContent>();
        content.SetupGet(item => item.Properties).Returns(new PropertyCollection([property.Object]));
        content.SetupGet(item => item.AvailableCultures).Returns(["en-US", "da-DK"]);

        var validator = Validator(
            DataTypes((ParentDataType, Blocks(Rule(Image, max: 1)))),
            ContentTypes((Image, "Image Block", [])));

        var publishingEnglish = await validator.ValidateAsync(content.Object, culture => culture == "en-US");
        var publishingDanish = await validator.ValidateAsync(content.Object, culture => culture == "da-DK");

        Assert.That(publishingEnglish, Is.Empty);
        Assert.That(publishingDanish.Single(), Does.Contain("Blocks (da-DK)").And.Contain(BlockCountMessages.AboveMaximum("Image Block", 1, 4)));
    }

    [Test]
    public async Task Invariant_properties_are_always_checked()
    {
        var propertyType = PropertyType("blocks", "Blocks", BlockEditors.BlockList, ParentDataType, varies: false);
        var property = new Mock<IProperty>();
        property.SetupGet(item => item.PropertyType).Returns(propertyType);
        property.Setup(item => item.GetValue(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>())).Returns(BlockList(Image, 0));

        var content = new Mock<IContent>();
        content.SetupGet(item => item.Properties).Returns(new PropertyCollection([property.Object]));
        content.SetupGet(item => item.AvailableCultures).Returns(["en-US"]);

        var validator = Validator(
            DataTypes((ParentDataType, Blocks(Rule(Image, min: 1)))),
            ContentTypes((Image, "Image Block", [])));

        var messages = await validator.ValidateAsync(content.Object, _ => false);

        Assert.That(messages.Single(), Does.Contain(BlockCountMessages.BelowMinimum("Image Block", 1, 0)));
    }

    [Test]
    public async Task Nested_block_properties_use_their_own_data_type_rules()
    {
        var slides = PropertyType("slides", "Slides", BlockEditors.BlockList, NestedDataType, varies: false);
        var parentType = PropertyType("blocks", "Blocks", BlockEditors.BlockList, ParentDataType, varies: false);
        var property = new Mock<IProperty>();
        property.SetupGet(item => item.PropertyType).Returns(parentType);
        property.Setup(item => item.GetValue(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .Returns(ParentWithNestedSlides(Image, 3));

        var content = new Mock<IContent>();
        content.SetupGet(item => item.Properties).Returns(new PropertyCollection([property.Object]));
        content.SetupGet(item => item.AvailableCultures).Returns(Array.Empty<string>());

        var validator = Validator(
            DataTypes(
                (ParentDataType, Blocks(Rule(Image, max: 1), Rule(Card, 1, 1))),
                (NestedDataType, Blocks(Rule(Image, max: 1)))),
            ContentTypes(
                (Image, "Image Block", []),
                (Card, "Card", [slides])));

        var messages = await validator.ValidateAsync(content.Object, _ => true);

        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0], Does.Contain("Blocks / Card / Slides"));
        Assert.That(messages[0], Does.Contain(BlockCountMessages.AboveMaximum("Image Block", 1, 3)));
    }

    [Test]
    public async Task Publishing_handler_cancels_when_counts_are_invalid()
    {
        var propertyType = PropertyType("blocks", "Blocks", BlockEditors.BlockGrid, ParentDataType, varies: false);
        var property = new Mock<IProperty>();
        property.SetupGet(item => item.PropertyType).Returns(propertyType);
        property.Setup(item => item.GetValue(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>())).Returns(BlockList(Image, 2).Replace(BlockEditors.BlockList, BlockEditors.BlockGrid, StringComparison.Ordinal));

        var content = new Mock<IContent>();
        content.SetupGet(item => item.Properties).Returns(new PropertyCollection([property.Object]));
        content.SetupGet(item => item.AvailableCultures).Returns(Array.Empty<string>());

        var validator = Validator(
            DataTypes((ParentDataType, Blocks(Rule(Image, 1, 1)))),
            ContentTypes((Image, "Image Block", [])));
        var handler = new BlockCountPublishingHandler(validator);
        var eventMessages = new EventMessages();
        var notification = new ContentPublishingNotification([content.Object], eventMessages);

        await handler.HandleAsync(notification, CancellationToken.None);

        Assert.That(notification.Cancel, Is.True);
        Assert.That(eventMessages.GetAll().Select(message => message.Message), Has.Some.Contains(BlockCountMessages.Exact("Image Block", 1, 2)));
    }

    [Test]
    public void Nested_culture_filter_keeps_invariant_values_and_the_culture_being_published()
    {
        Assert.That(BlockCountContentValidator.IncludeNested(null, "en-US", nestedVaries: false, _ => false), Is.True);
        Assert.That(BlockCountContentValidator.IncludeNested("en-US", "en-US", nestedVaries: false, _ => true), Is.False);
        Assert.That(BlockCountContentValidator.IncludeNested("da-DK", null, nestedVaries: true, culture => culture == "en-US"), Is.False);
        Assert.That(BlockCountContentValidator.IncludeNested("en-US", null, nestedVaries: true, culture => culture == "en-US"), Is.True);
        Assert.That(BlockCountContentValidator.IncludeNested(null, "en-US", nestedVaries: true, _ => false), Is.True);
    }

    private static BlockCountContentValidator Validator(
        IReadOnlyDictionary<Guid, IDataType> dataTypes,
        IReadOnlyDictionary<Guid, IContentType> contentTypes)
    {
        var dataTypeService = new Mock<IDataTypeService>();
        dataTypeService.Setup(service => service.GetAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid key) => dataTypes.GetValueOrDefault(key));

        var contentTypeService = new Mock<IContentTypeService>();
        contentTypeService.Setup(service => service.GetAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid key) => contentTypes.GetValueOrDefault(key));

        return new BlockCountContentValidator(dataTypeService.Object, contentTypeService.Object);
    }

    private static Dictionary<Guid, IDataType> DataTypes(params (Guid Key, JsonArray Blocks)[] items)
    {
        var map = new Dictionary<Guid, IDataType>();
        foreach (var (key, blocks) in items)
        {
            var dataType = new Mock<IDataType>();
            dataType.SetupGet(item => item.ConfigurationData).Returns(new Dictionary<string, object> { ["blocks"] = blocks });
            map[key] = dataType.Object;
        }

        return map;
    }

    private static Dictionary<Guid, IContentType> ContentTypes(params (Guid Key, string Name, IPropertyType[] Properties)[] items)
    {
        var map = new Dictionary<Guid, IContentType>();
        foreach (var (key, name, properties) in items)
        {
            var contentType = new Mock<IContentType>();
            contentType.SetupGet(item => item.Name).Returns(name);
            contentType.SetupGet(item => item.CompositionPropertyTypes).Returns(properties);
            map[key] = contentType.Object;
        }

        return map;
    }

    private static IPropertyType PropertyType(string alias, string name, string editorAlias, Guid dataTypeKey, bool varies)
    {
        var propertyType = new Mock<IPropertyType>();
        propertyType.SetupGet(item => item.Alias).Returns(alias);
        propertyType.SetupGet(item => item.Name).Returns(name);
        propertyType.SetupGet(item => item.PropertyEditorAlias).Returns(editorAlias);
        propertyType.SetupGet(item => item.DataTypeKey).Returns(dataTypeKey);
        propertyType.SetupGet(item => item.Variations).Returns(varies ? ContentVariation.Culture : ContentVariation.Nothing);
        return propertyType.Object;
    }

    private static JsonArray Blocks(params BlockCountRule[] rules)
    {
        var blocks = new JsonArray();
        foreach (var rule in rules)
        {
            blocks.Add(new JsonObject
            {
                ["contentElementTypeKey"] = rule.ContentElementTypeKey.ToString(),
                ["blockCount"] = new JsonObject
                {
                    ["min"] = rule.Min,
                    ["max"] = rule.Max,
                },
            });
        }

        return blocks;
    }

    private static BlockCountRule Rule(Guid key, int? min = null, int? max = null) =>
        new() { ContentElementTypeKey = key, Min = min, Max = max };

    private static string BlockList(Guid type, int count)
    {
        var contentData = new JsonArray();
        var layout = new JsonArray();
        for (var i = 0; i < count; i++)
        {
            var key = Guid.NewGuid();
            contentData.Add(new JsonObject
            {
                ["contentTypeKey"] = type.ToString(),
                ["key"] = key.ToString(),
                ["values"] = new JsonArray(),
            });
            layout.Add(new JsonObject { ["contentKey"] = key.ToString() });
        }

        return new JsonObject
        {
            ["contentData"] = contentData,
            ["settingsData"] = new JsonArray(),
            ["layout"] = new JsonObject { [BlockEditors.BlockList] = layout },
        }.ToJsonString();
    }

    private static string ParentWithNestedSlides(Guid nestedType, int nestedCount)
    {
        var cardKey = Guid.NewGuid();
        var nested = JsonNode.Parse(BlockList(nestedType, nestedCount))!.AsObject();
        return new JsonObject
        {
            ["contentData"] = new JsonArray
            {
                new JsonObject
                {
                    ["contentTypeKey"] = Card.ToString(),
                    ["key"] = cardKey.ToString(),
                    ["values"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["alias"] = "slides",
                            ["editorAlias"] = BlockEditors.BlockList,
                            ["culture"] = null,
                            ["value"] = nested,
                        },
                    },
                },
            },
            ["settingsData"] = new JsonArray(),
            ["layout"] = new JsonObject
            {
                [BlockEditors.BlockList] = new JsonArray
                {
                    new JsonObject { ["contentKey"] = cardKey.ToString() },
                },
            },
        }.ToJsonString();
    }
}
