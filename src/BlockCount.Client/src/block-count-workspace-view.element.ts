import { UMB_PROPERTY_DATASET_CONTEXT } from '@umbraco-cms/backoffice/property';
import { UMB_VALIDATION_CONTEXT } from '@umbraco-cms/backoffice/validation';
import { css, customElement, html } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import type { UmbNumberRangeValueType } from '@umbraco-cms/backoffice/models';
import type { UmbWorkspaceViewElement } from '@umbraco-cms/backoffice/workspace';
import { negativeMessage } from './block-count.js';

@customElement('block-count-workspace-view')
export class BlockCountWorkspaceViewElement extends UmbLitElement implements UmbWorkspaceViewElement {
    #validationContext?: typeof UMB_VALIDATION_CONTEXT.TYPE;
    readonly #messageKey = 'blockCount:negative';

    constructor() {
        super();

        this.consumeContext(UMB_VALIDATION_CONTEXT, (context) => {
            this.#validationContext = context;
        });

        this.consumeContext(UMB_PROPERTY_DATASET_CONTEXT, (dataset) => {
            if (!dataset) {
                return;
            }

            void this.#observeCount(dataset);
        });
    }

    async #observeCount(dataset: typeof UMB_PROPERTY_DATASET_CONTEXT.TYPE): Promise<void> {
        const value = await dataset.propertyValueByAlias<UmbNumberRangeValueType>('blockCount');
        if (!value) {
            return;
        }

        this.observe(value, (count) => this.#validate(count), 'blockCountConfiguration');
    }

    #validate(count: UmbNumberRangeValueType | undefined): void {
        const context = this.#validationContext;
        if (!context) {
            return;
        }

        context.messages.removeMessageByKey(this.#messageKey);
        const min = count?.min;
        const max = count?.max;
        if ((min !== undefined && min !== null && min < 0) || (max !== undefined && max !== null && max < 0)) {
            context.messages.addMessage('custom', '$', negativeMessage('this block'), this.#messageKey);
        }
    }

    override render() {
        return html`
            <uui-box headline=${this.localize.term('blockCount_amount')}>
                <umb-property
                    .label=${this.localize.term('blockCount_amount')}
                    .description=${this.localize.term('blockCount_amountDescription')}
                    alias="blockCount"
                    property-editor-ui-alias="Umb.PropertyEditorUi.NumberRange">
                </umb-property>
            </uui-box>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                margin: var(--uui-size-layout-1);
                padding-bottom: var(--uui-size-layout-1);
            }

            uui-box {
                margin-top: var(--uui-size-layout-1);
            }
        `,
    ];
}

export default BlockCountWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        'block-count-workspace-view': BlockCountWorkspaceViewElement;
    }
}
