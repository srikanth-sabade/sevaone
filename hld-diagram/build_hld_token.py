import base64,os,subprocess,html
here=os.path.dirname(os.path.abspath(__file__))
E=html.escape
def logo(name):
    d=open(os.path.join(here,'logos',name+'.svg'),'rb').read()
    return 'data:image/svg+xml;base64,'+base64.b64encode(d).decode()
W,H=2400,1230
out=[]
def add(s): out.append(s)
F="font-family=\"'Inter','Noto Sans','Carlito',sans-serif\""
# ---------- defs ----------
add(f'''<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}" {F}>
<defs>
<filter id="sh" x="-5%" y="-5%" width="110%" height="120%"><feDropShadow dx="0" dy="2" stdDeviation="3" flood-color="#1B2A41" flood-opacity="0.14"/></filter>
<marker id="ar" viewBox="0 0 10 10" refX="8.5" refY="5" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 z" fill="#475569"/></marker>
<marker id="ab" viewBox="0 0 10 10" refX="8.5" refY="5" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 z" fill="#2563EB"/></marker>
<marker id="ao" viewBox="0 0 10 10" refX="8.5" refY="5" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 z" fill="#EA580C"/></marker>
<marker id="ap" viewBox="0 0 10 10" refX="8.5" refY="5" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 z" fill="#7C3AED"/></marker>
<linearGradient id="cyl" x1="0" x2="1"><stop offset="0" stop-color="#C7D2FE"/><stop offset="0.5" stop-color="#EEF2FF"/><stop offset="1" stop-color="#C7D2FE"/></linearGradient>
<linearGradient id="cylg" x1="0" x2="1"><stop offset="0" stop-color="#D1D5DB"/><stop offset="0.5" stop-color="#F3F4F6"/><stop offset="1" stop-color="#D1D5DB"/></linearGradient>
<linearGradient id="hdr" x1="0" x2="1"><stop offset="0" stop-color="#0F172A"/><stop offset="1" stop-color="#1E3A8A"/></linearGradient>
</defs>
<rect width="{W}" height="{H}" fill="#FFFFFF"/>''')
def card(x,y,w,h,title,lines,lg=None,chips=(),dashed=False,wso2=False,accent='#CBD5E1',tsize=20):
    ds=' stroke-dasharray="6 4"' if dashed else ''
    add(f'<g filter="url(#sh)"><rect x="{x}" y="{y}" width="{w}" height="{h}" rx="12" fill="#fff" stroke="{accent}" stroke-width="1.6"{ds}/></g>')
    tx=x+16
    if lg:
        add(f'<image href="{logo(lg)}" x="{x+14}" y="{y+14}" width="34" height="34"/>'); tx=x+60
    if wso2:
        add(f'<rect x="{x+12}" y="{y+16}" width="40" height="30" rx="7" fill="#FF7300"/><text x="{x+32}" y="{y+36}" font-size="13" font-weight="800" fill="#fff" text-anchor="middle">WSO2</text>'); tx=x+62
    add(f'<text x="{tx}" y="{y+32}" font-size="{tsize}" font-weight="700" fill="#0F172A">{E(title)}</text>')
    for i,l in enumerate(lines):
        add(f'<text x="{tx}" y="{y+56+i*20}" font-size="14.5" fill="#475569">{E(l)}</text>')
    cx=x+w-12
    for c in chips[::-1]:
        col={'WEB':'#2563EB','MOBILE':'#EA580C','AI':'#7C3AED'}[c]; wd={'WEB':42,'MOBILE':58,'AI':32}[c]
        cx-=wd; add(f'<rect x="{cx}" y="{y+h-26}" width="{wd}" height="18" rx="9" fill="{col}"/><text x="{cx+wd/2}" y="{y+h-13}" font-size="10.5" font-weight="700" fill="#fff" text-anchor="middle">{c}</text>'); cx-=6
