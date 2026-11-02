import html,os,subprocess
from content import P,EXTRAS,UNK,TOP,CATS
here=os.path.dirname(os.path.abspath(__file__))
E=html.escape
TAG={'unk':('Understanding needed','#B7791F')}
def ring(sz=30,col='#0F766E'):
    pale='#E6F4F1'
    parts=''
    for k in (2,1,0):
        y=9+k*11
        parts+=f'<path d="M5 {y} v9 a19 6 0 0 0 38 0 v-9" fill="{pale}" stroke="{col}" stroke-width="1.8"/><ellipse cx="24" cy="{y}" rx="19" ry="6" fill="#fff" stroke="{col}" stroke-width="1.8"/>'
    return f'<svg width="{sz}" height="{int(sz*1.05)}" viewBox="0 0 48 50">{parts}</svg>'
def tag(k):
    if k not in TAG: return ''
    l,c=TAG[k]; return f'<span class="tag" style="background:{c}">{l}</span>'
def chips(ms): return ''.join(f'<span class="mc">M{m}</span>' for m in ms)
def step(s,n,last,compact=False):
    db=f'<div class="db">{ring(26)}<span>{E(s["db"])}</span></div>' if s.get('db') else ''
    ar='' if last else '<div class="arr"></div>'
    return f'<div class="st"><div class="num">{n}</div><img src="art/{s["i"]}"><div class="stt">{E(s["t"])}</div><div class="stx">{E(s["x"])}</div><div class="meta">{tag(s["tag"])}{chips(s["mods"])}</div>{db}{ar}</div>'
def cols(p):
    def col(c,title,items):
        return f'<div class="col" style="--k:{c}"><h3>{title}</h3><ul>'+''.join(f'<li>{E(i)}</li>' for i in items)+'</ul></div>'
    return '<div class="cols">'+col('#0E7490','What we will build',p['build'])+col('#B7791F','Understanding needed',p['need'])+'</div>'
def loops(p,N):
    out=''
    for n,(a,b,lab) in enumerate(p.get('loops',[])):
        lo,hi=min(a,b),max(a,b)
        mid=(lo+hi+1)/2/N*100; w=max((hi-lo)/N*100,2.4/N*100); left=mid-w/2
        out+=f'<div class="loop" style="left:{left}%;width:{w}%;top:{n*34}px"><span>↩ {E(lab)}</span></div>'
    return f'<div class="loops" style="height:{max(1,len(p.get("loops",[])))*34}px">{out}</div>' if p.get('loops') else ''
def header(p,mods,i):
    allm=sorted(set(mods))
    return f'<div class="hd"><div><h1>{E(p["title"])}</h1><p class="sub">{E(p["sub"])}</p></div><div class="mods">Modules {" · ".join(str(m) for m in allm)}</div></div>'
def foot(n): return f'<div class="foot"><span>Illustrations: unDraw.</span><b>{n}</b></div>'
pages=[]
pages.append('<div class="p cover"><div class="bar"></div><h1 class="ct">SevaOne - Service Plus</h1><p class="cs">Functional Architecture</p><p class="cd">The lifecycles we will build, and the understanding we still need</p><p class="cd2">Proposal for discussion</p></div>')

cards=''
for nm,im,fs in CATS:
    li=''.join(f'<li><span class="mc">M{m}</span><b>{E(t)}</b><i>{E(d)}</i></li>' for m,t,d in fs)
    cards+=f'<div class="cat"><img src="art/{im}"><h3>{E(nm)}</h3><ul>{li}</ul></div>'
