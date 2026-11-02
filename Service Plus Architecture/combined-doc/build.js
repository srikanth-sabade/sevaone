const fs=require('fs');const D=require('docx');
const {Document,Packer,Paragraph,TextRun,ImageRun,HeadingLevel,Table,TableRow,TableCell,WidthType,ShadingType,BorderStyle,AlignmentType,PageOrientation,Header,Footer,PageNumber,LevelFormat}=D;
const IMG=process.env.HOME+'/Desktop/Projects/Codes/SevaOne-All/sevaone/hld-diagram/';
const TEAL='0F766E',INK='1B3A3F',GREY='5B6B70',FONT='Calibri';
const IN=1440, PW=22*IN, PH=15*IN, MX=Math.round(0.7*IN), TOP=Math.round(0.85*IN), BOT=Math.round(0.6*IN);
const UW=PW-2*MX; // usable width DXA
const sz=f=>{const b=fs.readFileSync(f);return [b.readUInt32BE(16),b.readUInt32BE(20)];};
function img(file,maxWin,maxHin){const [w,h]=sz(IMG+file);let W=maxWin*96,H=W*h/w;if(H>maxHin*96){H=maxHin*96;W=H*w/h;}
 return new Paragraph({alignment:AlignmentType.CENTER,spacing:{before:40,after:0},children:[new ImageRun({type:'png',data:fs.readFileSync(IMG+file),transformation:{width:Math.round(W),height:Math.round(H)},altText:{title:file,description:file,name:file}})]});}
const H1=(t,pb=true)=>new Paragraph({heading:HeadingLevel.HEADING_1,pageBreakBefore:pb,children:[new TextRun(t)]});
const H2=(t,pb=true)=>new Paragraph({heading:HeadingLevel.HEADING_2,pageBreakBefore:pb,children:[new TextRun(t)]});
const CAP=t=>new Paragraph({spacing:{after:60},children:[new TextRun({text:t,size:34,color:GREY})]});
const P=t=>new Paragraph({spacing:{after:160,line:320},children:[new TextRun({text:t,size:40})]});
const B=(l,t)=>new Paragraph({numbering:{reference:'bul',level:0},spacing:{after:120,line:320},children:[...(l?[new TextRun({text:l,bold:true,size:40})]:[]),new TextRun({text:t,size:40})]});
const bd={style:BorderStyle.SINGLE,size:6,color:'C9D3D5'};const borders={top:bd,bottom:bd,left:bd,right:bd};
function table(cols,rows,fs=36){const tot=cols.reduce((a,c)=>a+c,0);
 const cell=(t,w,hd,bold)=>new TableCell({width:{size:w,type:WidthType.DXA},borders,margins:{top:100,bottom:100,left:160,right:160},shading:hd?{fill:TEAL,type:ShadingType.CLEAR,color:'auto'}:undefined,children:[new Paragraph({children:[new TextRun({text:t,bold:hd||bold,color:hd?'FFFFFF':INK,size:fs})]})]});
 return new Table({width:{size:tot,type:WidthType.DXA},columnWidths:cols,rows:rows.map((r,i)=>new TableRow({cantSplit:true,tableHeader:i===0,children:r.map((c,j)=>cell(c,cols[j],i===0,j===0&&i>0))}))});}