def cyl(x,y,w,h,title,lines,lg=None,dashed=False,grey=False):
    g='cylg' if grey else 'cyl'; ry=13; st='#94A3B8' if grey else '#818CF8'
    ds=' stroke-dasharray="6 4"' if dashed else ''
    add(f'<g filter="url(#sh)"><path d="M{x} {y+ry} v{h-2*ry} a{w/2} {ry} 0 0 0 {w} 0 v-{h-2*ry} a{w/2} {ry} 0 0 0 -{w} 0 z" fill="url(#{g})" stroke="{st}" stroke-width="1.6"{ds}/>'
        f'<ellipse cx="{x+w/2}" cy="{y+ry}" rx="{w/2}" ry="{ry}" fill="#fff" fill-opacity="0.85" stroke="{st}" stroke-width="1.6"{ds}/>'
        f'<path d="M{x} {y+ry+ (h-2*ry)*0.36} a{w/2} {ry} 0 0 0 {w} 0" fill="none" stroke="{st}" stroke-opacity="0.55" stroke-width="1.2"/>'
        f'<path d="M{x} {y+ry+ (h-2*ry)*0.68} a{w/2} {ry} 0 0 0 {w} 0" fill="none" stroke="{st}" stroke-opacity="0.55" stroke-width="1.2"/></g>')
    tx=x+18
    if lg:
        add(f'<image href="{logo(lg)}" x="{x+16}" y="{y+ry+10}" width="30" height="30"/>'); tx=x+56
    add(f'<text x="{tx}" y="{y+ry+30}" font-size="18.5" font-weight="700" fill="#1E1B4B">{E(title)}</text>')
    for i,l in enumerate(lines):
        add(f'<text x="{tx}" y="{y+ry+52+i*18}" font-size="13.5" fill="#3F3F63">{E(l)}</text>')
