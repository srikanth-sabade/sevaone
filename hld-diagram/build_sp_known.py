import os,subprocess
src=open(os.path.join(os.path.dirname(os.path.abspath(__file__)),'hld_common.py')).read().replace("W,H=2400,1600","W,H=3400,2150")
exec(src)
def wrap(text,width_px,fs=14):
    cpl=max(int(width_px/(fs*0.53)),8); words=text.split(); ls=[];cur=''
    for w_ in words:
        if len(cur)+len(w_)+1>cpl and cur: ls.append(cur);cur=w_
        else: cur=(cur+' '+w_).strip()
    if cur: ls.append(cur)
    return ls
def mod(x,y,w,h,title,lines,tag='',unk=False,accent='#CBD5E1',ic=None):
    ds=' stroke-dasharray="7 5"' if unk else ''
    col='#B7791F' if unk else accent
    add(f'<g filter="url(#sh)"><rect x="{x}" y="{y}" width="{w}" height="{h}" rx="12" fill="{"#FFFBF2" if unk else "#fff"}" stroke="{col}" stroke-width="1.7"{ds}/></g>')
    tagw=(len(tag)*7+16+14) if tag else 0
    add(f'<text x="{x+16}" y="{y+30}" font-size="19" font-weight="700" fill="#0F172A">{E(title)}</text>')
    yy=y+58
    for l in lines:
        for ln in wrap(l,w-32): add(f'<text x="{x+16}" y="{yy}" font-size="14" fill="#475569">{E(ln)}</text>'); yy+=19
    if tag:
        tw=len(tag)*7+16; add(f'<rect x="{x+w-tw-10}" y="{y+h-30}" width="{tw}" height="20" rx="10" fill="#E2E8F0"/><text x="{x+w-tw/2-10}" y="{y+h-16}" font-size="11.5" font-weight="700" fill="#334155" text-anchor="middle">{tag}</text>')
add(f'<rect x="0" y="0" width="{W}" height="92" fill="url(#hdr)"/>')
add('<text x="48" y="45" font-size="32" font-weight="700" fill="#FFFFFF">Service Plus: Architecture as Known Today</text>')
add('<text x="48" y="73" font-size="15" fill="#C7D2FE">Drawn only from what the NIC deck, the conversation and the API documents state. Dashed amber boxes are not disclosed.</text>')
add('<g font-size="13.5" fill="#E2E8F0"><rect x="2760" y="30" width="26" height="16" rx="4" fill="#fff"/><text x="2794" y="43">Stated by NIC</text><rect x="2930" y="30" width="26" height="16" rx="4" fill="#FFFBF2" stroke="#F2B173" stroke-dasharray="4 3"/><text x="2964" y="43">Not disclosed</text><text x="2760" y="76">M1 to M25 are the module numbers used in the architecture documents.</text></g>')
def gbox(x,y,w,h,num,title,fill,stroke):
    add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="20" fill="{fill}" stroke="{stroke}" stroke-width="1.8"/>')
    add(f'<circle cx="{x+30}" cy="{y+30}" r="15" fill="{stroke}"/><text x="{x+30}" y="{y+36}" font-size="16" font-weight="800" fill="#fff" text-anchor="middle">{num}</text><text x="{x+56}" y="{y+37}" font-size="19" font-weight="700" fill="#1E293B" letter-spacing="0.3">{E(title)}</text>')
# groups (staircase)
gbox(60,130,1560,200,'1','People who use it','#EAF2FF','#6E96E8')
gbox(1760,130,1580,200,'2','Channels','#E8F7EF','#4DB583')
gbox(60,430,1560,520,'3','Set up a service (design time)','#F1EDFF','#9B83F0')
gbox(1760,520,1580,640,'4','Run a service (run time)','#FFF3E6','#EE9A4D')
gbox(60,1050,1560,470,'5','Supporting capabilities','#E6F6F7','#4BB6BD')
gbox(1760,1260,1580,420,'6','Connected systems','#FBEFF4','#D9779A')
gbox(60,1620,1560,440,'7','Foundation','#F3F4F6','#8D96A4')
gbox(1760,1780,1580,280,'8','NIC roadmap (slide 21, completion not shown)','#F3F4F6','#8D96A4')
# people
ppl=[('Citizen / applicant','Applies by self or assisted mode, tracks, gets alerts, lodges grievances','M8'),('Assisted-mode operator','Applies for a citizen through a centre','M13'),('Officer (workflow)','Reviews, remarks, acts on assigned cases','M9'),('Service owner','Configures the service, grievance policy, launch','M4'),('Appellate authority','Up to three levels','M15')]
for i,(t,d,tg) in enumerate(ppl):
    w=290; mod(90+i*305,180,w,125,t,[d],tg)
# channels
ch=[('Web portal','Responsive forms, all devices','M8'),('Mobile app','ServicePlus mobile app and API for other apps','M8'),('Branded entry points','Pages and URLs per service, per department or for the whole deployment','M4'),('Assisted mode','Franchise logins (K1, B1, G1, AJSK, BSK)','M13')]
for i,(t,d,tg) in enumerate(ch):
    mod(1790+i*385,180,365,125,t,[d],tg)
