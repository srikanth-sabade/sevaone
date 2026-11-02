import json,sys,subprocess,os
from specs import S,X,Y,T
h,w=60,150
only=[int(a) for a in sys.argv[1:]] or sorted(S)
for n in only:
    s=S[n]; comps=[]
    for nd in s['nodes']:
        i,t,l,sub,c,r=nd[:6]
        comps.append(dict(id=i,type=T[t],label=l,sublabel=sub,pos=[X[c],Y[r]],size=[w,h]))
    conns=[]
    for e in s['edges']:
        d=dict(id=f'{e[0]}-{e[1]}',from_=e[0],to=e[1])
        d['from']=d.pop('from_')
        if e[2]: d['label']=e[2]
        if len(e)>3: d['variant']=e[3]
        conns.append(d)
    cand=dict(schema_version=1,diagram_type='architecture',meta=dict(title=s['title'],output=f"{s['slug']}.html",quality_profile='showcase'),components=comps,connections=conns,cards=s['cards'])
    json.dump(cand,open(f"{s['slug']}.json",'w'),indent=1)
    r=subprocess.run(['node','/home/srikanth/.claude/skills/archify/bin/archify.mjs','finalize','architecture',f"{s['slug']}.json",f"{s['slug']}.html",'--quality','showcase','--json'],capture_output=True,text=True)
    try:
        d=json.loads(r.stdout); rec=d.get('visualReviewRecommendation') or {}
        print(n,s['slug'],d['status'],[ (x.get('code') or x.get('id'),str(x.get('message',''))[:140]) for x in d['diagnostics']][:4], (rec.get('signals') or ''))
    except Exception as ex:
        print(n,s['slug'],'ERR',r.stdout[:300],r.stderr[:300])
