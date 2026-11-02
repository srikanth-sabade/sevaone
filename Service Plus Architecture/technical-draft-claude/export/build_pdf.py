import json,subprocess,os,html
from PIL import Image
here=os.path.dirname(os.path.abspath(__file__))
COLORS={'rose':'#C0392B','cyan':'#0E7490','emerald':'#059669','amber':'#B7791F'}
def mod(path):
    return json.load(open(os.path.join(here,'..',path)))
import sys
sys.path.insert(0,os.path.join(here,'..','modules'))
from specs import S
from extra import V, ER
def view(v,ctitle=None):
    return dict(title=v['title'],sub=v['sub'],img=v['slug']+'.png',cards=v['cards'],ctitle=ctitle or (v['title']+': how it works and what we use'))
wf=dict(title='Workflow Engine',sub='Module 7: how cases move through the steps a service owner designs.',img='workflow.png',cards=mod('modules/workflow.json')['cards'],ctitle='Workflow Engine: how it works and what we use')
mods={n:dict(title=sp['title'],sub=sp['sub'],img=sp['slug']+'.png',cards=sp['cards'],ctitle=sp['title']+': how it works and what we use') for n,sp in S.items()}
mods[7]=wf
sections=[dict(title='High-Level Diagram',sub='Built on WSO2 Identity Server and WSO2 API Manager.',img='high-level.png',cards=mod('candidate.json')['cards'],ctitle='What each part does')]
sections.append(dict(divider='End-to-End Flows',note='One application and its money, seen across all the modules.'))
for k in ('seq1','seq2','seq3','seq4'): sections.append(view(V[k]))
sections.append(dict(divider='The 20 Modules',note='Each module: how it works and what we use.'))
for n in sorted(mods): sections.append(mods[n])
sections.append(dict(divider='More ServicePlus Features',note='Features NIC lists that sit outside the first 20 modules.'))
for k in ('m21','m22','m23','m24','m25'): sections.append(view(V[k]))
sections.append(dict(divider='Platform Views',note='Channels, data, security and deployment across the whole platform.'))
sections.append(view(V['channels']))
sections.append(view(ER))
sections.append(view(V['security']))
sections.append(view(V['deploy']))
css='''@page{size:13.333in 7.5in;margin:0}*{box-sizing:border-box}body{margin:0;font-family:Calibri,Carlito,'Noto Sans',sans-serif;background:#fff;color:#1B3A3F}
.p{width:13.333in;height:7.5in;padding:.35in .5in;page-break-after:always;overflow:hidden;position:relative}
h1{font-size:30px;margin:0 0 4px}.sub{color:#5B6B70;font-size:15px;margin:0 0 10px}img{width:100%;max-height:5.9in;object-fit:contain;display:block}
.g{display:grid;grid-template-columns:1fr 1fr;gap:.25in;margin-top:.25in}
.c{background:#fff;border:1px solid #D5DCDD;border-left:7px solid var(--k);border-radius:8px;padding:.16in .25in;min-height:2.75in}
.c h2{margin:0 0 8px;font-size:20px;color:var(--k)}.c ul{margin:0;padding-left:20px;font-size:14.5px;line-height:1.35}.c li{margin-bottom:5px}
.cover{display:flex;flex-direction:column;justify-content:center;padding:0 1.2in}.cover .bar{position:absolute;left:0;top:0;bottom:0;width:.35in;background:#0F766E}
.ct2{font-size:48px;margin:0 0 14px}.ct{font-size:64px;margin:0 0 14px}.cs{font-size:30px;color:#0F766E;margin:0 0 8px;font-weight:600}.cd{font-size:18px;color:#5B6B70;margin:0}'''
out=['<!doctype html><html><head><meta charset="utf-8"><style>'+css+'</style></head><body>',
 '<div class="p cover"><div class="bar"></div><h1 class="ct">SevaOne - Service Plus</h1><p class="cs">Architecture</p><p class="cd">Draft for discussion</p></div>']
for s in sections:
    if 'divider' in s:
        out.append(f'<div class="p cover"><div class="bar"></div><h1 class="ct2">{html.escape(s["divider"])}</h1><p class="cd">{html.escape(s["note"])}</p></div>'); continue
    out.append(f'<div class="p"><h1>{html.escape(s["title"])}</h1><p class="sub">{html.escape(s["sub"])}</p><img src="{s["img"]}"></div>')
    out.append(f'<div class="p"><h1>{html.escape(s["ctitle"])}</h1><div class="g">')
    for c in s['cards']:
        k=COLORS.get(c['dot'],'#0E7490')
        out.append(f'<div class="c" style="--k:{k}"><h2>{html.escape(c["title"])}</h2><ul>'+''.join(f'<li>{html.escape(i)}</li>' for i in c['items'])+'</ul></div>')
    out.append('</div></div>')
out.append('</body></html>')
open(os.path.join(here,'pdf.html'),'w').write('\n'.join(out))
pdf=os.path.join(here,'SevaOne_ServicePlus_Architecture.pdf')
subprocess.run(['google-chrome','--headless=new','--no-sandbox','--no-pdf-header-footer',f'--print-to-pdf={pdf}',f'file://{here}/pdf.html'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
print(pdf)