# design time (3x2)
dz=[('Form Designer','Drag and drop, multi-page, multilingual','Applicant and official forms, field links,','checks, help text, JavaScript insertion','M5'),
 ('Process Flow Designer','Drag and drop workflow','Conditions, fork and merge, timer tasks,','webservice tasks, real-time monitoring','M7'),
 ('Document Designer','Templates for acknowledgement,','delivery, rejection, intermediate certificates','QR, DSC stamp, watermark, unique URL','M10'),
 ('Notification Designer','Content and trigger points','SMS, email, Sandes, WhatsApp','OTP verification, bulk module','M12'),
 ('Organisation structure','Departments, hierarchy, designations','Jurisdiction mapping between departments','Constituency data','M2'),
 ('Service lifecycle wizard','Guide, tenancy extension, configure, test,','train, freeze, activate service units,','assign users, launch. Major and minor versions','M17')]
for i,(t,a,b_,c,tg) in enumerate(dz):
    mod(90+(i%3)*510,490+(i//3)*200,490,180,t,[a+' · '+b_+' · '+c],tg)
mod(90,480+440,1500,0,'',[]) if False else None
add('<text x="90" y="940" font-size="14" fill="#64748B">Business rules: rule injection, Velocity templates, MVEL and JavaScript insertion are listed in the history slide (slide 5).</text>')
# run time
rt=[('Form rendering','Shows the configured forms to applicants and officials','M5'),('Workflow execution','Moves each application through the designed steps','M7'),('Worklists and cases','Role-based access, task queues, remarks, history','M9'),('Tracking and alerts','Status online, alerts for submitted applications','M8'),('Grievance handling','Policies, routing, escalation, three appeal levels','M15'),('Bulk, FIFO, derived services','Listed in the history of capabilities (slide 5)','M21')]
for i,(t,d,tg) in enumerate(rt):
    mod(1790+(i%2)*780,580+(i//2)*160,760,140,t,[d],tg)
mod(1790,1065,1520,80,'Multi-tenancy and role-based access',['Claimed in the deck (slides 6, 7, 20). How tenants are separated is not described.'],'M19')
# supporting
sp=[('Document generation','PDF, HTML, XML, spreadsheet outputs','M10'),('Notifications','SMS, email, Sandes, WhatsApp','M12'),('Payments','Gateways and wallets (right)','M11'),('SPReport','Dashboards, filters, exports, role access','M16')]
for i,(t,d,tg) in enumerate(sp):
    mod(90+i*385,1100,365,125,t,[d],tg)
mod(90,1250,1500,95,'Security as claimed',['SSL/TLS encryption and OAuth 2.0 authentication (slide 6). Security audit results and controls are not described.'],'M3')
mod(90,1370,1500,125,'Service delivery acts and grievance policy',['Administrators can define policy "in line with service delivery acts". No act is named.'],'M15')
# connected systems
cx=[('Identity','Aadhaar, DigiLocker, JanParichay'),('Signing','eSign'),('Payments','BharatKosh, SBI ePay, PayU, BillDesk, ICICI, CSC eWallet'),('Money transfer','PFMS and DBT'),('Messaging','Sandes, WhatsApp, SMS, email'),('Other','eTaal, any REST API, sync and async data sharing, UMANG launch API')]
for i,(t,d) in enumerate(cx):
    mod(1790+(i%3)*510,1310+(i//3)*170,490,150,t,[d],'M14')
# foundation (not disclosed)
fd=[('Database and storage','Product, schema and document storage','are not disclosed'),('Hosting and deployment','Servers, tenants, availability and','recovery are not disclosed'),('Source and internals','Code, component boundaries','and versions are not disclosed'),('Data export','Whether service settings and','applications can be exported is not disclosed')]
for i,(t,a,b_) in enumerate(fd):
    mod(90+(i%2)*760,1670+(i//2)*190,740,170,t,[a,b_],'',unk=True)
# roadmap
rm=[('Sept 2024','Bhashini, Elasticsearch, RabbitMQ'),('Nov 2024','SPReport with live data'),('April 2025','Microservices'),('July 2025','Docker containers'),('May 2026','Web apps from ServicePlus')]
for i,(t,d) in enumerate(rm):
    mod(1790+(i%3)*510,1840+(i//3)*100,490,88,t,[d],'',unk=True)
# arrows
rail=lambda pts,col='#475569',dash=False: add('<path d="M'+' L'.join(f'{x} {y}' for x,y in pts)+f'" fill="none" stroke="{col}" stroke-width="2.4" stroke-linejoin="round"'+(' stroke-dasharray="7 5"' if dash else '')+'/>')
arrow([(840,330),(840,430)],'configure',(840,380))
arrow([(1620,235),(1760,235)],'use',(1690,235))
arrow([(2550,330),(2550,520)],'apply and process',(2550,425))
arrow([(1620,690),(1760,690)],'published services',(1690,660),dash=True)
arrow([(1760,990),(1620,1250)],None)
arrow([(2550,1160),(2550,1260)],'calls out',(2550,1210))
arrow([(840,1520),(840,1620)],None,dash=True)
add('</svg>')
svg='\n'.join(out)
open('SevaOne_ServicePlus_Known.svg','w').write(svg)
open('sp_known.html','w').write(f'<!doctype html><html><head><meta charset="utf-8"><style>@page{{size:{W}px {H}px;margin:0}}html,body{{margin:0;background:#fff}}svg{{display:block}}</style></head><body>{svg}</body></html>')
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--no-pdf-header-footer',f'--print-to-pdf=SevaOne_ServicePlus_Known.pdf','file://'+os.getcwd()+'/sp_known.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--hide-scrollbars','--force-device-scale-factor=2',f'--window-size={W},{H}','--screenshot=SevaOne_ServicePlus_Known.png','file://'+os.getcwd()+'/sp_known.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
print('done')
