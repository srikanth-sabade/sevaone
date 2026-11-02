import json,sys,subprocess,os
from extra import V,X,Y,ER
T=dict(ext='external',be='backend',db='database',sec='security',cl='cloud',bus='messagebus',fe='frontend')
H,W=60,150
def build(key):
    v=ER if key=='erd' else V[key]
    kind=v['kind']; slug=v['slug']
    meta=dict(title=v['title'],output=f'{slug}.html',quality_profile='showcase')
    if kind=='arch':
        comps=[]
        for nd in v['nodes']:
            i,t,l,sub,c,r=nd[:6]; ex=nd[6] if len(nd)>6 else {}
            comps.append(dict(id=i,type=T[t],label=l,sublabel=sub,pos=[X[c],ex.get('y',Y[r])],size=[ex.get('w',W),ex.get('h',H)]))
        conns=[]
        for e in v['edges']:
            d={'id':f'{e[0]}-{e[1]}','from':e[0],'to':e[1]}
            if e[2]: d['label']=e[2]
            if len(e)>3: d['variant']=e[3]
            conns.append(d)
        c=dict(schema_version=1,diagram_type='architecture',meta=meta,components=comps,connections=conns,cards=v['cards'])
        if v['boundaries']: c['boundaries']=[dict(kind=k,label=l,wraps=w) for k,l,w in v['boundaries']]
    elif kind=='seq':
        parts=[dict(id=i,type=T[t],label=l,sublabel=s) for i,t,l,s in v['parts']]
        msgs=[];first={};last={}
        for n,(a,b,l,var) in enumerate(v['msgs']):
            y=160+32*n
            msgs.append(dict(id=f'm{n}',**{'from':a},to=b,y=y,label=l,variant=var))
            for p in (a,b):
                first.setdefault(p,y); last[p]=y
        acts=[dict(participant=p,**{'from':first[p]-5,'to':last[p]+10}) for p in first]
        segs=[dict(**{'from':160+32*s-14,'to':160+32*e+16},label=l) for s,e,l in v['segs']]
        c=dict(schema_version=1,diagram_type='sequence',meta=meta,participants=parts,segments=segs,messages=msgs,activations=acts,cards=v['cards'])
    else:
        ents=[dict(id=i,label=i,tag=tag,row=r,col=cc,attributes=at) for i,tag,r,cc,at in v['entities']]
        rels=[dict(id=f'{a}-{b}',**{'from':a},to=b,fromCardinality=fc,toCardinality=tc) for a,b,fc,tc in v['rels']]
        c=dict(schema_version=1,diagram_type='erd',meta=meta,layout=dict(mode='grid',gapX=60,gapY=40),entities=ents,relationships=rels,cards=v['cards'])
    return slug,kind,c
keys=sys.argv[1:] or list(V)+['erd']
for k in keys:
    slug,kind,c=build(k)
    json.dump(c,open(f'{slug}.json','w'),indent=1)
    q='standard' if kind=='erd' else 'showcase'
    if kind=='erd': c['meta']['quality_profile']='standard'; json.dump(c,open(f'{slug}.json','w'),indent=1)
    r=subprocess.run(['node','/home/srikanth/.claude/skills/archify/bin/archify.mjs','finalize',{'arch':'architecture','seq':'sequence','erd':'erd'}[kind],f'{slug}.json',f'{slug}.html','--quality',q,'--out-dir',f'review-3/{slug}','--json'],capture_output=True,text=True)
    try:
        d=json.loads(r.stdout); rec=d.get('visualReviewRecommendation') or {}
        print(k,slug,d['status'],[(x.get('code'),str(x.get('message',''))[:150]) for x in d['diagnostics']][:4],rec.get('signals') or '')
    except Exception:
        print(k,slug,'ERR',r.stdout[:400],r.stderr[:300])
