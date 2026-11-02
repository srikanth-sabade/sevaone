import os,subprocess
src=open(os.path.join(os.path.dirname(os.path.abspath(__file__)),'hld_common.py')).read().replace("W,H=2400,1600","W,H=3640,2480")
exec(src)
def wrap(text,width_px,fs=14):
    cpl=max(int(width_px/(fs*0.53)),8); words=text.split(); ls=[];cur=''
    for w_ in words:
        if len(cur)+len(w_)+1>cpl and cur: ls.append(cur);cur=w_
        else: cur=(cur+' '+w_).strip()
    if cur: ls.append(cur)
    return ls
def mod(x,y,w,h,title,lines,tag='',unk=False,accent='#CBD5E1',ic=None,lg=None,wso2=False):
    ds=' stroke-dasharray="7 5"' if unk else ''
    col='#B7791F' if unk else accent
    add(f'<g filter="url(#sh)"><rect x="{x}" y="{y}" width="{w}" height="{h}" rx="12" fill="{"#FFFBF2" if unk else "#fff"}" stroke="{col}" stroke-width="1.7"{ds}/></g>')
    tagw=(len(tag)*7+16+14) if tag else 0
    tx=x+16
    if lg: add(f'<image href="{logo(lg)}" x="{x+12}" y="{y+10}" width="28" height="28"/>'); tx=x+48
    if wso2: add(f'<rect x="{x+12}" y="{y+10}" width="40" height="26" rx="6" fill="#FF7300"/><text x="{x+32}" y="{y+28}" font-size="12" font-weight="800" fill="#fff" text-anchor="middle">WSO2</text>'); tx=x+60
    add(f'<text x="{tx}" y="{y+30}" font-size="19" font-weight="700" fill="#0F172A">{E(title)}</text>')
    yy=y+58
    for l in lines:
        for ln in wrap(l,w-32): add(f'<text x="{x+16}" y="{yy}" font-size="14" fill="#475569">{E(ln)}</text>'); yy+=19
    if tag:
        tw=len(tag)*7+16; add(f'<rect x="{x+w-tw-10}" y="{y+h-30}" width="{tw}" height="20" rx="10" fill="#E2E8F0"/><text x="{x+w-tw/2-10}" y="{y+h-16}" font-size="11.5" font-weight="700" fill="#334155" text-anchor="middle">{tag}</text>')

add(f'<rect x="0" y="0" width="{W}" height="92" fill="url(#hdr)"/>')
add('<text x="48" y="45" font-size="32" font-weight="700" fill="#FFFFFF">Service Plus: Proposed Architecture</text>')
add('<text x="48" y="73" font-size="15" fill="#C7D2FE">All 25 modules on WSO2 Identity Server and API Manager, with an event backbone, one core database and a separate integration layer.</text>')
add('<g font-size="13.5" fill="#E2E8F0"><rect x="2960" y="30" width="26" height="16" rx="4" fill="#fff"/><text x="2994" y="43">Service</text><path d="M3090 38 h30" stroke="#fff" stroke-width="2" marker-end="url(#ar)"/><text x="3128" y="43">Request</text><path d="M3210 38 h30" stroke="#fff" stroke-width="2" stroke-dasharray="6 4" marker-end="url(#ar)"/><text x="3248" y="43">Event</text><text x="2960" y="76">M1 to M25 are the module numbers.</text></g>')
def gbox(x,y,w,h,num,title,fill,stroke):
    add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="20" fill="{fill}" stroke="{stroke}" stroke-width="1.8"/>')
    add(f'<circle cx="{x+30}" cy="{y+30}" r="15" fill="{stroke}"/><text x="{x+30}" y="{y+36}" font-size="16" font-weight="800" fill="#fff" text-anchor="middle">{num}</text><text x="{x+56}" y="{y+37}" font-size="19" font-weight="700" fill="#1E293B" letter-spacing="0.3">{E(title)}</text>')
