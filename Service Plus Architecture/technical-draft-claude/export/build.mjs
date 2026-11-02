import { createRequire } from 'module';
const require = createRequire('/home/srikanth/Desktop/Projects/Codes/SevaOne-All/sevaone/Service Plus Architecture/.build/review1/package.json');
const PptxGenJS = require('pptxgenjs');
const p = new PptxGenJS(); p.layout='LAYOUT_WIDE'; // 13.33 x 7.5
const teal='0F766E', ink='1B3A3F', grey='5B6B70';
let s=p.addSlide(); s.background={color:'FAFAF8'};
s.addText('High-Level Diagram',{x:0.5,y:0.3,w:12.3,h:0.6,fontFace:'Calibri',fontSize:30,bold:true,color:ink});
s.addText('Built on WSO2 Identity Server and WSO2 API Manager. A draft for discussion.',{x:0.5,y:0.9,w:12.3,h:0.4,fontFace:'Calibri',fontSize:15,color:grey});
const ratio=3720/1542; const w=12.3, h=w/ratio;
s.addImage({path:'diagram.png',x:0.5,y:1.45,w:w,h:Math.min(h,5.6)});
s.addText('Draft for discussion.',{x:0.5,y:7.05,w:12.3,h:0.3,fontFace:'Calibri',fontSize:11,color:grey});
const cards=[
 ['Sign-in and access','C0392B',['WSO2 Identity Server handles sign-in, roles and one set of users per state.','WSO2 API Manager is the only way in. It checks tokens, applies limits and versions, and gives departments an API portal.','Officers get access by office and role, not by designation alone.']],
 ['How an application moves','0E7490',['The citizen applies through the front door.','The Application Service saves it and starts a case in the Workflow Engine.','The Office Directory says which office and role handle it.','Every change also leaves an event on the bus.']],
 ['Events do the follow-up work','059669',['Payments, messages, certificates and history all react to events.','A failure in one does not undo the application.','Payments are matched against bank records, not trusted from the browser.']],
 ['What we use and why','B7791F',['Kafka for events: proven at scale and open source.', 'WSO2 Micro Integrator for all outside connections, so each one is built and watched the same way.', 'One PostgreSQL with a separate schema per state, so states stay apart without many databases.', 'S3-compatible object storage for files and certificates, hosted by the government.']]];
let s2=p.addSlide(); s2.background={color:'FAFAF8'};
s2.addText('What each part does',{x:0.5,y:0.3,w:12.3,h:0.6,fontFace:'Calibri',fontSize:30,bold:true,color:ink});
cards.forEach((c,i)=>{const x=0.5+(i%2)*6.25, y=1.2+Math.floor(i/2)*2.95;
 s2.addShape(p.ShapeType.roundRect,{x,y,w:6.0,h:2.75,fill:{color:'FFFFFF'},line:{color:'D5DCDD',width:1},rectRadius:0.08});
 s2.addShape(p.ShapeType.rect,{x,y,w:0.09,h:2.75,fill:{color:c[1]},line:{color:c[1]}});
 s2.addText(c[0],{x:x+0.25,y:y+0.1,w:5.6,h:0.5,fontFace:'Calibri',fontSize:20,bold:true,color:c[1]});
 s2.addText(c[2].map(t=>({text:t,options:{bullet:true,breakLine:true}})),{x:x+0.25,y:y+0.65,w:5.55,h:2.0,fontFace:'Calibri',fontSize:15,color:ink,valign:'top',paraSpaceAfter:5});
});
await p.writeFile({fileName:'ServicePlus_High_Level_Diagram.pptx'});
