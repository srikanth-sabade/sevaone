from specs import card, T
X=[40,290,540,790,1040,1290]; Y=[40,160,280,400,520]
V={}   # views: key -> dict(kind=...)
# ---------- sequences ----------
def seq(slug,title,sub,parts,msgs,segs,cards):
    return dict(kind='seq',slug=slug,title=title,sub=sub,parts=parts,msgs=msgs,segs=segs,cards=cards)
V['seq1']=seq('s1-apply-to-certificate','From Apply to Certificate','One application, start to finish, across the modules.',
 [('citizen','ext','Citizen','web or mobile'),('app','be','App Service','draft, submit'),('rules','be','Rules Engine','documents, fee'),('pay','be','Payments','fee and receipt'),('wf','be','Workflow Engine','runs the case'),('officer','ext','Officer','work list'),('doc','be','Documents','certificate'),('notif','be','Notifications','messages')],
 [('citizen','app','sign in and apply','default'),('app','rules','which papers and fee?','default'),('rules','app','papers list, amount','return'),('app','citizen','form and fee shown','return'),
  ('citizen','app','submit with papers','emphasis'),('app','pay','fee due','default'),('citizen','pay','pays online','security'),('pay','app','paid','dashed'),
  ('app','wf','start case','emphasis'),('wf','officer','case in work list','default'),('officer','wf','approve','emphasis'),
  ('wf','doc','approved','dashed'),('doc','notif','certificate ready','dashed'),('notif','citizen','SMS and app message','dashed'),('doc','citizen','certificate with QR','return')],
 [(0,3,'Apply'),(4,7,'Submit and pay'),(8,10,'Process'),(11,14,'Issue')],
 card(['Every step passes through the API Manager, which checks the sign-in token first','The Application Service saves the application, then starts a case','The officer decides in the Workflow Engine; the certificate follows from the decision event'],
  ['Each box is a module from the earlier pages','Messages shown with dashes are events, not waiting calls'],
  ['The citizen is never asked twice for the same paper','A decision and its certificate are separate: a failed certificate does not undo the decision'],
  ['If payment is not confirmed the case waits; see the payment flow','If the officer asks for a correction the case returns to the citizen; see the correction flow']))
V['seq2']=seq('s2-payment','Payment, Including When It Is Unclear','What happens when a payment succeeds, fails or never answers.',
 [('citizen','ext','Citizen','pays a fee'),('app','be','App Service','needs the fee'),('pay','be','Payment Service','records everything'),('mi','be','Micro Integrator','gateway link'),('gw','ext','Payment gateway','takes the money'),('bank','ext','Bank and treasury','daily files'),('match','be','Matching Job','bank vs records')],
 [('app','pay','fee due','default'),('pay','citizen','go to payment page','return'),('citizen','gw','pays','security'),('gw','mi','signed callback','security'),('mi','pay','payment result','default'),('pay','app','paid, not yet matched','dashed'),
  ('bank','mi','daily file','dashed'),('mi','match','file','dashed'),('match','pay','matched','emphasis'),('pay','app','confirmed','dashed'),
  ('gw','mi','same callback again','dashed'),('mi','pay','ignored: same transaction','return'),
  ('pay','mi','no answer: check status','default'),('mi','gw','status?','default'),('gw','mi','paid or failed','return'),('mi','pay','result','return')],
 [(0,5,'Normal payment'),(6,9,'Matching'),(10,11,'Duplicate'),(12,15,'No answer')],
 card(['The Payment Service keeps the amount due, the attempt, the receipt and the money received as four separate things','A gateway callback marks the attempt; a later bank file confirms it','Only after matching is the payment confirmed to the application'],
  ['A browser return or a screenshot never counts as proof','A repeated callback is ignored by its transaction number'],
  ['Payment never approves a case','Each state uses its own collection accounts and gateways'],
  ['No answer from the gateway: the status is asked for again until it settles','If the application cannot be saved after payment, the money stays tied to the application and is applied or refunded','Anything unmatched goes to a finance problem list']))
