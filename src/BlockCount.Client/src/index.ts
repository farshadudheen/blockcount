import type { UmbEntryPointOnInit } from '@umbraco-cms/backoffice/extension-api';
import { UMB_WORKSPACE_CONDITION_ALIAS } from '@umbraco-cms/backoffice/workspace';
import { BlockCountPropertyContext } from './block-count-property-context.js';
import './block-count-workspace-view.element.js';

const blockListWorkspace = 'Umb.Workspace.BlockListType';
const blockGridWorkspace = 'Umb.Workspace.BlockGridType';

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: 'localization',
        alias: 'BlockCount.Localization.En',
        name: 'BlockCount English',
        meta: {
            culture: 'en',
            localizations: {
                blockCount: {
                    amount: 'Amount',
                    amountDescription:
                        'Minimum and maximum number of this block type. Leave either value empty for no limit. Set both to the same number for an exact count.',
                    count: 'Count',
                },
            },
        },
    },
    workspaceView('BlockCount.WorkspaceView.BlockListType', blockListWorkspace),
    workspaceView('BlockCount.WorkspaceView.BlockGridType', blockGridWorkspace),
    {
        type: 'propertyContext',
        alias: 'BlockCount.PropertyContext',
        name: 'Block Count Property Context',
        api: BlockCountPropertyContext,
        forPropertyEditorUis: ['Umb.PropertyEditorUi.BlockList', 'Umb.PropertyEditorUi.BlockGrid'],
        meta: {},
    },
];

function workspaceView(alias: string, workspaceAlias: string): UmbExtensionManifest {
    return {
        type: 'workspaceView',
        alias,
        name: 'Block Count',
        elementName: 'block-count-workspace-view',
        weight: 900,
        meta: {
            label: '#blockCount_count',
            pathname: 'count',
            icon: 'icon-list',
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: workspaceAlias,
            },
        ],
    };
}

export const onInit: UmbEntryPointOnInit = (_host, extensionRegistry) => {
    extensionRegistry.registerMany(manifests);
};
