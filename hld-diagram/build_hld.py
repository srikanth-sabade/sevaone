import base64,os,subprocess,html
here=os.path.dirname(os.path.abspath(__file__))
E=html.escape
def logo(name):
    d=open(os.path.join(here,'logos',name+'.svg'),'rb').read()
    return 'data:image/svg+xml;base64,'+base64.b64encode(d).decode()
W,H=1920,1222
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
# ---------- header ----------
add(f'<rect x="0" y="0" width="{W}" height="92" fill="url(#hdr)"/>')
add('<text x="40" y="45" font-size="31" font-weight="700" fill="#FFFFFF">SevaOne: High-Level Architecture</text>')
add('<text x="40" y="72" font-size="15" fill="#C7D2FE">Current implementation. Drawn from the repository and the running stack.</text>')
# legend
lx=1180
add(f'<g font-size="13" fill="#E2E8F0"><rect x="{lx}" y="26" width="26" height="16" rx="4" fill="#fff"/><text x="{lx+34}" y="39">Service</text>'
    f'<path d="M{lx+110} 34 h30" stroke="#fff" stroke-width="2" marker-end="url(#ar)"/><text x="{lx+148}" y="39">Request</text>'
    f'<path d="M{lx+225} 34 h30" stroke="#fff" stroke-width="2" stroke-dasharray="6 4" marker-end="url(#ar)"/><text x="{lx+263}" y="39">Event or callback</text>'
    f'<rect x="{lx+405}" y="26" width="26" height="16" rx="4" fill="none" stroke="#fff" stroke-dasharray="4 3"/><text x="{lx+439}" y="39">Disabled or off-box</text>'
    f'<ellipse cx="{lx+13}" cy="64" rx="13" ry="5" fill="#C7D2FE"/><path d="M{lx} 64 v8 a13 5 0 0 0 26 0 v-8" fill="#A5B4FC"/><text x="{lx+34}" y="72">Data store</text>'
    f'<rect x="{lx+120}" y="58" width="44" height="18" rx="9" fill="#2563EB"/><text x="{lx+130}" y="71" font-size="11" font-weight="700" fill="#fff">WEB</text>'
    f'<rect x="{lx+172}" y="58" width="58" height="18" rx="9" fill="#EA580C"/><text x="{lx+181}" y="71" font-size="11" font-weight="700" fill="#fff">MOBILE</text>'
    f'<rect x="{lx+238}" y="58" width="34" height="18" rx="9" fill="#7C3AED"/><text x="{lx+247}" y="71" font-size="11" font-weight="700" fill="#fff">AI</text>'
    f'<text x="{lx+282}" y="72">Which channel uses an outside system</text></g>')
# ---------- tiers ----------
tiers=[('1','Clients and channels',112,160,'#EAF2FF','#93B4F0'),('2','Edge and security',290,96,'#E8F7EF','#7CCBA0'),('3','Gateway and identity',404,150,'#FFF3E6','#F2B173'),
       ('4','Application services',572,234,'#F1EDFF','#B5A3F2'),('5','Data and streaming',824,176,'#E6F6F7','#78C7CC'),('6','External ecosystem',1018,172,'#F3F4F6','#B6BCC6')]
LP={'1':(W-52,'end',27),'2':(W-52,'end',27),'3':(1200,'start',27),'4':(1640,'end',24),'5':(1060,'middle',17),'6':(215,'start',44)}
for n,t,y,h,fill,stroke in tiers:
    add(f'<rect x="28" y="{y}" width="{W-56}" height="{h}" rx="16" fill="{fill}" stroke="{stroke}" stroke-width="1.4"/>')
    lx_,anc,dy=LP[n]
    add(f'<text x="{lx_}" y="{y+dy}" font-size="14" font-weight="700" fill="#334155" letter-spacing="0.4" text-anchor="{anc}">{n}.  {E(t.upper())}</text>')
