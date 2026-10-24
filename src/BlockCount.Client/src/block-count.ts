export const blockListEditorAlias = 'Umbraco.BlockList';
export const blockGridEditorAlias = 'Umbraco.BlockGrid';

export interface BlockCountLimit {
    min?: number | null;
    max?: number | null;
}

export interface BlockCountRule {
    contentElementTypeKey: string;
    min?: number | null;
    max?: number | null;
}

export interface BlockLayoutItem {
    contentKey?: string;
    areas?: Array<{ items?: BlockLayoutItem[] }>;
}

export interface BlockValue {
    contentData?: Array<{ key?: string; contentTypeKey?: string }>;
    layout?: Record<string, BlockLayoutItem[] | undefined>;
}

export type BlockCountIssueKind = 'belowMinimum' | 'aboveMaximum' | 'exact' | 'invalidConfiguration';

export interface BlockCountIssue {
    contentElementTypeKey: string;
    blockName: string;
    message: string;
    kind: BlockCountIssueKind;
}

export function entryLabel(count: number): string {
    return count === 1 ? 'entry' : 'entries';
}

export function belowMinimumMessage(name: string, minimum: number, count: number): string {
    return `${name} requires at least ${minimum} ${entryLabel(minimum)}. The current count is ${count}.`;
}

export function aboveMaximumMessage(name: string, maximum: number, count: number): string {
    return `${name} allows at most ${maximum} ${entryLabel(maximum)}. The current count is ${count}.`;
}

export function exactMessage(name: string, expected: number, count: number): string {
    return `${name} must contain exactly ${expected} ${entryLabel(expected)}. The current count is ${count}.`;
}

export function minimumAboveMaximumMessage(name: string): string {
    return `The count configuration for ${name} is invalid. The minimum must be less than or equal to the maximum.`;
}

export function negativeMessage(name: string): string {
    return `The count configuration for ${name} is invalid. Minimum and maximum must be zero or greater.`;
}

export function blockCountValidationMessage(issues: readonly BlockCountIssue[]): string {
    return issues.map((issue) => escapeHtml(issue.message)).join('<br>');
}

function escapeHtml(value: string): string {
    return value
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;');
}

export function evaluateBlockCount(
    value: BlockValue | null | undefined,
    rules: BlockCountRule[],
    names?: ReadonlyMap<string, string>,
    editorAlias?: string,
): BlockCountIssue[] {
    const counts = countLayoutItems(value, editorAlias);
    const issues: BlockCountIssue[] = [];
    const seen = new Set<string>();

    for (const rule of rules) {
        const key = normalizeKey(rule.contentElementTypeKey);
        if (!key || seen.has(key)) {
            continue;
        }

        seen.add(key);
        const name = names?.get(key) || names?.get(rule.contentElementTypeKey) || 'Block';
        const min = normalizeLimit(rule.min);
        const max = normalizeLimit(rule.max);

        if ((min !== undefined && min < 0) || (max !== undefined && max < 0)) {
            issues.push(issue(key, name, 'invalidConfiguration', negativeMessage(name)));
            continue;
        }

        if (min !== undefined && max !== undefined && min > max) {
            issues.push(issue(key, name, 'invalidConfiguration', minimumAboveMaximumMessage(name)));
            continue;
        }

        if (min === undefined && max === undefined) {
            continue;
        }

        const count = counts.get(key) ?? 0;
        if (min !== undefined && max === min && count !== min) {
            issues.push(issue(key, name, 'exact', exactMessage(name, min, count)));
            continue;
        }

        if (min !== undefined && count < min) {
            issues.push(issue(key, name, 'belowMinimum', belowMinimumMessage(name, min, count)));
        }

        if (max !== undefined && count > max) {
            issues.push(issue(key, name, 'aboveMaximum', aboveMaximumMessage(name, max, count)));
        }
    }

    return issues;
}

export function countLayoutItems(value: BlockValue | null | undefined, editorAlias?: string): Map<string, number> {
    const counts = new Map<string, number>();
    const contentTypes = new Map<string, string>();

    for (const entry of value?.contentData ?? []) {
        if (!entry.key || !entry.contentTypeKey) {
            continue;
        }

        contentTypes.set(normalizeKey(entry.key), normalizeKey(entry.contentTypeKey));
    }

    const layout = value?.layout ?? {};
    for (const [alias, items] of Object.entries(layout)) {
        if (!Array.isArray(items)) {
            continue;
        }

        const normalizedAlias = alias.toLowerCase();
        const includeAreas = normalizedAlias === blockGridEditorAlias.toLowerCase();
        const isList = normalizedAlias === blockListEditorAlias.toLowerCase();
        if (!isList && !includeAreas) {
            continue;
        }

        if (editorAlias && normalizedAlias !== editorAlias.toLowerCase()) {
            continue;
        }

        walk(items, contentTypes, counts, includeAreas);
    }

    return counts;
}

function walk(
    items: BlockLayoutItem[],
    contentTypes: Map<string, string>,
    counts: Map<string, number>,
    includeAreas: boolean,
): void {
    for (const item of items) {
        const contentKey = item.contentKey ? normalizeKey(item.contentKey) : '';
        const contentTypeKey = contentKey ? contentTypes.get(contentKey) : undefined;
        if (contentTypeKey) {
            counts.set(contentTypeKey, (counts.get(contentTypeKey) ?? 0) + 1);
        }

        if (!includeAreas) {
            continue;
        }

        for (const area of item.areas ?? []) {
            if (area.items?.length) {
                walk(area.items, contentTypes, counts, includeAreas);
            }
        }
    }
}

function normalizeLimit(value: number | null | undefined): number | undefined {
    return value === null || value === undefined ? undefined : value;
}

function normalizeKey(value: string): string {
    return value.trim().toLowerCase();
}

function issue(contentElementTypeKey: string, blockName: string, kind: BlockCountIssueKind, message: string): BlockCountIssue {
    return { contentElementTypeKey, blockName, kind, message };
}
