// Records pptxgenjs drawing calls and renders an HTML preview (stand-in for LibreOffice Impress).
const fs = require("fs");
const pptxgen = require("pptxgenjs");
const TH = { dk1: "1B2A41", lt1: "FFFFFF", dk2: "0B1F3A", lt2: "EEF2F8", accent1: "F29F05", accent2: "0F8B8D", accent3: "B5303F", accent4: "3E5CA8", accent5: "4C9A6A", accent6: "6B7A90" };
const SCH = { tx1: "dk1", tx2: "dk2", bg1: "lt1", bg2: "lt2", text1: "dk1", text2: "dk2", background1: "lt1", background2: "lt2" };
const col = (c) => (c === undefined || c === null ? null : "#" + (TH[SCH[c] || c] || c));
const slides = [];
const masters = {};
const P = pptxgen.prototype;
const origAddSlide = P.addSlide;
const origMaster = P.defineSlideMaster;
P.defineSlideMaster = function (o) { masters[o.title] = o; return origMaster.call(this, o); };
P.addSlide = function (o) {
  const s = origAddSlide.call(this, o);
  const rec = { master: o && o.masterName, items: [] };
  slides.push(rec);
  for (const m of ["addText", "addShape", "addImage"]) {
    const f = s[m].bind(s);
    s[m] = (...a) => { rec.items.push([m, ...JSON.parse(JSON.stringify(a, (k, v) => (v && v.length > 5000 && typeof v === "string" ? v : v)))]); return f(...a); };
  }
  return s;
};
const origWrite = P.writeFile;
P.writeFile = async function (o) { await origWrite.call(this, o); render(); };

