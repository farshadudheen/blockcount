import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UMB_DOCUMENT_TYPE_ITEM_REPOSITORY_ALIAS, type UmbDocumentTypeItemModel } from '@umbraco-cms/backoffice/document-type';
import { UMB_PROPERTY_CONTEXT } from '@umbraco-cms/backoffice/property';
import { UmbRepositoryItemsManager } from '@umbraco-cms/backoffice/repository';
import { UMB_VALIDATION_CONTEXT } from '@umbraco-cms/backoffice/validation';
import { blockGridEditorAlias, blockListEditorAlias, evaluateBlockCount, type BlockCountRule, type BlockValue } from './block-count.js';

const editorAliasByUi: Record<string, string> = {
    'Umb.PropertyEditorUi.BlockList': blockListEditorAlias,
    'Umb.PropertyEditorUi.BlockGrid': blockGridEditorAlias,
};

export class BlockCountPropertyContext extends UmbControllerBase {
    #validationContext?: typeof UMB_VALIDATION_CONTEXT.TYPE;
    #rules: BlockCountRule[] = [];
    #value?: BlockValue;
    #editorAlias?: string;
    #names = new Map<string, string>();
    #messageKeys = new Set<string>();
    #items = new UmbRepositoryItemsManager<UmbDocumentTypeItemModel>(this, UMB_DOCUMENT_TYPE_ITEM_REPOSITORY_ALIAS);

    constructor(host: UmbControllerHost) {
        super(host);

        this.consumeContext(UMB_VALIDATION_CONTEXT, (context) => {
            this.#validationContext = context;
            this.#validate();
        });

        this.consumeContext(UMB_PROPERTY_CONTEXT, (context) => {
            if (!context) {
                return;
            }

            this.observe(
                context.value,
                (value) => {
                    this.#value = value as BlockValue | undefined;
                    this.#validate();
                },
                'blockCountValue',
            );

            this.observe(
                context.config,
                (config) => {
                    this.#rules = readRules(config);
                    const keys = this.#rules.map((rule) => rule.contentElementTypeKey).filter((key): key is string => !!key);
                    this.#items.setUniques(keys);
                    this.#validate();
                },
                'blockCountConfig',
            );

            this.observe(
                context.editorManifest,
                (manifest) => {
                    this.#editorAlias = manifest?.alias ? editorAliasByUi[manifest.alias] : undefined;
                    this.#validate();
                },
                'blockCountEditor',
            );
        });

        this.observe(this.#items.items, (items: UmbDocumentTypeItemModel[]) => {
            this.#names = new Map(items.map((item) => [item.unique.toLowerCase(), item.name || 'Block']));
            this.#validate();
        });
    }

    #validate(): void {
        const context = this.#validationContext;
        if (!context) {
            return;
        }

        for (const key of this.#messageKeys) {
            context.messages.removeMessageByKey(key);
        }

        this.#messageKeys.clear();
        if (!this.#editorAlias) {
            return;
        }

        const issues = evaluateBlockCount(this.#value, this.#rules, this.#names, this.#editorAlias);
        for (const issue of issues) {
            const key = `blockCount:${issue.kind}:${issue.contentElementTypeKey}`;
            context.messages.addMessage('custom', '$', issue.message, key);
            this.#messageKeys.add(key);
        }
    }
}

function readRules(config: unknown): BlockCountRule[] {
    const blocks = readBlocks(config);
    return blocks
        .filter((block) => !!block.contentElementTypeKey)
        .map((block) => ({
            contentElementTypeKey: block.contentElementTypeKey as string,
            min: block.blockCount?.min,
            max: block.blockCount?.max,
        }));
}

function readBlocks(config: unknown): Array<{ contentElementTypeKey?: string; blockCount?: { min?: number | null; max?: number | null } }> {
    if (!config || typeof config !== 'object' || !('getValueByAlias' in config)) {
        return [];
    }

    const blocks = (config as { getValueByAlias: (alias: string) => unknown }).getValueByAlias('blocks');
    return Array.isArray(blocks) ? blocks : [];
}

export default BlockCountPropertyContext;