def arrow(pts,label=None,lp=None,dash=False,col='#475569',mk='ar',both=False,w=2):
    d='M'+' L'.join(f'{x} {y}' for x,y in pts)
    ds=' stroke-dasharray="7 5"' if dash else ''
    ms=f' marker-end="url(#{mk})"'+(f' marker-start="url(#{mk})"' if both else '')
    add(f'<path d="{d}" fill="none" stroke="{col}" stroke-width="{w}" stroke-linejoin="round"{ds}{ms}/>')
    if label:
        x,y=lp or pts[len(pts)//2]
        tw=len(label)*6.4+14
        add(f'<rect x="{x-tw/2}" y="{y-10}" width="{tw}" height="20" rx="10" fill="#fff" stroke="#CBD5E1"/><text x="{x}" y="{y+4}" font-size="11.5" fill="#334155" text-anchor="middle" font-weight="600">{E(label)}</text>')

add(f'<rect x="0" y="0" width="{W}" height="92" fill="url(#hdr)"/>')
add('<text x="48" y="45" font-size="32" font-weight="700" fill="#FFFFFF">Token-Based Integration: Department Plug and Play</text>')
add('<text x="48" y="73" font-size="15" fill="#C7D2FE">Target contract (Seva Sindhu Token-Based Integration, V3): what SevaOne sends, what a department provides, and what the department calls back.</text>')
lx=1640
add(f'<g font-size="13.5" fill="#E2E8F0"><rect x="{lx}" y="30" width="26" height="16" rx="4" fill="#fff"/><text x="{lx+34}" y="43">Service</text>'
    f'<path d="M{lx+112} 38 h30" stroke="#fff" stroke-width="2" marker-end="url(#ar)"/><text x="{lx+150}" y="43">Request</text>'
    f'<path d="M{lx+232} 38 h30" stroke="#fff" stroke-width="2" stroke-dasharray="6 4" marker-end="url(#ar)"/><text x="{lx+270}" y="43">Event or callback</text>'
    f'<ellipse cx="{lx+13}" cy="68" rx="13" ry="5" fill="#C7D2FE"/><path d="M{lx} 68 v8 a13 5 0 0 0 26 0 v-8" fill="#A5B4FC"/><text x="{lx+34}" y="76">Data store</text>'
    f'<rect x="{lx+112}" y="62" width="26" height="16" rx="4" fill="none" stroke="#fff" stroke-dasharray="4 3"/><text x="{lx+146}" y="76">Runs on a separate server</text></g>')
def gbox(x,y,w,h,num,title,fill,stroke,right=False,bottom=False):
    add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="20" fill="{fill}" stroke="{stroke}" stroke-width="1.8"/>')
    cxn=(x+w-30) if right else (x+30)
    if bottom: y=y+h-70
    add(f'<circle cx="{cxn}" cy="{y+30}" r="15" fill="{stroke}"/><text x="{cxn}" y="{y+36}" font-size="16" font-weight="800" fill="#fff" text-anchor="middle">{num}</text>')
    add(f'<text x="{(x+w-56) if right else (x+54)}" y="{y+37}" font-size="19" font-weight="700" fill="#1E293B" letter-spacing="0.3" text-anchor="{"end" if right else "start"}">{E(title)}</text>')
def rail(pts,col='#475569',dash=False):
    d='M'+' L'.join(f'{x} {y}' for x,y in pts); ds=' stroke-dasharray="7 5"' if dash else ''
    add(f'<path d="{d}" fill="none" stroke="{col}" stroke-width="2.2" stroke-linejoin="round"{ds}/>')


def num(x,y,n):
    add(f'<circle cx="{x}" cy="{y}" r="15" fill="#1E293B"/><text x="{x}" y="{y+5}" font-size="14" font-weight="800" fill="#fff" text-anchor="middle">{n}</text>')
def pill(cx,cy,text,w=None,stroke='#CBD5E1',col='#334155'):
    w=w or len(text)*6.9+22
    add(f'<rect x="{cx-w/2}" y="{cy-12}" width="{w}" height="24" rx="12" fill="#fff" stroke="{stroke}"/><text x="{cx}" y="{cy+4}" font-size="12" font-weight="600" fill="{col}" text-anchor="middle">{E(text)}</text>')
def need(x,y,text):
    w=len(text)*6.6+24
    add(f'<rect x="{x}" y="{y}" width="{w}" height="22" rx="11" fill="#B7791F"/><text x="{x+w/2}" y="{y+15}" font-size="11.5" font-weight="700" fill="#fff" text-anchor="middle">{E(text)}</text>')
def icon(kind,x,y,col='#475569'):
    if kind=='user': add(f'<circle cx="{x+17}" cy="{y+11}" r="8" fill="none" stroke="{col}" stroke-width="2.4"/><path d="M{x+3} {y+34} c0 -11 28 -11 28 0" fill="none" stroke="{col}" stroke-width="2.4" stroke-linecap="round"/>')
    if kind=='code': add(f'<rect x="{x}" y="{y+2}" width="36" height="30" rx="7" fill="none" stroke="{col}" stroke-width="2.4"/><text x="{x+18}" y="{y+23}" font-size="14" font-weight="800" fill="{col}" text-anchor="middle">&lt;/&gt;</text>')
    if kind=='form': add(f'<rect x="{x+3}" y="{y}" width="30" height="36" rx="5" fill="none" stroke="{col}" stroke-width="2.4"/><path d="M{x+9} {y+11} h18 M{x+9} {y+19} h18 M{x+9} {y+27} h10" stroke="{col}" stroke-width="2.4" stroke-linecap="round"/>')
    if kind=='door': add(f'<path d="M{x+4} {y+36} V{y+4} h22 v32 M{x} {y+36} h32" fill="none" stroke="{col}" stroke-width="2.4" stroke-linejoin="round"/><circle cx="{x+21}" cy="{y+21}" r="2.4" fill="{col}"/>')
    if kind=='doc': add(f'<path d="M{x+6} {y} h16 l9 9 v27 h-25 z M{x+22} {y} v9 h9" fill="none" stroke="{col}" stroke-width="2.4" stroke-linejoin="round"/>')
    if kind=='search': add(f'<circle cx="{x+15}" cy="{y+15}" r="10" fill="none" stroke="{col}" stroke-width="2.4"/><path d="M{x+23} {y+23} l10 10" stroke="{col}" stroke-width="3" stroke-linecap="round"/>')
def icard(x,y,w,h,ic,title,lines,accent,tsize=19,fs=14):
    add(f'<g filter="url(#sh)"><rect x="{x}" y="{y}" width="{w}" height="{h}" rx="12" fill="#fff" stroke="{accent}" stroke-width="1.6"/></g>')
    icon(ic,x+16,y+14,accent)
    add(f'<text x="{x+66}" y="{y+34}" font-size="{tsize}" font-weight="700" fill="#0F172A">{E(title)}</text>')
    for i,l in enumerate(lines): add(f'<text x="{x+66}" y="{y+58+i*20}" font-size="{fs}" fill="#475569">{E(l)}</text>')
gbox(60,130,1000,190,'A','Users','#EAF2FF','#6E96E8')
gbox(60,420,1000,740,'B','SevaOne (EDCS)','#F1EDFF','#9B83F0')
gbox(1300,430,1040,700,'C','Department','#FBEFF4','#D9779A')
icard(90,180,440,110,'user','Citizen',['Logs in to Seva Sindhu directly'],'#6E96E8')
icard(560,180,470,110,'user','Assisted-mode operator',['Franchise login: K1 / B1 / G1 / AJSK / BSK'],'#6E96E8')
icard(90,480,920,130,'door','Login, token and redirect',['Citizen or assisted-mode login. Issues the token and the unique edcsApplicationRefNo.','Sends the browser to the department with an auto-submitting POST form.'],'#9B83F0')
need(110,480+98,'Understanding needed: token format and lifetime')
icard(90,640,920,150,'code','URL 2: acknowledgement receiver API',['POST .../api/TokenIntegration/department-update  (UAT path shown in the document)','Content-Type: application/json · Authorization: Basic · JSON request with payment and acknowledgement','Answers DepartmentApplicationUpdateId, or "Data Not Inserted" on failure'],'#9B83F0')
need(600,640+120,'Understanding needed: production URL and credentials')
# database cylinder
cyl(90,830,920,150,'SevaOne database',['Application response, payment details, acknowledgement file','Status: SUCCESS, or Failed until the department calls URL 2 again','The acknowledgement is printed from here'])
icard(90,1020,920,110,'search','URL 3 client: status check',['Asks the department or Sakala (GSE) for the status of an application','Request carries the GSC number'],'#9B83F0')
icard(1330,490,980,130,'door','URL 1: Department URL',['Accepts a POST form: UserId, edcsApplicationRefNo, Token, assistedmode,','ServiceId, plus mobile and digilockerid when the department needs them. Answers HTTP 200.'],'#D9779A')
need(1690,490+84,'Understanding needed: how the token is checked')
icard(1330,650,980,110,'form','Application form and fee',['The citizen fills the form and pays the department fee on the department\'s own site'],'#D9779A')
icard(1330,790,980,110,'doc','Final acknowledgement',['The department generates the acknowledgement and calls URL 2 on SevaOne','If the call fails, it must call again until it succeeds'],'#D9779A')
icard(1330,930,980,110,'search','URL 3: status check API',['Provided by the department, or by Sakala (GSE)','Looks an application up by its GSC number'],'#D9779A')
need(1990,930+76,'Understanding needed: response format')
# arrows
rail([(310,290),(310,345),(560,345)]); rail([(795,290),(795,345),(560,345)]); arrow([(560,345),(560,480)],None)
pill(560,372,'log in: citizen or assisted mode'); num(560,418,1)
arrow([(1010,545),(1330,545)],None); num(1056,545,2)
pill(1180,512,'browser POST form',w=170)
arrow([(1820,620),(1820,650)],None); num(1850,635,3)
arrow([(1820,760),(1820,790)],None); num(1850,775,4)
rail([(1330,845),(1180,845),(1180,715)]); arrow([(1180,715),(1010,715)],None); num(1230,845,5)
pill(1180,778,'POST JSON · Basic auth',w=190,stroke='#C4B5FD',col='#4C1D95')
arrow([(550,790),(550,830)],None); num(590,812,6)
rail([(1330,705),(1260,705),(1260,590)],col='#7C3AED',dash=True); arrow([(1260,590),(1010,590)],None,dash=True,col='#7C3AED',mk='ap'); num(1290,705,7)
pill(1135,572,'redirect back to SevaOne',w=200,stroke='#C4B5FD',col='#6D28D9')
rail([(1010,1075),(1200,1075),(1200,985)]); arrow([(1200,985),(1330,985)],None); num(1000,1100,8)
pill(1200,1040,'POST { GscNumber }',w=160)
# whitelist chip
add('<rect x="1100" y="440" width="190" height="52" rx="12" fill="#FEF2F2" stroke="#F87171"/><text x="1195" y="462" font-size="12.5" font-weight="700" fill="#B91C1C" text-anchor="middle">IP allow-list in both</text><text x="1195" y="480" font-size="12.5" font-weight="700" fill="#B91C1C" text-anchor="middle">directions</text>')
add('</svg>')
svg='\n'.join(out)
open(os.path.join(here,'SevaOne_Token_HLD.svg'),'w').write(svg)
page=f'<!doctype html><html><head><meta charset="utf-8"><style>@page{{size:{W}px {H}px;margin:0}}html,body{{margin:0;padding:0;background:#fff}}svg{{display:block}}</style></head><body>{svg}</body></html>'
open(os.path.join(here,'token_hld.html'),'w').write(page)
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--no-pdf-header-footer',f'--print-to-pdf={here}/SevaOne_Token_HLD.pdf',f'file://{here}/token_hld.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--hide-scrollbars','--force-device-scale-factor=2',f'--window-size={W},{H}',f'--screenshot={here}/SevaOne_Token_HLD.png',f'file://{here}/token_hld.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
print('done')
