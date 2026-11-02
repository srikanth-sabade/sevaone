// SevaOne architecture deck: High-level -> Solution flow -> Technical flow
const pptxgen = require("pptxgenjs");
const React = require("react");
const ReactDOMServer = require("react-dom/server");
const sharp = require("sharp");
const fa = require("react-icons/fa");
const md = require("react-icons/md");
const SKILL = process.env.PPTX_SKILL;
const { applyTheme } = require(SKILL + "/scripts/apply_theme.js");

// ---------- theme ----------
const THEME = {
  name: "SevaOne",
  headFontFace: "Cambria",
  bodyFontFace: "Calibri",
  colors: {
    dk1: "1B2A41", lt1: "FFFFFF", dk2: "0B1F3A", lt2: "EEF2F8",
    accent1: "F29F05", accent2: "0F8B8D", accent3: "B5303F",
    accent4: "3E5CA8", accent5: "4C9A6A", accent6: "6B7A90",
    hlink: "3E5CA8", folHlink: "6B7A90",
  },
};
const HEX = THEME.colors;

const pres = new pptxgen();
pres.layout = "LAYOUT_WIDE"; // 13.33 x 7.5
pres.theme = { headFontFace: THEME.headFontFace, bodyFontFace: THEME.bodyFontFace };
pres.title = "SevaOne - Architecture, Solution Flow and Technical Flow";
pres.author = "SevaOne";
const C = pres.SchemeColor;
const S = pres.ShapeType;

// ---------- icons ----------
async function ic(Comp, hex, px = 512) {
  if (!Comp) throw new Error("missing icon");
  const svg = ReactDOMServer.renderToStaticMarkup(React.createElement(Comp, { color: "#" + hex, size: px }));
  const buf = await sharp(Buffer.from(svg)).png().toBuffer();
  return "image/png;base64," + buf.toString("base64");
}
const I = {}; // filled in main()

// ---------- layouts ----------
function defineLayouts() {
  pres.defineSlideMaster({
    title: "TITLE",
    objects: [
      { rect: { x: 0, y: 0, w: 13.333, h: 7.5, fill: { color: C.text2 } } },
      { placeholder: { options: { name: "title", type: "title", x: 0.8, y: 2.2, w: 7.2, h: 1.5, fontSize: 66, bold: true, color: C.background1, valign: "middle", margin: 0 }, text: "" } },
      { placeholder: { options: { name: "body", type: "body", x: 0.8, y: 3.85, w: 6.6, h: 1.3, fontSize: 20, color: C.background2, valign: "top", margin: 0 }, text: "" } },
    ],
  });
  pres.defineSlideMaster({
    title: "SECTION",
    objects: [
      { rect: { x: 0, y: 0, w: 13.333, h: 7.5, fill: { color: C.text2 } } },
      { placeholder: { options: { name: "title", type: "title", x: 0.8, y: 2.9, w: 8.2, h: 1.1, fontSize: 48, bold: true, color: C.background1, valign: "middle", margin: 0 }, text: "" } },
      { placeholder: { options: { name: "body", type: "body", x: 0.8, y: 4.1, w: 7.4, h: 1.2, fontSize: 20, color: C.background2, valign: "top", margin: 0 }, text: "" } },
    ],
  });
  pres.defineSlideMaster({
    title: "CONTENT",
    background: { color: "FFFFFF" },
    objects: [
      { placeholder: { options: { name: "title", type: "title", x: 0.6, y: 0.4, w: 12.13, h: 0.8, fontSize: 32, bold: true, color: C.text2, valign: "middle", margin: 0 }, text: "" } },
      { text: { text: "SevaOne  |  Architecture and flows", options: { x: 0.6, y: 7.02, w: 6, h: 0.3, fontSize: 10, color: C.accent6, margin: 0, isTextBox: true } } },
    ],
    slideNumber: { x: 12.0, y: 7.02, w: 0.73, h: 0.3, fontSize: 10, color: C.accent6, align: "right" },
  });
}

// ---------- drawing primitives ----------
let n = 0;
const nm = (p) => `${p}-${++n}`;

function txt(s, text, x, y, w, h, o = {}) {
  s.addText(text, Object.assign({ x, y, w, h, margin: 0, fontSize: 14, color: C.text1, valign: "top", isTextBox: true, objectName: nm("text") }, o));
}

// rounded node with title (bold) + optional sub text
function node(s, x, y, w, h, title, sub, o = {}) {
  const fill = o.fill || C.background2;
  const fg = o.fg || C.text1;
  const runs = [{ text: title, options: { bold: true, fontSize: o.tsz || 14, color: fg, breakLine: !!sub } }];
  if (sub) runs.push({ text: sub, options: { fontSize: o.ssz || 12, color: o.sfg || fg } });
  s.addText(runs, {
    shape: o.shape || S.roundRect, x, y, w, h, rectRadius: o.r === undefined ? 0.1 : o.r,
    fill: o.noFill ? undefined : { color: fill },
    line: o.line ? { color: o.line, width: o.lw || 1.25, dashType: o.dash || "solid" } : { type: "none" },
    align: o.align || "center", valign: "middle", margin: (o.margin === undefined ? 0.08 : o.margin) * 72,
    isTextBox: true, objectName: nm("node"),
  });
}

function arrow(s, x1, y1, x2, y2, o = {}) {
  const color = o.color || C.accent6;
  s.addShape(S.line, {
    x: Math.min(x1, x2), y: Math.min(y1, y2), w: Math.abs(x2 - x1), h: Math.abs(y2 - y1),
    flipH: x2 < x1, flipV: y2 < y1,
    line: { color, width: o.w || 1.75, dashType: o.dash || "solid", endArrowType: o.head === false ? undefined : "triangle" },
    objectName: nm("arrow"),
  });
}
// polyline; arrowhead on final segment
function path(s, pts, o = {}) {
  for (let i = 0; i < pts.length - 1; i++) {
    const last = i === pts.length - 2;
    arrow(s, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1], Object.assign({}, o, { head: last && o.head !== false }));
  }
}
function label(s, text, cx, cy, w, o = {}) {
  const h = o.h || 0.28;
  s.addText(text, {
    x: cx - w / 2, y: cy - h / 2, w, h, margin: 1.5, fontSize: o.sz || 11, color: o.color || C.text1,
    bold: !!o.bold, italic: !!o.italic, align: "center", valign: "middle",
    fill: o.fill === null ? undefined : { color: o.fill || C.background1 }, isTextBox: true, objectName: nm("label"),
  });
}
function img(s, data, x, y, w, h, alt) {
  s.addImage({ data, x, y, w, h, altText: alt, objectName: nm("icon") });
}
// icon inside colored circle
function badge(s, data, cx, cy, d, color, alt) {
  s.addShape(S.ellipse, { x: cx - d / 2, y: cy - d / 2, w: d, h: d, fill: { color }, line: { type: "none" }, objectName: nm("badge") });
  const p = d * 0.52;
  img(s, data, cx - p / 2, cy - p / 2, p, p, alt);
}
function numDot(s, num, cx, cy, d = 0.3, color = C.accent1, fg = C.text2) {
  s.addText(String(num), { shape: S.ellipse, x: cx - d / 2, y: cy - d / 2, w: d, h: d, fill: { color }, line: { type: "none" },
    fontSize: 11, bold: true, color: fg, align: "center", valign: "middle", margin: 0, isTextBox: true, objectName: nm("num") });
}
function slide(title, sec) {
  const s = pres.addSlide({ masterName: "CONTENT", sectionTitle: sec });
  s.addText(title, { placeholder: "title" });
  return s;
}

