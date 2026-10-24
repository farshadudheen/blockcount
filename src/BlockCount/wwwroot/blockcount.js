var I = (t) => {
  throw TypeError(t);
};
var P = (t, e, o) => e.has(t) || I("Cannot " + o);
var s = (t, e, o) => (P(t, e, "read from private field"), o ? o.call(t) : e.get(t)), d = (t, e, o) => e.has(t) ? I("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(t) : e.set(t, o), f = (t, e, o, i) => (P(t, e, "write to private field"), i ? i.call(t, o) : e.set(t, o), o), k = (t, e, o) => (P(t, e, "access private method"), o);
import { UMB_WORKSPACE_CONDITION_ALIAS as J } from "@umbraco-cms/backoffice/workspace";
import { UmbControllerBase as Q } from "@umbraco-cms/backoffice/class-api";
import { UMB_DOCUMENT_TYPE_ITEM_REPOSITORY_ALIAS as Z } from "@umbraco-cms/backoffice/document-type";
import { UMB_PROPERTY_CONTEXT as tt, UMB_PROPERTY_DATASET_CONTEXT as et } from "@umbraco-cms/backoffice/property";
import { UmbRepositoryItemsManager as ot } from "@umbraco-cms/backoffice/repository";
import { UMB_VALIDATION_CONTEXT as it } from "@umbraco-cms/backoffice/validation";
import { css as nt, customElement as rt, html as st } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as at } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles as ct } from "@umbraco-cms/backoffice/style";
const D = "Umbraco.BlockList", N = "Umbraco.BlockGrid";
function z(t) {
  return t === 1 ? "entry" : "entries";
}
function lt(t, e, o) {
  return `${t} requires at least ${e} ${z(e)}. The current count is ${o}.`;
}
function ut(t, e, o) {
  return `${t} allows at most ${e} ${z(e)}. The current count is ${o}.`;
}
function mt(t, e, o) {
  return `${t} must contain exactly ${e} ${z(e)}. The current count is ${o}.`;
}
function pt(t) {
  return `The count configuration for ${t} is invalid. The minimum must be less than or equal to the maximum.`;
}
function G(t) {
  return `The count configuration for ${t} is invalid. Minimum and maximum must be zero or greater.`;
}
function ht(t) {
  return t.map((e) => dt(e.message)).join("<br>");
}
function dt(t) {
  return t.replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#39;");
}
function ft(t, e, o, i) {
  const n = yt(t, i), r = [], c = /* @__PURE__ */ new Set();
  for (const u of e) {
    const a = A(u.contentElementTypeKey);
    if (!a || c.has(a))
      continue;
    c.add(a);
    const p = (o == null ? void 0 : o.get(a)) || (o == null ? void 0 : o.get(u.contentElementTypeKey)) || "Block", m = S(u.min), y = S(u.max);
    if (m !== void 0 && m < 0 || y !== void 0 && y < 0) {
      r.push(w(a, p, "invalidConfiguration", G(p)));
      continue;
    }
    if (m !== void 0 && y !== void 0 && m > y) {
      r.push(w(a, p, "invalidConfiguration", pt(p)));
      continue;
    }
    if (m === void 0 && y === void 0)
      continue;
    const v = n.get(a) ?? 0;
    if (m !== void 0 && y === m && v !== m) {
      r.push(w(a, p, "exact", mt(p, m, v)));
      continue;
    }
    m !== void 0 && v < m && r.push(w(a, p, "belowMinimum", lt(p, m, v))), y !== void 0 && v > y && r.push(w(a, p, "aboveMaximum", ut(p, y, v)));
  }
  return r;
}
function yt(t, e) {
  const o = /* @__PURE__ */ new Map(), i = /* @__PURE__ */ new Map();
  for (const r of (t == null ? void 0 : t.contentData) ?? [])
    !r.key || !r.contentTypeKey || i.set(A(r.key), A(r.contentTypeKey));
  const n = (t == null ? void 0 : t.layout) ?? {};
  for (const [r, c] of Object.entries(n)) {
    if (!Array.isArray(c))
      continue;
    const u = r.toLowerCase(), a = u === N.toLowerCase();
    !(u === D.toLowerCase()) && !a || e && u !== e.toLowerCase() || q(c, i, o, a);
  }
  return o;
}
function q(t, e, o, i) {
  var n;
  for (const r of t) {
    const c = r.contentKey ? A(r.contentKey) : "", u = c ? e.get(c) : void 0;
    if (u && o.set(u, (o.get(u) ?? 0) + 1), !!i)
      for (const a of r.areas ?? [])
        (n = a.items) != null && n.length && q(a.items, e, o, i);
  }
}
function S(t) {
  return t ?? void 0;
}
function A(t) {
  return t.trim().toLowerCase();
}
function w(t, e, o, i) {
  return { contentElementTypeKey: t, blockName: e, kind: o, message: i };
}
const kt = {
  "Umb.PropertyEditorUi.BlockList": D,
  "Umb.PropertyEditorUi.BlockGrid": N
};
var _, T, g, B, E, h, b, M, l, Y, K, C;
class bt extends Q {
  constructor(o) {
    super(o);
    d(this, l);
    d(this, _, []);
    d(this, T);
    d(this, g);
    d(this, B, /* @__PURE__ */ new Map());
    d(this, E, []);
    d(this, h);
    d(this, b);
    d(this, M, new ot(this, Z));
    this.consumeContext(tt, (i) => {
      i && (this.observe(
        i.value,
        (n) => {
          f(this, T, n), k(this, l, C).call(this);
        },
        "blockCountValue"
      ), this.observe(
        i.config,
        (n) => {
          f(this, _, Ct(n));
          const r = s(this, _).map((c) => c.contentElementTypeKey).filter((c) => !!c);
          s(this, M).setUniques(r), k(this, l, C).call(this);
        },
        "blockCountConfig"
      ), this.observe(
        i.editorManifest,
        (n) => {
          f(this, g, n != null && n.alias ? kt[n.alias] : void 0), k(this, l, C).call(this);
        },
        "blockCountEditor"
      ), this.observe(
        i.editor,
        (n) => {
          k(this, l, Y).call(this, n), k(this, l, C).call(this);
        },
        "blockCountEditorElement"
      ));
    }), this.observe(s(this, M).items, (i) => {
      f(this, B, new Map(i.map((n) => [n.unique.toLowerCase(), n.name || "Block"]))), k(this, l, C).call(this);
    });
  }
  destroy() {
    k(this, l, K).call(this), super.destroy();
  }
}
_ = new WeakMap(), T = new WeakMap(), g = new WeakMap(), B = new WeakMap(), E = new WeakMap(), h = new WeakMap(), b = new WeakMap(), M = new WeakMap(), l = new WeakSet(), Y = function(o) {
  const i = vt(o) ? o : void 0;
  s(this, h) !== i && (k(this, l, K).call(this), i && (f(this, h, i), f(this, b, i.addValidator(
    "customError",
    () => ht(s(this, E)),
    () => s(this, E).length > 0
  ))));
}, K = function() {
  s(this, h) && s(this, b) && s(this, h).removeValidator(s(this, b)), f(this, h, void 0), f(this, b, void 0);
}, C = function() {
  f(this, E, s(this, g) ? ft(s(this, T), s(this, _), s(this, B), s(this, g)) : []), s(this, h) && s(this, h).pristine === !1 && s(this, h).checkValidity();
};
function vt(t) {
  if (!t || typeof t != "object")
    return !1;
  const e = t;
  return typeof e.addValidator == "function" && typeof e.removeValidator == "function" && typeof e.checkValidity == "function";
}
function Ct(t) {
  return _t(t).filter((o) => !!o.contentElementTypeKey).map((o) => {
    var i, n;
    return {
      contentElementTypeKey: o.contentElementTypeKey,
      min: (i = o.blockCount) == null ? void 0 : i.min,
      max: (n = o.blockCount) == null ? void 0 : n.max
    };
  });
}
function _t(t) {
  if (!t || typeof t != "object" || !("getValueByAlias" in t))
    return [];
  const e = t.getValueByAlias("blocks");
  return Array.isArray(e) ? e : [];
}
var gt = Object.getOwnPropertyDescriptor, X = (t) => {
  throw TypeError(t);
}, Et = (t, e, o, i) => {
  for (var n = i > 1 ? void 0 : i ? gt(e, o) : e, r = t.length - 1, c; r >= 0; r--)
    (c = t[r]) && (n = c(n) || n);
  return n;
}, W = (t, e, o) => e.has(t) || X("Cannot " + o), V = (t, e, o) => (W(t, e, "read from private field"), e.get(t)), $ = (t, e, o) => e.has(t) ? X("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(t) : e.set(t, o), wt = (t, e, o, i) => (W(t, e, "write to private field"), e.set(t, o), o), j = (t, e, o) => (W(t, e, "access private method"), o), U, x, L, H, F;
let O = class extends at {
  constructor() {
    super(), $(this, L), $(this, U), $(this, x, "blockCount:negative"), this.consumeContext(it, (t) => {
      wt(this, U, t);
    }), this.consumeContext(et, (t) => {
      t && j(this, L, H).call(this, t);
    });
  }
  render() {
    return st`
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
U = /* @__PURE__ */ new WeakMap();
x = /* @__PURE__ */ new WeakMap();
L = /* @__PURE__ */ new WeakSet();
H = async function(t) {
  const e = await t.propertyValueByAlias("blockCount");
  e && this.observe(e, (o) => j(this, L, F).call(this, o), "blockCountConfiguration");
};
F = function(t) {
  const e = V(this, U);
  if (!e)
    return;
  e.messages.removeMessageByKey(V(this, x));
  const o = t == null ? void 0 : t.min, i = t == null ? void 0 : t.max;
  (o != null && o < 0 || i != null && i < 0) && e.messages.addMessage("custom", "$", G("this block"), V(this, x));
};
O.styles = [
  ct,
  nt`
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
O = Et([
  rt("block-count-workspace-view")
], O);
const Tt = "Umb.Workspace.BlockListType", Bt = "Umb.Workspace.BlockGridType", Mt = [
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
  R("BlockCount.WorkspaceView.BlockListType", Tt),
  R("BlockCount.WorkspaceView.BlockGridType", Bt),
  {
    type: "propertyContext",
    alias: "BlockCount.PropertyContext",
    name: "Block Count Property Context",
    api: bt,
    forPropertyEditorUis: ["Umb.PropertyEditorUi.BlockList", "Umb.PropertyEditorUi.BlockGrid"],
    meta: {}
  }
];
function R(t, e) {
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
        alias: J,
        match: e
      }
    ]
  };
}
const Wt = (t, e) => {
  e.registerMany(Mt);
};
export {
  Mt as manifests,
  Wt as onInit
};
//# sourceMappingURL=blockcount.js.map
