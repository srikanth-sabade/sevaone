const fs=require('fs');const D=require('docx');
const {Document,Packer,Paragraph,TextRun,HeadingLevel,Table,TableRow,TableCell,WidthType,ShadingType,BorderStyle,AlignmentType,Header,Footer,PageNumber,LevelFormat}=D;
const TEAL='0F766E',INK='1B3A3F',GREY='5B6B70',FONT='Calibri';
const W=11906-2*1100;
const H1=t=>new Paragraph({heading:HeadingLevel.HEADING_1,children:[new TextRun(t)]});
const H2=t=>new Paragraph({heading:HeadingLevel.HEADING_2,children:[new TextRun(t)]});
const P=t=>new Paragraph({spacing:{after:140,line:300},children:[new TextRun({text:t,size:23})]});
const B=(l,t)=>new Paragraph({numbering:{reference:'bul',level:0},spacing:{after:90,line:290},children:[...(l?[new TextRun({text:l,bold:true,size:23})]:[]),new TextRun({text:t,size:23})]});
const bd={style:BorderStyle.SINGLE,size:4,color:'C9D3D5'};const borders={top:bd,bottom:bd,left:bd,right:bd};
function table(cols,rows,fs=21){const tot=cols.reduce((a,c)=>a+c,0);
 const cell=(t,w,hd,bold)=>new TableCell({width:{size:w,type:WidthType.DXA},borders,margins:{top:80,bottom:80,left:120,right:120},shading:hd?{fill:TEAL,type:ShadingType.CLEAR,color:'auto'}:undefined,children:[new Paragraph({children:[new TextRun({text:t,bold:hd||bold,color:hd?'FFFFFF':INK,size:fs})]})]});
 return new Table({width:{size:tot,type:WidthType.DXA},columnWidths:cols,rows:rows.map((r,i)=>new TableRow({cantSplit:true,tableHeader:i===0,children:r.map((c,j)=>cell(c,cols[j],i===0,j===0&&i>0))}))});}
const C=[];
C.push(new Paragraph({spacing:{before:1800,after:200},children:[new TextRun({text:'SevaOne - Service Plus',size:72,bold:true,color:INK})]}));
C.push(new Paragraph({spacing:{after:200},children:[new TextRun({text:'Gaps and Possible Blockers',size:48,bold:true,color:TEAL})]}));
C.push(new Paragraph({spacing:{before:200,after:100},border:{top:{style:BorderStyle.SINGLE,size:10,color:TEAL,space:10}},children:[new TextRun({text:'What we still need to understand, and what could stop the project  ·  Draft for discussion  ·  19 October 2026',size:24,color:GREY})]}));
C.push(H1('1. Summary'));
C.push(P('We understand the shape of the platform we want to build. We do not yet understand enough about what exists today, or about the decisions that belong to the government. Ten items could stop or delay the project if they stay open. A further group needs clarity but would not stop work.'));
C.push(P('Most blockers are about moving what already exists: the data, the users and the service settings. They depend on access and decisions that sit outside the build team.'));
C.push(H1('2. Gaps That Could Be Blockers'));
C.push(table([480,2300,4400,2446],[
 ['#','Gap','Why it could block us','Who can answer'],
 ['1','Data migration','We do not know whether Karnataka\'s existing applications can be exported, in what format, or who may release them. Papers, signatures, payment records and history may not all be included. Open applications must continue at the right step. Application numbers and certificate QR checks must keep working.','Government and the current platform owner'],
 ['2','Service settings migration','Every service has its own form, steps, certificate and messages. We know of no export for these. Special scripts (JavaScript, MVEL, Velocity) may not translate, and each service would have to be rebuilt and checked by hand.','Government, service owners'],
 ['3','User migration','We do not know how existing citizens, officers, operators and departments are held, or how they sign in. Citizens may be tied to Aadhaar or DigiLocker. Officers need their roles and postings. Operators need their centres and wallets. Moving personal data may need consent.','Government, identity owners'],
 ['4','No access to how the current platform works','We know it only from a presentation, one conversation and a few API documents. We have no code, database design, API contract or test environment. The recording of the conversation is not transcribed.','Government and the current platform owner'],
 ['5','Hosting not decided','The government will decide where the platform runs. Until then, we cannot size it, price it or plan recovery.','Government'],
 ['6','Outside connections in our name','Identity, signing, payment, benefit and message connections each need approval, credentials and test access. Payment settlement accounts must be set up. Lead times are unknown and can be long.','Government, each provider'],
 ['7','Departments already integrated','Departments call addresses they were given. Moving them needs those addresses redirected, and each side\'s IP allow-lists updated. A department\'s willingness and timing are unknown.','Departments, current platform owner'],
 ['8','Signing and certificate validity','We do not know who signs, with what authority, or whether certificates already issued stay valid and can still be checked after the move.','Government, legal'],
 ['9','Nobody owns the service rules','Fees, who can apply, deadlines and appeal rules are the department\'s. Without named owners, services cannot be configured or approved to go live.','Departments'],
 ['10','Security and compliance approvals','We have not identified the audits and clearances needed before go-live, or how long they take.','Government, security reviewers']],20));