// sequence diagram: participants [{label, sub, icon, size}], rows [{from,to,text,dash,self,note,band,num}]
function sequence(s, o) {
  const { x0, x1, top, parts, rows, rowH } = o;
  const pitch = (x1 - x0) / (parts.length - 1);
  const px = parts.map((_, i) => x0 + i * pitch);
  const headH = 1.0;
  const bottom = top + headH + rows.length * rowH + 0.1;
  parts.forEach((p, i) => {
    const sz = p.size || 0.6;
    img(s, p.icon, px[i] - sz / 2, top + (0.62 - sz) / 2 + (p.dy || 0), sz, sz, p.label);
    txt(s, p.label, px[i] - pitch / 2 + 0.05, top + 0.64, pitch - 0.1, 0.3, { align: "center", bold: true, fontSize: 12, color: C.text2 });
    s.addShape(S.line, { x: px[i], y: top + headH, w: 0, h: bottom - top - headH, line: { color: C.accent6, width: 1, dashType: "dash" }, objectName: nm("lifeline") });
  });
  let y = top + headH + 0.12;
  let k = 0;
  for (const r of rows) {
    const cy = y + rowH / 2;
    if (r.band) {
      s.addText(r.band, { shape: S.rect, x: x0 - 0.35, y: y + 0.02, w: x1 - x0 + 0.7, h: rowH - 0.04, fill: { color: C.background2 }, line: { type: "none" },
        fontSize: 11, bold: true, italic: true, color: C.accent4, align: "left", valign: "middle", margin: 7, isTextBox: true, objectName: nm("band") });
    } else if (r.note) {
      const a = px[r.cols[0]] - 0.25, b = px[r.cols[1]] + 0.25;
      s.addText(r.note, { shape: S.roundRect, rectRadius: 0.06, x: a, y: y + 0.03, w: b - a, h: rowH - 0.06, fill: { color: C.accent1 }, line: { type: "none" },
        fontSize: 11, color: C.text2, bold: true, align: "center", valign: "middle", margin: 3, isTextBox: true, objectName: nm("note") });
    } else if (r.self) {
      const xs = px[r.from];
      path(s, [[xs, cy - rowH * 0.25], [xs + 0.55, cy - rowH * 0.25], [xs + 0.55, cy + rowH * 0.25], [xs, cy + rowH * 0.25]], { color: r.color || C.accent4, dash: r.dash ? "dash" : "solid" });
      txt(s, r.text, xs + 0.65, cy - rowH * 0.4, 3.2, rowH * 0.8, { fontSize: 11, valign: "middle" });
      numDot(s, ++k, x0 - 0.6, cy);
    } else {
      const a = px[r.from], b = px[r.to];
      arrow(s, a, cy + 0.07, b, cy + 0.07, { color: r.color || (r.dash ? C.accent6 : C.accent4), dash: r.dash ? "dash" : "solid", w: 1.75 });
      const lo = Math.min(a, b), w = Math.abs(b - a);
      txt(s, r.text, lo + 0.08, y + 0.0, w - 0.16, rowH * 0.62, { align: "center", valign: "bottom", fontSize: 11, bold: false });
      numDot(s, ++k, x0 - 0.6, cy);
    }
    y += rowH;
  }
  return { px, bottom };
}