V['seq3']=seq('s3-handover','Handing a Citizen to a Department Portal','The token and callback path for services run by another department.',
 [('citizen','ext','Citizen','chooses the service'),('cat','be','Catalogue Service','knows the type'),('hand','be','Handover Service','makes the token'),('iam','sec','Identity Server','signs the token'),('dept','ext','Department portal','does the work'),('apim','cl','API Manager','callback entry'),('app','be','App Service','keeps the record')],
 [('citizen','cat','open service','default'),('cat','citizen','handled by the department','return'),('citizen','hand','continue','default'),('hand','iam','make a short-lived token','security'),('iam','hand','signed token','return'),('hand','citizen','send to department with token','emphasis'),
  ('citizen','dept','open with token','default'),('dept','apim','is this token valid?','security'),('apim','dept','yes, citizen details','return'),('dept','citizen','form and payment there','return'),
  ('dept','apim','callback: result, payment','security'),('apim','app','record result','default'),('app','citizen','status updated','dashed')],
 [(0,5,'Hand over'),(6,9,'In the department'),(10,12,'Result comes back')],
 card(['The catalogue knows each service is "handed over", so the citizen is not shown a form we do not own','A short-lived signed token carries who the citizen is','The department asks us to check the token before it trusts it'],
  ['WSO2 Identity Server signs tokens; WSO2 API Manager receives callbacks','Callbacks are signed and carry the application reference'],
  ['A token works once and expires quickly','A callback can be repeated without making a second record'],
  ['If the citizen stops halfway, the application shows "with department" and can be resumed','If the callback never arrives, the status is asked for again','A failed handover tells the citizen clearly and starts nothing']))
V['seq4']=seq('s4-correction','Correction and Resubmission','The same case goes back to the citizen and returns to the same officer.',
 [('officer','ext','Officer','finds a problem'),('wf','be','Workflow Engine','holds the case'),('app','be','App Service','citizen side'),('notif','be','Notifications','messages'),('citizen','ext','Citizen','fixes it')],
 [('officer','wf','ask for correction, with reason','emphasis'),('wf','app','case waits for citizen','default'),('wf','notif','correction needed','dashed'),('notif','citizen','SMS and app message','dashed'),
  ('citizen','app','open application','default'),('app','citizen','only the allowed parts can be edited','return'),('citizen','app','edit and resubmit','emphasis'),('app','wf','resubmitted, same case','default'),('wf','officer','back in the same officer list','default'),('officer','wf','approve','emphasis'),
  ('wf','notif','reminder after the set days','dashed')],
 [(0,3,'Ask'),(4,7,'Fix'),(8,9,'Decide'),(10,10,'If no reply')],
 card(['The case is not closed and not new; it waits with the citizen','Only the parts the officer allowed can be changed','Earlier versions of the form stay on record'],
  ['Workflow Engine holds the state; Application Service shows the editable form','Notifications are sent from the event'],
  ['Corrections are limited to the stages and fields the service allows','A correction may need a new payment only if the service says so'],
  ['No reply by the deadline: a reminder, then escalation','A failed message does not stop the case']))
# ---------- architecture views ----------
def arch(slug,title,sub,nodes,edges,cards,boundaries=None):
    return dict(kind='arch',slug=slug,title=title,sub=sub,nodes=nodes,edges=edges,cards=cards,boundaries=boundaries or [])
V['channels']=arch('p1-channels','Channels and the AI Assistant','Who uses the platform, and how the AI assistant fits in.',
 [('web','fe','Citizen web portal','React',0,0),('mob','fe','Mobile app','Flutter',0,1),('off','fe','Officer workspace','web',0,2),('adm','fe','Admin and Studio','web',0,3),
  ('apim','cl','WSO2 API Manager','the one front door',1,1,dict(h=240,y=100)),('iam','sec','WSO2 Identity Server','sign-in',2,0),('svc','be','Platform services','all the modules',2,1),('aigw','be','AI Gateway','limits and logs AI calls',2,2),
  ('pg','db','PostgreSQL','per-state data',3,1),('ai','ext','AI assistant','stays on the GPU server',3,2)],
 [('web','apim','HTTPS'),('mob','apim','HTTPS'),('off','apim','HTTPS'),('adm','apim','HTTPS'),('apim','iam','check token'),('apim','svc','api'),('apim','aigw','chat'),('svc','pg','data'),('aigw','ai','ask')],
 card(['Four front ends all use the same APIs through the API Manager','The mobile app and web portal show the same services and forms','The AI assistant answers questions; it does not decide cases'],
  ['React for the web, Flutter for mobile','WSO2 API Manager and Identity Server for every call','An AI Gateway in front of the assistant'],
  ['The assistant sees only what the signed-in person may see','AI calls are limited, logged and cannot change data'],
  ['If the assistant is down everything else works','The AI server is separate; this platform only calls it']))
