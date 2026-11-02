import base64,os,subprocess,html
here=os.path.dirname(os.path.abspath(__file__))
E=html.escape
def logo(name):
    d=open(os.path.join(here,'logos',name+'.svg'),'rb').read()
    return 'data:image/svg+xml;base64,'+base64.b64encode(d).decode()
W,H=2400,1560
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
add('<text x="48" y="45" font-size="32" font-weight="700" fill="#FFFFFF">SevaOne: Department Integration (WSO2)</text>')
add('<text x="48" y="73" font-size="15" fill="#C7D2FE">Current implementation. How a department calls SevaOne: WSO2 API Manager, WSO2 Identity Server and the .NET backend.</text>')
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
gbox(60,130,520,190,'A','SevaOne web portal','#EAF2FF','#6E96E8')
gbox(1000,130,1340,190,'B','Department systems','#FBEFF4','#D9779A')
gbox(1000,430,700,170,'C','Edge','#E8F7EF','#4DB583')
gbox(1180,700,620,480,'D','Gateway and identity','#FFF3E6','#EE9A4D')
gbox(60,740,1040,430,'E','SevaOne backend (.NET 10)','#F1EDFF','#9B83F0')
add('<rect x="60" y="1240" width="1040" height="290" rx="20" fill="#E6F6F7" stroke="#4BB6BD" stroke-width="1.8"/>')
add('<circle cx="90" cy="1500" r="15" fill="#4BB6BD"/><text x="90" y="1506" font-size="16" font-weight="800" fill="#fff" text-anchor="middle">F</text><text x="116" y="1507" font-size="19" font-weight="700" fill="#1E293B" letter-spacing="0.3">Data</text>')
# cards
card(90,180,460,110,'SevaOne citizen portal',['React 19 · "Apply" button','Sends the citizen to the department'],lg='react',accent='#6E96E8')
card(1030,180,620,110,'Department portal',['The citizen completes the real form','and pays here, on the department\'s own site'],accent='#D9779A')
card(1690,180,620,110,'Department backend',['Server-to-server calls to SevaOne','Holds its ApiKey and Client ID / secret'],accent='#D9779A')
card(1030,480,640,100,'NGINX reverse proxy',['Routes /api/v1/dept/* to the department API on the gateway'],lg='nginx',accent='#4DB583')
card(1210,760,560,140,'WSO2 API Manager 4.7',['API "SevaOne-API" · gateway :8243 · /sevaone/1.0.0','Checks the ApiKey (api_key-only security scheme)','Stamps X-SevaOne-Dept-Gateway-Secret on dept/* calls'],wso2=True,accent='#EE9A4D')
card(1210,930,560,200,'WSO2 Identity Server 7.3',['One client_credentials app per department (11)','Scope dept:<code> names the department','Token endpoint and introspection endpoint :9443','Departments: revenue, health, rdpr, kaic, food,','transport, education, social-welfare, wcd, labour,','agriculture'],wso2=True,accent='#EE9A4D')
# backend inner
add('<rect x="90" y="790" width="980" height="64" rx="12" fill="#EDE9FE" stroke="#C4B5FD"/><text x="580" y="817" font-size="15" font-weight="700" fill="#4C1D95" text-anchor="middle">Department endpoints under /api/v1/dept</text><text x="580" y="840" font-size="14" fill="#4C1D95" text-anchor="middle">user-details · payment-status · get-acknowledgement · status-update · test-token</text>')
steps=[('1','DeptGatewayAuthMiddleware','Checks the stamped gateway secret. No secret, no entry: it fails closed.'),('2','DeptController: who is calling','Introspects the Bearer token, reads the dept:<code> scope, and checks the body departmentId matches.'),('3','DeptTokenGate: is the citizen token genuine','Decrypts the one-time token, looks up its hash, verifies the JWS signature and the department, and redeems it once.')]
for i,(n_,t,d) in enumerate(steps):
    y=872+i*98
    add(f'<g filter="url(#sh)"><rect x="90" y="{y}" width="980" height="86" rx="12" fill="#fff" stroke="#9B83F0" stroke-width="1.6"/></g><circle cx="124" cy="{y+43}" r="16" fill="#9B83F0"/><text x="124" y="{y+49}" font-size="16" font-weight="800" fill="#fff" text-anchor="middle">{n_}</text><text x="156" y="{y+34}" font-size="19" font-weight="700" fill="#0F172A">{E(t)}</text><text x="156" y="{y+60}" font-size="14" fill="#475569">{E(d)}</text>')