pages.append(f'<div class="p"><div class="hd"><div><h1>What We Will Build</h1><p class="sub">Twenty-five features, in five categories. The pages that follow show how each works.</p></div></div><div class="cats">{cards}</div>{foot(2)}</div>')
# how to read
mk=''.join(f'<div class="mk"><b>M{n}</b> {E(nm)}</div>' for n,nm,_ in UNK)
pages.append(f'''<div class="p"><div class="hd"><div><h1>How to Read This</h1><p class="sub">This is our proposal. Every page shows one process from start to finish, what we will build, and where we still need understanding.</p></div></div>
<div class="legend"><div class="lg"><span class="tag" style="background:#B7791F">Understanding needed</span><p>A step or topic where we still need to learn the department\'s rules or choices before we can finish the design.</p></div>
<div class="lg">{ring(40)}<p>The stacked-rings picture marks a place where records are kept.</p></div>
<div class="lg"><span class="mc">M4</span><p>Small grey tags are module numbers: the parts of the platform that do this step. The key is below.</p></div></div>
<h2>Module numbers</h2><div class="mkg">{mk}</div>{foot(3)}</div>''')
n=4
for p in P:
    allm=[]
    body=''
    if 'steps' in p:
        N=len(p['steps']); allm=[m for s in p['steps'] for m in s['mods']]
        body=f'<div class="flow" style="--N:{N}">'+''.join(step(s,k+1,k==N-1) for k,s in enumerate(p['steps']))+'</div>'+loops(p,N)
        if p.get('band'): body+=f'<div class="band">{E(p["band"])}</div>'
    elif 'lanes' in p:
        allm=p['mods']
        rows=''
        for ln in p['lanes']:
            sts=''.join(f'<div class="ls"><img src="art/{s["i"]}"><div><div class="stt">{E(s["t"])}</div><div class="stx">{E(s["x"])}</div>{tag(s["tag"])}</div></div>'+('' if k==2 else '<div class="larr"></div>') for k,s in enumerate(ln['steps']))
            rows+=f'<div class="lane"><div class="ln">{E(ln["name"])}</div>{sts}</div>'
        body=f'<div class="lanes">{rows}</div>'
    elif 'hub' in p:
        allm=p['mods']; h=p['hub']; sp=h['spokes']
        def spk(s): return f'<div class="sp"><img src="art/{s["i"]}"><div class="stt">{E(s["t"])}</div><div class="stx">{E(s["x"])}</div>{tag(s["tag"])}</div>'
        body=f'<div class="hub"><div class="hr">{"".join(spk(s) for s in sp[:3])}</div><div class="hc"><img src="art/{h["centre_i"]}"><b>{E(h["centre"])}</b><span>retries · logs · named owner</span></div><div class="hr">{"".join(spk(s) for s in sp[3:])}</div></div>'
    pages.append(f'<div class="p">{header(p,allm,n)}{body}{cols(p)}{foot(n)}</div>'); n+=1
# extras
cards=''
for c in EXTRAS['cards']:
    cards+=f'<div class="xc"><div class="xh"><img src="art/{c["i"]}"><b>{E(c["name"])}</b></div><div class="xs">{E(c["steps"])}</div><p><i style="background:#0E7490"></i><b>We will build:</b> {E(c["build"])}</p><p><i style="background:#B7791F"></i><b>Understanding needed:</b> {E(c["need"])}</p></div>'
pages.append(f'<div class="p"><div class="hd"><div><h1>{E(EXTRAS["title"])}</h1><p class="sub">{E(EXTRAS["sub"])}</p></div><div class="mods">Modules 21 · 22 · 23 · 24 · 25</div></div><div class="xg">{cards}</div>{foot(n)}</div>'); n+=1
# unknown tables
per=10
for k in range(0,len(UNK),per):
    chunk=UNK[k:k+per]
    cs=''.join(f'<div class="uc"><div class="uh"><span class="un">M{m}</span><b>{E(nm)}</b></div><ul>'+''.join(f'<li>{E(x)}</li>' for x in us)+'</ul></div>' for m,nm,us in chunk)
    pages.append(f'<div class="p"><div class="hd"><div><h1>Understanding Needed, Module by Module</h1><p class="sub">Part {k//per+1} of {(len(UNK)+per-1)//per}. Topics we still need to understand with the department.</p></div></div><div class="ug">{cs}</div>{foot(n)}</div>'); n+=1