V['security']=arch('p2-security','Security','Zones, keys, secrets and watching for trouble.',
 [('users','ext','Internet users','citizens, officers',0,1),('waf','cl','Firewall and WAF','blocks attacks, TLS',1,1),('apim','cl','WSO2 API Manager','keys, limits',2,1),('iam','sec','WSO2 Identity Server','sign-in, MFA',2,0),
  ('svc','be','Platform services','encrypted traffic',3,1),('vault','sec','Secrets vault','keys and passwords',3,0),('hsm','sec','Signing keys (HSM)','certificate signing',4,0),('db','db','PostgreSQL','encrypted, per-state keys',4,1),
  ('audit','be','Audit Log','write once',3,2),('siem','be','Security monitoring','alerts on odd activity',3,3),('bak','db','Encrypted backups','copies kept apart',4,2)],
 [('users','waf','HTTPS'),('waf','apim','clean traffic'),('apim','iam','check token'),('apim','svc','encrypted'),('svc','vault','get secrets'),('svc','hsm','sign'),('svc','db','encrypted'),('svc','audit','every action'),('audit','siem','watch'),('db','bak','copy')],
 card(['Traffic is cleaned at the edge, then every call is checked at the API Manager','Services run in a private zone and use secrets from a vault, never from files','Certificates are signed with keys held in a hardware module'],
  ['WSO2 Identity Server for sign-in and MFA','A secrets vault, an HSM for signing, TLS everywhere','Encrypted storage with a separate key per state'],
  ['No service talks to the internet directly','Every action is written once and cannot be edited','Support staff reach a state only with approval'],
  ['A strange pattern raises an alert to the support team','A leaked key can be replaced without stopping services','Backups are encrypted and restores are rehearsed']),
 [('security-group','Public zone',['waf']),('security-group','Gateway zone',['apim','iam']),('region','Private zone',['svc','vault','hsm','db','audit','siem','bak'])])
V['deploy']=arch('p3-deployment','Deployment and Recovery','How it is built, released, run and recovered.',
 [('repo','cl','Code repository','all source',0,0),('ci','be','Build pipeline','build, test, scan',1,0),('reg','db','Image registry','signed images',2,0),('test','be','Test environment','trial before live',3,0),
  ('users','ext','Users','all channels',0,2),('lb','cl','Load balancer','spreads traffic',1,2),('k8s','be','Container platform','many copies of each service',2,2),('bus','bus','Kafka cluster','events',3,1),
  ('db','db','PostgreSQL primary','all data',3,2),('rep','db','PostgreSQL replica','reports and backup',3,3),('obj','db','Object storage','files, certificates',2,3),('mon','be','Monitoring','health and alarms',1,3),('dr','be','Standby site','takes over if needed',4,2)],
 [('repo','ci','push'),('ci','reg','build'),('reg','test','deploy'),('reg','k8s','approved'),('users','lb','HTTPS'),('lb','k8s','route'),('k8s','bus','events'),('k8s','db','data'),('db','rep','copy'),('k8s','obj','files'),('k8s','mon','health'),('db','dr','copy')],
 card(['Code goes through a build pipeline that tests and scans it, then makes signed images','Images are tried in a test environment, then released to the live platform after approval','Live runs several copies of each service so one failure does not stop service'],
  ['Containers on a standard container platform (Kubernetes)','The government chooses where this runs: its own data centre or an approved cloud','Replicas, standby site and tested backups'],
  ['Nothing is changed by hand on live','A release can be rolled back','Each state can be backed up and restored on its own'],
  ['If a copy fails, another takes its place','If the main site is lost, the standby site takes over; recovery targets are agreed with the department','Alarms go to the support team before citizens notice']),
 [('region','Live site',['lb','k8s','bus','db','rep','obj','mon']),('region','Standby site',['dr'])])
# ---------- extra modules ----------
V['m21']=arch('m21-bulk','Bulk Processing','Extra feature (Module 21): act on many cases at once.',
 [('officer','ext','Officer','selects many cases',0,1),('apim','cl','WSO2 API Manager','the one front door',1,1),('bulk','be','Bulk Processing','one request, many cases',2,1),('q','bus','Job queue','Kafka',3,0),('wk','be','Batch Worker','one case at a time',3,1),('wf','be','Workflow Engine','acts on each case',4,1),('rep','be','Result Report','done and failed',3,2)],
 [('officer','apim','select cases'),('apim','bulk','bulk action'),('bulk','q','one job each','dashed'),('q','wk','take job','dashed'),('wk','wf','act on case','emphasis'),('wk','rep','result'),('rep','officer','download')],
 card(['The officer selects many cases and one action','Each case becomes its own job, handled one at a time in the Workflow Engine','A report lists what worked and what failed'],
  ['A Kafka queue for the jobs','The same Workflow Engine and access checks as single actions'],
  ['Every case still passes the access check','Each case keeps its own history entry','The same action ID is used as for a single case'],
  ['A failed case does not stop the rest','Failed cases are listed and can be retried','Running the same bulk request twice does not double-act']))