C.push(H1('3. Gaps That Need Clarity'));
C.push(P('These would not stop work today, but each needs an answer before we finish the design.'));
C.push(table([2300,W-2300],[
 ['Area','What we need to know'],
 ['Size','How many services, applications, users and documents Karnataka has, and the busiest day.'],
 ['Officer work','The exact meaning of each action an officer can take. How one officer is chosen when several share a role, or when one is absent.'],
 ['Payments','Which gateways and accounts are used. Refunds and settlement. What happens when money is taken but the application is not saved. Whether payment comes before or after submitting.'],
 ['Messages','Which channels are switched on, the approved senders and templates, and consent rules.'],
 ['Time limits','How deadlines are counted, when they pause, and who receives escalations.'],
 ['Appeals','The law and procedure for appeals, who may appeal, within what time.'],
 ['Operators','How centres are approved, and whether wallets, commissions and settlement are needed.'],
 ['Reports','The agreed meaning of pending, disposed, overdue and completed. Who sees named records and who sees totals.'],
 ['Languages and access','Languages needed on every screen, who approves the wording, and the accessibility standard to meet.'],
 ['History and recovery','How long records are kept, who may read them, and the recovery targets after a failure.'],
 ['Support','Who supports citizens, officers and departments day to day, and to what service level.'],
 ['Existing mobile app','The current app depends on the existing platform. We need a plan for the period when both run.']],21));
C.push(H1('4. Suggested First Steps'));
C.push(B('Ask for an export. ','Ask whether Karnataka can get its services, applications and users out, and in what form. This answers blockers 1, 2 and 3.'));
C.push(B('Name the owners. ','Name who approves service rules, launches, fees and appeals for each department.'));
C.push(B('Choose three services to map first. ','One simple, one with several reviews, one with a fee and a certificate.'));
C.push(B('List the outside connections. ','Decide which are needed on day one and start the approvals early.'));
C.push(B('Get the recording transcribed ','and add what it says to this list.'));
C.push(B('Settle hosting ','so sizing and recovery planning can start.'));
const doc=new Document({creator:'SevaOne',title:'Service Plus: Gaps and Possible Blockers',
 styles:{default:{document:{run:{font:FONT,size:23,color:INK}}},paragraphStyles:[
  {id:'Heading1',name:'Heading 1',basedOn:'Normal',next:'Normal',quickFormat:true,run:{size:36,bold:true,color:TEAL,font:FONT},paragraph:{spacing:{before:360,after:160},outlineLevel:0}},
  {id:'Heading2',name:'Heading 2',basedOn:'Normal',next:'Normal',quickFormat:true,run:{size:28,bold:true,color:INK,font:FONT},paragraph:{spacing:{before:240,after:120},outlineLevel:1}}]},
 numbering:{config:[{reference:'bul',levels:[{level:0,format:LevelFormat.BULLET,text:'•',alignment:AlignmentType.LEFT,style:{paragraph:{indent:{left:600,hanging:300}}}}]}]},
 sections:[{properties:{page:{size:{width:11906,height:16838},margin:{top:1200,bottom:1100,left:1100,right:1100}}},
  headers:{default:new Header({children:[new Paragraph({border:{bottom:{style:BorderStyle.SINGLE,size:6,color:TEAL,space:4}},children:[new TextRun({text:'SevaOne - Service Plus  |  Gaps and Possible Blockers',size:18,color:GREY})]})]})},
  footers:{default:new Footer({children:[new Paragraph({alignment:AlignmentType.RIGHT,children:[new TextRun({text:'Draft for discussion  ·  Page ',size:18,color:GREY}),new TextRun({children:[PageNumber.CURRENT],size:18,color:GREY})]})]})},
  children:C}]});
Packer.toBuffer(doc).then(b=>{fs.writeFileSync('SevaOne_ServicePlus_Gaps_and_Blockers.docx',b);console.log('ok')});
