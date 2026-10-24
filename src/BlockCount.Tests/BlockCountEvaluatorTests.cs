using System.Text.Json;
using System.Text.Json.Nodes;

namespace BlockCount.Tests;

public class BlockCountEvaluatorTests
{
    private static readonly Guid Image = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Rte = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Video = Guid.Parse("33333333-3332-3332-3332-333333333333");

    private static readonly IReadOnlyDictionary<Guid, string> Names = new Dictionary<Guid, string>
    {
        [Image] = "Image Block",
        [Rte] = "RTE Block",
        [Video] = "Video Block",
    };

    [Test]
    public void Minimum_fails_when_a_block_type_is_missing()
    {
        var issues = BlockCountEvaluator.Evaluate(BlockEditors.BlockList, BlockList(), [Rule(Image, min: 1)], Names);

        Assert.That(issues.Select(issue => issue.Message), Is.EqualTo(new[] { BlockCountMessages.BelowMinimum("Image Block", 1, 0) }));
    }

    [Test]
    public void Maximum_fails_only_for_the_block_type_that_exceeds_it()
    {
        var issues = BlockCountEvaluator.Evaluate(
            BlockEditors.BlockList,
            BlockList((Image, 4)),
            [Rule(Rte, max: 3), Rule(Image, max: 3)],
            Names);

        Assert.That(issues.Select(issue => issue.Message), Is.EqualTo(new[] { BlockCountMessages.AboveMaximum("Image Block", 3, 4) }));
    }

    [Test]
    public void Equal_minimum_and_maximum_require_an_exact_count()
    {
        var tooFew = BlockCountEvaluator.Evaluate(BlockEditors.BlockList, BlockList((Image, 1)), [Rule(Image, 2, 2)], Names);
        var exact = BlockCountEvaluator.Evaluate(BlockEditors.BlockList, BlockList((Image, 2)), [Rule(Image, 2, 2)], Names);

        Assert.That(tooFew.Single().Message, Is.EqualTo(BlockCountMessages.Exact("Image Block", 2, 1)));
        Assert.That(exact, Is.Empty);
    }