V['m22']=arch('m22-derived','Derived Services','Extra feature (Module 22): one approved service offers a follow-on service.',
 [('wf','be','Workflow Engine','case approved',0,0),('bus','bus','Event bus','Kafka',1,0),('der','be','Derived Service Rule','what can follow',2,0),('cat','be','Catalogue Service','offers the next service',3,0),('pre','be','Prefill','copies known data',2,1),('app','be','Application Service','new draft',3,1),('cit','ext','Citizen','continues',4,1)],
 [('wf','bus','approved','dashed'),('bus','der','event','dashed'),('der','cat','offer'),('der','pre','prefill'),('pre','app','draft'),('app','cit','ready'),('cat','cit','offered')],
 card(['When a case is approved, a rule decides which service can follow, for example a licence after a registration','The citizen is offered it and the new draft is prefilled from the first case','The two applications stay linked'],
  ['Rules in the same rule tables','Events on Kafka, prefill by the Application Service'],
  ['The follow-on service has its own rules, fees and workflow','Prefilled data can be changed by the citizen','The link to the first case is kept'],
  ['If the first case is later cancelled the follow-on is flagged','If prefill fails the citizen starts a normal blank form']))
V['m23']=arch('m23-benefits','Benefit Payments (PFMS and DBT)','Extra feature (Module 23): paying a benefit after approval.',
 [('wf','be','Workflow Engine','approved with a benefit',0,1),('bus','bus','Event bus','Kafka',1,1),('ben','be','Benefit Service','who, how much, release',2,1),('ver','be','Account Check','bank account verified',2,0),('led','db','Benefit records','kept separately',2,2),
  ('notif','be','Notifications','tells the beneficiary',3,0),('mi','be','WSO2 Micro Integrator','link to PFMS',3,1),('rec','be','Matching Job','release status',3,2),('pf','ext','PFMS and DBT','government payment system',4,1)],
 [('wf','bus','approved','dashed'),('bus','ben','event','dashed'),('ben','ver','check account'),('ben','led','records'),('ben','mi','release order'),('mi','pf','APIs','emphasis'),('mi','rec','status'),('ben','notif','paid','dashed')],
 card(['After approval the Benefit Service prepares who is paid, how much and how','The bank account is verified before a release order is sent','Status comes back and is matched against the order'],
  ['WSO2 Micro Integrator for the PFMS link','A separate benefit record store and a Matching Job'],
  ['A benefit is paid once per approval','The amount comes from the approved case, never typed again','Money records are kept apart from application records'],
  ['A rejected account sends the case back for a correction','A release with no answer is checked again','Unmatched releases go to a finance problem list']))
V['m24']=arch('m24-time','Time Limits and Tracking','Extra feature (Module 24): statutory deadlines and who is told when late.',
 [('owner','ext','Service owner','sets the time limit',0,0),('studio','be','Service Studio','limit per service',1,0),('cal','db','Calendars','working days, holidays',2,0),('wf','be','Workflow Engine','runs the case',2,1),('timer','be','Deadline Timers','start, pause, stop',3,1),
  ('esc','be','Escalation Rules','who is told, who takes over',3,2),('notif','be','Notifications','tells people',4,1),('rep','be','Reports and Audit','late and overdue',3,0),('app','be','Application Service','shows the due date',1,2)],
 [('owner','studio','set limit'),('studio','wf','publish'),('wf','timer','start clock'),('cal','timer','working days'),('timer','esc','late'),('esc','notif','tell','dashed'),('timer','rep','overdue','dashed'),('wf','app','due date')],
 card(['Each service has a time limit, such as 21 working days','A clock starts when the case starts and uses a working-day calendar','Late cases are escalated and counted in reports'],
  ['Deadline Timers in the Workflow Engine','A calendar per state with holidays','Notifications and Reports for the alerts'],
  ['The clock can pause while waiting for the citizen if the service allows','Escalation authorities are set per service','The citizen sees the due date'],
  ['If a timer fails, the case is flagged for checking','A missed deadline never changes the decision','Calendar changes apply only to new clocks']))
