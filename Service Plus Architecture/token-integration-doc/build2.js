const fs=require('fs');
const D=require('docx');
const {Document,Packer,Paragraph,TextRun,ImageRun,HeadingLevel,Table,TableRow,TableCell,WidthType,ShadingType,BorderStyle,AlignmentType,PageOrientation,Header,Footer,PageNumber,LevelFormat,SectionType}=D;
const IMG=process.env.HOME+'/Desktop/Projects/Codes/SevaOne-All/sevaone/hld-diagram/';
const DI=IMG+'docimg/';
const FONT='Calibri',TEAL='0F766E',INK='1B3A3F',GREY='5B6B70';
const IN=1440;
// custom page sizes (Word allows up to 22in)
const WIDE={w:22*IN,h:17*IN,mx:Math.round(0.8*IN),top:Math.round(0.9*IN),bot:Math.round(0.7*IN)};
const TALL={w:20*IN,h:22*IN,mx:Math.round(0.6*IN),top:Math.round(0.9*IN),bot:Math.round(0.6*IN)};
const sz=f=>{const b=fs.readFileSync(f);return [b.readUInt32BE(16),b.readUInt32BE(20)];};
function img(file,maxWin,maxHin){const [w,h]=sz(file);let W=maxWin*96,H=W*h/w;if(H>maxHin*96){H=maxHin*96;W=H*w/h;}
 return new Paragraph({alignment:AlignmentType.CENTER,spacing:{before:60,after:60},children:[new ImageRun({type:'png',data:fs.readFileSync(file),transformation:{width:Math.round(W),height:Math.round(H)},altText:{title:file,description:file,name:file}})]});}
const H1=t=>new Paragraph({heading:HeadingLevel.HEADING_1,children:[new TextRun(t)]});
const H2=t=>new Paragraph({heading:HeadingLevel.HEADING_2,children:[new TextRun(t)]});
const P=(t)=>new Paragraph({spacing:{after:140,line:320},children:[new TextRun({text:t,size:50})]});
const BL=(lead,t)=>new Paragraph({numbering:{reference:'bul',level:0},spacing:{after:130,line:320},children:[...(lead?[new TextRun({text:lead,bold:true,size:50})]:[]),new TextRun({text:t,size:50})]});
const NUM=(t,r)=>new Paragraph({numbering:{reference:r,level:0},spacing:{after:200,line:330},children:[new TextRun({text:t,size:52})]});
const bd={style:BorderStyle.SINGLE,size:6,color:'C9D3D5'};const borders={top:bd,bottom:bd,left:bd,right:bd};
function table(cols,rows,fs_=38){const total=cols.reduce((a,c)=>a+c,0);
 const cell=(t,w,hd)=>new TableCell({width:{size:w,type:WidthType.DXA},borders,margins:{top:110,bottom:110,left:170,right:170},shading:hd?{fill:TEAL,type:ShadingType.CLEAR,color:'auto'}:undefined,children:[new Paragraph({children:[new TextRun({text:t,bold:hd,color:hd?'FFFFFF':INK,size:fs_})]})]});
 return new Table({width:{size:total,type:WidthType.DXA},columnWidths:cols,rows:rows.map((r,i)=>new TableRow({cantSplit:true,tableHeader:i===0,children:r.map((c,j)=>cell(c,cols[j],i===0))}))});}
const hdr=new Header({children:[new Paragraph({border:{bottom:{style:BorderStyle.SINGLE,size:8,color:TEAL,space:4}},children:[new TextRun({text:'SevaOne - Service Plus  |  Token-Based Integration with Departments',size:26,color:GREY})]})]});
const ftr=new Footer({children:[new Paragraph({alignment:AlignmentType.RIGHT,children:[new TextRun({text:'Draft for discussion  ·  Page ',size:26,color:GREY}),new TextRun({children:[PageNumber.CURRENT],size:26,color:GREY})]})]});
const sec=(pg,children,first=false)=>({properties:{type:first?undefined:SectionType.NEXT_PAGE,page:{size:{width:Math.min(pg.w,pg.h),height:Math.max(pg.w,pg.h),orientation:pg.w>pg.h?PageOrientation.LANDSCAPE:PageOrientation.PORTRAIT},margin:{top:pg.top,bottom:pg.bot,left:pg.mx,right:pg.mx,header:400,footer:350}}},headers:{default:hdr},footers:{default:ftr},children});
const WU=WIDE.w-2*WIDE.mx; // usable width wide

