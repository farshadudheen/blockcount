import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UMB_DOCUMENT_TYPE_ITEM_REPOSITORY_ALIAS, type UmbDocumentTypeItemModel } from '@umbraco-cms/backoffice/document-type';
import { UMB_PROPERTY_CONTEXT } from '@umbraco-cms/backoffice/property';
import { UmbRepositoryItemsManager } from '@umbraco-cms/backoffice/repository';
import type { UmbFormControlValidatorConfig } from '@umbraco-cms/backoffice/validation';
import {
    blockCountValidationMessage,
    blockGridEditorAlias,
    blockListEditorAlias,
    evaluateBlockCount,
    type BlockCountIssue,
    type BlockCountRule,
    type BlockValue,
} from './block-count.js';

const editorAliasByUi: Record<string, string> = {
    'Umb.PropertyEditorUi.BlockList': blockListEditorAlias,
    'Umb.PropertyEditorUi.BlockGrid': blockGridEditorAlias,
};

interface BlockEditorControl {
    addValidator: (
        flagKey: 'customError',
        getMessageMethod: () => string,
        checkMethod: () => boolean,
    ) => UmbFormControlValidatorConfig;
    removeValidator: (validator: UmbFormControlValidatorConfig) => void;
    checkValidity: () => boolean;
    pristine: boolean;
}

export class BlockCountPropertyContext extends UmbControllerBase {
    #rules: BlockCountRule[] = [];
    #value?: BlockValue;
    #editorAlias?: string;
    #names = new Map<string, string>();
    #issues: BlockCountIssue[] = [];
    #editor?: BlockEditorControl;
    #validator?: UmbFormControlValidatorConfig;
    #items = new UmbRepositoryItemsManager<UmbDocumentTypeItemModel>(this, UMB_DOCUMENT_TYPE_ITEM_REPOSITORY_ALIAS);

    constructor(host: UmbControllerHost) {
        super(host);

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

            this.observe(
                context.editor,
                (editor) => {
                    this.#attachEditor(editor);
                    this.#validate();
                },
                'blockCountEditorElement',
            );
        });

        this.observe(this.#items.items, (items: UmbDocumentTypeItemModel[]) => {
            this.#names = new Map(items.map((item) => [item.unique.toLowerCase(), item.name || 'Block']));
            this.#validate();
        });
    }

    override destroy(): void {
        this.#detachEditor();
        super.destroy();
    }

    #attachEditor(editor: unknown): void {
        const control = isBlockEditorControl(editor) ? editor : undefined;
        if (this.#editor === control) {
            return;
        }

        this.#detachEditor();
        if (!control) {
            return;
        }

        this.#editor = control;
        this.#validator = control.addValidator(
            'customError',
            () => blockCountValidationMessage(this.#issues),
            () => this.#issues.length > 0,
        );
    }

    #detachEditor(): void {
        if (this.#editor && this.#validator) {
            this.#editor.removeValidator(this.#validator);
        }

        this.#editor = undefined;
        this.#validator = undefined;
    }

    #validate(): void {
        this.#issues = this.#editorAlias
            ? evaluateBlockCount(this.#value, this.#rules, this.#names, this.#editorAlias)
            : [];

        if (this.#editor && this.#editor.pristine === false) {
            this.#editor.checkValidity();
        }
    }
}

function isBlockEditorControl(editor: unknown): editor is BlockEditorControl {
    if (!editor || typeof editor !== 'object') {
        return false;
    }

    const candidate = editor as Partial<BlockEditorControl>;
    return typeof candidate.addValidator === 'function'
        && typeof candidate.removeValidator === 'function'
        && typeof candidate.checkValidity === 'function';
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
