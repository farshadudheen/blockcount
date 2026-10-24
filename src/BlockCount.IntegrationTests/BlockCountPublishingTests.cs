using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace BlockCount.IntegrationTests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerFixture)]
public class BlockCountPublishingTests : UmbracoIntegrationTest
{
    private IContentService ContentService => GetRequiredService<IContentService>();

    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private IDataTypeService DataTypeService => GetRequiredService<IDataTypeService>();

    private IContentEditingService EditingService => GetRequiredService<IContentEditingService>();

    private ILanguageService LanguageService => GetRequiredService<ILanguageService>();

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        builder.Services.AddTransient<BlockCountContentValidator>();
        builder.AddNotificationAsyncHandler<ContentPublishingNotification, BlockCountPublishingHandler>();
    }

    [Test]
    public async Task Block_list_publish_rejects_invalid_counts_and_accepts_valid_counts()
    {
        var image = await CreateElementAsync("imageBlock");
        var rte = await CreateElementAsync("rteBlock");
        var dataType = await CreateBlockDataTypeAsync(BlockEditors.BlockList, "Umb.PropertyEditorUi.BlockList",
        [
            Rule(image, 1, 1),
            Rule(rte, 0, 3),
        ]);
        var reloaded = await DataTypeService.GetAsync(dataType.Key);
        var rules = BlockCountConfigurationReader.Read(reloaded!.ConfigurationData);

        Assert.That(rules.Select(rule => (rule.ContentElementTypeKey, rule.Min, rule.Max)), Is.EquivalentTo(new[]
        {
            (image.Key, (int?)1, (int?)1),
            (rte.Key, (int?)0, (int?)3),
        }));

        var page = await CreatePageAsync("listPage", dataType);
        var invalid = ContentService.Create("Invalid list", Constants.System.Root, page.Alias);
        invalid.SetValue("blocks", BlockList((image.Key, 0), (rte.Key, 4)));
        ContentService.Save(invalid, Constants.Security.SuperUserId, null);

        var rejected = ContentService.Publish(invalid, [], Constants.Security.SuperUserId);
        Assert.That(rejected.Success, Is.False, Describe(rejected));
        Assert.That(rejected.Result, Is.EqualTo(PublishResultType.FailedPublishCancelledByEvent));
        Assert.That(Describe(rejected), Does.Contain("exactly 1").And.Contain("at most 3"));

        var valid = ContentService.Create("Valid list", Constants.System.Root, page.Alias);
        valid.SetValue("blocks", BlockList((image.Key, 1), (rte.Key, 3)));
        ContentService.Save(valid, Constants.Security.SuperUserId, null);
        var published = ContentService.Publish(valid, [], Constants.Security.SuperUserId);
        Assert.That(published.Success, Is.True, Describe(published));
    }

    [Test]
    public async Task Block_grid_areas_share_one_quota()
    {
        var image = await CreateElementAsync("gridImage");
        var dataType = await CreateBlockDataTypeAsync(BlockEditors.BlockGrid, "Umb.PropertyEditorUi.BlockGrid", [Rule(image, max: 1)]);
        var page = await CreatePageAsync("gridPage", dataType);
        var content = ContentService.Create("Grid", Constants.System.Root, page.Alias);
        content.SetValue("blocks", BlockGrid(image.Key, image.Key));
        ContentService.Save(content, Constants.Security.SuperUserId, null);

        var rejected = ContentService.Publish(content, [], Constants.Security.SuperUserId);
        Assert.That(rejected.Result, Is.EqualTo(PublishResultType.FailedPublishCancelledByEvent), Describe(rejected));
        Assert.That(Describe(rejected), Does.Contain("at most 1"));

        var valid = ContentService.Create("Valid grid", Constants.System.Root, page.Alias);
        valid.SetValue("blocks", BlockGrid(image.Key));
        ContentService.Save(valid, Constants.Security.SuperUserId, null);
        var published = ContentService.Publish(valid, [], Constants.Security.SuperUserId);
        Assert.That(published.Success, Is.True, Describe(published));
    }

    [Test]
    public async Task Nested_block_properties_are_validated_separately()
    {
        var image = await CreateElementAsync("nestedImage");
        var slides = await CreateBlockDataTypeAsync(BlockEditors.BlockList, "Umb.PropertyEditorUi.BlockList", [Rule(image, max: 1)]);
        var card = await CreateElementAsync("card", ("slides", "Slides", slides));
        var cards = await CreateBlockDataTypeAsync(BlockEditors.BlockList, "Umb.PropertyEditorUi.BlockList", [Rule(card, 1, 1), Rule(image, max: 1)]);
        var page = await CreatePageAsync("nestedPage", cards);
        var content = ContentService.Create("Nested", Constants.System.Root, page.Alias);
        content.SetValue("blocks", NestedCards(card.Key, image.Key, 3));
        ContentService.Save(content, Constants.Security.SuperUserId, null);

        var rejected = ContentService.Publish(content, [], Constants.Security.SuperUserId);
        var description = Describe(rejected);
        Assert.That(rejected.Result, Is.EqualTo(PublishResultType.FailedPublishCancelledByEvent), description);
        Assert.That(description, Does.Contain("Blocks / Card / Slides"));
        Assert.That(description, Does.Not.Contain("Blocks:"));

        var valid = ContentService.Create("Valid nested", Constants.System.Root, page.Alias);
        valid.SetValue("blocks", NestedCards(card.Key, image.Key, 1));
        ContentService.Save(valid, Constants.Security.SuperUserId, null);
        var published = ContentService.Publish(valid, [], Constants.Security.SuperUserId);
        Assert.That(published.Success, Is.True, Describe(published));
    }

    [Test]
    public async Task Only_cultures_being_published_are_checked()
    {
        await EnsureLanguageAsync("en-US", "English (United States)");
        await EnsureLanguageAsync("da-DK", "Danish (Denmark)");
        var image = await CreateElementAsync("cultureImage");
        var dataType = await CreateBlockDataTypeAsync(BlockEditors.BlockList, "Umb.PropertyEditorUi.BlockList", [Rule(image, max: 1)]);
        var page = await CreatePageAsync("culturePage", dataType, variesByCulture: true);
        var content = ContentService.Create("Cultures", Constants.System.Root, page.Alias);
        content.SetCultureName("English", "en-US");
        content.SetCultureName("Danish", "da-DK");
        content.SetValue("blocks", BlockList((image.Key, 1)), "en-US");
        content.SetValue("blocks", BlockList((image.Key, 4)), "da-DK");
        ContentService.Save(content, Constants.Security.SuperUserId, null);

        var english = ContentService.Publish(content, ["en-US"], Constants.Security.SuperUserId);
        Assert.That(english.Success, Is.True, Describe(english));

        var danish = ContentService.Publish(content, ["da-DK"], Constants.Security.SuperUserId);
        Assert.That(danish.Result, Is.EqualTo(PublishResultType.FailedPublishCancelledByEvent), Describe(danish));
        Assert.That(Describe(danish), Does.Contain("da-DK").And.Contain("at most 1"));
    }

    [Test]
    public async Task Editing_service_save_keeps_drafts_and_publish_is_cancelled()
    {
        var image = await CreateElementAsync("editingImage");
        var dataType = await CreateBlockDataTypeAsync(BlockEditors.BlockList, "Umb.PropertyEditorUi.BlockList", [Rule(image, 1, 1)]);
        var page = await CreatePageAsync("editingPage", dataType);
        var draft = await EditingService.CreateAsync(CreateModel(page.Key, BlockList((image.Key, 0))), Constants.Security.SuperUserKey);

        Assert.That(draft.Success, Is.True, draft.Status.ToString());

        var rejected = await EditingService.CreateAndPublishAsync(CreateModel(page.Key, BlockList((image.Key, 0))), [], Constants.Security.SuperUserKey);
        Assert.That(rejected.Success, Is.False, rejected.Status.ToString());
        Assert.That(rejected.Status, Is.EqualTo(ContentEditingOperationStatus.CancelledByNotification));

        var published = await EditingService.UpdateAndPublishAsync(
            draft.Result!.Content!.Key,
            UpdateModel(BlockList((image.Key, 1))),
            [],
            Constants.Security.SuperUserKey);
        Assert.That(published.Success, Is.True, published.Status.ToString());
    }

    private async Task<IContentType> CreateElementAsync(string alias, params (string Alias, string Name, IDataType DataType)[] properties)
    {
        var unique = Unique(alias);
        var contentType = new ContentType(ShortStringHelper, -1)
        {
            Alias = unique,
            Name = alias == "card" ? "Card" : "Block",
            Icon = "icon-document",
            IsElement = true,
        };

        foreach (var property in properties)
        {
            contentType.AddPropertyType(new PropertyType(ShortStringHelper, property.DataType, property.Alias) { Name = property.Name }, "content", "Content");
        }

        ContentTypeService.Save(contentType, Constants.Security.SuperUserId);
        return contentType;
    }

    private async Task<IContentType> CreatePageAsync(string alias, IDataType blocks, bool variesByCulture = false)
    {
        var contentType = new ContentType(ShortStringHelper, -1)
        {
            Alias = Unique(alias),
            Name = alias,
            Icon = "icon-document",
            AllowedAsRoot = true,
            Variations = variesByCulture ? ContentVariation.Culture : ContentVariation.Nothing,
        };
        var property = new PropertyType(ShortStringHelper, blocks, "blocks")
        {
            Name = "Blocks",
            Variations = variesByCulture ? ContentVariation.Culture : ContentVariation.Nothing,
        };
        contentType.AddPropertyType(property, "content", "Content");
        ContentTypeService.Save(contentType, Constants.Security.SuperUserId);
        return contentType;
    }

    private async Task<IDataType> CreateBlockDataTypeAsync(string editorAlias, string editorUiAlias, IReadOnlyList<BlockCountRule> rules)
    {
        var editors = GetRequiredService<PropertyEditorCollection>();
        Assert.That(editors.TryGet(editorAlias, out var editor), Is.True, $"Missing property editor {editorAlias}.");
        var dataType = new DataType(editor!, GetRequiredService<IConfigurationEditorJsonSerializer>())
        {
            Name = Unique(editorAlias),
            DatabaseType = ValueStorageType.Ntext,
            EditorUiAlias = editorUiAlias,
            ConfigurationData = new Dictionary<string, object>
            {
                ["blocks"] = rules.Select(rule => new Dictionary<string, object?>
                {
                    ["contentElementTypeKey"] = rule.ContentElementTypeKey.ToString(),
                    ["blockCount"] = new Dictionary<string, object?>
                    {
                        ["min"] = rule.Min,
                        ["max"] = rule.Max,
                    },
                }).ToList(),
            },
        };

        await DataTypeService.CreateAsync(dataType, Constants.Security.SuperUserKey);
        return (await DataTypeService.GetAsync(dataType.Key))!;
    }

    private async Task EnsureLanguageAsync(string isoCode, string name)
    {
        if (await LanguageService.GetAsync(isoCode) is not null)
        {
            return;
        }

        await LanguageService.CreateAsync(new Language(isoCode, name), Constants.Security.SuperUserKey);
    }

    private static ContentCreateModel CreateModel(Guid contentTypeKey, string value) => new()
    {
        ContentTypeKey = contentTypeKey,
        ParentKey = null,
        Variants = [new VariantModel { Name = "Home" }],
        Properties = [new PropertyValueModel { Alias = "blocks", Value = value }],
    };

    private static ContentUpdateModel UpdateModel(string value) => new()
    {
        Variants = [new VariantModel { Name = "Home" }],
        Properties = [new PropertyValueModel { Alias = "blocks", Value = value }],
    };

    private static BlockCountRule Rule(IContentType contentType, int? min = null, int? max = null) => new()
    {
        ContentElementTypeKey = contentType.Key,
        Min = min,
        Max = max,
    };

    private static string Unique(string alias) => alias + Guid.NewGuid().ToString("N")[..8];

    private static string Describe(PublishResult result)
    {
        var messages = string.Join(" | ", result.EventMessages?.GetAll().Select(message => message.Message) ?? []);
        return $"{result.Result}: {messages}";
    }

    private static string BlockList(params (Guid Type, int Count)[] blocks)
    {
        var contentData = new JsonArray();
        var layout = new JsonArray();
        foreach (var (type, count) in blocks)
        {
            for (var index = 0; index < count; index++)
            {
                var key = Guid.NewGuid();
                contentData.Add(new JsonObject
                {
                    ["key"] = key.ToString(),
                    ["contentTypeKey"] = type.ToString(),
                    ["values"] = new JsonArray(),
                });
                layout.Add(new JsonObject { ["contentKey"] = key.ToString() });
            }
        }

        return new JsonObject
        {
            ["contentData"] = contentData,
            ["settingsData"] = new JsonArray(),
            ["layout"] = new JsonObject { [BlockEditors.BlockList] = layout },
        }.ToJsonString();
    }

    private static string BlockGrid(params Guid[] types)
    {
        var contentData = new JsonArray();
        JsonObject? root = null;
        var areaItems = new JsonArray();
        foreach (var type in types)
        {
            var key = Guid.NewGuid();
            contentData.Add(new JsonObject
            {
                ["key"] = key.ToString(),
                ["contentTypeKey"] = type.ToString(),
                ["values"] = new JsonArray(),
            });
            var item = new JsonObject { ["contentKey"] = key.ToString() };
            if (root is null)
            {
                root = item;
            }
            else
            {
                areaItems.Add(item);
            }
        }

        if (root is null)
        {
            throw new InvalidOperationException("A block grid value needs at least one block.");
        }

        root["areas"] = new JsonArray
        {
            new JsonObject
            {
                ["key"] = Guid.NewGuid().ToString(),
                ["items"] = areaItems,
            },
        };

        return new JsonObject
        {
            ["contentData"] = contentData,
            ["settingsData"] = new JsonArray(),
            ["layout"] = new JsonObject { [BlockEditors.BlockGrid] = new JsonArray { root } },
        }.ToJsonString();
    }

    private static string NestedCards(Guid cardType, Guid imageType, int imageCount)
    {
        var images = new JsonArray();
        var imageLayout = new JsonArray();
        for (var index = 0; index < imageCount; index++)
        {
            var key = Guid.NewGuid();
            images.Add(new JsonObject
            {
                ["key"] = key.ToString(),
                ["contentTypeKey"] = imageType.ToString(),
                ["values"] = new JsonArray(),
            });
            imageLayout.Add(new JsonObject { ["contentKey"] = key.ToString() });
        }

        var slides = new JsonObject
        {
            ["contentData"] = images,
            ["settingsData"] = new JsonArray(),
            ["layout"] = new JsonObject { [BlockEditors.BlockList] = imageLayout },
        };
        var cardKey = Guid.NewGuid();
        return new JsonObject
        {
            ["contentData"] = new JsonArray
            {
                new JsonObject
                {
                    ["key"] = cardKey.ToString(),
                    ["contentTypeKey"] = cardType.ToString(),
                    ["values"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["alias"] = "slides",
                            ["editorAlias"] = BlockEditors.BlockList,
                            ["value"] = slides,
                        },
                    },
                },
            },
            ["settingsData"] = new JsonArray(),
            ["layout"] = new JsonObject
            {
                [BlockEditors.BlockList] = new JsonArray { new JsonObject { ["contentKey"] = cardKey.ToString() } },
            },
        }.ToJsonString();
    }
}