V['m25']=arch('m25-search','Service Search','Extra feature (Module 25): finding the right service.',
 [('cit','ext','Citizens','search or browse',0,1),('apim','cl','WSO2 API Manager','the one front door',1,1),('cat','be','Catalogue Service','publishes changes',2,0),('srch','be','Search Service','words and life events',2,1),('tr','ext','Translation service','Bhashini',2,2),('feed','be','Index Feeder','updates on publish',3,0),('idx','db','Search index','OpenSearch',3,1)],
 [('cit','apim','find service'),('apim','srch','search'),('srch','idx','query'),('srch','tr','translate'),('cat','feed','changes'),('feed','idx','update')],
 card(['The citizen types in their own language or picks a life event','The Search Service finds matching services and shows who can apply','The list updates when a service is published or retired'],
  ['OpenSearch for the index','Bhashini for translation','The Catalogue Service stays the source of truth'],
  ['Search shows only services available to that state and channel','Results never show retired services as open'],
  ['If search is down, the full list still opens from the catalogue','If translation fails the original language is shown']))
# ---------- ERD ----------
def A(n,t,k=None,r=None):
    d=dict(name=n,type=t)
    if k: d['key']=k
    if r: d['references']=r
    return d
ER=dict(kind='erd',slug='p4-data-model',title='Data Model',sub='The main records, and how a state keeps its own.',
 entities=[
  ('tenant','people',0,0,[A('id','bigint','pk'),A('code','varchar(10)','uk'),A('name','varchar(80)')]),
  ('user_account','people',1,0,[A('id','bigint','pk'),A('tenant_id','bigint','fk','tenant.id'),A('iam_subject','varchar(64)','uk'),A('type','varchar(16)')]),
  ('posting','people',2,0,[A('id','bigint','pk'),A('user_id','bigint','fk','user_account.id'),A('office_id','bigint','fk','office.id'),A('from_date','date')]),
  ('office','services',0,1,[A('id','bigint','pk'),A('tenant_id','bigint','fk','tenant.id'),A('name','varchar(120)'),A('level','varchar(24)')]),
  ('service','services',1,1,[A('id','bigint','pk'),A('tenant_id','bigint','fk','tenant.id'),A('code','varchar(32)','uk'),A('kind','varchar(16)')]),
  ('service_version','services',2,1,[A('id','bigint','pk'),A('service_id','bigint','fk','service.id'),A('version','int'),A('status','varchar(16)')]),
  ('application','applications',0,2,[A('id','bigint','pk'),A('service_version_id','bigint','fk','service_version.id'),A('applicant_id','bigint','fk','user_account.id'),A('number','varchar(32)','uk'),A('status','varchar(24)')]),
  ('application_file','applications',1,2,[A('id','bigint','pk'),A('application_id','bigint','fk','application.id'),A('file_key','varchar(120)')]),
  ('workflow_case','applications',2,2,[A('id','bigint','pk'),A('application_id','bigint','fk','application.id'),A('current_step','varchar(48)')]),
  ('task','work and money',0,3,[A('id','bigint','pk'),A('case_id','bigint','fk','workflow_case.id'),A('office_id','bigint','fk','office.id'),A('due_at','timestamptz')]),
  ('case_action','work and money',1,3,[A('id','bigint','pk'),A('case_id','bigint','fk','workflow_case.id'),A('action_id','int'),A('by_user','bigint','fk','user_account.id')]),
  ('payment','work and money',2,3,[A('id','bigint','pk'),A('application_id','bigint','fk','application.id'),A('amount_due','numeric(12,2)'),A('status','varchar(16)')]),
  ('certificate','results',0,4,[A('id','bigint','pk'),A('application_id','bigint','fk','application.id'),A('number','varchar(32)','uk'),A('file_key','varchar(120)')]),
  ('payment_attempt','results',1,4,[A('id','bigint','pk'),A('payment_id','bigint','fk','payment.id'),A('txn_no','varchar(40)','uk'),A('status','varchar(16)')]),
  ('audit_event','results',2,4,[A('id','bigint','pk'),A('tenant_id','bigint','fk','tenant.id'),A('actor','varchar(64)'),A('action','varchar(48)')])],
 rels=[('user_account','tenant','many','one'),('office','tenant','many','one'),('service','tenant','many','one'),('posting','user_account','many','one'),('posting','office','many','one'),('service_version','service','many','one'),
  ('application','service_version','many','one'),('application','user_account','many','one'),('application_file','application','many','one'),('workflow_case','application','one','one'),('task','workflow_case','many','one'),('case_action','workflow_case','many','one'),
  ('payment','application','many','one'),('payment_attempt','payment','many','one'),('certificate','application','many','one')],
 cards=card(['Every record belongs to a state, so a state sees only its own rows','Service versions pin the form, process and rules an application used','Money records are separate from application records'],
  ['One PostgreSQL with a schema per state','Keys and table names are the same in every state'],
  ['History tables are never updated or deleted','Applications keep the version they started with','Files are stored outside the database; only their keys are kept'],
  ['A failed write is retried, never half-saved','Backups and restores can be done per state','Migrations are rolled out state by state']))