// ======================================================================
async function main() {
  defineLayouts();
  const A = HEX; // hex palette for icon colours
  I.citizenDk = await ic(md.MdPerson, A.dk2);
  I.citizenGold = await ic(md.MdPerson, A.accent1);
  I.citizenTeal = await ic(md.MdPerson, A.accent2);
  I.operator = await ic(md.MdSupportAgent, A.accent4);
  I.gov = await ic(md.MdAccountBalance, A.accent3);
  I.govDk = await ic(md.MdAccountBalance, A.dk2);
  I.dept = await ic(md.MdBusiness, A.accent5);
  I.deptWhite = await ic(md.MdBusiness, "FFFFFF");
  I.admin = await ic(md.MdAdminPanelSettings, A.accent3);
  I.platform = await ic(fa.FaLayerGroup, A.accent2);
  const W = (c) => ic(c, "FFFFFF");
  I.key = await W(fa.FaKey); I.shield = await W(fa.FaShieldAlt); I.db = await W(fa.FaDatabase);
  I.server = await W(fa.FaServer); I.search = await W(fa.FaSearch); I.sign = await W(fa.FaFileSignature);
  I.robot = await W(fa.FaRobot); I.chart = await W(fa.FaChartLine); I.bell = await W(fa.FaBell);
  I.route = await W(fa.FaRoute); I.layers = await W(fa.FaLayerGroup); I.cogs = await W(fa.FaCogs);
  I.login = await W(fa.FaSignInAlt); I.clip = await W(fa.FaClipboardCheck); I.building = await W(fa.FaBuilding);
  I.fingerprint = await W(fa.FaFingerprint); I.exchange = await W(fa.FaExchangeAlt); I.net = await W(fa.FaNetworkWired);
  I.lock = await W(fa.FaLock); I.cubes = await W(fa.FaCubes); I.cloud = await W(fa.FaCloud);
  I.laptop = await W(fa.FaLaptop); I.stream = await W(fa.FaStream); I.users = await W(fa.FaUsers);
  I.rocket = await W(fa.FaRocket); I.id = await W(fa.FaIdCard); I.sms = await W(fa.FaMobileAlt);
  // dark-coloured sequence participants (on white)
  const D = (c, hex) => ic(c, hex);
  I.s_browser = await D(fa.FaLaptop, A.dk2); I.s_gw = await D(fa.FaNetworkWired, A.accent4);
  I.s_bff = await D(fa.FaServer, A.accent2); I.s_is = await D(fa.FaKey, A.accent1);
  I.s_dl = await D(fa.FaIdCard, A.accent3); I.s_dept = await D(md.MdBusiness, A.accent5);
  I.s_ai = await D(fa.FaRobot, A.accent4); I.s_idx = await D(fa.FaDatabase, A.accent6);
  I.s_portal = await D(fa.FaLaptop, A.accent4); I.s_gpu = await D(fa.FaMicrochip || fa.FaServer, A.accent3);

  // ============================ OPENING ============================
  pres.addSection({ title: "Opening" });
  {
    const s = pres.addSlide({ masterName: "TITLE", sectionTitle: "Opening" });
    s.addText("SevaOne", { placeholder: "title" });
    s.addText("One platform for citizen services: high-level architecture, solution flow and technical flow", { placeholder: "body" });
    txt(s, "ARCHITECTURE REVIEW  |  OCTOBER 2026", 0.8, 1.6, 7, 0.35, { fontSize: 14, bold: true, color: C.accent1 });
    // giant citizen icon
    s.addShape(S.ellipse, { x: 8.0, y: 1.25, w: 4.7, h: 4.7, fill: { color: C.accent4, transparency: 82 }, line: { type: "none" }, objectName: "title-halo" });
    img(s, I.citizenGold, 8.45, 1.55, 3.8, 3.8, "Citizen");
    txt(s, "Built around the citizen", 8.0, 6.15, 4.7, 0.4, { align: "center", fontSize: 18, bold: true, color: C.background1 });
  }
  {
    const s = slide("What this deck covers", "Opening");
    const items = [
      ["1", "High-level architecture", "Who uses SevaOne, the building blocks, and how the platform is laid out and hosted.", I.layers, C.accent2],
      ["2", "Solution flow", "The citizen journey in plain language: sign in, discover, apply, complete, return.", I.route, C.accent1],
      ["3", "Technical flow", "Request-level sequences, security layers, internals, data and deployment.", I.cogs, C.accent4],
    ];
    items.forEach((it, i) => {
      const x = 0.6 + i * 4.1;
      s.addShape(S.roundRect, { x, y: 1.7, w: 3.9, h: 4.6, rectRadius: 0.12, fill: { color: C.background2 }, line: { type: "none" }, objectName: nm("card") });
      badge(s, it[3], x + 0.85, 2.85, 1.2, it[4], it[1]);
      txt(s, it[0], x + 2.9, 2.0, 0.8, 1.2, { fontSize: 72, bold: true, color: C.accent6, align: "right" });
      txt(s, it[1], x + 0.35, 3.8, 3.2, 0.5, { fontSize: 22, bold: true, color: C.text2 });
      txt(s, it[2], x + 0.35, 4.4, 3.2, 1.6, { fontSize: 15 });
    });
  }

  // ============================ PART 1 : HIGH LEVEL ============================
  pres.addSection({ title: "1  High-level architecture" });
  {
    const s = pres.addSlide({ masterName: "SECTION", sectionTitle: "1  High-level architecture" });
    s.addText("High-level architecture", { placeholder: "title" });
    s.addText("The people, the platform and the systems around it.", { placeholder: "body" });
    txt(s, "01", 9.2, 1.7, 3.6, 2.2, { fontSize: 140, bold: true, color: C.accent1, align: "right" });
    badge(s, I.layers, 11.2, 5.0, 1.5, C.accent2, "Layers");
  }
  const P1 = "1  High-level architecture";
  {
    // ---- platform on one page
    const s = slide("The platform on one page", P1);
    // actors
    img(s, I.citizenDk, 0.55, 1.45, 1.55, 1.55, "Citizen");
    txt(s, "Citizen", 0.35, 3.0, 1.95, 0.3, { align: "center", bold: true, fontSize: 15, color: C.text2 });
    img(s, I.operator, 0.95, 3.8, 0.8, 0.8, "Kiosk operator");
    txt(s, "Kiosk operator", 0.35, 4.62, 1.95, 0.3, { align: "center", bold: true, fontSize: 12, color: C.text2 });
    img(s, I.gov, 0.95, 5.25, 0.8, 0.8, "Government admin");
    txt(s, "Government admin", 0.35, 6.07, 1.95, 0.3, { align: "center", bold: true, fontSize: 12, color: C.text2 });
    // portal + admin
    node(s, 2.95, 1.6, 2.0, 3.1, "Citizen portal", "React, Vite, TanStack Router\nCitizens and assisted-mode operators", { fill: C.accent4, fg: C.background1, tsz: 16, ssz: 12 });
    node(s, 2.95, 5.0, 2.0, 1.1, "Admin panel", "React, five government tiers", { fill: C.accent3, fg: C.background1, tsz: 15, ssz: 12 });
    arrow(s, 2.15, 2.3, 2.95, 2.3); arrow(s, 1.8, 4.2, 2.95, 4.2); arrow(s, 1.8, 5.65, 2.95, 5.65);
    // dept + apim
    node(s, 5.4, 1.5, 2.0, 0.85, "Department systems", "own backend and login", { fill: C.accent5, fg: C.background1, tsz: 13, ssz: 11 });
    s.addShape(S.roundRect, { x: 5.4, y: 2.75, w: 2.0, h: 2.35, rectRadius: 0.1, fill: { color: C.accent2 }, line: { type: "none" }, objectName: nm("apim") });
    txt(s, "WSO2 API Manager", 5.4, 2.85, 2.0, 0.3, { align: "center", bold: true, fontSize: 14, color: C.background1 });
    node(s, 5.55, 3.3, 1.7, 0.75, "sevaone-citizen", "citizen API product", { fill: C.background1, fg: C.text2, tsz: 12, ssz: 10 });
    node(s, 5.55, 4.2, 1.7, 0.75, "sevaone", "department API product", { fill: C.background1, fg: C.text2, tsz: 12, ssz: 10 });
    arrow(s, 4.95, 3.65, 5.55, 3.65); label(s, "/api", 5.25, 3.45, 0.5);
    arrow(s, 6.4, 2.35, 6.4, 2.75); label(s, "/dept/*", 6.4, 2.55, 0.7);
    // BFF
    s.addShape(S.roundRect, { x: 7.9, y: 1.5, w: 2.45, h: 4.6, rectRadius: 0.1, fill: { color: C.text2 }, line: { type: "none" }, objectName: nm("bff") });
    txt(s, ".NET backend (BFF)", 7.9, 1.6, 2.45, 0.3, { align: "center", bold: true, fontSize: 15, color: C.accent1 });
    ["Controllers and auth", "Business services", "Infrastructure clients\nand Dapper repositories", "Background workers"].forEach((t, i) => {
      node(s, 8.1, 2.1 + i * 0.98, 2.05, 0.8, t, null, { fill: C.accent4, fg: C.background1, tsz: 12 });
    });
    arrow(s, 7.4, 3.9, 7.9, 3.9);
    arrow(s, 4.95, 5.55, 7.9, 5.55); label(s, "/api/v1/admin/gov", 6.4, 5.55, 1.5);
    // data
    node(s, 10.85, 1.5, 1.9, 1.5, "PostgreSQL", "6 schemas\nfunction-only access", { fill: C.accent5, fg: C.background1, tsz: 14, ssz: 11 });
    node(s, 10.85, 3.2, 1.9, 1.15, "Redis", "sessions and token cache", { fill: C.accent3, fg: C.background1, tsz: 14, ssz: 11 });
    node(s, 10.85, 4.55, 1.9, 1.55, "Kafka", "outbox events", { fill: C.accent1, fg: C.text2, tsz: 14, ssz: 11 });
    arrow(s, 10.35, 2.25, 10.85, 2.25); arrow(s, 10.35, 3.77, 10.85, 3.77); arrow(s, 10.35, 5.3, 10.85, 5.3);
    // externals band
    txt(s, "External services", 2.95, 6.35, 2.2, 0.4, { bold: true, fontSize: 13, color: C.text2, valign: "middle" });
    ["WSO2 Identity Server", "DigiLocker", "AI service (RAG)", "Sakala / SevaSindhu", "SMS gateway"].forEach((t, i) => {
      node(s, 5.3 + i * 1.5, 6.3, 1.4, 0.55, t, null, { fill: C.background2, fg: C.text2, tsz: 11, line: C.accent6, lw: 0.75, margin: 0.04 });
    });
    arrow(s, 9.1, 6.1, 9.1, 6.3, { head: true });
  }
  {
    // ---- who uses
    const s = slide("Who uses SevaOne", P1);
    img(s, I.citizenDk, 0.8, 1.55, 3.0, 3.0, "Citizen");
    txt(s, "Citizen", 0.6, 4.65, 3.4, 0.5, { align: "center", bold: true, fontSize: 28, color: C.text2 });
    txt(s, "Self-service: signs in, discovers services, applies and tracks every application.", 0.7, 5.25, 3.2, 1.1, { align: "center", fontSize: 15 });
    const rows = [
      [I.operator, "Assisted-mode operator", "Kiosk staff (Grama One, Karnataka One, Bangalore One style) apply on a citizen's behalf, with their own login and session."],
      [I.dept, "Department staff", "Process the application in the department's own system and login, entirely outside SevaOne. Departments integrate once against a published contract."],
      [I.admin, "Government administrator", "Five tiers: Superadmin, Chief Minister, Govt Official, District Head, Taluk Head. Read-only analytics, scoped by district or taluk."],
    ];
    rows.forEach((r, i) => {
      const y = 1.5 + i * 1.8;
      if (i > 0) s.addShape(S.line, { x: 4.9, y: y - 0.1, w: 7.83, h: 0, line: { color: C.accent6, width: 0.5 }, objectName: nm("rule") });
      img(s, r[0], 5.0, y + 0.1, 1.2, 1.2, r[1]);
      txt(s, r[1], 6.55, y + 0.05, 6.1, 0.45, { fontSize: 20, bold: true, color: C.text2 });
      txt(s, r[2], 6.55, y + 0.55, 6.1, 1.1, { fontSize: 14 });
    });
  }
  {
    // ---- capability map
    const s = slide("What the platform provides", P1);
    const cards = [
      [I.fingerprint, C.accent4, "Identity and sign-in", "Mobile OTP for returning citizens, DigiLocker Aadhaar consent for new ones. WSO2 Identity Server issues the session."],
      [I.search, C.accent2, "Service catalogue", "Departments and schemes kept in the discovery schema, with Sakala and SevaSindhu catalogue clients behind it."],
      [I.exchange, C.accent1, "Secure department handoff", "Single-use encrypted passes carry the citizen to the department and back. Department APIs sit behind WSO2 API Manager."],
      [I.clip, C.accent5, "Application tracking", "Orchestration and status schemas record each application. A background poller keeps status in sync."],
      [I.robot, C.accent3, "AI service finder", "Ask in English or Kannada. Retrieval over the service index answers or asks a clarifying question."],
      [I.chart, C.accent6, "Government insights", "Aggregate-only analytics by district and taluk, plus service notices and notifications for citizens."],
    ];
    cards.forEach((c, i) => {
      const col = i % 3, row = Math.floor(i / 3);
      const x = 0.6 + col * 4.1, y = 1.55 + row * 2.7;
      s.addShape(S.roundRect, { x, y, w: 3.9, h: 2.5, rectRadius: 0.12, fill: { color: C.background2 }, line: { type: "none" }, objectName: nm("card") });
      badge(s, c[0], x + 0.7, y + 0.7, 0.85, c[1], c[2]);
      txt(s, c[2], x + 1.35, y + 0.4, 2.4, 0.7, { fontSize: 17, bold: true, color: C.text2, valign: "middle" });
      txt(s, c[3], x + 0.3, y + 1.3, 3.35, 1.1, { fontSize: 13 });
    });
  }
  {
    // ---- stack
    const s = slide("Technology stack by layer", P1);
    const layers = [
      ["Experience", C.accent4, ["React", "Vite", "TanStack Router", "Tailwind", "Citizen portal", "Admin panel"]],
      ["Gateway and identity", C.accent2, ["WSO2 API Manager", "WSO2 Identity Server", "SCIM2", "OAuth2 JWT-bearer"]],
      ["Application", C.accent5, ["ASP.NET Core", "Dapper (no ORM)", "FluentValidation", "AutoMapper"]],
      ["Data and messaging", C.accent3, ["PostgreSQL + Flyway", "Redis", "Kafka"]],
      ["Intelligence", C.accent1, ["Python + FastAPI", "FAISS vector index", "BAAI/bge-m3", "Pluggable LLM"]],
      ["Hosting", C.accent6, ["Docker Compose", "nginx", "Kubernetes design ready"]],
    ];
    layers.forEach((l, i) => {
      const y = 1.5 + i * 0.9;
      const dark = l[1] === C.accent1;
      node(s, 0.6, y, 2.7, 0.76, l[0], null, { fill: l[1], fg: dark ? C.text2 : C.background1, tsz: 16 });
      let x = 3.5;
      l[2].forEach((t) => {
        const w = Math.max(1.5, 0.095 * t.length + 0.6);
        node(s, x, y + 0.1, w, 0.56, t, null, { fill: C.background2, fg: C.text2, tsz: 13, line: l[1], lw: 1.25 });
        x += w + 0.15;
      });
    });
  }

  // ============================ PART 2 : SOLUTION FLOW ============================
  const P2 = "2  Solution flow";
  pres.addSection({ title: P2 });
  {
    const s = pres.addSlide({ masterName: "SECTION", sectionTitle: P2 });
    s.addText("Solution flow", { placeholder: "title" });
    s.addText("What a citizen experiences, and what SevaOne does at each step.", { placeholder: "body" });
    txt(s, "02", 9.2, 1.7, 3.6, 2.2, { fontSize: 140, bold: true, color: C.accent1, align: "right" });
    badge(s, I.route, 11.2, 5.0, 1.5, C.accent1, "Route");
  }
  {
    // ---- end-to-end journey
    const s = slide("The citizen journey, end to end", P2);
    img(s, I.citizenDk, 0.55, 2.2, 2.1, 2.1, "Citizen");
    txt(s, "Citizen", 0.4, 4.35, 2.4, 0.4, { align: "center", bold: true, fontSize: 20, color: C.text2 });
    const steps = [
      [I.login, C.accent4, "Sign in", "Mobile OTP for returning citizens, DigiLocker for new ones."],
      [I.search, C.accent2, "Discover", "Browse departments and schemes, or ask the AI assistant."],
      [I.sign, C.accent1, "Apply", "SevaOne issues a single-use secure pass and redirects."],
      [I.building, C.accent5, "Complete", "The department redeems the pass and the citizen fills the real form there."],
      [I.clip, C.accent3, "Return and track", "The citizen lands back on the SevaOne dashboard to follow status."],
    ];
    steps.forEach((st, i) => {
      const cx = 3.9 + i * 2.1;
      badge(s, st[0], cx, 3.0, 1.0, st[1], st[2]);
      numDot(s, i + 1, cx + 0.5, 2.5, 0.34, C.text2, C.background1);
      txt(s, st[2], cx - 0.95, 3.7, 1.9, 0.4, { align: "center", bold: true, fontSize: 16, color: C.text2 });
      txt(s, st[3], cx - 0.95, 4.15, 1.9, 1.5, { align: "center", fontSize: 12.5 });
      if (i < steps.length - 1) arrow(s, cx + 0.58, 3.0, cx + 2.1 - 0.58, 3.0);
    });
    arrow(s, 2.7, 3.0, 3.25, 3.0);
    node(s, 0.6, 5.85, 12.13, 0.85, "Two credentials, kept apart", "Login: WSO2 session cookies held by the citizen's browser.    Handoff: a single-use, time-bound encrypted pass held by the department.", { fill: C.background2, fg: C.text2, tsz: 15, ssz: 13 });
  }
  {
    // ---- sign-in
    const s = slide("Signing in: returning or new citizen", P2);
    img(s, I.citizenDk, 0.55, 2.95, 1.7, 1.7, "Citizen");
    txt(s, "Citizen", 0.45, 4.7, 1.9, 0.35, { align: "center", bold: true, fontSize: 16, color: C.text2 });
    arrow(s, 2.3, 3.9, 2.8, 3.9);
    node(s, 2.8, 3.2, 1.95, 1.4, "Mobile number on file?", null, { shape: S.diamond, fill: C.accent1, fg: C.text2, tsz: 12, margin: 0.2 });
    // yes branch
    path(s, [[3.78, 3.2], [3.78, 2.3], [5.3, 2.3]]); label(s, "Yes: returning", 4.45, 1.95, 1.3, { bold: true, color: C.accent5 });
    node(s, 5.3, 1.8, 2.4, 1.0, "Mobile OTP", "6-digit code by SMS", { fill: C.accent4, fg: C.background1, tsz: 15 });
    arrow(s, 7.7, 2.3, 8.15, 2.3);
    node(s, 8.15, 1.8, 2.5, 1.0, "Session issued", "WSO2 cookies set", { fill: C.accent2, fg: C.background1, tsz: 15 });
    // no branch
    path(s, [[3.78, 4.6], [3.78, 5.4], [5.3, 5.4]]); label(s, "No: new citizen", 4.45, 5.78, 1.4, { bold: true, color: C.accent3 });
    node(s, 5.3, 4.85, 2.4, 1.1, "DigiLocker", "Aadhaar OTP and consent", { fill: C.accent3, fg: C.background1, tsz: 15 });
    arrow(s, 7.7, 5.4, 8.15, 5.4);
    node(s, 8.15, 4.85, 2.5, 1.1, "Complete profile", "account and session created", { fill: C.accent2, fg: C.background1, tsz: 15 });
    // merge
    node(s, 11.0, 3.45, 1.73, 0.95, "Browse and apply", null, { fill: C.text2, fg: C.background1, tsz: 15 });
    path(s, [[10.65, 2.3], [11.86, 2.3], [11.86, 3.45]]);
    path(s, [[10.65, 5.4], [11.86, 5.4], [11.86, 4.4]]);
    txt(s, "A WSO2 password is never entered by the citizen: identity is proven first, then SevaOne provisions the WSO2 session on their behalf.", 0.6, 6.35, 12.13, 0.5, { fontSize: 13, italic: true, color: C.accent6 });
  }
  {
    // ---- AI discovery
    const s = slide("Finding the right service", P2);
    img(s, I.citizenDk, 0.5, 2.55, 1.75, 1.75, "Citizen");
    txt(s, "Citizen", 0.4, 4.3, 1.95, 0.35, { align: "center", bold: true, fontSize: 16, color: C.text2 });
    s.addShape(S.line, { x: 2.85, y: 2.13, w: 0, h: 2.42, line: { color: C.accent6, width: 1.75 }, objectName: nm("trunk") });
    arrow(s, 2.3, 3.4, 2.85, 3.4, { head: false });
    arrow(s, 2.85, 2.13, 3.3, 2.13); arrow(s, 2.85, 4.55, 3.3, 4.55);
    node(s, 3.3, 1.7, 2.7, 0.85, "Browse departments and schemes", null, { fill: C.accent2, fg: C.background1, tsz: 14 });
    arrow(s, 6.0, 2.13, 10.0, 2.13); label(s, "pick a scheme", 8.0, 1.9, 1.3, { italic: true });
    node(s, 10.0, 1.7, 2.73, 0.85, "Service details and Apply", null, { fill: C.text2, fg: C.background1, tsz: 14 });
    node(s, 3.3, 4.1, 2.7, 0.9, "Ask in plain language", "English or Kannada", { fill: C.accent4, fg: C.background1, tsz: 14 });
    arrow(s, 6.0, 4.55, 6.45, 4.55);
    node(s, 6.45, 4.1, 2.6, 0.9, "AI service finder", "searches the service index", { fill: C.accent3, fg: C.background1, tsz: 14 });
    arrow(s, 9.05, 4.55, 9.35, 4.55);
    node(s, 9.35, 3.9, 1.9, 1.3, "Confident match?", null, { shape: S.diamond, fill: C.accent1, fg: C.text2, tsz: 11, margin: 0.18 });
    path(s, [[10.3, 3.9], [10.3, 2.55]]); label(s, "Yes", 10.0, 3.2, 0.5, { bold: true, color: C.accent5 });
    arrow(s, 10.3, 5.2, 10.3, 5.5); label(s, "No", 10.0, 5.35, 0.45, { bold: true, color: C.accent3 });
    node(s, 8.7, 5.5, 3.2, 0.7, "Clarifying question", null, { fill: C.background2, fg: C.text2, tsz: 14, line: C.accent6 });
    path(s, [[8.7, 5.85], [4.65, 5.85], [4.65, 5.0]]); label(s, "citizen answers", 6.7, 5.85, 1.4, { italic: true });
    node(s, 0.6, 6.4, 12.13, 0.5, "If the AI service is slow or unreachable (35 s limit) the portal falls back to the bundled catalogue, so sign-in and apply never depend on it.", null, { fill: C.background1, fg: C.accent6, tsz: 12, line: C.accent6, dash: "dash", lw: 1 });
  }
  {
    // ---- apply handoff swimlanes
    const s = slide("Applying: the department handoff", P2);
    const lanes = [["Citizen", I.citizenDk, 1.1], ["SevaOne", I.platform, 0.75], ["Department", I.dept, 0.75]];
    lanes.forEach((l, i) => {
      const y = 1.5 + i * 1.75;
      s.addShape(S.roundRect, { x: 0.6, y, w: 12.13, h: 1.6, rectRadius: 0.08, fill: { color: C.background2 }, line: { type: "none" }, objectName: nm("lane") });
      img(s, l[1], 1.25 - l[2] / 2, y + 0.45 - (l[2] - 0.75) / 2 - 0.1, l[2], l[2], l[0]);
      txt(s, l[0], 0.65, y + 1.22, 1.2, 0.3, { align: "center", bold: true, fontSize: 12, color: C.text2 });
    });
    const laneY = (k) => 1.5 + k * 1.75 + 0.22;
    const st = [
      [0, "Clicks Apply on a scheme", C.accent4], [1, "Issues one-time pass (30 min)", C.accent2],
      [2, "Redeems pass, receives citizen profile", C.accent5], [0, "Fills the real form on the department site", C.accent4],
      [2, "Sends acknowledgement", C.accent5], [1, "Issues return pass (10 min)", C.accent2],
      [0, "Lands on the SevaOne dashboard", C.accent4],
    ];
    st.forEach((t, i) => {
      const x = 2.15 + i * 1.5, y = laneY(t[0]);
      node(s, x, y, 1.38, 1.16, t[1], null, { fill: t[2], fg: C.background1, tsz: 12, margin: 0.06 });
      numDot(s, i + 1, x + 0.05, y + 0.02, 0.3, C.accent1, C.text2);
      if (i < st.length - 1) {
        const nx = 2.15 + (i + 1) * 1.5, ny = laneY(st[i + 1][0]);
        const y0 = y + 0.58;
        const cxn = nx + 0.69;
        const down = st[i + 1][0] > t[0];
        path(s, [[x + 1.38, y0], [cxn, y0], [cxn, down ? ny : ny + 1.16]]);
      }
    });
  }
  {
    // ---- operators + gov
    const s = slide("Beyond the citizen: operators and government", P2);
    s.addShape(S.roundRect, { x: 0.6, y: 1.5, w: 5.9, h: 5.2, rectRadius: 0.12, fill: { color: C.background2 }, line: { type: "none" }, objectName: nm("panel") });
    img(s, I.operator, 0.9, 1.75, 1.3, 1.3, "Operator");
    txt(s, "Assisted mode", 2.4, 1.85, 3.9, 0.5, { fontSize: 22, bold: true, color: C.text2 });
    txt(s, "Kiosk operators apply for a citizen", 2.4, 2.4, 3.9, 0.5, { fontSize: 14, color: C.accent6 });
    txt(s, [
      { text: "Own operator identity in WSO2 and a separate session cookie from citizens.", options: { bullet: true, breakLine: true } },
      { text: "Applies on the citizen's behalf using the same secure department handoff.", options: { bullet: true, breakLine: true } },
      { text: "Every action is audited in the audit schema.", options: { bullet: true } },
    ], 0.95, 3.4, 5.2, 2.6, { fontSize: 15, paraSpaceAfter: 8 });
    s.addShape(S.roundRect, { x: 6.83, y: 1.5, w: 5.9, h: 5.2, rectRadius: 0.12, fill: { color: C.background2 }, line: { type: "none" }, objectName: nm("panel") });
    img(s, I.admin, 7.13, 1.75, 1.3, 1.3, "Government admin");
    txt(s, "Government admin panel", 8.63, 1.85, 4.0, 0.5, { fontSize: 22, bold: true, color: C.text2 });
    txt(s, "Read-only, aggregate-only analytics", 8.63, 2.4, 4.0, 0.5, { fontSize: 14, color: C.accent6 });
    const tiers = [["Superadmin", C.text2], ["Chief Minister", C.accent4], ["Govt Official", C.accent4], ["District Head", C.accent2], ["Taluk Head", C.accent2]];
    tiers.forEach((t, i) => node(s, 7.13 + (i % 3) * 1.8, 3.3 + Math.floor(i / 3) * 0.7, 1.7, 0.55, t[0], null, { fill: t[1], fg: C.background1, tsz: 12, margin: 0.03 }));
    txt(s, [
      { text: "District and Taluk Heads see only their own area.", options: { bullet: true, breakLine: true } },
      { text: "No citizen name, Aadhaar, mobile or address ever leaves.", options: { bullet: true, breakLine: true } },
      { text: "Superadmin also publishes service notices and checks system health.", options: { bullet: true } },
    ], 7.13, 4.85, 5.3, 1.8, { fontSize: 14, paraSpaceAfter: 6 });
  }

  // ============================ PART 3 : TECHNICAL FLOW ============================
  const P3 = "3  Technical flow";
  pres.addSection({ title: P3 });
  {
    const s = pres.addSlide({ masterName: "SECTION", sectionTitle: P3 });
    s.addText("Technical flow", { placeholder: "title" });
    s.addText("Requests, tokens, gateways, data and deployment.", { placeholder: "body" });
    txt(s, "03", 9.2, 1.7, 3.6, 2.2, { fontSize: 140, bold: true, color: C.accent1, align: "right" });
    badge(s, I.cogs, 11.2, 5.0, 1.5, C.accent4, "Cogs");
  }
  {
    // ---- auth sequence
    const s = slide("Authentication sequence", P3);
    sequence(s, {
      x0: 2.2, x1: 11.9, top: 1.35, rowH: 0.43,
      parts: [
        { label: "Citizen browser", icon: I.s_browser, size: 0.55 },
        { label: "Citizen gateway", icon: I.s_gw, size: 0.55 },
        { label: "SevaOne BFF", icon: I.s_bff, size: 0.55 },
        { label: "WSO2 IS", icon: I.s_is, size: 0.55 },
        { label: "DigiLocker", icon: I.s_dl, size: 0.55 },
      ],
      rows: [
        { band: "Returning citizen: mobile OTP" },
        { from: 0, to: 1, text: "POST /auth/mobile/start, then /verify (OTP)" },
        { from: 1, to: 2, text: "forward, gateway secret stamped" },
        { band: "New citizen: DigiLocker (when found = false)" },
        { from: 0, to: 1, text: "GET /auth/digilocker/start" },
        { from: 2, to: 4, text: "redirect for Aadhaar OTP and consent" },
        { from: 4, to: 2, text: "signed eAadhaar via callback", dash: true },
        { band: "Both paths: provision and exchange" },
        { from: 2, to: 3, text: "SCIM2 find-or-create user" },
        { from: 2, to: 3, text: "JWT-bearer exchange (RFC 7523)" },
        { from: 3, to: 2, text: "access + refresh token", dash: true },
        { from: 2, to: 0, text: "Set-Cookie seva.wso2.at / seva.wso2.rt (HttpOnly)", dash: true },
      ],
    });
  }
  {
    // ---- apply handoff sequence
    const s = slide("Apply and department handoff sequence", P3);
    sequence(s, {
      x0: 2.2, x1: 11.9, top: 1.35, rowH: 0.4,
      parts: [
        { label: "Citizen browser", icon: I.s_browser, size: 0.55 },
        { label: "Citizen gateway", icon: I.s_gw, size: 0.55 },
        { label: "SevaOne BFF", icon: I.s_bff, size: 0.55 },
        { label: "Dept gateway", icon: I.s_gw, size: 0.55 },
        { label: "Department", icon: I.s_dept, size: 0.55 },
      ],
      rows: [
        { from: 0, to: 1, text: "POST /schemes/{code}/apply-token" },
        { from: 1, to: 2, text: "forward" },
        { self: true, from: 2, text: "sign JWS, encrypt, store in portal.sevaone_token" },
        { from: 2, to: 0, text: "applyUrl + one-time Token (30 min)", dash: true },
        { from: 0, to: 4, text: "browser opens the department site" },
        { from: 4, to: 3, text: "POST /dept/user-details (ApiKey)" },
        { from: 3, to: 2, text: "forward, secret stamped" },
        { from: 2, to: 4, text: "citizen profile, pass consumed", dash: true },
        { note: "Citizen completes the real form on the department side", cols: [0, 4] },
        { from: 4, to: 3, text: "POST /dept/get-acknowledgement" },
        { from: 3, to: 2, text: "forward" },
        { from: 2, to: 4, text: "returnUrl + return pass (10 min)", dash: true },
        { from: 4, to: 0, text: "redirect back", dash: true },
        { from: 0, to: 2, text: "GET /apply/return via citizen gateway; 302 to dashboard" },
      ],
    });
  }
  {
    // ---- security layers
    const s = slide("Security: layered checks and short-lived tokens", P3);
    const panel = (x, title, color, icon, chain) => {
      s.addShape(S.roundRect, { x, y: 1.45, w: 5.95, h: 3.65, rectRadius: 0.12, fill: { color: C.background2 }, line: { type: "none" }, objectName: nm("panel") });
      img(s, icon, x + 0.2, 1.52, 0.6, 0.6, title);
      txt(s, title, x + 0.95, 1.55, 4.8, 0.55, { fontSize: 18, bold: true, color: C.text2, valign: "middle" });
      chain.forEach((c, i) => {
        const y = 2.25 + i * 0.95;
        node(s, x + 0.3, y, 5.35, 0.72, c[0], c[1], { fill: color, fg: C.background1, tsz: 14, ssz: 12, align: "left", margin: 0.18 });
        numDot(s, i + 1, x + 0.3, y + 0.04, 0.3, C.accent1, C.text2);
        if (i < chain.length - 1) arrow(s, x + 2.97, y + 0.72, x + 2.97, y + 0.95, { w: 1.5 });
      });
    };
    panel(0.6, "Citizen traffic", C.accent4, I.citizenDk, [
      ["sevaone-citizen API (WSO2 APIM)", "only route to /api/*, stamps a shared secret"],
      ["CitizenGatewayAuthMiddleware", "rejects any request that skipped the gateway"],
      ["Cookie session check", "seva.wso2.at validated by introspection, cached in Redis"],
    ]);
    panel(6.78, "Department traffic", C.accent5, I.dept, [
      ["APIM API key", "per department, generated once at onboarding"],
      ["DeptGatewayAuthMiddleware", "shared secret stamped by APIM, fails closed outside dev"],
      ["DeptMachineAuth", "client id and secret decide which department is calling"],
    ]);
    const stats = [["30 min", "apply pass lifetime"], ["10 min", "return pass lifetime"], ["1 use", "each pass, encrypted at rest"], ["4", "separate session schemes: citizen, operator, admin, gov-admin"]];
    stats.forEach((t, i) => {
      const x = 0.6 + i * 3.1;
      txt(s, t[0], x, 5.3, 2.9, 0.8, { fontSize: 40, bold: true, color: C.accent1 });
      txt(s, t[1], x, 6.1, 2.9, 0.7, { fontSize: 13 });
    });
  }
  {
    // ---- backend internals
    const s = slide("Inside the .NET backend", P3);
    const layers = [
      ["SevaOne.Services", "Controllers (Auth, Catalog, Applications, Dept, Operator, GovAdmin, AiChat), auth handlers, middleware, health checks", C.accent4],
      ["SevaOne.Business", "Orchestration, Discovery, Portal, Status, Notification, Audit, AiChat services with validators and DTOs", C.accent2],
      ["SevaOne.Infrastructure", "Dapper repositories, Redis session stores, Kafka messaging, clients for WSO2, DigiLocker, Sakala, SMS and AI, department connectors", C.accent5],
      ["SevaOne.Domain", "Models, enums and the stable error-code contract", C.accent6],
    ];
    layers.forEach((l, i) => {
      const y = 1.5 + i * 1.3;
      node(s, 0.6, y, 2.6, 1.05, l[0], null, { fill: l[2], fg: C.background1, tsz: 15 });
      node(s, 3.3, y, 4.7, 1.05, l[1], null, { fill: C.background2, fg: C.text1, tsz: 12.5, align: "left", margin: 0.15 });
      if (i < layers.length - 1) arrow(s, 1.9, y + 1.05, 1.9, y + 1.3, { w: 1.5 });
    });
    txt(s, "Runs in the background", 8.6, 1.45, 4.1, 0.4, { fontSize: 18, bold: true, color: C.text2 });
    const w = [
      [I.stream, C.accent3, "OutboxDispatcher", "publishes outbox rows to Kafka"],
      [I.bell, C.accent1, "NotificationDispatcher", "delivers outbound messages"],
      [I.cogs, C.accent2, "ApplicationStatusPoller", "keeps application status in sync"],
    ];
    w.forEach((r, i) => {
      const y = 2.05 + i * 1.0;
      badge(s, r[0], 8.95, y + 0.4, 0.7, r[1], r[2]);
      txt(s, r[2], 9.5, y + 0.05, 3.2, 0.35, { fontSize: 15, bold: true, color: C.text2 });
      txt(s, r[3], 9.5, y + 0.42, 3.2, 0.4, { fontSize: 13 });
    });
    txt(s, "Cross-cutting", 8.6, 5.1, 4.1, 0.4, { fontSize: 18, bold: true, color: C.text2 });
    txt(s, "Gateway-auth middleware, one global exception handler that maps every failure to a stable SEVA-* code, tracing and health checks for Postgres, Redis, Kafka, WSO2 IS and APIM.", 8.6, 5.55, 4.1, 1.35, { fontSize: 13 });
  }
  {
    // ---- data architecture
    const s = slide("Data architecture", P3);
    s.addShape(S.roundRect, { x: 0.6, y: 1.45, w: 7.6, h: 4.7, rectRadius: 0.12, fill: { color: C.background2 }, line: { type: "none" }, objectName: nm("panel") });
    badge(s, I.db, 1.15, 1.95, 0.7, C.accent5, "PostgreSQL");
    txt(s, "PostgreSQL: one database, six schemas", 1.7, 1.65, 6.3, 0.6, { fontSize: 19, bold: true, color: C.text2, valign: "middle" });
    const sch = [
      ["discovery", "departments and service catalogue"], ["portal", "citizens, operators, tokens"], ["orchestration", "applications, department sessions"],
      ["status", "timelines and payments"], ["audit", "who did what"], ["notification", "outbound messages"],
    ];
    sch.forEach((c, i) => {
      const x = 0.85 + (i % 3) * 2.4, y = 2.65 + Math.floor(i / 3) * 1.35;
      node(s, x, y, 2.25, 1.15, c[0], c[1], { fill: C.background1, fg: C.text2, tsz: 15, ssz: 11.5, line: C.accent5, lw: 1.25 });
    });
    txt(s, "Every read and write goes through a named Postgres function: no inline SQL, no ORM. All keys are uuid. Schema changes ship as Flyway migrations.", 0.85, 5.4, 7.1, 0.7, { fontSize: 13 });
    const cards = [
      [I.lock, C.accent3, "Redis", "Sessions for citizen, operator, admin and gov-admin, plus the WSO2 token-introspection cache."],
      [I.stream, C.accent1, "Kafka", "Events published from the transactional outbox, so a write and its event never drift apart."],
      [I.robot, C.accent4, "AI service store", "Its own conversation and index data, with a FAISS vector index. No shared keys with Postgres."],
    ];
    cards.forEach((c, i) => {
      const y = 1.45 + i * 1.6;
      s.addShape(S.roundRect, { x: 8.5, y, w: 4.23, h: 1.45, rectRadius: 0.1, fill: { color: C.background2 }, line: { type: "none" }, objectName: nm("card") });
      badge(s, c[0], 9.05, y + 0.5, 0.65, c[1], c[2]);
      txt(s, c[2], 9.65, y + 0.12, 2.9, 0.35, { fontSize: 16, bold: true, color: C.text2 });
      txt(s, c[3], 9.65, y + 0.5, 2.95, 0.95, { fontSize: 11.5 });
    });
    txt(s, "Department data stays in the department's system. SevaOne holds the citizen identity, the catalogue and the application record.", 0.6, 6.35, 12.1, 0.5, { fontSize: 13, italic: true, color: C.accent6 });
  }
  {
    // ---- AI technical sequence
    const s = slide("AI service finder sequence", P3);
    sequence(s, {
      x0: 2.2, x1: 11.9, top: 1.35, rowH: 0.52,
      parts: [
        { label: "Citizen", icon: I.citizenTeal, size: 0.62 },
        { label: "Citizen portal", icon: I.s_portal, size: 0.55 },
        { label: "SevaOne RAG (FastAPI)", icon: I.s_ai, size: 0.55 },
        { label: "Service index (FAISS)", icon: I.s_idx, size: 0.55 },
        { label: "LLM on GPU host", icon: I.s_gpu, size: 0.55 },
      ],
      rows: [
        { from: 0, to: 1, text: "types a question (EN or KN)" },
        { from: 1, to: 2, text: "POST /rag-api/chat (35 s timeout)" },
        { from: 2, to: 3, text: "embed query, similarity search" },
        { from: 3, to: 2, text: "ranked services", dash: true },
        { from: 2, to: 4, text: "reason over candidates" },
        { from: 4, to: 2, text: "answer or follow-up", dash: true },
        { note: "status = answered: reply + apply_url      status = clarifying: follow-up question", cols: [1, 4] },
        { from: 2, to: 1, text: "reply, apply_url, status", dash: true },
        { from: 1, to: 0, text: "renders answer with Apply deep link", dash: true },
        { note: "On any error or timeout the portal falls back to the bundled catalogue", cols: [0, 4] },
      ],
    });
  }
  {
    // ---- deployment
    const s = slide("Deployment topology", P3);
    s.addShape(S.roundRect, { x: 0.6, y: 1.45, w: 9.6, h: 5.35, rectRadius: 0.12, fill: { color: C.background2 }, line: { color: C.accent6, width: 1.25, dashType: "dash" }, objectName: nm("compose") });
    txt(s, "Docker Compose network (sevaone)", 0.85, 1.55, 6, 0.3, { fontSize: 13, bold: true, color: C.accent6 });
    node(s, 3.2, 1.95, 4.4, 0.7, "nginx", "ports 80 and 443: TLS, static portal, API routing", { fill: C.text2, fg: C.background1, tsz: 15, ssz: 11 });
    node(s, 0.9, 3.15, 2.5, 0.85, "Citizen portal", "static bundle behind nginx", { fill: C.accent4, fg: C.background1, tsz: 14, ssz: 11 });
    node(s, 3.75, 3.15, 2.5, 0.85, "Admin panel", "port 5096", { fill: C.accent3, fg: C.background1, tsz: 14, ssz: 11 });
    node(s, 6.6, 3.15, 3.35, 0.85, "SevaOne BFF (.NET)", "port 5080", { fill: C.accent2, fg: C.background1, tsz: 14, ssz: 11 });
    node(s, 0.9, 4.55, 2.5, 0.85, "WSO2 Identity Server", "port 9443", { fill: C.accent1, fg: C.text2, tsz: 14, ssz: 11 });
    node(s, 3.75, 4.55, 2.5, 0.85, "WSO2 API Manager", "9444 admin, 8244 gateway", { fill: C.accent1, fg: C.text2, tsz: 14, ssz: 11 });
    node(s, 0.9, 5.8, 2.5, 0.75, "PostgreSQL", "port 5432", { fill: C.accent5, fg: C.background1, tsz: 14, ssz: 11 });
    node(s, 3.75, 5.8, 2.5, 0.75, "Redis", "port 6379", { fill: C.accent5, fg: C.background1, tsz: 14, ssz: 11 });
    node(s, 6.6, 5.8, 3.35, 0.75, "Kafka", "port 9092", { fill: C.accent5, fg: C.background1, tsz: 14, ssz: 11 });
    arrow(s, 4.4, 2.65, 2.15, 3.15); arrow(s, 5.4, 2.65, 5.0, 3.15); arrow(s, 6.4, 2.65, 8.2, 3.15);
    arrow(s, 8.2, 4.0, 6.0, 4.55); label(s, "via gateway", 7.2, 4.18, 1.0);
    arrow(s, 8.2, 4.0, 8.2, 5.8, { dash: "dash" });
    // external
    node(s, 10.6, 1.9, 2.13, 1.1, "AI service", "FastAPI + LLM on the GPU host", { fill: C.accent3, fg: C.background1, tsz: 14, ssz: 11 });
    node(s, 10.6, 3.35, 2.13, 1.1, "Department systems", "reach the platform via /dept/*", { fill: C.accent5, fg: C.background1, tsz: 14, ssz: 11 });
    node(s, 10.6, 4.8, 2.13, 1.1, "DigiLocker, SMS, Sakala", "outbound integrations", { fill: C.accent6, fg: C.background1, tsz: 14, ssz: 11 });
    txt(s, "Outside the compose network", 10.5, 1.5, 2.3, 0.35, { fontSize: 12, bold: true, color: C.accent6, align: "center" });
    txt(s, "setup-stack.sh brings up infra, WSO2 and the three apps in order. A Kubernetes design is documented for production.", 10.45, 6.0, 2.3, 0.85, { fontSize: 11 });
  }
  // ============================ CLOSE ============================
  pres.addSection({ title: "Close" });
  {
    const s = pres.addSlide({ masterName: "TITLE", sectionTitle: "Close" });
    s.addText("Built for the citizen, ready to extend", { placeholder: "title", fontSize: 42 });
    s.addText("", { placeholder: "body" });
    const nx = [
      ["Stage Engine", "Native multi-stage services with per-stage fee and officer approval. Proven in a spike, not yet wired in."],
      ["More departments", "Onboard through the generic HTTP connector and the published department contract."],
      ["Production hosting", "Kubernetes deployment design is written and awaiting validation."],
    ];
    nx.forEach((t, i) => {
      const y = 4.0 + i * 0.95;
      txt(s, t[0], 0.8, y, 2.4, 0.4, { fontSize: 17, bold: true, color: C.accent1 });
      txt(s, t[1], 3.2, y, 4.6, 0.85, { fontSize: 14, color: C.background2 });
    });
    img(s, I.citizenGold, 8.9, 1.5, 3.4, 3.4, "Citizen");
    txt(s, "Thank you", 8.4, 5.2, 4.4, 0.7, { align: "center", fontSize: 32, bold: true, color: C.background1 });
  }

  await pres.writeFile({ fileName: "SevaOne_Architecture.pptx" });
  await applyTheme("SevaOne_Architecture.pptx", THEME);
  console.log("written");
}
main().catch((e) => { console.error(e); process.exit(1); });
