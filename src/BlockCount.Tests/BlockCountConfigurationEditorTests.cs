using Moq;
using Newtonsoft.Json.Linq;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Infrastructure.Services;

namespace BlockCount.Tests;

public class BlockCountConfigurationEditorTests
{
    private static readonly Guid Image = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Test]
    public void Block_list_editor_keeps_count_limits_on_the_block()
    {
        var editor = new BlockCountListConfigurationEditor(IoHelper(), new EditorConfigurationParser());
        Assert.That(editor.Fields.Select(field => field.Key), Does.Contain("blocks").And.Contain("validationLimit"));
        Assert.That(editor.Fields.Select(field => field.Key), Does.Not.Contain("blockCounts"));

        var parsed = editor.FromConfigurationEditor(PostedBlocks(), null);

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Blocks, Has.Length.EqualTo(1));
        Assert.That(parsed.Blocks[0].Label, Is.EqualTo("Image"));
        Assert.That(parsed.BlockCounts, Has.Length.EqualTo(1));
        Assert.That(parsed.BlockCounts![0].ContentElementTypeKey, Is.EqualTo(Image));
        Assert.That(parsed.BlockCounts[0].Min, Is.EqualTo(1));
        Assert.That(parsed.BlockCounts[0].Max, Is.EqualTo(2));

        var blocks = editor.ToConfigurationEditor(parsed)["blocks"] as JArray;
        Assert.That(blocks, Is.Not.Null);
        Assert.That(blocks![0]!["blockCount"]!["min"]!.Value<int>(), Is.EqualTo(1));
        Assert.That(blocks[0]!["blockCount"]!["max"]!.Value<int>(), Is.EqualTo(2));
        Assert.That(BlockCountConfigurationReader.Read(parsed).Single().Min, Is.EqualTo(1));
    }

    [Test]
    public void Block_grid_editor_keeps_the_native_block_fields()
    {
        var editor = new BlockCountGridConfigurationEditor(IoHelper(), new EditorConfigurationParser());
        Assert.That(editor.Fields.Select(field => field.Key), Does.Contain("blocks").And.Contain("validationLimit"));

        var parsed = editor.FromConfigurationEditor(PostedBlocks(), null);
        Assert.That(parsed!.BlockCounts!.Single().Max, Is.EqualTo(2));
        Assert.That(parsed.Blocks.Single().ContentElementTypeKey, Is.EqualTo(Image));
    }

    [Test]
    public void Dictionary_configuration_still_reads_block_count()
    {
        var rules = BlockCountConfigurationReader.Read(new Dictionary<string, object>
        {
            ["blocks"] = new System.Text.Json.Nodes.JsonArray
            {
                new System.Text.Json.Nodes.JsonObject
                {
                    ["contentElementTypeKey"] = Image.ToString(),
                    ["blockCount"] = new System.Text.Json.Nodes.JsonObject
                    {
                        ["min"] = 0,
                        ["max"] = 3,
                    },
                },
            },
        });

        Assert.That(rules.Single().Min, Is.EqualTo(0));
        Assert.That(rules.Single().Max, Is.EqualTo(3));
    }

    private static Dictionary<string, object?> PostedBlocks() => new()
    {
        ["blocks"] = new JArray
        {
            new JObject
            {
                ["contentElementTypeKey"] = Image.ToString(),
                ["label"] = "Image",
                ["blockCount"] = new JObject
                {
                    ["min"] = 1,
                    ["max"] = 2,
                },
            },
        },
    };

    private static IIOHelper IoHelper() => Mock.Of<IIOHelper>();
}