const IMW=20.6, IMH=11.6;
const C=[];
C.push(new Paragraph({spacing:{before:2600,after:240},children:[new TextRun({text:'SevaOne - Service Plus',size:140,bold:true,color:INK})]}));
C.push(new Paragraph({spacing:{after:240},children:[new TextRun({text:'Architecture Diagrams and Gaps',size:84,bold:true,color:TEAL})]}));
C.push(new Paragraph({spacing:{after:240},children:[new TextRun({text:'Current architecture, proposed architecture, and what we still need to understand',size:46,color:GREY})]}));
C.push(new Paragraph({spacing:{before:300},border:{top:{style:BorderStyle.SINGLE,size:14,color:TEAL,space:14}},children:[new TextRun({text:'Draft for discussion  ·  19 October 2026',size:38,color:GREY})]}));
// Part 1
C.push(H1('1. Current Architecture (As Is)'));
C.push(H2('1.1 SevaOne: High-Level Architecture (Web)',false));
C.push(CAP('How the web platform is built today: clients, gateway and identity, the .NET backend, data and outside systems.'));
C.push(img('SevaOne_HLD_Web.png',IMW,IMH));
C.push(H2('1.2 SevaOne: Department Integration (Currently Implemented)'));
C.push(CAP('How a department calls SevaOne today through WSO2 API Manager and WSO2 Identity Server.'));
C.push(img('SevaOne_Dept_HLD.png',IMW,IMH));
C.push(H2('1.3 Service Plus: Architecture as Known Today'));
C.push(CAP('Drawn only from what is stated. Dashed amber boxes are not disclosed.'));
C.push(img('SevaOne_ServicePlus_Known.png',IMW,IMH));
// Part 2
C.push(H1('2. Proposed Architecture (To Be)'));
C.push(H2('2.1 SevaOne: Department Integration (Plug and Play on WSO2)',false));
C.push(CAP('The Seva Sindhu token contract served by SevaOne, with WSO2 carrying the token, the callback and the status check.'));
C.push(img('SevaOne_Wso2_HLD.png',IMW,IMH));
C.push(H2('2.2 Service Plus: Proposed Architecture'));
C.push(CAP('All 25 modules on WSO2 Identity Server and API Manager, with an event backbone and one core database.'));
C.push(img('SevaOne_ServicePlus_Proposed.png',IMW,IMH));
// Part 3 gaps
C.push(H1('3. Gaps and Possible Blockers'));
C.push(H2('3.1 Summary',false));
C.push(P('We understand the shape of the platform we want to build. We do not yet understand enough about what exists today, or about the decisions that belong to the government. Ten items could stop or delay the project if they stay open. Most are about moving what already exists: the data, the users and the service settings. They depend on access and decisions outside the build team.'));
C.push(H2('3.2 Gaps That Could Be Blockers'));
const cw=[700,4600,17000,UW-22300];
C.push(table(cw,[
 ['#','Gap','Why it could block us','Who can answer'],
 ['1','Data migration','We do not know whether Karnataka\'s existing applications can be exported, in what format, or who may release them. Papers, signatures, payment records and history may not all be included. Open applications must continue at the right step. Application numbers and certificate QR checks must keep working.','Government and the current platform owner'],
 ['2','Service settings migration','Every service has its own form, steps, certificate and messages. We know of no export for these. Special scripts (JavaScript, MVEL, Velocity) may not translate, so each service may have to be rebuilt and checked by hand.','Government, service owners'],
 ['3','User migration','We do not know how existing citizens, officers, operators and departments are held, or how they sign in. Citizens may be tied to Aadhaar or DigiLocker. Officers need their roles and postings. Operators need their centres and wallets. Moving personal data may need consent.','Government, identity owners'],
 ['4','No access to how the current platform works','We know it only from a presentation, one conversation and a few API documents. We have no code, database design, API contract or test environment. The recording is not transcribed.','Government and the current platform owner'],
 ['5','Hosting not decided','The government will decide where the platform runs. Until then we cannot size it, price it or plan recovery.','Government'],
 ['6','Outside connections in our name','Identity, signing, payment, benefit and message connections each need approval, credentials and test access. Payment settlement accounts must be set up. Lead times are unknown and can be long.','Government, each provider'],
 ['7','Departments already integrated','Departments call addresses they were given. Moving them needs those addresses redirected and each side\'s IP allow-lists updated. A department\'s willingness and timing are unknown.','Departments, current platform owner'],
 ['8','Signing and certificate validity','We do not know who signs, with what authority, or whether certificates already issued stay valid and can still be checked after the move.','Government, legal'],
 ['9','Nobody owns the service rules','Fees, who can apply, deadlines and appeal rules are the department\'s. Without named owners, services cannot be configured or approved to go live.','Departments'],
 ['10','Security and compliance approvals','We have not identified the audits and clearances needed before go-live, or how long they take.','Government, security reviewers']]));
