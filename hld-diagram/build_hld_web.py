import base64,os,subprocess,html
here=os.path.dirname(os.path.abspath(__file__))
E=html.escape
def logo(name):
    d=open(os.path.join(here,'logos',name+'.svg'),'rb').read()
    return 'data:image/svg+xml;base64,'+base64.b64encode(d).decode()
W,H=2400,1600
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
add('<text x="48" y="45" font-size="32" font-weight="700" fill="#FFFFFF">SevaOne: High-Level Architecture (Web)</text>')
add('<text x="48" y="73" font-size="15" fill="#C7D2FE">Current implementation of the web platform, drawn from the repository and the running stack.</text>')
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
# ---- group boxes ----
gbox(980,130,1360,190,'1','Clients','#EAF2FF','#6E96E8')
gbox(980,430,700,170,'2','Edge and security','#E8F7EF','#4DB583')
gbox(1180,700,620,480,'3','Gateway and identity','#FFF3E6','#EE9A4D')
gbox(60,740,1040,420,'4','Application services','#F1EDFF','#9B83F0')
add('<rect x="320" y="1260" width="780" height="290" rx="20" fill="#E6F6F7" stroke="#4BB6BD" stroke-width="1.8"/>')
add('<circle cx="350" cy="1520" r="15" fill="#4BB6BD"/><text x="350" y="1526" font-size="16" font-weight="800" fill="#fff" text-anchor="middle">5</text><text x="376" y="1527" font-size="19" font-weight="700" fill="#1E293B" letter-spacing="0.3">Data and streaming</text>')
gbox(1240,1250,1130,250,'6','External systems','#F3F4F6','#8D96A4')
# ---- cards ----
card(1010,180,400,110,'Citizen web portal',['React 19 · Vite · TanStack Router','Tailwind 4 · Zustand'],lg='react',accent='#6E96E8')
card(1440,180,400,110,'Operator console',['Assisted / kiosk mode','Part of the same React app'],lg='react',accent='#6E96E8')
card(1870,180,400,110,'Admin panel',['Government admin console','React 19 · analytics, catalogue'],lg='react',accent='#6E96E8')
card(1010,480,640,100,'NGINX reverse proxy',['TLS 1.2 / 1.3 · security headers (CSP, frame deny)'],lg='nginx',accent='#4DB583')
card(1210,760,560,110,'WSO2 API Manager 4.7: citizen',['API product sevaone-citizen · gateway :8243','Citizen routes, API key and gateway secret'],wso2=True,accent='#EE9A4D')
card(1210,920,560,110,'WSO2 API Manager 4.7: department',['API product sevaone · gateway :8243','Operator and department routes'],wso2=True,accent='#EE9A4D')
card(1210,1060,560,100,'WSO2 Identity Server 7.3',['SCIM2 · OAuth2 JWT-bearer (RFC 7523)','Citizen, operator and admin issuers'],wso2=True,accent='#EE9A4D')
# BFF
add('<g filter="url(#sh)"><rect x="470" y="800" width="600" height="320" rx="14" fill="#fff" stroke="#9B83F0" stroke-width="1.8"/></g>')
add(f'<image href="{logo("dotnet")}" x="488" y="816" width="40" height="40"/><text x="540" y="840" font-size="21" font-weight="700" fill="#0F172A">.NET 10 backend (SevaOne.Services)</text><text x="540" y="865" font-size="14" fill="#475569">Dapper + Npgsql · FluentValidation · Serilog · OpenTelemetry</text>')
chips=[('Services','Controllers and middleware'),('Business','Apply, track, department handoff'),('Infrastructure','Postgres, WSO2, DigiLocker, SMS clients'),('Domain','Models and error codes')]
for i,(t,d) in enumerate(chips):
    cx=490+(i%2)*285; cy=884+(i//2)*86
    add(f'<rect x="{cx}" y="{cy}" width="270" height="74" rx="10" fill="#F5F3FF" stroke="#C4B5FD"/><text x="{cx+14}" y="{cy+28}" font-size="17" font-weight="700" fill="#4C1D95">{t}</text><text x="{cx+14}" y="{cy+52}" font-size="13.5" fill="#475569">{E(d)}</text>')
add('<rect x="490" y="1062" width="560" height="42" rx="10" fill="#EDE9FE"/><text x="770" y="1088" font-size="13.5" font-weight="600" fill="#4C1D95" text-anchor="middle">OutboxDispatcher publishes events to Kafka · Flyway migrations at start-up</text>')
card(100,800,330,320,'AI services',['GPU server, own network','','sevaone-rag: agentic RAG','vLLM language model','FAISS or Qdrant index','Bhashini for Kannada','','ai-admin and ai-monitor','over SSH tunnels'],lg='python',dashed=True,accent='#9B83F0')
# data
cyl(340,1320,260,150,'PostgreSQL 18',['6 schemas: discovery, portal,','orchestration, status, audit,','notification','Function-only access'],lg='postgresql')
cyl(625,1320,200,150,'Redis 8',['Sessions','Token-check cache','Data-protection keys'],lg='redis')
cyl(850,1320,230,150,'Apache Kafka 4.3',['Transactional outbox','events'],lg='apachekafka')
# external
ex=[('DigiLocker',['CeG gateway','Aadhaar OTP, eAadhaar']),('SMS gateway',['One-time passwords','Mobile Seva']),('Public catalogue sites',['Sakala · ESCOM','Seva Sindhu catalogue']),('Department systems',['Nadakacheri (Revenue)','Department Hub · AJSK'])]
for i,(t,l) in enumerate(ex):
    card(1270+i*270,1330,250,130,t,l,accent='#8D96A4',tsize=18)
# ---- arrows ----
rail([(1210,290),(1210,385),(1330,385)]); rail([(1640,290),(1640,385),(1330,385)]); rail([(2070,290),(2070,385),(1330,385)])
arrow([(1330,385),(1330,480)],'HTTPS',(1330,432))
arrow([(1450,580),(1450,760)],'citizen routes',(1450,670))
rail([(1650,530),(1850,530),(1850,975)]); arrow([(1850,975),(1770,975)],None)
add('<rect x="1730" y="640" width="240" height="24" rx="12" fill="#fff" stroke="#CBD5E1"/><text x="1850" y="656" font-size="11.5" font-weight="600" fill="#334155" text-anchor="middle">operator + department routes</text>')
arrow([(1210,815),(1070,815)],'forwarded + secret',(1140,790))
arrow([(1210,975),(1070,975)],'forwarded + secret',(1140,950))
arrow([(1070,1110),(1210,1110)],'SCIM2 + JWT',(1140,1086),both=True)
arrow([(470,960),(430,960)],None)
add('<rect x="403" y="925" width="94" height="22" rx="11" fill="#fff" stroke="#CBD5E1"/><text x="450" y="940" font-size="11.5" font-weight="600" fill="#334155" text-anchor="middle">ai-chat proxy</text>')
arrow([(500,1120),(500,1320)],'Npgsql · functions',(500,1220))
arrow([(725,1120),(725,1320)],'sessions · cache',(725,1220))
arrow([(965,1120),(965,1320)],'outbox events',(965,1220),dash=True)
rail([(1050,1120),(1050,1215),(1500,1215),(1500,1308)])
add('<path d="M1500 1215 V1295" stroke="#2563EB" stroke-width="0"/>')
rail([(1395,1308),(2205,1308)],col='#2563EB')
for cx in (1395,1665,1935,2205): add(f'<path d="M{cx} 1308 V1328" stroke="#2563EB" stroke-width="2.2" marker-end="url(#ab)"/>')
add('<rect x="1190" y="1203" width="236" height="22" rx="11" fill="#fff" stroke="#93B4F0"/><text x="1308" y="1218" font-size="11.5" font-weight="700" fill="#1D4ED8" text-anchor="middle">outbound calls and redirects</text>')
rail([(2290,1330),(2290,1005),(1770,1005)],col='#7C3AED',dash=True); add('<path d="M1778 1005 L1770 1005" stroke="#7C3AED" stroke-width="2" marker-end="url(#ap)"/>')
add('<rect x="2185" y="1100" width="210" height="24" rx="12" fill="#fff" stroke="#C4B5FD"/><text x="2290" y="1116" font-size="11.5" font-weight="700" fill="#6D28D9" text-anchor="middle">department callbacks</text>')
add('</svg>')
svg='\n'.join(out)
open(os.path.join(here,'SevaOne_HLD_Web.svg'),'w').write(svg)
page=f'<!doctype html><html><head><meta charset="utf-8"><style>@page{{size:{W}px {H}px;margin:0}}html,body{{margin:0;padding:0;background:#fff}}svg{{display:block}}</style></head><body>{svg}</body></html>'
open(os.path.join(here,'hld_web.html'),'w').write(page)
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--no-pdf-header-footer',f'--print-to-pdf={here}/SevaOne_HLD_Web.pdf',f'file://{here}/hld_web.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--hide-scrollbars','--force-device-scale-factor=2',f'--window-size={W},{H}',f'--screenshot={here}/SevaOne_HLD_Web.png',f'file://{here}/hld_web.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
print('done')
