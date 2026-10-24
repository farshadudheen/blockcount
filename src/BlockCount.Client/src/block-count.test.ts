import { describe, expect, it } from 'vitest';
import {
    aboveMaximumMessage,
    belowMinimumMessage,
    blockCountValidationMessage,
    blockGridEditorAlias,
    blockListEditorAlias,
    evaluateBlockCount,
    exactMessage,
    type BlockValue,
} from './block-count.js';

const image = '11111111-1111-1111-1111-111111111111';
const rte = '22222222-2222-2222-2222-222222222222';
const video = '33333333-3333-3333-3333-333333333333';

const names = new Map([
    [image, 'Image Block'],
    [rte, 'RTE Block'],
    [video, 'Video Block'],
]);

describe('evaluateBlockCount', () => {
    it('enforces a minimum on one block type', () => {
        const issues = evaluateBlockCount(blockList(), [{ contentElementTypeKey: image, min: 1 }], names);
        expect(issues.map((issue) => issue.message)).toEqual([belowMinimumMessage('Image Block', 1, 0)]);
    });

    it('enforces a maximum on one block type', () => {
        const issues = evaluateBlockCount(
            blockList([image, image, image, image]),
            [{ contentElementTypeKey: rte, max: 3 }, { contentElementTypeKey: image, max: 3 }],
            names,
        );
        expect(issues.map((issue) => issue.message)).toEqual([aboveMaximumMessage('Image Block', 3, 4)]);
    });

    it('treats equal minimum and maximum as an exact count', () => {
        const tooFew = evaluateBlockCount(blockList([image]), [{ contentElementTypeKey: image, min: 2, max: 2 }], names);
        const exact = evaluateBlockCount(blockList([image, image]), [{ contentElementTypeKey: image, min: 2, max: 2 }], names);
        expect(tooFew.map((issue) => issue.message)).toEqual([exactMessage('Image Block', 2, 1)]);
        expect(exact).toEqual([]);
    });

    it('allows either limit to stay unset', () => {
        const issues = evaluateBlockCount(
            blockList([image, image, rte]),
            [
                { contentElementTypeKey: image, min: null, max: null },
                { contentElementTypeKey: rte, min: 0 },
                { contentElementTypeKey: video },
            ],
            names,
        );
        expect(issues).toEqual([]);
    });

    it('applies rules independently for multiple block types', () => {
        const issues = evaluateBlockCount(
            blockList([image, rte, rte, rte, rte]),
            [
                { contentElementTypeKey: image, min: 1, max: 1 },
                { contentElementTypeKey: rte, min: 0, max: 3 },
                { contentElementTypeKey: video, min: 1, max: 2 },
            ],
            names,
        );
        expect(issues.map((issue) => issue.blockName)).toEqual(['RTE Block', 'Video Block']);
    });

    it('counts block grid areas and deeper areas as one quota', () => {
        const value = blockGrid([
            { type: image, areas: [{ type: image, areas: [{ type: rte }] }] },
            { type: rte },
        ]);
        const issues = evaluateBlockCount(
            value,
            [
                { contentElementTypeKey: image, max: 1 },
                { contentElementTypeKey: rte, max: 2 },
            ],
            names,
        );
        expect(issues.map((issue) => issue.message)).toEqual([aboveMaximumMessage('Image Block', 1, 2)]);
    });

    it('does not count blocks that belong to a nested block editor property', () => {
        const nested = blockList([image, image, image]);
        const value = blockList([rte], {
            [key(0)]: nested,
        });
        const issues = evaluateBlockCount(value, [{ contentElementTypeKey: image, max: 1 }, { contentElementTypeKey: rte, max: 1 }], names);
        expect(issues).toEqual([]);
    });

    it('ignores content data that is not in the layout', () => {
        const value = blockList([image]);
        value.contentData?.push({ key: 'orphan', contentTypeKey: image });
        const issues = evaluateBlockCount(value, [{ contentElementTypeKey: image, max: 1 }], names);
        expect(issues).toEqual([]);
    });
});

describe('blockCountValidationMessage', () => {
    it('returns an empty string when there are no issues', () => {
        expect(blockCountValidationMessage([])).toBe('');
    });

    it('escapes block names and joins every issue', () => {
        const imageName = `Image <Block> & "O'Hare"`;
        const issues = evaluateBlockCount(
            blockList([image, rte]),
            [
                { contentElementTypeKey: image, max: 0 },
                { contentElementTypeKey: rte, min: 2 },
            ],
            new Map([
                [image, imageName],
                [rte, 'RTE Block'],
            ]),
        );

        expect(blockCountValidationMessage(issues)).toBe(
            [
                'Image &lt;Block&gt; &amp; &quot;O&#39;Hare&quot; allows at most 0 entries. The current count is 1.',
                'RTE Block requires at least 2 entries. The current count is 1.',
            ].join('<br>'),
        );
    });
});

function key(index: number): string {
    return `00000000-0000-0000-0000-${(index + 1).toString().padStart(12, '0')}`;
}

function blockList(types: string[] = [], nestedByContentKey: Record<string, BlockValue> = {}): BlockValue {
    return {
        contentData: types.map((type, index) => ({
            key: key(index),
            contentTypeKey: type,
            values: nestedByContentKey[key(index)]
                ? [{ alias: 'nested', editorAlias: blockListEditorAlias, value: nestedByContentKey[key(index)] }]
                : [],
        })) as BlockValue['contentData'],
        layout: {
            [blockListEditorAlias]: types.map((_, index) => ({ contentKey: key(index) })),
        },
    };
}

function blockGrid(items: Array<{ type: string; areas?: Array<{ type: string; areas?: Array<{ type: string }> }> }>): BlockValue {
    const contentData: NonNullable<BlockValue['contentData']> = [];
    let index = 0;
    const next = (type: string) => {
        const contentKey = key(index++);
        contentData.push({ key: contentKey, contentTypeKey: type });
        return contentKey;
    };

    const layout = items.map((item) => ({
        contentKey: next(item.type),
        areas: (item.areas ?? []).map((area) => ({
            items: [
                {
                    contentKey: next(area.type),
                    areas: (area.areas ?? []).map((child) => ({
                        items: [{ contentKey: next(child.type) }],
                    })),
                },
            ],
        })),
    }));

    return {
        contentData,
        layout: {
            [blockGridEditorAlias]: layout,
        },
    };
}