    [Test]
    public void Unset_limits_do_not_fail()
    {
        var issues = BlockCountEvaluator.Evaluate(
            BlockEditors.BlockList,
            BlockList((Image, 2), (Rte, 1)),
            [Rule(Image), Rule(Rte, min: 0), new BlockCountRule { ContentElementTypeKey = Video }],
            Names);

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public void Multiple_block_types_are_validated_independently()
    {
        var issues = BlockCountEvaluator.Evaluate(
            BlockEditors.BlockList,
            BlockList((Image, 1), (Rte, 4)),
            [Rule(Image, 1, 1), Rule(Rte, 0, 3), Rule(Video, 1, 2)],
            Names);

        Assert.That(issues.Select(issue => issue.BlockName), Is.EqualTo(new[] { "RTE Block", "Video Block" }));
    }

    [Test]
    public void Block_grid_counts_areas_and_deeper_areas_together()
    {
        var value = BlockGrid();
        var issues = BlockCountEvaluator.Evaluate(
            BlockEditors.BlockGrid,
            value,
            [Rule(Image, max: 1), Rule(Rte, max: 2)],
            Names);

        Assert.That(issues.Select(issue => issue.Message), Is.EqualTo(new[] { BlockCountMessages.AboveMaximum("Image Block", 1, 2) }));
    }

    [Test]
    public void Nested_block_editor_properties_are_not_counted_in_the_parent()
    {
        var nested = JsonNode.Parse(BlockList((Image, 3)))!.AsObject();
        var parent = JsonNode.Parse(BlockList((Rte, 1)))!.AsObject();
        var values = new JsonArray
        {
            new JsonObject
            {
                ["alias"] = "slides",
                ["editorAlias"] = BlockEditors.BlockList,
                ["culture"] = null,
                ["value"] = nested,
            },
        };
        parent["contentData"]![0]!["values"] = values;

        var issues = BlockCountEvaluator.Evaluate(
            BlockEditors.BlockList,
            parent.ToJsonString(),
            [Rule(Image, max: 1), Rule(Rte, max: 1)],
            Names);

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public void Orphan_content_data_is_ignored()
    {
        var value = JsonNode.Parse(BlockList((Image, 1)))!.AsObject();
        (value["contentData"] as JsonArray)!.Add(new JsonObject
        {
            ["key"] = Guid.NewGuid().ToString(),
            ["contentTypeKey"] = Image.ToString(),
        });

        var issues = BlockCountEvaluator.Evaluate(BlockEditors.BlockList, value.ToJsonString(), [Rule(Image, max: 1)], Names);

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public void Invalid_configuration_is_reported()
    {
        var negative = BlockCountEvaluator.Evaluate(BlockEditors.BlockList, BlockList(), [Rule(Image, min: -1, max: 2)], Names);
        var reversed = BlockCountEvaluator.Evaluate(BlockEditors.BlockList, BlockList(), [Rule(Image, min: 4, max: 1)], Names);

        Assert.That(negative.Single().Message, Is.EqualTo(BlockCountMessages.Negative("Image Block")));
        Assert.That(reversed.Single().Message, Is.EqualTo(BlockCountMessages.MinimumAboveMaximum("Image Block")));
    }

    [Test]
    public void Empty_or_unreadable_values_count_as_zero()
    {
        var issues = BlockCountEvaluator.Evaluate(BlockEditors.BlockList, "not-json", [Rule(Image, min: 1)], Names);

        Assert.That(issues.Single().Kind, Is.EqualTo(BlockCountIssueKind.BelowMinimum));
    }

    private static BlockCountRule Rule(Guid key, int? min = null, int? max = null) =>
        new()
        {
            ContentElementTypeKey = key,
            Min = min,
            Max = max,
        };

    private static string BlockList(params (Guid Type, int Count)[] groups)
    {
        var contentData = new JsonArray();
        var layout = new JsonArray();
        var index = 0;
        foreach (var (type, count) in groups)
        {
            for (var i = 0; i < count; i++)
            {
                var key = GuidFrom(index++);
                contentData.Add(new JsonObject
                {
                    ["contentTypeKey"] = type.ToString(),
                    ["key"] = key.ToString(),
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

    private static string BlockGrid()
    {
        var rootImage = GuidFrom(1);
        var areaImage = GuidFrom(2);
        var nestedRte = GuidFrom(3);
        var rootRte = GuidFrom(4);
        var value = new JsonObject
        {
            ["contentData"] = new JsonArray
            {
                Block(rootImage, Image),
                Block(areaImage, Image),
                Block(nestedRte, Rte),
                Block(rootRte, Rte),
            },
            ["layout"] = new JsonObject
            {
                [BlockEditors.BlockGrid] = new JsonArray
                {
                    new JsonObject
                    {
                        ["contentKey"] = rootImage.ToString(),
                        ["areas"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["key"] = Guid.NewGuid().ToString(),
                                ["items"] = new JsonArray
                                {
                                    new JsonObject
                                    {
                                        ["contentKey"] = areaImage.ToString(),
                                        ["areas"] = new JsonArray
                                        {
                                            new JsonObject
                                            {
                                                ["key"] = Guid.NewGuid().ToString(),
                                                ["items"] = new JsonArray
                                                {
                                                    new JsonObject { ["contentKey"] = nestedRte.ToString() },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                    new JsonObject { ["contentKey"] = rootRte.ToString(), ["areas"] = new JsonArray() },
                },
            },
        };

        return value.ToJsonString();
    }

    private static JsonObject Block(Guid key, Guid type) => new()
    {
        ["key"] = key.ToString(),
        ["contentTypeKey"] = type.ToString(),
        ["values"] = new JsonArray(),
    };

    private static Guid GuidFrom(int index) => Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}");
}

public class BlockCountConfigurationReaderTests
{
    [Test]
    public void Reads_limits_from_json_objects_dictionaries_and_elements()
    {
        var image = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var rte = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var fromNode = BlockCountConfigurationReader.Read(new Dictionary<string, object>
        {
            ["blocks"] = new JsonArray
            {
                new JsonObject
                {
                    ["contentElementTypeKey"] = image.ToString(),
                    ["label"] = "Image",
                    ["blockCount"] = new JsonObject { ["min"] = 1, ["max"] = null },
                },
            },
        });

        var fromDictionary = BlockCountConfigurationReader.Read(new Dictionary<string, object>
        {
            ["blocks"] = new List<Dictionary<string, object?>>
            {
                new()
                {
                    ["contentElementTypeKey"] = rte,
                    ["blockCount"] = new Dictionary<string, object?> { ["min"] = 0, ["max"] = 3 },
                },
            },
        });

        using var document = JsonDocument.Parse(
            "[{\"contentElementTypeKey\":\"" + image + "\",\"blockCount\":{\"min\":1,\"max\":1}}]");
        var fromElement = BlockCountConfigurationReader.Read(new Dictionary<string, object>
        {
            ["blocks"] = document.RootElement.Clone(),
        });

        Assert.That(fromNode.Single().Min, Is.EqualTo(1));
        Assert.That(fromNode.Single().Max, Is.Null);
        Assert.That(fromDictionary.Single().ContentElementTypeKey, Is.EqualTo(rte));
        Assert.That(fromDictionary.Single().Max, Is.EqualTo(3));
        Assert.That(fromElement.Single().Min, Is.EqualTo(1));
        Assert.That(fromElement.Single().Max, Is.EqualTo(1));
    }

    [Test]
    public void Skips_blocks_without_a_count_configuration()
    {
        var rules = BlockCountConfigurationReader.Read(new Dictionary<string, object>
        {
            ["blocks"] = new JsonArray
            {
                new JsonObject { ["contentElementTypeKey"] = Guid.NewGuid().ToString() },
            },
        });

        Assert.That(rules, Is.Empty);
    }
}
