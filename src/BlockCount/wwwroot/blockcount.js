var W = (t) => {
  throw TypeError(t);
};
var A = (t, e, o) => e.has(t) || W("Cannot " + o);
var c = (t, e, o) => (A(t, e, "read from private field"), o ? o.call(t) : e.get(t)), y = (t, e, o) => e.has(t) ? W("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(t) : e.set(t, o), d = (t, e, o, i) => (A(t, e, "write to private field"), i ? i.call(t, o) : e.set(t, o), o), k = (t, e, o) => (A(t, e, "access private method"), o);
import { UMB_WORKSPACE_CONDITION_ALIAS as H } from "@umbraco-cms/backoffice/workspace";
import { UmbControllerBase as J } from "@umbraco-cms/backoffice/class-api";
import { UMB_DOCUMENT_TYPE_ITEM_REPOSITORY_ALIAS as Q } from "@umbraco-cms/backoffice/document-type";
import { UMB_PROPERTY_CONTEXT as Z, UMB_PROPERTY_DATASET_CONTEXT as j } from "@umbraco-cms/backoffice/property";
import { UmbRepositoryItemsManager as tt } from "@umbraco-cms/backoffice/repository";
import { UMB_VALIDATION_CONTEXT as V } from "@umbraco-cms/backoffice/validation";
import { css as et, customElement as ot, html as it } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as nt } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles as st } from "@umbraco-cms/backoffice/style";
const R = "Umbraco.BlockList", D = "Umbraco.BlockGrid";
function O(t) {
  return t === 1 ? "entry" : "entries";
}
function rt(t, e, o) {
  return `${t} requires at least ${e} ${O(e)}. The current count is ${o}.`;
}
function at(t, e, o) {
  return `${t} allows at most ${e} ${O(e)}. The current count is ${o}.`;
}
function ct(t, e, o) {
  return `${t} must contain exactly ${e} ${O(e)}. The current count is ${o}.`;
}
function ut(t) {
  return `The count configuration for ${t} is invalid. The minimum must be less than or equal to the maximum.`;
}
function N(t) {
  return `The count configuration for ${t} is invalid. Minimum and maximum must be zero or greater.`;
}
function lt(t, e, o, i) {
  const n = mt(t, i), s = [], a = /* @__PURE__ */ new Set();
  for (const u of e) {
    const r = M(u.contentElementTypeKey);
    if (!r || a.has(r))
      continue;
    a.add(r);
    const m = (o == null ? void 0 : o.get(r)) || (o == null ? void 0 : o.get(u.contentElementTypeKey)) || "Block", l = S(u.min), p = S(u.max);
    if (l !== void 0 && l < 0 || p !== void 0 && p < 0) {
      s.push(_(r, m, "invalidConfiguration", N(m)));
      continue;
    }
    if (l !== void 0 && p !== void 0 && l > p) {
      s.push(_(r, m, "invalidConfiguration", ut(m)));
      continue;
    }
    if (l === void 0 && p === void 0)
      continue;
    const f = n.get(r) ?? 0;
    if (l !== void 0 && p === l && f !== l) {
      s.push(_(r, m, "exact", ct(m, l, f)));
      continue;
    }
    l !== void 0 && f < l && s.push(_(r, m, "belowMinimum", rt(m, l, f))), p !== void 0 && f > p && s.push(_(r, m, "aboveMaximum", at(m, p, f)));
  }
  return s;
}
function mt(t, e) {
  const o = /* @__PURE__ */ new Map(), i = /* @__PURE__ */ new Map();
  for (const s of (t == null ? void 0 : t.contentData) ?? [])
    !s.key || !s.contentTypeKey || i.set(M(s.key), M(s.contentTypeKey));
  const n = (t == null ? void 0 : t.layout) ?? {};
  for (const [s, a] of Object.entries(n)) {
    if (!Array.isArray(a))
      continue;
    const u = s.toLowerCase(), r = u === D.toLowerCase();
    !(u === R.toLowerCase()) && !r || e && u !== e.toLowerCase() || G(a, i, o, r);
  }
  return o;
}
function G(t, e, o, i) {
  var n;
  for (const s of t) {
    const a = s.contentKey ? M(s.contentKey) : "", u = a ? e.get(a) : void 0;
    if (u && o.set(u, (o.get(u) ?? 0) + 1), !!i)
      for (const r of s.areas ?? [])
        (n = r.items) != null && n.length && G(r.items, e, o, i);
  }
}
function S(t) {
  return t ?? void 0;
}
function M(t) {
  return t.trim().toLowerCase();
}
function _(t, e, o, i) {
  return { contentElementTypeKey: t, blockName: e, kind: o, message: i };
}
const pt = {
  "Umb.PropertyEditorUi.BlockList": R,
  "Umb.PropertyEditorUi.BlockGrid": D
};
var w, C, T, v, B, g, E, h, b;
class yt extends J {
  constructor(o) {
    super(o);
    y(this, h);
    y(this, w);
    y(this, C, []);
    y(this, T);
    y(this, v);
    y(this, B, /* @__PURE__ */ new Map());
    y(this, g, /* @__PURE__ */ new Set());
    y(this, E, new tt(this, Q));
    this.consumeContext(V, (i) => {
      d(this, w, i), k(this, h, b).call(this);
    }), this.consumeContext(Z, (i) => {
      i && (this.observe(
        i.value,
        (n) => {
          d(this, T, n), k(this, h, b).call(this);
        },
        "blockCountValue"
      ), this.observe(
        i.config,
        (n) => {
          d(this, C, ht(n));
          const s = c(this, C).map((a) => a.contentElementTypeKey).filter((a) => !!a);
          c(this, E).setUniques(s), k(this, h, b).call(this);
        },
        "blockCountConfig"
      ), this.observe(
        i.editorManifest,
        (n) => {
          d(this, v, n != null && n.alias ? pt[n.alias] : void 0), k(this, h, b).call(this);
        },
        "blockCountEditor"
      ));
    }), this.observe(c(this, E).items, (i) => {
      d(this, B, new Map(i.map((n) => [n.unique.toLowerCase(), n.name || "Block"]))), k(this, h, b).call(this);
    });
  }
}
w = new WeakMap(), C = new WeakMap(), T = new WeakMap(), v = new WeakMap(), B = new WeakMap(), g = new WeakMap(), E = new WeakMap(), h = new WeakSet(), b = function() {
  const o = c(this, w);
  if (!o)
    return;
  for (const n of c(this, g))
    o.messages.removeMessageByKey(n);
  if (c(this, g).clear(), !c(this, v))
    return;
  const i = lt(c(this, T), c(this, C), c(this, B), c(this, v));
  for (const n of i) {
    const s = `blockCount:${n.kind}:${n.contentElementTypeKey}`;
    o.messages.addMessage("custom", "$", n.message, s), c(this, g).add(s);
  }
};
function ht(t) {
  return ft(t).filter((o) => !!o.contentElementTypeKey).map((o) => {
    var i, n;
    return {
      contentElementTypeKey: o.contentElementTypeKey,
      min: (i = o.blockCount) == null ? void 0 : i.min,
      max: (n = o.blockCount) == null ? void 0 : n.max
    };
  });
}
function ft(t) {
  if (!t || typeof t != "object" || !("getValueByAlias" in t))
    return [];
  const e = t.getValueByAlias("blocks");
  return Array.isArray(e) ? e : [];
}
var dt = Object.getOwnPropertyDescriptor, q = (t) => {
  throw TypeError(t);
}, kt = (t, e, o, i) => {
  for (var n = i > 1 ? void 0 : i ? dt(e, o) : e, s = t.length - 1, a; s >= 0; s--)
    (a = t[s]) && (n = a(n) || n);
  return n;
}, z = (t, e, o) => e.has(t) || q("Cannot " + o), $ = (t, e, o) => (z(t, e, "read from private field"), e.get(t)), K = (t, e, o) => e.has(t) ? q("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(t) : e.set(t, o), bt = (t, e, o, i) => (z(t, e, "write to private field"), e.set(t, o), o), Y = (t, e, o) => (z(t, e, "access private method"), o), x, U, L, X, F;
let P = class extends nt {
  constructor() {
    super(), K(this, L), K(this, x), K(this, U, "blockCount:negative"), this.consumeContext(V, (t) => {
      bt(this, x, t);
    }), this.consumeContext(j, (t) => {
      t && Y(this, L, X).call(this, t);
    });
  }
  render() {
    return it`
            <uui-box headline=${this.localize.term("blockCount_amount")}>
                <umb-property
                    .label=${this.localize.term("blockCount_amount")}
                    .description=${this.localize.term("blockCount_amountDescription")}
                    alias="blockCount"
                    property-editor-ui-alias="Umb.PropertyEditorUi.NumberRange">
                </umb-property>
            </uui-box>
        `;
  }
};
x = /* @__PURE__ */ new WeakMap();
U = /* @__PURE__ */ new WeakMap();
L = /* @__PURE__ */ new WeakSet();
X = async function(t) {
  const e = await t.propertyValueByAlias("blockCount");
  e && this.observe(e, (o) => Y(this, L, F).call(this, o), "blockCountConfiguration");
};
F = function(t) {
  const e = $(this, x);
  if (!e)
    return;
  e.messages.removeMessageByKey($(this, U));
  const o = t == null ? void 0 : t.min, i = t == null ? void 0 : t.max;
  (o != null && o < 0 || i != null && i < 0) && e.messages.addMessage("custom", "$", N("this block"), $(this, U));
};
P.styles = [
  st,
  et`
            :host {
                display: block;
                margin: var(--uui-size-layout-1);
                padding-bottom: var(--uui-size-layout-1);
            }

            uui-box {
                margin-top: var(--uui-size-layout-1);
            }
        `
];
P = kt([
  ot("block-count-workspace-view")
], P);
const Ct = "Umb.Workspace.BlockListType", vt = "Umb.Workspace.BlockGridType", gt = [
  {
    type: "localization",
    alias: "BlockCount.Localization.En",
    name: "BlockCount English",
    meta: {
      culture: "en",
      localizations: {
        blockCount: {
          amount: "Amount",
          amountDescription: "Minimum and maximum number of this block type. Leave either value empty for no limit. Set both to the same number for an exact count.",
          count: "Count"
        }
      }
    }
  },
  I("BlockCount.WorkspaceView.BlockListType", Ct),
  I("BlockCount.WorkspaceView.BlockGridType", vt),
  {
    type: "propertyContext",
    alias: "BlockCount.PropertyContext",
    name: "Block Count Property Context",
    api: yt,
    forPropertyEditorUis: ["Umb.PropertyEditorUi.BlockList", "Umb.PropertyEditorUi.BlockGrid"],
    meta: {}
  }
];
function I(t, e) {
  return {
    type: "workspaceView",
    alias: t,
    name: "Block Count",
    elementName: "block-count-workspace-view",
    weight: 900,
    meta: {
      label: "#blockCount_count",
      pathname: "count",
      icon: "icon-list"
    },
    conditions: [
      {
        alias: H,
        match: e
      }
    ]
  };
}
const $t = (t, e) => {
  e.registerMany(gt);
};
export {
  gt as manifests,
  $t as onInit
};
//# sourceMappingURL=blockcount.js.map
