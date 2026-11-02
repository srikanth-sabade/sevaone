import os,subprocess
src=open(os.path.join(os.path.dirname(os.path.abspath(__file__)),'hld_common.py')).read().replace("W,H=2400,1600","W,H=3400,2300")
exec(src)
def wrap(text,width_px,fs=14):
    cpl=max(int(width_px/(fs*0.53)),8); words=text.split(); ls=[];cur=''
    for w_ in words:
        if len(cur)+len(w_)+1>cpl and cur: ls.append(cur);cur=w_
        else: cur=(cur+' '+w_).strip()
    if cur: ls.append(cur)
    return ls
def mh(w,title,bullets,tag):
    n=sum(len(wrap('• '+b,w-32)) for b in bullets)
    return 58+n*19+(34 if tag else 14)
def mod(x,y,w,h,title,bullets,tag='',unk=False,accent='#CBD5E1'):
    ds=' stroke-dasharray="7 5"' if unk else ''
    col='#B7791F' if unk else accent
    add(f'<g filter="url(#sh)"><rect x="{x}" y="{y}" width="{w}" height="{h}" rx="12" fill="{"#FFFBF2" if unk else "#fff"}" stroke="{col}" stroke-width="1.7"{ds}/></g>')
    add(f'<text x="{x+16}" y="{y+30}" font-size="19" font-weight="700" fill="#0F172A">{E(title)}</text>')
    yy=y+58
    for b in bullets:
        for ln in wrap('• '+b,w-32): add(f'<text x="{x+16}" y="{yy}" font-size="14" fill="#475569">{E(ln)}</text>'); yy+=19
    if tag:
        tw=len(tag)*7+16; add(f'<rect x="{x+w-tw-10}" y="{y+h-30}" width="{tw}" height="20" rx="10" fill="#E2E8F0"/><text x="{x+w-tw/2-10}" y="{y+h-16}" font-size="11.5" font-weight="700" fill="#334155" text-anchor="middle">{tag}</text>')
def grid(x,y,w,cols,items,unk=False,gap=18):
    cw=(w-gap*(cols-1))/cols; cur=y
    for r in range(0,len(items),cols):
        row=items[r:r+cols]; rh=max(mh(cw,t,b,tg) for t,b,tg in row)
        for i,(t,b,tg) in enumerate(row): mod(x+i*(cw+gap),cur,cw,rh,t,b,tg,unk)
        cur+=rh+gap
    return cur-gap