const S=[];
// ---- cover ----
S.push(sec(WIDE,[
 new Paragraph({spacing:{before:3200,after:240},children:[new TextRun({text:'SevaOne - Service Plus',size:130,bold:true,color:INK})]}),
 new Paragraph({spacing:{after:240},children:[new TextRun({text:'Token-Based Integration with Departments',size:80,bold:true,color:TEAL})]}),
 new Paragraph({spacing:{after:240},children:[new TextRun({text:'Plug-and-play design on WSO2 Identity Server and API Manager',size:48,color:GREY})]}),
 new Paragraph({spacing:{before:300},border:{top:{style:BorderStyle.SINGLE,size:14,color:TEAL,space:14}},children:[new TextRun({text:'Draft for discussion  ·  19 October 2026',size:38,color:GREY})]})],true));
// ---- 1 HLD ----
S.push(sec(WIDE,[H1('1. High-Level Design (Web)'),img(IMG+'SevaOne_HLD_Web.png',20.4,13.2)]));
// ---- 1b features ----
S.push(sec(WIDE,[H1('1.1 Features Implemented in the Codebase Today'),
 table([5600,WU-5600],[
 ['Area','What is working'],
 ['Citizen sign-in','Mobile OTP and DigiLocker (Aadhaar). WSO2 Identity Server session. Sessions kept in Redis.'],
 ['Service catalogue','Departments, sub-departments and services, filtered by user type. Search. Live notices per service.'],
 ['Apply and track','Apply to a department service, return from the department, application tracking and timeline.'],
 ['Assisted mode (operator)','Operator login, apply on behalf of a citizen, citizen OTP binding.'],
 ['Department integration','Five endpoints under /api/v1/dept, protected by WSO2 API Manager and Identity Server.'],
 ['Other redirect methods','AJSK and Sakala (including ESCOM) token redirects, chosen per service.'],
 ['Admin panel','Overview, catalogue, departments, districts, citizens, operators, funnel, aging, Sakala SLA, notices, notifications, activity log, AI operations, admin management.'],
 ['AI assistant','Chat for finding services, on the GPU server (separate), reached through the backend.'],
 ['Platform','Kafka outbox events, Redis cache, PostgreSQL with 6 schemas, Flyway migrations, health checks, stable SEVA-* error codes.']],48)]));
// ---- 2 current ----
S.push(sec(WIDE,[H1('2. Department Integration [Currently Implemented]'),H2('2.1 Architecture'),img(IMG+'SevaOne_Dept_HLD.png',20.4,12.8)]));
S.push(sec(TALL,[H1('2. Department Integration [Currently Implemented]'),H2('2.2 Sequence of Calls'),img(DI+'dept_seq_full.png',17.6,17.6)]));
S.push(sec(WIDE,[H1('2. Department Integration [Currently Implemented]'),H2('2.3 How It Works'),
 NUM('The citizen clicks Apply. SevaOne creates a one-time, encrypted token and sends the citizen to the department.','n1'),
 NUM('The department gets its own identity from WSO2 Identity Server (one account per department).','n1'),
 NUM('The department calls SevaOne through WSO2 API Manager with its ApiKey and Bearer token.','n1'),
 NUM('SevaOne checks the gateway secret, the department identity and the citizen token, then redeems the token once.','n1'),
 NUM('When the citizen finishes, the department calls get-acknowledgement. SevaOne creates the application and sends the citizen back.','n1'),
 NUM('The department pushes status updates whenever the status changes.','n1'),
 H2('WSO2 is used for'),
 BL('API Manager: ','ApiKey check and the gateway secret on every department call.'),
 BL('Identity Server: ','one identity per department, checked on every call, and disabled instantly if needed.')]));
