import base64,os,subprocess,html
here=os.path.dirname(os.path.abspath(__file__))
E=html.escape
def logo(name):
    d=open(os.path.join(here,'logos',name+'.svg'),'rb').read()
    return 'data:image/svg+xml;base64,'+base64.b64encode(d).decode()
W,H=2600,1600
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
add('<text x="48" y="45" font-size="32" font-weight="700" fill="#FFFFFF">Token-Based Integration on WSO2: Our Implementation</text>')
add('<text x="48" y="73" font-size="15" fill="#C7D2FE">SevaOne takes the Seva Sindhu role. Departments change nothing. WSO2 Identity Server and API Manager carry the token, the callback and the status check.</text>')
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

def wcard(x,y,w,h,title,lines,accent='#EE9A4D',tsize=20,fs=14.5):
    add(f'<g filter="url(#sh)"><rect x="{x}" y="{y}" width="{w}" height="{h}" rx="12" fill="#fff" stroke="{accent}" stroke-width="1.8"/></g>')
    add(f'<rect x="{x+14}" y="{y+14}" width="46" height="32" rx="7" fill="#FF7300"/><text x="{x+37}" y="{y+35}" font-size="14" font-weight="800" fill="#fff" text-anchor="middle">WSO2</text>')
    add(f'<text x="{x+74}" y="{y+37}" font-size="{tsize}" font-weight="700" fill="#0F172A">{E(title)}</text>')
    for i,l in enumerate(lines): add(f'<text x="{x+24}" y="{y+72+i*22}" font-size="{fs}" fill="#475569">{E(l)}</text>')

