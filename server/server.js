// Zero-dependency server (Node 18+). Run: node server.js
const http=require('http'),fs=require('fs'),path=require('path'),crypto=require('crypto');
const E=process.env,PORT=E.PORT||3000,BASE=E.BASE_URL||`http://localhost:${PORT}`,
OWNER=E.OWNER_ID||'1385410100381876317',SECRET=E.SESSION_SECRET||'change-me-now',
CID=E.DISCORD_CLIENT_ID,CSEC=E.DISCORD_CLIENT_SECRET,DBF=path.join(__dirname,'db.json'),
REQUIRE_PLAN=E.REQUIRE_PLAN!=='0',FOUNDER_CAN_REMOVE_OWNER=E.FOUNDER_CAN_REMOVE_OWNER==='1';
const DUR={'1m':30,'2m':60,'1y':365,lifetime:0};
let db={users:{},pins:{},scans:[],players:{}};try{db=JSON.parse(fs.readFileSync(DBF))}catch{}db.players=db.players||{};
const save=()=>fs.writeFileSync(DBF,JSON.stringify(db));
const sign=v=>crypto.createHmac('sha256',SECRET).update(v).digest('base64url');
const mk=o=>{const b=Buffer.from(JSON.stringify(o)).toString('base64url');return b+'.'+sign(b)};
const rd=s=>{try{const[b,g]=(s||'').split('.');if(crypto.timingSafeEqual(Buffer.from(g),Buffer.from(sign(b))))return JSON.parse(Buffer.from(b,'base64url'))}catch{}};
const hash=(p,s=crypto.randomBytes(16).toString('hex'))=>s+':'+crypto.scryptSync(p,s,32).toString('hex');
const check=(p,h)=>{const[s]=h.split(':');return crypto.timingSafeEqual(Buffer.from(hash(p,s)),Buffer.from(h))};
const cookies=r=>Object.fromEntries((r.headers.cookie||'').split(';').map(c=>c.trim().split(/=(.*)/s).slice(0,2)));
const secure=BASE.startsWith('https')?'; Secure':'';
const setc=(res,n,v,age=604800)=>res.setHeader('Set-Cookie',[...(res.getHeader('Set-Cookie')||[]),`${n}=${v}; HttpOnly; SameSite=Lax; Path=/; Max-Age=${age}${secure}`]);
const send=(res,c,o)=>{const h={'Content-Type':'application/json'};if(res.getHeader('Set-Cookie'))h['Set-Cookie']=res.getHeader('Set-Cookie');res.writeHead(c,h);res.end(JSON.stringify(o))};
const body=r=>new Promise(ok=>{let d='';r.on('data',c=>{d+=c;if(d.length>1e6)r.destroy()});r.on('end',()=>{try{ok(JSON.parse(d))}catch{ok({})}})});
const risk=f=>{const w=f.reduce((a,x)=>a+x.sev,0),m=Math.max(0,...f.map(x=>x.sev));return w>=6?'Severe':(m>=3||w>=4)?'High':w>=2?'Medium':'Low'};
const hits={};const limited=ip=>{const n=Date.now(),a=(hits[ip]=(hits[ip]||[]).filter(t=>n-t<6e4));a.push(n);return a.length>120};
const isStaff=u=>!!u&&(u.role==='owner'||u.role==='founder');
const hasAccess=u=>!!u&&(!REQUIRE_PLAN||isStaff(u)||!!(u.plan&&(u.plan.exp===0||u.plan.exp>Date.now())));
const canGrant=(a,t)=>isStaff(a)&&t.role!=='owner'&&t.id!==a.id;
const canRemove=(a,t)=>isStaff(a)&&a.id!==t.id&&(t.role==='owner'?(a.role==='founder'&&FOUNDER_CAN_REMOVE_OWNER):true);
const pub=(u,a)=>({id:u.id,name:u.name,avatar:u.avatar||'',role:u.role,exp:u.plan?u.plan.exp:null,active:hasAccess(u),pending:!!u.pending,removable:a?canRemove(a,u):false});
const score=id=>(id&&db.players[id])?db.players[id].score:100;
const redir=(res,l)=>{const h={Location:l};if(res.getHeader('Set-Cookie'))h['Set-Cookie']=res.getHeader('Set-Cookie');res.writeHead(302,h);res.end()};