tp=''.join(f'<div class="tp"><span class="tn">{i+1}</span><div><b>{E(a)}</b><p>{E(b)}</p></div></div>' for i,(a,b) in enumerate(TOP))
pages.append(f'<div class="p"><div class="hd"><div><h1>Understanding Needed First</h1><p class="sub">These topics change the design the most.</p></div></div><div class="tg">{tp}</div>{foot(n)}</div>')
css='''@page{size:13.333in 7.5in;margin:0}*{box-sizing:border-box}body{margin:0;font-family:Calibri,Carlito,'Noto Sans',sans-serif;background:#fff;color:#1B3A3F}
.p{width:13.333in;height:7.5in;padding:.3in .45in .3in;page-break-after:always;overflow:hidden;position:relative}
h1{font-size:28px;margin:0 0 2px}.sub{color:#5B6B70;font-size:14.5px;margin:0}h2{font-size:18px;margin:.2in 0 .1in}
.hd{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:.12in}.mods{font-size:12.5px;color:#0F766E;border:1px solid #BFE0DA;border-radius:999px;padding:4px 12px;white-space:nowrap;background:#F2FAF8}
.flow{display:grid;grid-template-columns:repeat(var(--N),1fr);gap:.2in;margin-top:.04in}
.st{position:relative;text-align:center;padding:0 2px}.st img{height:1.2in;max-width:100%;object-fit:contain;display:block;margin:0 auto 4px}
.num{position:absolute;left:0;top:0;width:24px;height:24px;border-radius:50%;background:#0F766E;color:#fff;font-weight:700;font-size:13px;line-height:24px}
.stt{font-weight:700;font-size:15.5px;line-height:1.15;margin-bottom:3px;min-height:2.3em}.stx{font-size:13px;color:#44565B;line-height:1.28;min-height:.62in}
.meta{margin-top:5px;display:flex;flex-wrap:wrap;gap:3px;justify-content:center}.tag{display:inline-block;color:#fff;font-size:10.5px;font-weight:700;border-radius:999px;padding:2px 8px}
.mc{font-size:10px;color:#44565B;background:#EEF2F2;border-radius:4px;padding:2px 5px}
.db{display:flex;align-items:center;justify-content:center;gap:5px;margin-top:5px;font-size:11px;color:#0F766E;font-weight:700}
.arr{position:absolute;right:-.2in;top:.42in;width:.2in;height:2px;background:#0F766E}.arr:after{content:"";position:absolute;right:-1px;top:-4px;border-left:7px solid #0F766E;border-top:5px solid transparent;border-bottom:5px solid transparent}
.loops{position:relative;margin-top:.04in}.loop{position:absolute;height:22px;border:2px dashed #B7791F;border-top:none;border-radius:0 0 12px 12px}.loop span{position:absolute;left:50%;transform:translateX(-50%);top:9px;background:#fff;padding:0 8px;font-size:11.5px;color:#8A5A12;white-space:nowrap}
.band{margin-top:.08in;background:#F2FAF8;border:1px solid #BFE0DA;border-radius:8px;padding:6px 12px;font-size:12.5px;color:#1B3A3F;text-align:center}
.cols{position:absolute;left:.45in;right:.45in;bottom:.5in;display:grid;grid-template-columns:repeat(2,1fr);gap:.2in}
.col{border:1px solid #D5DCDD;border-top:5px solid var(--k);border-radius:8px;padding:.08in .15in .06in;background:#fff}.col h3{margin:0 0 5px;font-size:16px;color:var(--k)}.col ul{margin:0;padding-left:18px;font-size:13.8px;line-height:1.32}.col li{margin-bottom:3px}
.foot{position:absolute;left:.45in;right:.45in;bottom:.14in;font-size:10px;color:#6B7B80;display:flex;justify-content:space-between}
.cover{display:flex;flex-direction:column;justify-content:center;padding:0 1.2in}.cover .bar{position:absolute;left:0;top:0;bottom:0;width:.35in;background:#0F766E}.ct{font-size:64px;margin:0 0 14px}.cs{font-size:32px;color:#0F766E;margin:0 0 10px;font-weight:600}.cd{font-size:19px;color:#44565B;margin:0 0 6px;max-width:9in}.cd2{font-size:16px;color:#6B7B80;margin:8px 0 0}
.legend{display:grid;grid-template-columns:repeat(3,1fr);gap:.2in;margin-top:.1in}.lg{border:1px solid #D5DCDD;border-radius:8px;padding:.12in .15in;font-size:13.5px;line-height:1.35}.lg p{margin:6px 0 0}
.cats{display:grid;grid-template-columns:repeat(5,1fr);gap:.14in}.cat{border:1px solid #D5DCDD;border-top:5px solid #0F766E;border-radius:10px;padding:.1in .12in;height:5.95in}.cat img{height:1.0in;display:block;margin:0 auto 6px;max-width:100%;object-fit:contain}.cat h3{margin:0 0 6px;font-size:17px;color:#0F766E;text-align:center}.cat ul{list-style:none;margin:0;padding:0}.cat li{margin-bottom:5px;font-size:12px;line-height:1.25}.cat li b{display:block;font-size:13.5px;margin-top:2px}.cat li i{display:block;font-style:normal;color:#44565B}.mkg{display:grid;grid-template-columns:repeat(5,1fr);gap:6px .15in;font-size:12.5px}.mk b{color:#0F766E}
.lanes{display:flex;flex-direction:column;gap:.08in;margin-top:.04in}.lane{display:flex;align-items:center;gap:.12in;border:1px solid #E3E8E8;border-radius:10px;padding:.06in .12in}.ln{width:1.5in;font-weight:700;font-size:15px;color:#0F766E}
.ls{flex:1;display:flex;align-items:center;gap:8px}.ls img{height:.82in;width:1in;object-fit:contain}.ls .stt{font-size:13.5px;min-height:0}.ls .stx{font-size:11.8px;min-height:0;margin-bottom:3px}.larr{width:.3in;height:2px;background:#0F766E;position:relative}.larr:after{content:"";position:absolute;right:-1px;top:-4px;border-left:7px solid #0F766E;border-top:5px solid transparent;border-bottom:5px solid transparent}
.hub{display:flex;flex-direction:column;gap:.06in}.hr{display:grid;grid-template-columns:repeat(3,1fr);gap:.2in;text-align:center}.sp img{height:.7in;object-fit:contain;display:block;margin:0 auto 2px}.sp .stt{font-size:13.5px;min-height:0}.sp .stx{font-size:11.8px;min-height:0}
.hc{display:flex;align-items:center;justify-content:center;gap:12px;background:#F2FAF8;border:2px solid #0F766E;border-radius:12px;padding:.04in}.hc img{height:.6in}.hc b{font-size:17px;color:#0F766E}.hc span{font-size:12px;color:#44565B}
.xg{display:grid;grid-template-columns:repeat(3,1fr);gap:.18in}.xc{border:1px solid #D5DCDD;border-radius:10px;padding:.1in .15in;font-size:13.2px;line-height:1.3;height:2.2in}.xh{display:flex;align-items:center;gap:10px}.xh img{height:.7in;width:.9in;object-fit:contain}.xh b{font-size:17px;color:#0F766E}.xs{background:#F2FAF8;border-radius:6px;padding:4px 8px;margin:4px 0 6px;font-size:12px}.xc p{margin:3px 0}.xc i{display:inline-block;width:9px;height:9px;border-radius:50%;margin-right:5px}
.ug{display:grid;grid-template-columns:1fr 1fr;gap:.1in .22in}.uc{border:1px solid #D5DCDD;border-left:5px solid #B7791F;border-radius:8px;padding:.06in .14in;height:1.12in;overflow:hidden}.uh{display:flex;gap:8px;align-items:center;font-size:14px;margin-bottom:2px}.un{background:#B7791F;color:#fff;border-radius:5px;font-size:11px;font-weight:700;padding:1px 6px}.uc ul{margin:0;padding-left:17px;font-size:12.3px;line-height:1.25}
.tg{display:grid;grid-template-columns:1fr 1fr;gap:.12in .3in;margin-top:.1in}.tp{display:flex;gap:12px;align-items:flex-start;border:1px solid #D5DCDD;border-radius:10px;padding:.1in .15in}.tn{background:#0F766E;color:#fff;width:30px;height:30px;border-radius:50%;text-align:center;line-height:30px;font-weight:700;flex:none}.tp b{font-size:16px}.tp p{margin:2px 0 0;font-size:13.5px;color:#44565B}'''
open(os.path.join(here,'functional.html'),'w').write('<!doctype html><html><head><meta charset="utf-8"><style>'+css+'</style></head><body>'+''.join(pages)+'</body></html>')
pdf=os.path.join(here,'SevaOne_ServicePlus_Functional_Architecture.pdf')
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--no-pdf-header-footer',f'--print-to-pdf={pdf}',f'file://{here}/functional.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
print(pdf,len(pages),'pages')