gbox(60,130,1060,190,'1','Users and portal','#EAF2FF','#6E96E8')
gbox(60,420,720,150,'2','Edge','#E8F7EF','#4DB583')
gbox(60,640,720,570,'3','SevaOne backend (.NET 10)','#F1EDFF','#9B83F0')
gbox(860,420,780,680,'4','WSO2: the integration backbone','#FFF3E6','#EE9A4D')
gbox(1780,130,780,1050,'5','Department systems (no change needed)','#FBEFF4','#D9779A')
add('<rect x="60" y="1260" width="1580" height="300" rx="20" fill="#E6F6F7" stroke="#4BB6BD" stroke-width="1.8"/>')
add('<circle cx="90" cy="1530" r="15" fill="#4BB6BD"/><text x="90" y="1536" font-size="16" font-weight="800" fill="#fff" text-anchor="middle">6</text><text x="116" y="1537" font-size="19" font-weight="700" fill="#1E293B" letter-spacing="0.3">Data and streaming</text>')
# users
icard(90,180,300,110,'user','Citizen',['Logs in with OTP or DigiLocker'],'#6E96E8')
icard(410,180,300,110,'user','Assisted-mode operator',['Franchise login (K1, B1, G1,','AJSK, BSK)'],'#6E96E8')
icard(730,180,360,110,'form','SevaOne web portal',['React 19 · Apply button and','operator console'],'#6E96E8')
card(90,470,660,80,'NGINX reverse proxy',['TLS · routes portal and API traffic'],lg='nginx',accent='#4DB583',tsize=19)
# backend cards
icard(90,700,660,150,'door','Handoff service',['Creates the unique edcsApplicationRefNo and asks WSO2 Identity','Server for the Token. Builds the auto-submitting POST form:','UserId, edcsApplicationRefNo, Token, assistedmode, ServiceId.'],'#9B83F0')
need(110,700+118,'Understanding needed: edcsApplicationRefNo format')
icard(90,870,660,170,'code','URL 2 receiver (TokenIntegration controller)',['Takes the call that the gateway has already screened. Checks the','Token with WSO2 Identity Server, matches it to the stored handoff and','the department, saves the payment and acknowledgement once,','sets SUCCESS, and answers in the contract\'s format.'],'#9B83F0')
icard(90,1060,660,130,'search','Status service (URL 3 client)',['Asks each department or Sakala (GSE) for the status of an','application by GSC number, through the gateway.'],'#9B83F0')
# WSO2 cards
wcard(890,480,720,260,'WSO2 Identity Server 7.3',['Login: citizen, operator and admin issuers (already in place)','Mints the Token: OAuth2 access token for each handoff, with the','department and service in its scope. Opaque, single use, 30 minutes.','Introspection: is the Token active, and for which department?','Revocation: the Token is revoked after a successful URL 2 save.','Kill switch: disable a department\'s account or app, effect is live.'])
wcard(890,770,720,300,'WSO2 API Manager 4.7',['Inbound API: /api/TokenIntegration/department-update, same path','and JSON as the contract. Checks Basic credentials, IP allow-list,','throttle tier per department and payload size. Stamps the gateway secret.','Outbound APIs: one per department for URL 3. Retry, timeout and','throttling live here. Analytics and a department application for each.','Optional: a Token-check API backed by IS introspection.'])
need(900,770+266,'Understanding needed: how departments check the Token today')
# dept cards
icard(1810,190,720,140,'door','URL 1: Department URL',['Accepts the POST form and answers HTTP 200.','No change: this is what Seva Sindhu sends today.'],'#D9779A')
need(1830,190+112,'Understanding needed: our server IPs on their allow-lists')
icard(1810,360,720,100,'form','Application form and fee',['The citizen fills the form and pays on the department\'s site'],'#D9779A')
icard(1810,490,720,140,'doc','Final acknowledgement and URL 2 call',['Generates the acknowledgement and posts it with Basic auth.','Calls again if SevaOne answers "Data Not Inserted".'],'#D9779A')
need(1830,490+112,'Understanding needed: the address the departments call')
icard(1810,660,720,150,'search','URL 3: status check API',['Provided by the department, or by Sakala (GSE).','Looks an application up by GSC number.'],'#D9779A')
need(1830,660+118,'Understanding needed: its authentication and response')
# data
cyl(90,1330,360,170,'PostgreSQL 18',['Handoffs, token hashes, applications','Payment and acknowledgement records','Status timeline and outbox'],lg='postgresql')
cyl(480,1330,300,170,'Redis 8',['Token verdicts, 60 s at most','Data-protection keys'],lg='redis')
cyl(810,1330,330,170,'Apache Kafka 4.3',['Events: acknowledgement received,','status changed'],lg='apachekafka')
# arrows
rail([(1090,235),(1090,235)])
arrow([(1090,235),(1810,235)],None); num(1130,235,2); pill(1450,205,'browser POST form: UserId, edcsApplicationRefNo, Token, assistedmode, ServiceId',w=620)
arrow([(1810,285),(1090,285)],None,dash=True,col='#7C3AED',mk='ap'); num(1770,285,6); pill(1450,315,'redirect back to SevaOne with the acknowledgement',w=380,stroke='#C4B5FD',col='#6D28D9')
rail([(880,290),(880,380),(420,380)]); arrow([(420,380),(420,470)],None); num(420,425,1); pill(650,380,'log in, apply: HTTPS',w=190)
arrow([(420,550),(420,700)],None)
# 2 mint token
rail([(750,740),(815,740),(815,540)]); arrow([(815,540),(890,540)],None,col='#EA580C',mk='ao'); num(785,740,2)
pill(845,512,'mint Token',w=110,stroke='#F2B173',col='#9A3412')
# 3 URL2 from dept to APIM
rail([(1810,560),(1710,560),(1710,900)]); arrow([(1710,900),(1610,900)],None); num(1710,600,3)
pill(1710,520,'POST department-update',w=190); pill(1710,545-0,'',w=1) if False else None
add('<rect x="1664" y="470" width="92" height="22" rx="11" fill="#fff" stroke="#CBD5E1"/><text x="1710" y="485" font-size="11.5" font-weight="600" fill="#334155" text-anchor="middle">Basic auth</text>')
# 4 APIM to BFF
arrow([(890,960),(750,960)],None); num(820,985,4); pill(820,935,'forward + secret',w=130)
# 5 BFF to IS introspect
rail([(750,900),(850,900),(850,660)]); arrow([(850,660),(890,660)],None); num(850,780,5); pill(1000,... if False else 0,0,'') if False else None
add('<rect x="906" y="690" width="190" height="22" rx="11" fill="#fff" stroke="#CBD5E1"/><text x="1001" y="705" font-size="11.5" font-weight="600" fill="#334155" text-anchor="middle">introspect and revoke Token</text>')
# basic creds IS-APIM small arrow
arrow([(1250,740),(1250,770)],None,both=True); pill(1420,755,'Basic credentials and accounts kept in WSO2',w=330)
# data arrows
arrow([(240,1190),(240,1330)],None); num(240,1230,'7')
arrow([(570,1190),(570,1330)],None)
arrow([(740,1190),(740,1330)],None,dash=True)
pill(330,1250,'save · outbox',w=130); pill(570,1250,'cache',w=70); pill(790,1250,'events',w=90)
# 8 URL3 via APIM
rail([(750,1110),(850,1110),(850,1010)]); arrow([(850,1010),(890,1010)],None); num(790,1135,8)
rail([(1250,1070),(1250,1225),(2150,1225),(2150,810)]); add('<path d="M2150 818 L2150 810" stroke="#475569" stroke-width="2" marker-end="url(#ar)"/>')
pill(1700,1225,'outbound status API: retry, timeout, throttle  ·  POST { GscNumber }',w=520)
add('<rect x="1180" y="436" width="0" height="0"/>')
add('</svg>')
svg='\n'.join(out)
open(os.path.join(here,'SevaOne_Wso2_HLD.svg'),'w').write(svg)
page=f'<!doctype html><html><head><meta charset="utf-8"><style>@page{{size:{W}px {H}px;margin:0}}html,body{{margin:0;padding:0;background:#fff}}svg{{display:block}}</style></head><body>{svg}</body></html>'
open(os.path.join(here,'wso2_hld.html'),'w').write(page)
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--no-pdf-header-footer',f'--print-to-pdf={here}/SevaOne_Wso2_HLD.pdf',f'file://{here}/wso2_hld.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--hide-scrollbars','--force-device-scale-factor=2',f'--window-size={W},{H}',f'--screenshot={here}/SevaOne_Wso2_HLD.png',f'file://{here}/wso2_hld.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
print('done')