const pinFails={};
// public URL of the free Cloudflare tunnel started by start.bat (your localhost stays your server)
function pubUrl(){try{const m=(fs.readFileSync(path.join(__dirname,'tunnel.log'),'utf8').match(/https:\/\/[a-z0-9-]+\.trycloudflare\.com/g)||[]).filter(u=>!u.startsWith('https://api.'));if(m.length)return m[m.length-1]}catch{}return E.PUBLIC_URL||''}
http.createServer(async(req,res)=>{
 const u=new URL(req.url,BASE),p=u.pathname,ip=req.socket.remoteAddress;
 const me=(()=>{const s=rd(cookies(req).s);return s&&db.users[s.id]})();
 try{
 if(p==='/'||p==='/app'||p==='/index.html'){const fl=p==='/'?path.join(__dirname,'..','WEBSITE','index.html'):path.join(__dirname,'public','index.html');res.writeHead(200,{'Content-Type':'text/html; charset=utf-8'});return res.end(fs.readFileSync(fl))}
 if(p==='/dashboard.html'){res.writeHead(200,{'Content-Type':'text/html; charset=utf-8'});return res.end(fs.readFileSync(path.join(__dirname,'..','WEBSITE','dashboard.html')))}
 if(p==='/auth/discord'){if(!CID)return send(res,500,{error:'DISCORD_CLIENT_ID not set'});
  const st=crypto.randomBytes(12).toString('hex');setc(res,'st',mk({st}),600);
  return redir(res,`https://discord.com/oauth2/authorize?client_id=${CID}&response_type=code&scope=identify&redirect_uri=${encodeURIComponent(BASE+'/auth/discord/callback')}&state=${st}`)}
 if(p==='/auth/discord/callback'){const c=rd(cookies(req).st);
  if(!c||c.st!==u.searchParams.get('state')||!u.searchParams.get('code')){res.writeHead(400);return res.end('Invalid state')}
  if(!CSEC){res.writeHead(500);return res.end('DISCORD_CLIENT_SECRET not set')}
  const t=await(await fetch('https://discord.com/api/oauth2/token',{method:'POST',headers:{'Content-Type':'application/x-www-form-urlencoded'},body:new URLSearchParams({client_id:CID,client_secret:CSEC,grant_type:'authorization_code',code:u.searchParams.get('code'),redirect_uri:BASE+'/auth/discord/callback'})})).json();
  if(!t.access_token){console.error('Discord token error:',t);res.writeHead(400);return res.end('Discord login failed: '+(t.error_description||t.error||'unknown')+' (check client secret and that redirect URI '+BASE+'/auth/discord/callback is added in the Discord portal)')}
  const d=await(await fetch('https://discord.com/api/users/@me',{headers:{Authorization:'Bearer '+t.access_token}})).json();
  if(!d.id){res.writeHead(400);return res.end('Discord login failed')}
  const old=db.users[d.id]||{};db.users[d.id]={...old,id:d.id,name:d.global_name||d.username,avatar:d.avatar?`https://cdn.discordapp.com/avatars/${d.id}/${d.avatar}.png`:'',role:(d.id===OWNER&&!old.demoted)?'owner':(old.role||'user'),via:'discord'};delete db.users[d.id].pending;save();
  setc(res,'s',mk({id:d.id}));return redir(res,'/#dashboard')}
 if(p==='/logout'){setc(res,'s','',0);return redir(res,'/')}
 if(p.startsWith('/dl/')){const pn=db.pins[p.slice(4)],f=path.join(__dirname,'public','RzCore.exe');if(!pn||pn.used||pn.exp<Date.now()||!fs.existsSync(f)){res.writeHead(404);return res.end('Link expired or scanner not available');}const srv=pubUrl()||(BASE.includes('localhost')?((req.headers['x-forwarded-proto']||'http')+'://'+req.headers.host):BASE),b=Buffer.concat([fs.readFileSync(f),Buffer.from('\nRZSRV='+srv+'\n')]);res.writeHead(200,{'Content-Type':'application/octet-stream','Content-Disposition':'attachment; filename="RzCore.exe"','Content-Length':b.length});return res.end(b)}
 if(['/logo.png','/lt.png','/lp.png','/rzlogo.js'].includes(p)){const f=path.join(__dirname,'public',p);if(fs.existsSync(f)){res.writeHead(200,{'Content-Type':p.endsWith('.png')?'image/png':p.endsWith('.js')?'application/javascript':'application/octet-stream'});return fs.createReadStream(f).pipe(res)}res.writeHead(404);return res.end('Not found')}
 if(!p.startsWith('/api/')){res.writeHead(404);return res.end('Not found')}
 if(limited(ip))return send(res,429,{error:'Too many requests'});
 if(p==='/api/me')return send(res,200,{user:me?{id:me.id,name:me.name,avatar:me.avatar,role:me.role,exp:me.plan?me.plan.exp:null,access:hasAccess(me)}:null,pub:pubUrl()});
 if(p==='/api/scans'){if(!me)return send(res,401,{});
  return send(res,200,{scans:db.scans.filter(s=>isStaff(me)||s.by===me.id).slice(-100).reverse().map(s=>({...s,name:(db.pins[s.pin]||{}).name||'',score:score(s.discord)})),pins:Object.entries(db.pins).filter(([,v])=>!v.used&&v.exp>Date.now()&&(isStaff(me)||v.by===me.id)).map(([k,v])=>({pin:k,game:v.game,name:v.name||'',exp:v.exp}))})}
 if(p==='/api/admin/members'){if(!isStaff(me))return send(res,403,{error:'Not allowed'});
  const o={owner:0,founder:1,partner:2,user:3};
  return send(res,200,{members:Object.values(db.users).filter(u=>u.role!=='user'||u.plan).map(u=>pub(u,me)).sort((a,b)=>(o[a.role]-o[b.role])||String(a.name).localeCompare(b.name))})}
 if(p==='/api/session'){if(!me)return send(res,401,{});const pn=db.pins[u.searchParams.get('pin')];if(!pn||(!isStaff(me)&&pn.by!==me.id))return send(res,404,{});return send(res,200,{state:pn.used?'done':(pn.prog&&Date.now()-pn.prog.at<30000?'scanning':'waiting'),prog:pn.prog||null})}
 if(req.method!=='POST')return send(res,405,{});
 if(p==='/api/pin-check'){const ip=String(req.headers['x-forwarded-for']||req.socket.remoteAddress).split(',')[0],n=Date.now(),f=(pinFails[ip]=(pinFails[ip]||[]).filter(x=>n-x<6e4));if(f.length>=8)return send(res,429,{error:'Too many tries'});const b=await body(req),pn=db.pins[String(b.pin)];if(!pn||pn.used||pn.exp<n){f.push(n);return send(res,403,{error:'PIN incorrect'})}return send(res,200,{ok:true})}
 if(p==='/api/progress'){const b=await body(req),pn=db.pins[String(b.pin)];if(!pn||pn.used||pn.exp<Date.now())return send(res,403,{});pn.prog={pct:Math.min(100,Math.max(0,+b.pct||0)),status:String(b.status||'').slice(0,80),at:Date.now()};return send(res,200,{ok:true})}
 if(p==='/api/register'||p==='/api/login'){const{email='',password=''}=await body(req),id='e:'+String(email).toLowerCase().trim();
  if(!/^\S+@\S+\.\S+$/.test(email)||String(password).length<8)return send(res,400,{error:'Valid email and 8+ character password required'});
  if(p==='/api/register'){if(db.users[id])return send(res,409,{error:'Email already registered'});db.users[id]={id,name:email.split('@')[0],avatar:'',role:'user',via:'email',pw:hash(password)};save()}
  else if(!db.users[id]||!db.users[id].pw||!check(password,db.users[id].pw))return send(res,401,{error:'Wrong email or password'});
  setc(res,'s',mk({id}));return send(res,200,{ok:true})}
 if(p==='/api/pin'){if(!me)return send(res,401,{});if(!hasAccess(me))return send(res,403,{error:'No active plan'});const{game='FiveM',name=''}=await body(req);
  const pin=String(crypto.randomInt(0,1e6)).padStart(6,'0');db.pins[pin]={by:me.id,game:String(game).slice(0,20),name:String(name).slice(0,60),exp:Date.now()+864e5,used:false};save();return send(res,200,{pin})}
 if(p==='/api/note'){if(!me)return send(res,401,{});const{pin,name=''}=await body(req),pn=db.pins[String(pin)];if(!pn||(!isStaff(me)&&pn.by!==me.id))return send(res,403,{});pn.name=String(name).slice(0,60);save();return send(res,200,{ok:true})}
 if(p==='/api/admin/grant'){if(!isStaff(me))return send(res,403,{error:'Not allowed'});
  const b=await body(req),id=String(b.id||'').trim(),role=String(b.role||'user'),dur=String(b.duration||'1m');
  if(!/^\d{15,22}$/.test(id))return send(res,400,{error:'Enter a valid Discord ID (17 to 19 digits)'});
  if(!['user','partner','founder'].includes(role))return send(res,400,{error:'Unknown role'});
  if(role!=='founder'&&!(dur in DUR))return send(res,400,{error:'Unknown duration'});
  const t=db.users[id]||(db.users[id]={id,name:'Discord '+id.slice(-4),avatar:'',role:'user',via:'discord',pending:true});
  if(!canGrant(me,t))return send(res,403,{error:'You cannot change this account'});
  if(role!=='founder'&&t.plan&&t.plan.exp===0&&dur!=='lifetime')return send(res,409,{error:'Already lifetime. Remove the access first to change it.'});
  const now=Date.now();t.role=role;delete t.demoted;
  if(role==='founder')t.plan=null;
  else{const d=DUR[dur],base=(t.plan&&t.plan.exp>now)?t.plan.exp:now;t.plan={exp:d===0?0:base+d*864e5,by:me.id,at:now}}
  save();return send(res,200,{ok:true,member:pub(t,me)})}
 if(p==='/api/admin/remove'){if(!isStaff(me))return send(res,403,{error:'Not allowed'});
  const b=await body(req),t=db.users[String(b.id||'')];
  if(!t)return send(res,404,{error:'Member not found'});
  if(!canRemove(me,t))return send(res,403,{error:t.role==='owner'?'Only the owner role is protected from founders':'You cannot remove this account'});
  if(t.role==='owner')t.demoted=true;
  t.role='user';t.plan=null;save();return send(res,200,{ok:true})}
 if(p==='/api/scan/mark'){if(!me)return send(res,401,{});
  const b=await body(req),sc=db.scans.find(x=>x.pin===String(b.pin));
  if(!sc||(!isStaff(me)&&sc.by!==me.id))return send(res,404,{error:'Report not found'});
  const id=String(b.discord||'').trim();
  if(!/^\d{15,22}$/.test(id))return send(res,400,{error:'Enter a valid Discord ID (17 to 19 digits)'});
  if(sc.discord&&sc.discord!==id&&sc.marked)return send(res,409,{error:'This report is already marked for another Discord ID'});
  sc.discord=id;const pl=db.players[id]||(db.players[id]={score:100,marks:0});
  if(b.cheater===true){if(sc.marked)return send(res,409,{error:'This report is already marked as cheater'});sc.marked=true;pl.marks++;pl.score=Math.max(0,pl.score-5);pl.last=Date.now();}
  save();return send(res,200,{ok:true,score:pl.score,marks:pl.marks,marked:!!sc.marked})}
 if(p==='/api/scan'){const b=await body(req),pn=db.pins[String(b.pin)];
  if(!pn||pn.used||pn.exp<Date.now())return send(res,403,{error:'Invalid or expired PIN'});
  const f=(Array.isArray(b.findings)?b.findings:[]).slice(0,1000).map(x=>({sev:Math.min(3,Math.max(1,+x.sev||1)),text:String(x.text).slice(0,700),cat:String(x.cat||'Other').slice(0,40),src:String(x.src||'File').slice(0,20)}));
  const m=b.meta||{},st=(v,n)=>String(v==null?'':v).slice(0,n);
  const meta={install:st(m.install,40),days:Number.isFinite(+m.days)?Math.round(+m.days):-1,boots:(Array.isArray(m.boots)?m.boots:[]).slice(0,5).map(x=>st(x,40)),uninstalls:(Array.isArray(m.uninstalls)?m.uninstalls:[]).slice(0,25).map(x=>({n:st(x&&x.n,80),t:st(x&&x.t,40)}))};
  pn.used=true;db.scans.push({pin:b.pin,by:pn.by,game:pn.game,os:String(b.os||'').slice(0,100),findings:f,meta,risk:risk(f),at:Date.now()});save();return send(res,200,{ok:true})}
 send(res,404,{});
 }catch(e){console.error(e);send(res,500,{error:'Server error'})}
}).listen(PORT,()=>console.log('RzCore server on '+BASE+' | owner '+OWNER));