C.push(H2('3.3 Gaps That Need Clarity'));
C.push(P('These would not stop work today, but each needs an answer before we finish the design.'));
C.push(table([5000,UW-5000],[
 ['Area','What we need to know'],
 ['Size','How many services, applications, users and documents Karnataka has, and the busiest day.'],
 ['Officer work','The exact meaning of each action an officer can take. How one officer is chosen when several share a role, or when one is absent.'],
 ['Payments','Which gateways and accounts are used. Refunds and settlement. What happens when money is taken but the application is not saved. Whether payment comes before or after submitting.'],
 ['Messages','Which channels are switched on, the approved senders and templates, and consent rules.'],
 ['Time limits','How deadlines are counted, when they pause, and who receives escalations.'],
 ['Appeals','The law and procedure for appeals, who may appeal, and within what time.'],
 ['Operators','How centres are approved, and whether wallets, commissions and settlement are needed.'],
 ['Reports','The agreed meaning of pending, disposed, overdue and completed. Who sees named records and who sees totals.'],
 ['Languages and access','Languages needed on every screen, who approves the wording, and the accessibility standard to meet.'],
 ['History and recovery','How long records are kept, who may read them, and the recovery targets after a failure.'],
 ['Support','Who supports citizens, officers and departments day to day, and to what service level.'],
 ['Existing mobile app','The current app depends on the existing platform. We need a plan for the period when both run.']]));
C.push(H2('3.4 Suggested First Steps'));
C.push(B('Ask for an export. ','Ask whether Karnataka can get its services, applications and users out, and in what form. This answers blockers 1, 2 and 3.'));
C.push(B('Name the owners. ','Name who approves service rules, launches, fees and appeals for each department.'));
C.push(B('Choose three services to map first. ','One simple, one with several reviews, one with a fee and a certificate.'));
C.push(B('List the outside connections. ','Decide which are needed on day one and start the approvals early.'));
C.push(B('Get the recording transcribed ','and add what it says to this list.'));
C.push(B('Settle hosting ','so sizing and recovery planning can start.'));
const doc=new Document({creator:'SevaOne',title:'SevaOne - Service Plus: Architecture Diagrams and Gaps',
 styles:{default:{document:{run:{font:FONT,size:40,color:INK}}},paragraphStyles:[
  {id:'Heading1',name:'Heading 1',basedOn:'Normal',next:'Normal',quickFormat:true,run:{size:80,bold:true,color:TEAL,font:FONT},paragraph:{spacing:{before:80,after:140},outlineLevel:0}},
  {id:'Heading2',name:'Heading 2',basedOn:'Normal',next:'Normal',quickFormat:true,run:{size:56,bold:true,color:INK,font:FONT},paragraph:{spacing:{before:80,after:80},outlineLevel:1}}]},
 numbering:{config:[{reference:'bul',levels:[{level:0,format:LevelFormat.BULLET,text:'•',alignment:AlignmentType.LEFT,style:{paragraph:{indent:{left:900,hanging:450}}}}]}]},
 sections:[{properties:{page:{size:{width:PH,height:PW,orientation:PageOrientation.LANDSCAPE},margin:{top:TOP,bottom:BOT,left:MX,right:MX,header:350,footer:300}}},
  headers:{default:new Header({children:[new Paragraph({border:{bottom:{style:BorderStyle.SINGLE,size:8,color:TEAL,space:4}},children:[new TextRun({text:'SevaOne - Service Plus  |  Architecture Diagrams and Gaps',size:26,color:GREY})]})]})},
  footers:{default:new Footer({children:[new Paragraph({alignment:AlignmentType.RIGHT,children:[new TextRun({text:'Draft for discussion  ·  Page ',size:26,color:GREY}),new TextRun({children:[PageNumber.CURRENT],size:26,color:GREY})]})]})},
  children:C}]});
Packer.toBuffer(doc).then(b=>{fs.writeFileSync('SevaOne_ServicePlus_Architecture_and_Gaps.docx',b);console.log('ok',b.length)});
