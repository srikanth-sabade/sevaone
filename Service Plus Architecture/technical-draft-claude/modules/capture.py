import sys,subprocess,os
from PIL import Image, ImageChops
out=os.path.join(os.path.dirname(os.getcwd()),'export')
for name in sys.argv[1:]:
    h=open(f'{name}.html').read()
    css='<style>html,body{background:#fff!important;background-image:none!important}:root,:root[data-theme="light"]{--bg:#fff!important;--panel:#fff!important;--canvas-dot:transparent!important}.diagram-container{background:#fff!important;background-image:none!important;box-shadow:none!important;border-color:#fff!important}.header,.cards,[data-legend],.diagram-nav,.reader-rail,.rail-controls,.node-outline,.toolbar,.toolbar-group,.diagram-guide,[class*="legend"],[class*="node-finder"],[class*="overview-map"]{display:none!important}</style></head>'
    open(f'/tmp/claude-1000/{name}-white.html','w').write(h.replace('</head>',css,1))
    subprocess.run(['google-chrome','--headless=new','--no-sandbox','--hide-scrollbars','--force-device-scale-factor=2','--window-size=2048,1320','--virtual-time-budget=4000',f'--screenshot=/tmp/claude-1000/{name}-full.png',f'file:///tmp/claude-1000/{name}-white.html?embed=1'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
    im=Image.open(f'/tmp/claude-1000/{name}-full.png').convert('RGB')
    bbox=ImageChops.difference(im,Image.new('RGB',im.size,(255,255,255))).getbbox()
    pad=24
    b=(max(bbox[0]-pad,0),max(bbox[1]-pad,0),min(bbox[2]+pad,im.size[0]),min(bbox[3]+pad,im.size[1]))
    im.crop(b).save(f'{out}/{name}.png'); print(name,im.size,b)