# data
cyl(100,1290,360,150,'Redis 8',['Introspection verdicts, kept at most 60 s','Data-protection keys (decrypt apply tokens)'],lg='redis')
cyl(520,1290,540,150,'PostgreSQL 18',['portal.sevaone_token: hashed one-time tokens','orchestration.applications: the application row','status timeline and audit log'],lg='postgresql')
# arrows
arrow([(550,222),(1030,222)],None); add('<rect x="610" y="190" width="360" height="24" rx="12" fill="#fff" stroke="#CBD5E1"/><text x="790" y="206" font-size="12" font-weight="600" fill="#334155" text-anchor="middle">redirect + encrypted one-time token (30 min)</text>')
arrow([(1030,268),(550,268)],None,dash=True,col='#7C3AED',mk='ap'); add('<rect x="590" y="276" width="400" height="24" rx="12" fill="#fff" stroke="#C4B5FD"/><text x="790" y="292" font-size="12" font-weight="600" fill="#6D28D9" text-anchor="middle">redirect back with completion token (10 min)</text>')
num(588,222,1); num(590,268,8)
rail([(1900,290),(1900,385),(1350,385)]); arrow([(1350,385),(1350,480)],None)
add('<rect x="1520" y="361" width="360" height="24" rx="12" fill="#fff" stroke="#CBD5E1"/><text x="1700" y="377" font-size="12" font-weight="600" fill="#334155" text-anchor="middle">POST /api/v1/dept/* · ApiKey + Bearer token</text>')
num(1900,330,3)
arrow([(1450,580),(1450,760)],None); add('<rect x="1360" y="658" width="180" height="24" rx="12" fill="#fff" stroke="#CBD5E1"/><text x="1450" y="674" font-size="12" font-weight="600" fill="#334155" text-anchor="middle">to the department API</text>')
rail([(2200,290),(2200,1030)],col='#EA580C'); arrow([(2200,1030),(1770,1030)],None,col='#EA580C',mk='ao')
add('<rect x="2060" y="640" width="280" height="44" rx="12" fill="#fff" stroke="#F2B173"/><text x="2200" y="660" font-size="12" font-weight="600" fill="#9A3412" text-anchor="middle">client_credentials, scope dept:&lt;code&gt;</text><text x="2200" y="676" font-size="12" font-weight="600" fill="#9A3412" text-anchor="middle">returns the Bearer token</text>')
num(2200,330,2)
arrow([(1210,890),(1070,890)],None); add('<rect x="1096" y="856" width="104" height="24" rx="12" fill="#fff" stroke="#CBD5E1"/><text x="1148" y="872" font-size="11.5" font-weight="600" fill="#334155" text-anchor="middle">forward + secret</text>')
num(1140,918,4)
arrow([(1070,1013),(1210,1013)],None)
add('<rect x="1090" y="975" width="120" height="24" rx="12" fill="#fff" stroke="#CBD5E1"/><text x="1150" y="991" font-size="11.5" font-weight="600" fill="#334155" text-anchor="middle">introspect Bearer</text>')
num(1140,1046,5)
arrow([(280,1170),(280,1290)],None); add('<rect x="130" y="1196" width="300" height="24" rx="12" fill="#fff" stroke="#CBD5E1"/><text x="280" y="1212" font-size="12" font-weight="600" fill="#334155" text-anchor="middle">cache the verdict (60 s)  ·  keys</text>')
num(280,1236,6)
arrow([(790,1170),(790,1290)],None); add('<rect x="560" y="1196" width="460" height="24" rx="12" fill="#fff" stroke="#CBD5E1"/><text x="790" y="1212" font-size="12" font-weight="600" fill="#334155" text-anchor="middle">token lookup · single-use redeem · application · status</text>')
num(790,1236,7)
add('</svg>')
svg='\n'.join(out)
open(os.path.join(here,'SevaOne_Dept_HLD.svg'),'w').write(svg)
page=f'<!doctype html><html><head><meta charset="utf-8"><style>@page{{size:{W}px {H}px;margin:0}}html,body{{margin:0;padding:0;background:#fff}}svg{{display:block}}</style></head><body>{svg}</body></html>'
open(os.path.join(here,'dept_hld.html'),'w').write(page)
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--no-pdf-header-footer',f'--print-to-pdf={here}/SevaOne_Dept_HLD.pdf',f'file://{here}/dept_hld.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--hide-scrollbars','--force-device-scale-factor=2',f'--window-size={W},{H}',f'--screenshot={here}/SevaOne_Dept_HLD.png',f'file://{here}/dept_hld.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
print('done')