def card(x,y,w,h,title,lines,lg=None,chips=(),dashed=False,wso2=False,accent='#CBD5E1',tsize=17):
    ds=' stroke-dasharray="6 4"' if dashed else ''
    add(f'<g filter="url(#sh)"><rect x="{x}" y="{y}" width="{w}" height="{h}" rx="12" fill="#fff" stroke="{accent}" stroke-width="1.6"{ds}/></g>')
    tx=x+16
    if lg:
        add(f'<image href="{logo(lg)}" x="{x+14}" y="{y+14}" width="34" height="34"/>'); tx=x+60
    if wso2:
        add(f'<rect x="{x+12}" y="{y+16}" width="40" height="30" rx="7" fill="#FF7300"/><text x="{x+32}" y="{y+36}" font-size="13" font-weight="800" fill="#fff" text-anchor="middle">WSO2</text>'); tx=x+62
    add(f'<text x="{tx}" y="{y+32}" font-size="{tsize}" font-weight="700" fill="#0F172A">{E(title)}</text>')
    for i,l in enumerate(lines):
        add(f'<text x="{tx}" y="{y+52+i*17}" font-size="12.5" fill="#475569">{E(l)}</text>')
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
    add(f'<text x="{tx}" y="{y+ry+28}" font-size="16.5" font-weight="700" fill="#1E1B4B">{E(title)}</text>')
    for i,l in enumerate(lines):
        add(f'<text x="{tx}" y="{y+ry+46+i*16}" font-size="12" fill="#3F3F63">{E(l)}</text>')