const px = (v) => (v * 100).toFixed(1) + "px";
function textHtml(text, o, pad) {
  const runs = Array.isArray(text) ? text : [{ text: String(text), options: {} }];
  const paras = [[]];
  runs.forEach((r) => {
    const ro = Object.assign({}, o, r.options || {});
    String(r.text).split("\n").forEach((part, i, arr) => {
      if (i > 0) paras.push([]);
      paras[paras.length - 1].push({ t: part, o: ro });
    });
    if (r.options && r.options.breakLine) paras.push([]);
  });
  if (paras.length && paras[paras.length - 1].length === 0 && paras.length > 1) paras.pop();
  return paras.map((p) => {
    const first = p[0] ? p[0].o : o;
    const bullet = first.bullet ? "• " : "";
    const sp = first.paraSpaceAfter ? first.paraSpaceAfter * 1.333 : 0;
    const inner = p.map((r) => `<span style="font-size:${(r.o.fontSize || 14) * 1.3333}px;font-weight:${r.o.bold ? 700 : 400};font-style:${r.o.italic ? "italic" : "normal"};color:${col(r.o.color) || "#000"}">${r.t.replace(/&/g, "&amp;").replace(/</g, "&lt;")}</span>`).join("") || "&nbsp;";
    return `<div style="margin-bottom:${sp}px;${bullet ? "padding-left:1.1em;text-indent:-1.1em" : ""}">${bullet}${inner}</div>`;
  }).join("");
}
function renderItem(it, master) {
  const [m, a1, a2] = it;
  let h = "";
  if (m === "addImage") {
    const o = a1;
    return `<img src="data:${o.data}" style="position:absolute;left:${px(o.x)};top:${px(o.y)};width:${px(o.w)};height:${px(o.h)}">`;
  }
  if (m === "addShape") {
    const type = a1, o = a2;
    if (type === "line") {
      const x = o.x, y = o.y, w = o.w, hh = o.h;
      const x1 = o.flipH ? w : 0, x2 = o.flipH ? 0 : w, y1 = o.flipV ? hh : 0, y2 = o.flipV ? 0 : hh;
      const c = col(o.line.color) || "#000";
      const id = "m" + Math.random().toString(36).slice(2, 8);
      const head = o.line.endArrowType ? `marker-end="url(#${id})"` : "";
      const dash = o.line.dashType === "dash" ? `stroke-dasharray="${(o.line.width || 1) * 4},${(o.line.width || 1) * 3}"` : "";
      return `<svg style="position:absolute;left:${px(x - 0.1)};top:${px(y - 0.1)};overflow:visible" width="${px(w + 0.2)}" height="${px(hh + 0.2)}"><defs><marker id="${id}" markerWidth="6" markerHeight="6" refX="5" refY="3" orient="auto" markerUnits="strokeWidth"><path d="M0,0 L6,3 L0,6 z" fill="${c}"/></marker></defs><line x1="${(x1 * 100 + 10)}" y1="${(y1 * 100 + 10)}" x2="${(x2 * 100 + 10)}" y2="${(y2 * 100 + 10)}" stroke="${c}" stroke-width="${(o.line.width || 1) * 1.333}" ${dash} ${head}/></svg>`;
    }
    return box(type, o, "", {});
  }
  if (m === "addText") {
    let text = a1, o = a2;
    if (o.placeholder) {
      const ph = (master.objects || []).find((x) => x.placeholder && x.placeholder.options.name === o.placeholder);
      if (!ph) return "";
      o = Object.assign({}, ph.placeholder.options, o);
      o.x = ph.placeholder.options.x; o.y = ph.placeholder.options.y; o.w = ph.placeholder.options.w; o.h = ph.placeholder.options.h;
    }
    const serif = a2.placeholder === "title";
    return box(o.shape || "rect", o, serif ? `<div class="t">${textHtml(text, o)}</div>` : textHtml(text, o), { text: true });
  }
  return h;
}
function box(type, o, inner, f) {
  const fill = o.fill && o.fill.type !== "none" && o.fill.color ? col(o.fill.color) : null;
  const tr = o.fill && o.fill.transparency ? 1 - o.fill.transparency / 100 : 1;
  const lineC = o.line && o.line.type !== "none" && o.line.color ? col(o.line.color) : null;
  let st = `position:absolute;left:${px(o.x)};top:${px(o.y)};width:${px(o.w)};height:${px(o.h)};box-sizing:border-box;`;
  if (fill) st += `background:${fill};opacity:1;`;
  if (fill && tr < 1) st += `background:${fill}${Math.round(tr * 255).toString(16).padStart(2, "0")};`;
  if (lineC) st += `border:${(o.line.width || 1) * 1.333}px ${o.line.dashType === "dash" ? "dashed" : "solid"} ${lineC};`;
  if (type === "roundRect") st += `border-radius:${Math.min(((o.rectRadius === undefined ? 0.1 : o.rectRadius) * 100), o.h * 50)}px;`;
  if (type === "ellipse") st += "border-radius:50%;";
  const dia = type === "diamond";
  const padPt = o.margin === undefined ? 7.2 : o.margin; // pptx default inset ~0.1in
  const pad = (Array.isArray(padPt) ? padPt[0] : padPt) * 1.3333;
  const al = o.align || "left", va = o.valign || "top";
  if (f.text) {
    st += `display:flex;flex-direction:column;justify-content:${va === "middle" ? "center" : va === "bottom" ? "flex-end" : "flex-start"};text-align:${al};padding:${pad}px;line-height:1.2;overflow:visible;`;
  }
  const wrapper = dia ? `<div style="${st}clip-path:polygon(50% 0,100% 50%,50% 100%,0 50%)">${f.text ? "" : ""}</div>` : "";
  if (dia) {
    return `<div style="position:absolute;left:${px(o.x)};top:${px(o.y)};width:${px(o.w)};height:${px(o.h)};background:${fill};clip-path:polygon(50% 0,100% 50%,50% 100%,0 50%)"></div><div style="${st.replace(/background:[^;]+;/, "")}">${inner}</div>`;
  }
  return `<div style="${st}">${inner}</div>`;
}
function render() {
  let html = `<!doctype html><meta charset=utf-8><style>body{margin:0;background:#888;font-family:'Liberation Sans',Arial,sans-serif}.s{position:relative;width:1333px;height:750px;margin:0 auto 24px;background:#fff;overflow:hidden;box-shadow:0 0 8px #000}.s .t{font-family:'Liberation Serif',Cambria,serif}</style>`;
  slides.forEach((s, i) => {
    const master = masters[s.master] || {};
    let inner = "";
    for (const ob of master.objects || []) {
      if (ob.rect) inner += box("rect", ob.rect.w === "100%" ? Object.assign({}, ob.rect, { w: 13.333 }) : ob.rect, "", {});
      if (ob.text) inner += box("rect", ob.text.options, textHtml(ob.text.text, ob.text.options), { text: true });
    }
    if (master.slideNumber) inner += box("rect", master.slideNumber, textHtml(String(i + 1), master.slideNumber), { text: true });
    for (const it of s.items) inner += renderItem(it, master);
    html += `<div class="s" id="s${i + 1}">${inner}</div>`;
  });
  fs.writeFileSync("preview.html", html);
  console.log("preview.html written,", slides.length, "slides");
}
require("./build.js");