rail=lambda pts,col='#475569',dash=False: add('<path d="M'+' L'.join(f'{x} {y}' for x,y in pts)+f'" fill="none" stroke="{col}" stroke-width="2.4" stroke-linejoin="round"'+(' stroke-dasharray="7 5"' if dash else '')+'/>')
# groups
gbox(1300,130,2240,190,'1','Channels','#EAF2FF','#6E96E8')
gbox(1300,410,1100,150,'2','Edge and security','#E8F7EF','#4DB583')
gbox(1700,640,1840,210,'3','Gateway and identity (WSO2)','#FFF3E6','#EE9A4D')
# channels
chs=[('Citizen web portal','React, forms from definitions','M8',"react"),('Mobile app','Same services and forms','M8',"flutter"),('Operator console','Assisted applications','M13',"react"),('Officer workspace','Work lists and decisions','M9',"react"),('Admin and Service Studio','Design and publish services','M4',"react")]
for i,(t,d,tg,lg) in enumerate(chs): mod(1330+i*440,180,420,125,t,[d],tg,lg=lg)
mod(1330,460,1040,90,'Reverse proxy and web firewall',['TLS, security headers, rate limits. Routes portal and API traffic to the gateway.'],'M3',lg='nginx')
mod(1730,690,880,140,'WSO2 API Manager',['The one front door. Token and ApiKey checks, throttling, API versions, developer portal for departments.'],'M14',wso2=True)
mod(2630,690,880,140,'WSO2 Identity Server',['Sign-in, OTP and MFA, roles from postings, tokens for citizens, officers, operators and departments.'],'M1',wso2=True)
# domains
dom=[('Identity and offices','#F1EDFF','#9B83F0',[('Account service','Registration, verification, officer onboarding','M1'),('Office directory','Offices, areas, dated postings','M2'),('Access check','Role + office + area + case','M3'),('Operator service','Centres, operators, assisted by','M13')]),
('Service setup','#E8F0FE','#6E96E8',[('Service catalogue','Service types and launch','M4'),('Form builder','Versioned forms, one renderer','M5'),('Rules engine','Papers and fees as tables','M6'),('Publishing','Draft, test, approve, go live','M17'),('Derived services','Follow-on service offers','M22'),('Service search','Search in the citizen\'s language','M25')]),
('Application work','#EAF6EF','#4DB583',[('Application service','Draft, submit once, track','M8'),('Workflow engine','Flowable (BPMN): steps, actions','M7'),('Task API','Work lists, claim, decide','M9'),('Bulk processing','Many cases, one request','M21'),('Time limits','Clocks and escalation','M24'),('Grievances and appeals','Up to three levels','M15')]),
('Documents and money','#FFF3E6','#EE9A4D',[('Document service','Certificates, signing, QR check','M10'),('Payment service','Fees, receipts, matching','M11'),('Notification service','SMS, email, WhatsApp','M12'),('Benefit payments','After approval, kept separate','M23')]),
('Platform and support','#E6F6F7','#4BB6BD',[('Reports','Dashboards and exports','M16'),('Audit and operations','Every action, monitoring, recovery','M18'),('Platform settings','Name, languages, gateways, calendar','M19'),('Migration and import','Existing services and applications','M20')])]
dw=690
for k,(t,fill,stroke,items) in enumerate(dom):
    x=60+k*(dw+20)
    gbox(x,940,dw,880,'4'+'abcde'[k],t,fill,stroke)
    for j,(tt,dd,tg) in enumerate(items):
        mod(x+20,990+j*135,dw-40,120,tt,[dd],tg)
# data
gbox(60,1920,2240,500,'5','Data and streaming','#E6F6F7','#4BB6BD')
cyl(90,1980,340,170,'PostgreSQL 18',['One core database','Function-only access'],lg='postgresql')
cyl(460,1980,300,170,'Redis 8',['Sessions, token verdicts'],lg='redis')
cyl(790,1980,330,170,'Apache Kafka 4.3',['Outbox events: submitted,','paid, decided, issued'],lg='apachekafka')
cyl(1150,1980,320,170,'Object storage',['Papers and issued','certificates'])
cyl(1500,1980,330,170,'OpenSearch',['Service and case search'],lg='opensearch')
cyl(1860,1980,400,170,'Reporting store',['Read copy for dashboards'],lg='postgresql')
mod(90,2210,1060,170,'Event consumers',['Notifications, payments matching, audit, reports and search all react to events, so a failure in one never undoes a decision.'],'',accent='#4BB6BD')
mod(1180,2210,1080,170,'Keys and secrets',['A secrets vault and a signing-key module. Encrypted storage and backups, restores rehearsed.'],'M18',accent='#4BB6BD')
# integration + external
gbox(2380,1920,1160,200,'6','Integration layer','#FBEFF4','#D9779A')
mod(2410,1970,1100,130,'WSO2 Micro Integrator',['The single connection point: retries, timeouts, logs and a named owner for every outside link.'],'M14',wso2=True)
gbox(2380,2180,1160,240,'7','External systems','#F3F4F6','#8D96A4')
ex=['Identity: Aadhaar, DigiLocker','Signing: eSign','Payment gateways and treasury','PFMS and DBT','SMS, email, WhatsApp','Departments and their portals']
for i,t in enumerate(ex):
    mod(2410+(i%3)*368,2230+(i//3)*85,355,75,t,[],'')
# arrows
for cx in (1540,1980,2420,2860,3300): rail([(cx,305),(cx,365),(1850,365)])
arrow([(1850,365),(1850,460)],'HTTPS',(1850,415))
arrow([(2300,550),(2300,690)],None)
arrow([(2620,760),(2630,760)],None,both=True)
rail([(2170,830),(2170,905),(1400,905)]); 
for k in range(5):
    x=60+k*(dw+20)+dw//2
    arrow([(x,905),(x,940)],None)
rail([(60+dw//2,905),(1400,905)]); rail([(1400,905),(3300,905)])
add('<rect x="1950" y="890" width="440" height="26" rx="13" fill="#fff" stroke="#CBD5E1"/><text x="2170" y="908" font-size="12.5" font-weight="600" fill="#334155" text-anchor="middle">one front door to every service, token checked first</text>')
arrow([(700,1820),(700,1980)],'Npgsql · functions',(700,1900))
arrow([(1390,1820),(1390,1950)],None); 
arrow([(1960,1820),(1960,1980)],'events',(1960,1900),dash=True)
arrow([(2620,1820),(2620,1970)],'outside calls',(2620,1895))
arrow([(2960,2100),(2960,2180)],None)
add('</svg>')
svg='\n'.join(out)
open('SevaOne_ServicePlus_Proposed.svg','w').write(svg)
open('sp_prop.html','w').write(f'<!doctype html><html><head><meta charset="utf-8"><style>@page{{size:{W}px {H}px;margin:0}}html,body{{margin:0;background:#fff}}svg{{display:block}}</style></head><body>{svg}</body></html>')
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--no-pdf-header-footer','--print-to-pdf=SevaOne_ServicePlus_Proposed.pdf','file://'+os.getcwd()+'/sp_prop.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--hide-scrollbars','--force-device-scale-factor=2',f'--window-size={W},{H}','--screenshot=SevaOne_ServicePlus_Proposed.png','file://'+os.getcwd()+'/sp_prop.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
print('done')