def arrow(pts,label=None,lp=None,dash=False,col='#475569',mk='ar',both=False,w=2):
    d='M'+' L'.join(f'{x} {y}' for x,y in pts)
    ds=' stroke-dasharray="7 5"' if dash else ''
    ms=f' marker-end="url(#{mk})"'+(f' marker-start="url(#{mk})"' if both else '')
    add(f'<path d="{d}" fill="none" stroke="{col}" stroke-width="{w}" stroke-linejoin="round"{ds}{ms}/>')
    if label:
        x,y=lp or pts[len(pts)//2]
        tw=len(label)*6.4+14
        add(f'<rect x="{x-tw/2}" y="{y-10}" width="{tw}" height="20" rx="10" fill="#fff" stroke="#CBD5E1"/><text x="{x}" y="{y+4}" font-size="11.5" fill="#334155" text-anchor="middle" font-weight="600">{E(label)}</text>')
# ---------- tier 1 ----------
Y1=148
card(60,Y1,330,96,'Citizen web portal',['React 19 · Vite · TanStack Router','Tailwind 4 · Zustand · i18n'],lg='react',accent='#93B4F0')
card(410,Y1,270,96,'Operator console',['Assisted / kiosk mode','Same React app'],lg='react',accent='#93B4F0')
card(700,Y1,310,96,'Admin panel',['Government admin console','React 19 SPA · analytics, catalogue'],lg='react',accent='#93B4F0')
card(1500,Y1,360,96,'Mobile app',['Flutter · Android','in.vismai.sp.sevaone'],lg='flutter',accent='#F2B173')
# ---------- tier 2 ----------
card(60,316,950,66,'NGINX reverse proxy',[],lg='nginx',accent='#7CCBA0')
add('<text x="120" y="369" font-size="12.5" fill="#475569">TLS 1.2 / 1.3 · security headers (CSP, frame deny) · routes /api to the gateway, everything else to the SPA</text>')
# ---------- tier 3 ----------
card(60,442,520,92,'WSO2 API Manager 4.7',['Gateway :8243 · API products sevaone-citizen and sevaone','API keys · gateway secret stamped on every call'],wso2=True,accent='#F2B173')
card(620,442,540,92,'WSO2 Identity Server 7.3',['SCIM2 · OAuth2 JWT-bearer (RFC 7523)','Issuers: citizen · operator · government admin'],wso2=True,accent='#F2B173')
# ---------- tier 4 ----------
add('<g filter="url(#sh)"><rect x="60" y="610" width="1100" height="184" rx="14" fill="#fff" stroke="#B5A3F2" stroke-width="1.8"/></g>')
add(f'<image href="{logo("dotnet")}" x="76" y="624" width="38" height="38"/><text x="124" y="640" font-size="19" font-weight="700" fill="#0F172A">.NET 10 backend for frontend (SevaOne.Services)</text><text x="124" y="660" font-size="12.5" fill="#475569">Dapper + Npgsql (no ORM) · FluentValidation · Serilog · OpenTelemetry · health checks</text>')
layers=[('Services','Controllers + middleware','Auth · Catalogue · Applications · Operator · Department · Gov admin · AI chat'),('Business','Services + validators','Apply, track and department-handoff logic'),('Infrastructure','Clients + repositories','Postgres functions · WSO2 · DigiLocker · SMS · AI clients'),('Domain','Models + error codes','Stable SEVA-* error contract')]
lx0=76
widths=[330,200,260,200]
for (t,s,d),wd in zip(layers,widths):
    add(f'<rect x="{lx0}" y="678" width="{wd}" height="82" rx="10" fill="#F5F3FF" stroke="#C4B5FD"/><text x="{lx0+12}" y="700" font-size="14" font-weight="700" fill="#4C1D95">{t}</text><text x="{lx0+12}" y="718" font-size="11.5" font-weight="600" fill="#5B21B6">{E(s)}</text>')
    # wrap description
    words=d.split(); line='';ls=[]
    for wd_ in words:
        if len(line+' '+wd_)*5.9>wd-24 and line: ls.append(line); line=wd_
        else: line=(line+' '+wd_).strip()
    ls.append(line)
    for i,l in enumerate(ls[:2]): add(f'<text x="{lx0+12}" y="{736+i*15}" font-size="11" fill="#475569">{E(l)}</text>')
    lx0+=wd+10
add('<rect x="76" y="766" width="1068" height="20" rx="10" fill="#EDE9FE"/><text x="92" y="780" font-size="11.5" fill="#4C1D95" font-weight="600">Background: OutboxDispatcher reads the outbox table and publishes events to Kafka · Flyway runs the database migrations at start-up</text>')
card(1190,610,300,184,'AI services (GPU server)',['sevaone-rag: agentic RAG, :8000','vLLM model server · Bhashini Kannada','ai-admin :8090 · ai-monitor :8010','(reached over SSH tunnels)'],lg='python',chips=('AI',),dashed=True,accent='#B5A3F2')
card(1520,610,340,184,'Mobile backend (FastAPI)',['Mobile sign-in (OTP) · catalogue','Applications · documents · DigiLocker','Push notifications · grievances','Python 3 · psycopg'],lg='fastapi',chips=('MOBILE',),accent='#F2B173')
# ---------- tier 5 ----------
cyl(60,846,340,126,'PostgreSQL 18',['6 schemas: discovery, portal,','orchestration, status, audit,','notification. Function-only access.'],lg='postgresql')
cyl(430,846,230,126,'Redis 8',['Sessions and token cache','Data-protection keys'],lg='redis')
cyl(690,846,250,126,'Apache Kafka 4.3',['Transactional outbox events','Published by OutboxDispatcher'],lg='apachekafka')
cyl(970,846,180,126,'MinIO',['Object storage','Disabled for now'],lg='minio',dashed=True,grey=True)
cyl(1190,846,300,126,'Vector index',['FAISS or Qdrant','Services and knowledge passages'],lg='qdrant',dashed=True)
cyl(1520,846,340,126,'PostgreSQL and files',['Mobile catalogue, sessions','documents · local or object storage'],lg='postgresql')
# ---------- tier 6 ----------
ex=[('Department systems',['Nadakacheri (Revenue)','Department Hub · AJSK'],('WEB',)),('DigiLocker',['CeG gateway','Aadhaar OTP, eAadhaar'],('WEB','MOBILE')),('SMS gateway',['One-time passwords','Mobile Seva'],('WEB','MOBILE')),
    ('Public catalogue sites',['Sakala · ESCOM','Seva Sindhu catalogue'],('WEB','MOBILE')),('ServicePlus',['Live service catalogue','and applications'],('MOBILE',)),('Firebase',['Push messages','(FCM)'],('MOBILE',)),('Bhashini and LLM',['Kannada speech, translation','vLLM language model'],('AI','MOBILE'))]
for i,(t,l,c) in enumerate(ex):
    card(60+i*255,1078,235,104,t,l,chips=c,accent='#B6BCC6',tsize=16)
# ---------- arrows ----------
# clients -> nginx
arrow([(225,244),(225,316)],'HTTPS',(225,280))
arrow([(545,244),(545,316)],'HTTPS',(545,280))
arrow([(855,244),(855,316)],'HTTPS',(855,280))
# nginx -> apim
arrow([(300,382),(300,442)],'/api/v1/*',(300,412))
# apim <-> is
arrow([(580,488),(620,488)],None,both=True)
# apim -> bff
arrow([(300,534),(300,610)],'forwarded + secret',(300,572))
# bff -> is
arrow([(820,610),(820,534)],'SCIM2 + JWT',(820,572),both=False)
# bff -> data
arrow([(230,794),(230,846)],'Npgsql',(230,820))
arrow([(545,794),(545,846)],'sessions',(545,820))
arrow([(815,794),(815,846)],'outbox',(815,820),dash=True,col='#475569')
# bff -> ai
arrow([(1160,702),(1190,702)],None)
add('<text x="1175" y="690" font-size="10.5" fill="#334155" text-anchor="middle" font-weight="700">ai-chat</text>')
# ai -> vector
arrow([(1340,794),(1340,846)],'retrieve',(1340,820),col='#7C3AED',mk='ap')
# mobile
arrow([(1680,244),(1680,610)],'HTTPS · JWT',(1680,430),col='#EA580C',mk='ao')
arrow([(1690,794),(1690,846)],'psycopg',(1690,820),col='#EA580C',mk='ao')
# bff -> externals bus
add('<path d="M945 794 V1040 M60 1040 H1840" fill="none" stroke="#2563EB" stroke-width="2" stroke-dasharray="7 5"/>')
add('<path d="M945 794 L945 1040" stroke="#2563EB" stroke-width="2"/>')
for i in range(7):
    cxs=60+i*255+117
    col='#2563EB'
    add(f'<path d="M{cxs} 1040 V1076" stroke="{col}" stroke-width="2" marker-end="url(#ab)"/>')
add('<rect x="826" y="1026" width="238" height="22" rx="11" fill="#fff" stroke="#93B4F0"/><text x="945" y="1041" font-size="11.5" font-weight="700" fill="#1D4ED8" text-anchor="middle">outbound calls and redirects</text>')
# mobile backend to bus (right margin)
add('<path d="M1860 760 H1878 V1040 H1840" fill="none" stroke="#EA580C" stroke-width="2" stroke-dasharray="7 5"/>')
# department callbacks up the left margin
add('<path d="M60 1130 H40 V488 H60" fill="none" stroke="#7C3AED" stroke-width="2" stroke-dasharray="7 5" marker-end="url(#ap)"/>')
add('<rect x="-18" y="689" width="116" height="22" rx="11" transform="rotate(-90 40 700)" fill="#fff" stroke="#C4B5FD"/><text x="40" y="704" transform="rotate(-90 40 700)" font-size="11" font-weight="700" fill="#6D28D9" text-anchor="middle">department callbacks</text>')
add('</svg>')
svg='\n'.join(out)
open(os.path.join(here,'SevaOne_HLD.svg'),'w').write(svg)
page=f'<!doctype html><html><head><meta charset="utf-8"><style>@page{{size:{W}px {H}px;margin:0}}html,body{{margin:0;padding:0;background:#fff}}svg{{display:block}}</style></head><body>{svg}</body></html>'
open(os.path.join(here,'hld.html'),'w').write(page)
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--no-pdf-header-footer',f'--print-to-pdf={here}/SevaOne_HLD.pdf',f'file://{here}/hld.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--hide-scrollbars','--force-device-scale-factor=2',f'--window-size={W},{H}',f'--screenshot={here}/SevaOne_HLD.png',f'file://{here}/hld.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
print('done')