// ---- 3 plug and play ----
S.push(sec(WIDE,[H1('3. Department Integration [Plug and Play]'),H2('3.1 Architecture'),img(IMG+'SevaOne_Wso2_HLD.png',20.4,12.6)]));
S.push(sec(TALL,[H1('3. Department Integration [Plug and Play]'),H2('3.2 Sequence of Calls'),img(DI+'wso2_seq_full.png',17.4,17.6)]));
S.push(sec(WIDE,[H1('3. Department Integration [Plug and Play]'),H2('3.3 How It Works'),
 NUM('The citizen clicks Apply. SevaOne creates a handoff and asks WSO2 Identity Server for a Token.','n2'),
 NUM('SevaOne sends the browser to the department with a POST form: UserId, edcsApplicationRefNo, Token, assistedmode, ServiceId.','n2'),
 NUM('The department works exactly as it does with Seva Sindhu today. No change for the department.','n2'),
 NUM('The department posts the final acknowledgement to one URL. WSO2 API Manager checks the credentials, IP and limits.','n2'),
 NUM('SevaOne checks the Token with Identity Server, saves the acknowledgement once, sets SUCCESS and revokes the Token.','n2'),
 NUM('SevaOne asks the department for status using the GSC number, through an API Manager outbound API.','n2'),
 H2('WSO2 is used for'),
 BL('Identity Server: ','creates, checks and revokes the Token. Holds the department accounts.'),
 BL('API Manager: ','publishes the acknowledgement URL, checks credentials, IP and limits, and carries the status calls with retry.'),
 H2('Still to confirm'),
 BL('','The address departments call for the acknowledgement, and the IP allow-lists on both sides.')]));
// ---- 4 drawbacks ----
S.push(sec(WIDE,[H1('4. Drawbacks of Plug and Play: What We Give Up'),
 table([7000,WU-7000],[
 ['What we lose','The advantage we had today'],
 ['Per-department identity','Each department has its own WSO2 identity that can be disabled instantly. The contract uses one shared Basic credential.'],
 ['Status pushed to us','Departments tell us when a status changes. In the contract we must ask, call by call.'],
 ['Clear error codes','Departments get a specific code for each problem. The contract returns only "Data Not Inserted".'],
 ['A handoff that stands alone','Starting an application does not need Identity Server today. In the new way, every handoff does.'],
 ['A contract we control','We can change our own contract. The new one belongs to Seva Sindhu and has open points.'],
 ['Less gateway work','No extra inbound API, throttle tier or outbound API for each department.']],50)]));
// ---- 5 future ----
S.push(sec(WIDE,[H1('5. Future: Coming Back to the Original Architecture'),
 H2('How to come back'),
 BL('One core. ','Handoff, application record, status and events are the same for both contracts.'),
 BL('A switch per service. ','The integration-method setting on each service picks the contract. No release needed.'),
 BL('Keep the current endpoints. ','Nothing is removed; plug and play is added beside them.'),
 BL('Move department by department. ','Finish applications in progress on their own contract, then switch the service.'),
 BL('Finally ','unpublish the acknowledgement API and the outbound status APIs.'),
 H2('What we gain by coming back'),
 BL('','Our own contract, which we can version.'),
 BL('','Stronger department identity, revocable instantly.'),
 BL('','Status pushed to us, with clear error codes.'),
 BL('','Less to configure and support in WSO2.')]));
const doc=new Document({creator:'SevaOne',title:'SevaOne - Service Plus: Token-Based Integration',
 styles:{default:{document:{run:{font:FONT,size:40,color:INK}}},paragraphStyles:[
  {id:'Heading1',name:'Heading 1',basedOn:'Normal',next:'Normal',quickFormat:true,run:{size:72,bold:true,color:TEAL,font:FONT},paragraph:{spacing:{before:80,after:160},outlineLevel:0}},
  {id:'Heading2',name:'Heading 2',basedOn:'Normal',next:'Normal',quickFormat:true,run:{size:52,bold:true,color:INK,font:FONT},paragraph:{spacing:{before:200,after:120},outlineLevel:1}}]},
 numbering:{config:[{reference:'bul',levels:[{level:0,format:LevelFormat.BULLET,text:'•',alignment:AlignmentType.LEFT,style:{paragraph:{indent:{left:900,hanging:450}}}}]},
  ...['n1','n2'].map(r=>({reference:r,levels:[{level:0,format:LevelFormat.DECIMAL,text:'%1.',alignment:AlignmentType.LEFT,style:{paragraph:{indent:{left:900,hanging:560}}}}]}))]},
 sections:S});
Packer.toBuffer(doc).then(b=>{fs.writeFileSync('SevaOne_Token_Based_Integration.docx',b);console.log('ok',b.length)});
