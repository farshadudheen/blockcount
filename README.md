# BlockCount

Per-block-type minimum and maximum counts for Umbraco 17 Block List and Block Grid.

BlockCount adds a Count tab to each allowed block type. The limits are stored on the data type. The backoffice shows a validation message while the counts are wrong, and the server cancels publish when a document still breaks them.

## Requirements

- .NET 10
- Umbraco CMS 17.7.1 or later, and earlier than 18
- Node.js 24 or later, on `PATH`, when building this repository

The NuGet dependency is `[17.7.1, 18.0.0)`. This package does not support Umbraco 18.

## Install

Add a project reference:

```xml
<ProjectReference Include="..\BlockCount\BlockCount.csproj" />
```

Or pack and reference the NuGet package:

```bash
dotnet pack src/BlockCount/BlockCount.csproj -c Release
```

```xml
<PackageReference Include="BlockCount" Version="1.0.0" />
```

The package is a Razor Class Library. Umbraco discovers `App_Plugins/BlockCount/umbraco-package.json` from the referenced project or package. No composer registration is required in the website.

## Configuration

1. Open a Block List or Block Grid data type.
2. Open an allowed block type.
3. Open the **Count** tab.
4. Set **Amount**.

Leave either number empty for no limit on that side. Set both numbers to the same value for an exact count. `0` is a valid minimum. Negative numbers are rejected. A minimum greater than the maximum is invalid configuration and fails publish.

The existing Amount field on the data type still limits the total number of blocks. BlockCount limits each block type on its own.

Example:

| Block type | Minimum | Maximum |
| --- | --- | --- |
| Image | 1 | 1 |
| Rich text | 0 | 3 |
| Video | 1 | 2 |

## Counting rules

Counts come from layout items. Each layout item is joined to `contentData` by `contentKey` / `key`, and the block type is `contentTypeKey`. Content entries that are not in the layout are ignored.

- **Block List:** items in `layout["Umbraco.BlockList"]`.
- **Block Grid:** items in `layout["Umbraco.BlockGrid"]` and every nested `areas[].items` array, at any depth. Those items share one quota for the property. An Image maximum of 1 means one Image anywhere in that grid, including areas.
- **Nested block properties:** a Block List or Block Grid property on an element type is a separate property with its own data type. Its items are not added to the parent count. The server validates that nested value against the element property's data type. The same applies to block properties on the settings element.
- **Culture:** a variant property is checked only for cultures included in the publish. An invariant property is always checked. The `expose` flag does not change the count.

Draft save is not cancelled on the server, so an incomplete document can be saved. Publish is cancelled. In the backoffice, the property validation messages also keep Save and Publish invalid while the editor is open.

A nested Block List or Block Grid is validated in the backoffice when that nested editor is mounted. A nested editor inside a closed overlay is still validated when the document is published.

## Development

```bash
dotnet test src/BlockCount.Tests/BlockCount.Tests.csproj
dotnet test src/BlockCount.IntegrationTests/BlockCount.IntegrationTests.csproj
dotnet build src/BlockCount.Site/BlockCount.Site.csproj
```

`src/BlockCount.Site` is an empty Umbraco 17.7.1 site that references the package. Its Development settings install Umbraco unattended with the user `admin@localhost` and password `BlockCount-Site-17`. Start it with the `https` launch profile. The backoffice sign-in rejects plain HTTP.

The client project is built with Vite into `src/BlockCount/wwwroot` before the library compiles. Set `BlockCountSkipClientBuild=true` to skip that step. If Node.js is not installed under `C:\Program Files\nodejs`, set `BlockCountNpm` to `npm` or the full path of `npm.cmd`.

## Out of scope

Rich Text block elements, Single Block, and Umbraco 18 are not implemented. The evaluator takes plain models so a later version can replace the configuration reader and the publishing handler without changing the counting rules.