def group(x,y,w,num,title,fill,stroke,build):
    idx=len(out); b=build(x+24,y+60,w-48)
    h=b-y+24
    out.insert(idx,f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="20" fill="{fill}" stroke="{stroke}" stroke-width="1.8"/><circle cx="{x+30}" cy="{y+30}" r="15" fill="{stroke}"/><text x="{x+30}" y="{y+36}" font-size="16" font-weight="800" fill="#fff" text-anchor="middle">{num}</text><text x="{x+56}" y="{y+37}" font-size="19" font-weight="700" fill="#1E293B" letter-spacing="0.3">{E(title)}</text>')
    return y+h
add(f'<rect x="0" y="0" width="{W}" height="92" fill="url(#hdr)"/>')
add('<text x="48" y="45" font-size="32" font-weight="700" fill="#FFFFFF">Service Plus: Everything We Know It Does</text>')
add('<text x="48" y="73" font-size="15" fill="#C7D2FE">Feature view, drawn only from the NIC deck (22 slides) and the API documents. Dashed amber boxes are not disclosed or are future plans.</text>')
add('<g font-size="13.5" fill="#E2E8F0"><rect x="2760" y="30" width="26" height="16" rx="4" fill="#fff"/><text x="2794" y="43">Stated by NIC</text><rect x="2930" y="30" width="26" height="16" rx="4" fill="#FFFBF2" stroke="#F2B173" stroke-dasharray="4 3"/><text x="2964" y="43">Not disclosed / planned</text><text x="2760" y="76">M1 to M25 are the module numbers used in the architecture documents.</text></g>')
X1,X2,X3,CW=60,1180,2300,1040
# ---- column 1
y=group(X1,130,CW,'1','Platform at a glance','#EAF2FF','#6E96E8',lambda x,y,w: grid(x,y,w,3,[
 ('3,564 services',['Configured on the platform'],''),
 ('345 million applications',['Received to date'],''),
 ('33.6 billion revenue',['Collected (currency not stated)'],''),
 ('5.8 billion DBT',['Direct benefit transfer disbursed'],''),
 ('36 States/UTs',['Plus 18 central departments, under different brand names'],''),
 ('Metadata-based framework',['Low-code / no-code design, configure, run'],'')])+0)
y=group(X1,y+40,CW,'2','What it serves','#E8F7EF','#4DB583',lambda x,y,w: grid(x,y,w,2,[
 ('Service categories',['Government to Citizen (G2C)','Government to Business (G2B)','Government to Employee (G2E)','Government to Government (G2G)'],''),
 ('Service types',['Statutory','Regulatory','Developmental','Consumer utility'],''),
 ('Why ServicePlus (NIC claims)',['Rapid rollout, services available around the clock','One configurable platform for the whole service lifecycle','Multi-tenant architecture','SSL/TLS encryption and OAuth 2.0'],'M19'),
 ('Focus areas',['In-built tools to design, configure and run services','Agility: any government entity can roll out at any level','Unified framework for service delivery and grievance redressal'],'')]))
y=group(X1,y+40,CW,'3','Branded deployments and entry points','#F1EDFF','#9B83F0',lambda x,y,w: grid(x,y,w,2,[
 ('Named deployments',['Saral Haryana, SevaSindhu Karnataka','RTPS Bihar, Excise Department Kerala','eDistrict Tripura and Odisha','JanSugam Jammu & Kashmir, JharSewa Jharkhand'],'M4'),
 ('Separate pages and URLs',['One URL or page for each service','One for a department','One for an entire state'],'M4')]))
y=group(X1,y+40,CW,'4','Service definition lifecycle (slide 19)','#E6F6F7','#4BB6BD',lambda x,y,w: grid(x,y,w,3,[
 ('1  Service Definer Guide',['Fill in the guide'],''),('2  Tenancy extension',['Implement'],''),('3  Configure',['Configure the service'],''),
 ('4  Test',['Test the service'],''),('5  Train',['Train the officials'],''),('6  Freeze',['Freeze the service'],''),
 ('7  Activate units',['Activate service units'],''),('8  Assign users',['Assign users'],''),('9  Launch',['Launch the service'],'M17')]))
end1=y
# ---- column 2
y=group(X2,130,CW,'5','Design time: tools for service owners (no code)','#FFF3E6','#EE9A4D',lambda x,y,w: grid(x,y,w,2,[
 ('Form Designer',['Drag and drop, multi-page, multilingual','Forms for applicants and officials','Special fields, user-defined JavaScript insertion','Link fields across forms, predefined formats','Built-in, server-side and API-based validation','Help text at form and field level'],'M5'),
 ('Process Flow Designer',['Drag and drop workflow','Conditional logic','Fork and merge using gateways','Timer tasks and webservice tasks','Real-time workflow monitoring','Customizable workflows'],'M7'),
 ('Document Designer',['Integrated editor with form values and system variables','Acknowledgement, delivery, rejection, intermediate certificates','HTML, XML, spreadsheet, PDF','Data-based conditions, QR code, DSC stamp, watermark','Unique URL per certificate'],'M10'),
 ('Notification Designer',['SMS, email, Sandes, WhatsApp','Trigger points: accept, reject, process','User-defined content and triggers','Smart selection of phone and email contacts','OTP verification of mobile and email','Dedicated bulk notification module'],'M12'),
 ('Organisation structure',['Full structure and hierarchy of any department','Designations that exist in the organisation','Inter-departmental mapping by jurisdiction','Electoral constituency data and mapping'],'M2'),
 ('Service wizard and versions',['Wizard-style configuration of services','Launch new services in minutes','Grievance redressal policy for each service','Service versioning: major and minor'],'M17')]))
y=group(X2,y+40,CW,'6','Run time: applicants, officials and operators','#EAF2FF','#6E96E8',lambda x,y,w: grid(x,y,w,2,[
 ('Applicant',['Apply by self or through assisted mode','Track application status online','Status alerts for submitted applications','Lodge grievances for service failures'],'M8'),
 ('Assisted mode',['Operator applies on behalf of a citizen','Franchise logins (K1, B1, G1, AJSK, BSK)'],'M13'),
 ('Workflow player (officer)',['Role-Based Access Control (RBAC)','Task queues, remarks and history','Monitoring and management dashboard','Automated notification system'],'M9'),
 ('Grievance redressal',['Policy in line with service delivery acts','Automatic escalation and routing by rules','Up to three appellate levels','Tracking, notifications, pain-point dashboard'],'M15'),
 ('Listed in the journey slide (slide 5)',['Action forms, derived services, business rules','Bulk module and FIFO (named, not explained)','Audio captcha, configurable login, configurable reference number','Secured APIs'],'M21')]))
y=group(X2,y+40,CW,'7','Reports: SPReport','#E8F7EF','#4DB583',lambda x,y,w: grid(x,y,w,2,[
 ('Reports and dashboards',['Service-specific analytical and monitoring reports','Customizable templates and filters','Interactive dashboards and visualisations'],'M16'),
 ('Data and access',['Integrates external data sources','Role-based access control','Export in various formats','Unique URL for a report'],'M16')]))
end2=y
# ---- column 3
y=group(X3,130,CW,'8','Integrations with external systems','#FBEFF4','#D9779A',lambda x,y,w: grid(x,y,w,2,[
 ('Identity, KYC and signing',['Aadhaar, eKYC','DigiLocker, JanParichay','eSign (digital signature)','eTaal'],'M14'),
 ('Payment gateways',['BharatKosh, SBI ePay, PayU, BillDesk','ICICI EazyPay, CSC eWallet, eTreasury','Others "and more"'],'M11'),
 ('Money transfer',['DBT payments through PFMS'],'M14'),
 ('Messaging',['Sandes, WhatsApp, SMS, email','Bhashini (language) in the journey slide'],'M12'),
 ('RESTful API',['Configurable interface to any REST API'],'M14'),
 ('Data sharing',['Configurable API, synchronous and asynchronous'],'M14')]))
y=group(X3,y+40,CW,'9','Mobile enablement','#F1EDFF','#9B83F0',lambda x,y,w: grid(x,y,w,2,[
 ('Mobile app',['Customizable ServicePlus mobile app','Services launched dynamically on mobile','Real-time access on the go'],'M8'),
 ('Web and other apps',['Responsive forms on all devices','Services on other apps via secure API'],'M8')]))
y=group(X3,y+40,CW,'10','Security as claimed','#E6F6F7','#4BB6BD',lambda x,y,w: grid(x,y,w,1,[
 ('Stated by NIC',['SSL/TLS encryption and OAuth 2.0 authentication','Secured APIs. Audit results and controls are not described.'],'M3')]))
y=group(X3,y+40,CW,'11','NIC roadmap (slide 21): planned, not confirmed','#F3F4F6','#8D96A4',lambda x,y,w: grid(x,y,w,2,[
 ('Sept 2024',['Bhashini; data discovery with Elasticsearch and RabbitMQ'],''),
 ('Nov 2024',['Upgraded SPReport with live data'],''),
 ('April 2025',['Microservices architecture'],''),
 ('July 2025',['Docker containers instead of virtual machines'],''),
 ('May 2026',['Web apps generated through ServicePlus'],'')],unk=True))
y=group(X3,y+40,CW,'12','Not disclosed by NIC','#F3F4F6','#8D96A4',lambda x,y,w: grid(x,y,w,2,[
 ('Database and storage',['Product, schema and document storage'],''),
 ('Hosting and deployment',['Servers, tenants, availability, recovery'],''),
 ('Source and internals',['Code, component boundaries, versions'],''),
 ('Data export',['Whether services and applications can be exported'],''),
 ('Multi-tenant separation',['How tenants are separated'],''),
 ('Security audit',['Controls and results'],'')],unk=True))
H2=max(end1,end2,y)+50
svg='\n'.join(out+['</svg>']).replace('height="2300" viewBox="0 0 3400 2300"',f'height="{H2}" viewBox="0 0 3400 {H2}"',1)
open('SevaOne_ServicePlus_Features.svg','w').write(svg)
open('sp_features.html','w').write(f'<!doctype html><html><head><meta charset="utf-8"><style>@page{{size:3400px {H2}px;margin:0}}html,body{{margin:0;background:#fff}}svg{{display:block}}</style></head><body>{svg}</body></html>')
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--no-pdf-header-footer','--print-to-pdf=SevaOne_ServicePlus_Features.pdf','file://'+os.getcwd()+'/sp_features.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--hide-scrollbars','--force-device-scale-factor=2',f'--window-size=3400,{H2}','--screenshot=SevaOne_ServicePlus_Features.png','file://'+os.getcwd()+'/sp_features.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
print('done',H2)
