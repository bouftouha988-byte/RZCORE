// RzCore PC-Checker client (consent-based). Build with build.bat (Windows' built-in csc.exe).
using System;using System.Collections.Generic;using System.Diagnostics;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.IO;using System.Net;using System.Reflection;using System.Runtime.InteropServices;using System.Text;using System.Threading;using System.Windows.Forms;using Microsoft.Win32;
class RzCore:Form{
 const string SERVER="http://localhost:3000"; // put your public https:// server URL here before building
 const int W=900,H=640,MAXF=1000;
 // ---- detection lists: category, base severity (3 severe, 2 high, 1 medium), keywords ----
 static readonly string[][] R={
  new[]{"Cheat Menus & Sellers","3","keyser","susano","redengine","red engine","red-engine","eulen","modmenu","mod menu","aimbot","triggerbot","wallhack","luaexecutor","lua executor","silentaim","silent aim","cheatloader","cheat loader","godmode"},
  new[]{"CS:GO / CS2 Cheats","3","aimware","neverlose","gamesense.pub","skeet.cc","onetap.su","onetap.exe","nixware","fatality.win","legendware","spirthack","interium","plaguecheat","csgo cheat","cs2 cheat","csgocheat","cs2cheat","csgohack","cs2hack","csgo hack","cs2 hack","midnight.im"},
  new[]{"Roblox Executors","3","synapsex","synapse x","synapse_x","krnl.exe","krnl_","krnlss","krnl executor","fluxus","jjsploit","scriptware","script-ware","sirhurt","solarabootstrapper","solara executor","wave executor","waveexecutor","wavebootstrapper","deltaexecutor","delta executor","hydrogen executor","hydrogenm","arceus x","arceusx","oxygenu","evon executor","celery executor","roblox executor","robloxexecutor","roblox exploit","robloxexploit","rbxexploit","rbx executor","macsploit","valyse","xeno executor","swift executor","velocity executor"},
  new[]{"Injectors & Mappers","3","injector","kdmapper","xenos.exe","xenos64","extremeinjector","dllinject","manualmap","manual map","processinject"},
  new[]{"Spoofers","3","spoofer","hwid changer","hwidchanger","serial changer","macchanger"},
  new[]{"Security Blockers","3","defendercontrol","defender control","dcontrol","defenderremover","defender remover","disabledefender","disable-defender","nodefender","no-defender","windowsdefenderremover","wdremover","uacbypass","disablewindowsdefender"},
  new[]{"Vulnerable Drivers","1","iqvw64e","capcom.sys","gdrv.sys","dbutil_2_3","drvmap","osrloader","mhyprot","atillk64","ntiolib","wnbios","bsmem","kprocesshacker"},
  new[]{"Memory & Debug Tools","2","cheatengine","cheat engine","processhacker","process hacker","x64dbg","x32dbg","ollydbg","reclass.net","reclass.exe","scylla.exe","dnspy","ida64.exe","ghidra"},
  new[]{"Bypass Tools","2","bypasser","bypass.exe","bypass.dll","bypass.sys","unbanner"},
  new[]{"Trace Cleaners","1","ccleaner","bleachbit","privazer","eventlogclean","usncleaner","clearlogs"},
  new[]{"Macros & Clickers","1","autoclicker","auto clicker","autohotkey","xmouse","x-mouse","norecoil","no recoil","rapidfire","rapid fire"},
  new[]{"Remote Access Tools","1","anydesk","teamviewer","rustdesk","ultraviewer","remotepc"}};
 // web / DNS list (stricter, so shops and normal sites do not create false alarms)
 static readonly string[][] WB={
  new[]{"Cheat Menus & Sellers","3","keyser","susano","redengine","red-engine","red engine","eulen","fivem cheat","fivem hack","fivemhack","lua executor","luaexecutor","aimbot","triggerbot","wallhack"},
  new[]{"Spoofers & Unban","3","hwid spoofer","hwid-spoofer","spoofer","fivem unban","unban fivem","fivem bypass","ban bypass"},
  new[]{"Injectors & Mappers","3","dll injector","kdmapper","xenos injector","extreme injector"},
  new[]{"Security Blockers","3","defender remover","defendercontrol","disable defender","windows defender remover"},
  new[]{"CS:GO / CS2 Cheats","3","aimware","neverlose","gamesense.pub","nixware","onetap.su","skeet.cc","fatality.win"},
  new[]{"Roblox Executors","3","synapsex","jjsploit","scriptware","sirhurt","fluxteam","roblox executor","roblox exploit","robloxexecutor","macsploit","wave executor","krnl."},
  new[]{"Cheat Forums","2","unknowncheats","elitepvpers","mpgh.net","cheatengine.org","guidedhacking"}};
 static readonly string[] STG={"Processes","Recent & Prefetch","Drives & files","Recycle Bin","Deletion history","Windows event logs","Security settings","Browser & DNS","Execution traces"};
 static readonly string[] STS={"Running programs","Opened & executed items","File names + cheat signatures","Recently deleted","Windows deletion journal","Cleared logs, drivers, Defender","Defender, test-signing, drivers","History, DNS and browser memory","Registry run history"};
 static readonly string[] AR={".zip",".rar",".7z",".iso"};
 static readonly string[] EX={".exe",".dll",".sys",".bat",".cmd",".ps1",".asi",".lua",".scr",".msi",".zip",".rar",".7z",".iso"};
 static readonly string[] SK={"\\extensions\\","\\node_modules\\","\\site-packages\\","\\user data\\","\\winsxs\\","\\windowsapps\\","\\.vscode\\","\\servicing\\"};
 static bool Ar(string n){return Array.IndexOf(AR,Path.GetExtension(n??"").ToLower())>=0;}
 static string[] M(string n){n=(n??"").ToLower();foreach(var r in R)for(int i=2;i<r.Length;i++)if(n.Contains(r[i]))return r;return null;}
 static string[] MW(string n){n=(n??"").ToLower();foreach(var r in WB)for(int i=2;i<r.Length;i++)if(n.Contains(r[i]))return r;return null;}
 static bool Ex(string n){return Array.IndexOf(EX,Path.GetExtension(n??"").ToLower())>=0;}
 static bool Sk(string p){p=(p??"").ToLower();foreach(var s in SK)if(p.Contains(s))return true;return false;}
 [DllImport("user32.dll")]static extern bool ReleaseCapture();
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int m,int w,int l);
 // ---- state ----
 Bitmap bg,glow;static void Log(Exception ex){lastErr=ex.GetType().Name+": "+ex.Message;try{File.AppendAllText(Path.Combine(Path.GetTempPath(),"rzcore-error.log"),DateTime.Now+" "+ex+"\r\n");}catch{}}
 Image logo;List<string[]> found=new List<string[]>();HashSet<string> seen=new HashSet<string>();
 TextBox pin=new TextBox();volatile bool done=false;volatile int sendSt=0;string sendMsg="",skipped="";static string lastErr="";
 int state=0,tick=0,stg=-1,hov=0;bool agree=false,sent=false;float pct=0,target=0;string status="Enter the session PIN to begin",pinV="";DateTime lastPing=DateTime.MinValue;
 System.Windows.Forms.Timer tm=new System.Windows.Forms.Timer();
 Dictionary<string,Font> fc=new Dictionary<string,Font>();
 static readonly Color RED=Color.FromArgb(245,43,53),RED2=Color.FromArgb(255,115,107),OK=Color.FromArgb(61,220,151),MU=Color.FromArgb(150,150,156),MU2=Color.FromArgb(96,96,102);
 readonly Rectangle rGo=new Rectangle(28,512,540,44),rRetry=new Rectangle(270,430,170,44),rAgree=new Rectangle(56,296,484,190),rClose=new Rectangle(W-54,20,30,30),rMin=new Rectangle(W-90,20,30,30);
 [STAThread]static void Main(){try{if(!new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator)){Process.Start(new ProcessStartInfo(Application.ExecutablePath){Verb="runas",UseShellExecute=true});return;}}catch{return;}
  Application.EnableVisualStyles();Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
  Application.ThreadException+=(a,e)=>{Log(e.Exception);};AppDomain.CurrentDomain.UnhandledException+=(a,e)=>{Log(e.ExceptionObject as Exception??new Exception("unhandled"));};
  Application.Run(new RzCore());}
 Font F(float s,FontStyle st,bool mono){string k=s+"|"+st+"|"+mono;Font f;if(!fc.TryGetValue(k,out f))fc[k]=f=new Font(mono?"Consolas":"Segoe UI",s,st);return f;}
 static GraphicsPath RR(float x,float y,float w,float h,float r){var p=new GraphicsPath();p.AddArc(x,y,r*2,r*2,180,90);p.AddArc(x+w-r*2,y,r*2,r*2,270,90);p.AddArc(x+w-r*2,y+h-r*2,r*2,r*2,0,90);p.AddArc(x,y+h-r*2,r*2,r*2,90,90);p.CloseFigure();return p;}
 static Color SC(int s){return s>=3?Color.FromArgb(255,75,87):s==2?Color.FromArgb(255,138,61):Color.FromArgb(242,201,76);}
 static string SL(int s){return s>=3?"SEVERE":s==2?"HIGH":"MEDIUM";}
 static Color RC(string r){return r=="Severe"?SC(3):r=="High"?SC(2):r=="Medium"?SC(1):OK;}
 RzCore(){
  Text="RzCore PC-Checker";FormBorderStyle=FormBorderStyle.None;ClientSize=new Size(W,H);StartPosition=FormStartPosition.CenterScreen;DoubleBuffered=true;BackColor=Color.Black;ForeColor=Color.White;Font=new Font("Segoe UI",9.5f);
  try{var st=Assembly.GetExecutingAssembly().GetManifestResourceStream("logo.png");if(st!=null)logo=Image.FromStream(st);}catch{}
  try{var asm=Assembly.GetExecutingAssembly();lt=Image.FromStream(asm.GetManifestResourceStream("lt.png"));lp=Image.FromStream(asm.GetManifestResourceStream("lp.png"));}catch{lt=null;lp=null;}
  SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);
  Region=new Region(RR(0,0,W,H,22));BuildCache();
  pin.SetBounds(56,230,240,44);pin.MaxLength=6;pin.BorderStyle=BorderStyle.None;pin.TextAlign=HorizontalAlignment.Center;pin.Font=new Font("Consolas",20f,FontStyle.Bold);pin.BackColor=Color.FromArgb(16,16,18);pin.ForeColor=Color.White;
  pin.KeyPress+=(a,e)=>{if(!char.IsControl(e.KeyChar)&&!char.IsDigit(e.KeyChar))e.Handled=true;};
  pin.TextChanged+=(a,e)=>{if(pinErr.Length>0){pinErr="";status="Enter the session PIN to begin";Invalidate();}};Controls.Add(pin);
  SetState(0);
  tm.Interval=40;tm.Tick+=(a,b)=>{tick++;bool mv=Math.Abs(target-pct)>=0.05f||state==2;pct+=(target-pct)*0.12f;if(Math.Abs(target-pct)<0.1f)pct=target;if(state==1&&done){done=false;target=100;stg=STG.Length;SetState(2);StartSend();}if(state==2){status=sendSt==0?"Sending the report to the dashboard...":sendSt==1?"Report sent to the dashboard":"Could not send the report";}if(state==1||mv)Invalidate();};tm.Start();}


 // server URL: 1) rzcore-server.txt next to the exe  2) URL baked into the exe by /dl/<pin>  3) SERVER above
 static string SRV=GetSrv();
 static string GetSrv(){try{string f=Application.ExecutablePath,c=Path.Combine(Path.GetDirectoryName(f),"rzcore-server.txt");
   if(File.Exists(c)){var u=File.ReadAllText(c).Trim();if(u.StartsWith("http"))return u.TrimEnd('/');}
   using(var fs=new FileStream(f,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)){int n=(int)Math.Min(500,fs.Length);fs.Seek(-n,SeekOrigin.End);var b=new byte[n];fs.Read(b,0,n);string t=Encoding.ASCII.GetString(b);int i=t.LastIndexOf("RZSRV=");
    if(i>=0){int e=t.IndexOf('\n',i);if(e>i){var u=t.Substring(i+6,e-i-6).Trim();if(u.StartsWith("http"))return u.TrimEnd('/');}}}}catch{}return SERVER;}
 // ---- logo animation: black logo -> red "water" rises -> magnifier leaves, inspects the logo, comes back -> drains and restarts ----
 Image lt,lp;string pinErr="";bool checking=false;
 static ImageAttributes IA(float a,float or,float og,float ob,bool keep){float k=keep?1f:0f;var m=new ColorMatrix(new float[][]{new float[]{k,0,0,0,0},new float[]{0,k,0,0,0},new float[]{0,0,k,0,0},new float[]{0,0,0,a,0},new float[]{or,og,ob,0,1}});var ia=new ImageAttributes();ia.SetColorMatrix(m);return ia;}
 ImageAttributes aBlk=IA(1f,.07f,.07f,.08f,false),aHalf=IA(.5f,0,0,0,true),aLit=IA(1f,.2f,.14f,.14f,true);
 static float Sm(float x){x=Math.Max(0f,Math.Min(1f,x));return x*x*(3-2*x);}
 static readonly float[][] KP={new[]{0f,0f},new[]{-110f,115f},new[]{-250f,135f},new[]{-345f,115f},new[]{-215f,80f},new[]{-80f,40f},new[]{0f,0f}};
 static void DL(Graphics g,Image im,ImageAttributes ia){g.DrawImage(im,new Rectangle(0,0,560,412),0,0,560,412,GraphicsUnit.Pixel,ia);}
 void LogoAnim(Graphics g,float X0,float Y0,float s){
  int cyc=330;float t=(tick%cyc)/(float)cyc,f,p=-1f;
  f=t<.06f?0f:t<.44f?Sm((t-.06f)/.38f):t<.92f?1f:1f-Sm((t-.92f)/.08f);
  if(t>.52f&&t<.86f)p=(t-.52f)/.34f;
  float ox=0,oy=0,rot=0,sc=1f;
  if(p>=0){float sg=p*6f;int i=Math.Min(5,(int)sg);float fr=sg-i;fr=(fr+Sm(fr))/2f;ox=KP[i][0]+(KP[i+1][0]-KP[i][0])*fr;oy=KP[i][1]+(KP[i+1][1]-KP[i][1])*fr;rot=7f*(float)Math.Sin(p*Math.PI*6);sc=1f+.12f*(float)Math.Sin(p*Math.PI);}
  var gs=g.Save();g.TranslateTransform(X0,Y0);g.ScaleTransform(s,s);g.InterpolationMode=InterpolationMode.HighQualityBicubic;
  DL(g,lt,aBlk);DL(g,lp,aBlk);
  for(int w=0;w<2;w++){if(f<=0f)break;float sy=(w==0?418f:430f)-460f*f,ph=tick*(w==0?.13f:.19f)+w*2f,am=w==0?9f:6f;var pts=new List<PointF>();
   for(int x=-10;x<=570;x+=10)pts.Add(new PointF(x,sy+am*(float)Math.Sin(x*.028f+ph)));pts.Add(new PointF(570,480));pts.Add(new PointF(-10,480));
   using(var gp=new GraphicsPath()){gp.AddPolygon(pts.ToArray());g.SetClip(gp);ImageAttributes ia=w==0?aHalf:null;DL(g,lt,ia);if(p<0)DL(g,lp,ia);g.ResetClip();}}
  if(p>=0){float lx=442.7f+ox,ly=73f+oy,lr=57f*sc;
   var g2=g.Save();
   using(var gp=new GraphicsPath()){gp.AddEllipse(lx-lr,ly-lr,lr*2,lr*2);g.SetClip(gp);using(var br=new SolidBrush(Color.FromArgb(215,6,4,6)))g.FillEllipse(br,lx-lr,ly-lr,lr*2,lr*2);g.TranslateTransform(lx,ly);g.ScaleTransform(1.45f,1.45f);g.TranslateTransform(-lx,-ly);DL(g,lt,aLit);}
   g.Restore(g2);
   g.TranslateTransform(ox,oy);g.TranslateTransform(442.7f,73f);g.RotateTransform(rot);g.ScaleTransform(sc,sc);g.TranslateTransform(-442.7f,-73f);DL(g,lp,null);}
  g.Restore(gs);}
 void SetState(int s){state=s;pin.Visible=(s==0);Invalidate();}
 // ---------------- painting ----------------
 void Card(Graphics g,float x,float y,float w,float h,float r,int a){using(var p=RR(x,y,w,h,r)){using(var br=new LinearGradientBrush(new RectangleF(x,y,w,h),Color.FromArgb(a+10,255,255,255),Color.FromArgb(Math.Max(4,a-8),255,255,255),70f))g.FillPath(br,p);using(var pn=new Pen(Color.FromArgb(38,255,255,255)))g.DrawPath(pn,p);}}
 void T(Graphics g,string t,Font f,Color c,float x,float y){using(var b=new SolidBrush(c))g.DrawString(t,f,b,x,y);}
 void TR(Graphics g,string t,Font f,Color c,RectangleF r,StringAlignment h,StringAlignment v){using(var b=new SolidBrush(c))g.DrawString(t,f,b,r,new StringFormat{Alignment=h,LineAlignment=v,Trimming=StringTrimming.EllipsisCharacter});}
 void Btn(Graphics g,Rectangle r,string t,bool primary,bool hover,bool enabled){
  using(var p=RR(r.X,r.Y,r.Width,r.Height,12)){
   if(primary&&enabled){using(var br=new LinearGradientBrush(r,hover?Color.FromArgb(255,92,98):Color.FromArgb(255,70,80),Color.FromArgb(215,25,40),90f))g.FillPath(br,p);}
   else using(var br=new SolidBrush(Color.FromArgb(hover?34:20,255,255,255)))g.FillPath(br,p);
   using(var pn=new Pen(primary&&enabled?Color.FromArgb(120,255,150,150):Color.FromArgb(44,255,255,255)))g.DrawPath(pn,p);}
  TR(g,t,F(10f,FontStyle.Bold,false),enabled?Color.White:MU2,r,StringAlignment.Center,StringAlignment.Center);}
 void Chip(Graphics g,string t,Color c,float x,float y,float w){using(var p=RR(x,y,w,26,13))using(var br=new SolidBrush(Color.FromArgb(34,c)))using(var pn=new Pen(Color.FromArgb(150,c))){g.FillPath(br,p);g.DrawPath(pn,p);}TR(g,t,F(8f,FontStyle.Bold,false),c,new RectangleF(x,y,w,26),StringAlignment.Center,StringAlignment.Center);}
 void Orb(Graphics g,float cx,float cy,float sc,float spd){
  float[] rd={24,42,62},sw={105,150,95},sp={3.4f,-2.2f,1.5f};
  if(glow!=null){float sz=300*sc/1.4f;g.DrawImage(glow,cx-sz/2,cy-sz/2,sz,sz);}
  for(int i=0;i<3;i++){float rad=rd[i]*sc,st=tick*sp[i]*spd+i*80;
   using(var gl=new Pen(Color.FromArgb(40,RED),9*sc))using(var pn=new Pen(Color.FromArgb(235,i==1?RED2:RED),3.2f*sc)){gl.StartCap=gl.EndCap=pn.StartCap=pn.EndCap=LineCap.Round;g.DrawArc(gl,cx-rad,cy-rad,rad*2,rad*2,st,sw[i]);g.DrawArc(pn,cx-rad,cy-rad,rad*2,rad*2,st,sw[i]);}}
  using(var pn=new Pen(Color.FromArgb(255,190,170),2f*sc))g.DrawEllipse(pn,cx-5*sc,cy-5*sc,10*sc,10*sc);}
 void BuildCache(){
  bg=new Bitmap(W,H);using(var g=Graphics.FromImage(bg)){g.SmoothingMode=SmoothingMode.AntiAlias;
   g.FillRectangle(Brushes.Black,0,0,W,H);
   using(var pn=new Pen(Color.FromArgb(9,255,255,255))){for(int x=0;x<W;x+=60)g.DrawLine(pn,x,0,x,H);for(int y=0;y<H;y+=60)g.DrawLine(pn,0,y,W,y);}
   using(var p=new GraphicsPath()){p.AddEllipse(W-520,-260,760,560);using(var pg=new PathGradientBrush(p)){pg.CenterColor=Color.FromArgb(90,200,20,32);pg.SurroundColors=new[]{Color.FromArgb(0,0,0,0)};g.FillPath(pg,p);}}
   using(var p=new GraphicsPath()){p.AddEllipse(-260,H-300,620,420);using(var pg=new PathGradientBrush(p)){pg.CenterColor=Color.FromArgb(40,200,20,32);pg.SurroundColors=new[]{Color.FromArgb(0,0,0,0)};g.FillPath(pg,p);}}
   using(var p=RR(0.5f,0.5f,W-1,H-1,22))using(var pn=new Pen(Color.FromArgb(50,255,255,255)))g.DrawPath(pn,p);}
  glow=new Bitmap(256,256);using(var g=Graphics.FromImage(glow)){g.SmoothingMode=SmoothingMode.AntiAlias;using(var p=new GraphicsPath()){p.AddEllipse(0,0,255,255);using(var pg=new PathGradientBrush(p)){pg.CenterColor=Color.FromArgb(70,245,43,53);pg.SurroundColors=new[]{Color.FromArgb(0,245,43,53)};g.FillPath(pg,p);}}}}
 protected override void OnPaintBackground(PaintEventArgs e){if(bg!=null)e.Graphics.DrawImageUnscaled(bg,0,0);}
 int Cnt(int s){int n=0;lock(found){foreach(var f in found)if(int.Parse(f[0])==s)n++;}return n;}
 protected override void OnPaint(PaintEventArgs e){try{Paint2(e);}catch(Exception ex){Log(ex);}}
 void Paint2(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
  // header
  float tx=30;if(logo!=null){g.InterpolationMode=InterpolationMode.HighQualityBicubic;using(var p=RR(28,16,44,44,12)){g.SetClip(p);g.DrawImage(logo,28,16,44,44);g.ResetClip();using(var pn=new Pen(Color.FromArgb(60,255,255,255)))g.DrawPath(pn,p);}tx=84;}
  T(g,"RzCore",F(14f,FontStyle.Bold,false),Color.White,tx,17);T(g,"FORENSIC PC-CHECKER",F(7f,FontStyle.Regular,true),RED2,tx+1,41);
  var pr=new RectangleF(W-260,22,150,28);
  using(var p=RR(pr.X,pr.Y,pr.Width,pr.Height,14))using(var br=new SolidBrush(Color.FromArgb(18,255,255,255)))using(var pn=new Pen(Color.FromArgb(40,255,255,255))){g.FillPath(br,p);g.DrawPath(pn,p);}
  Color dc=state==1?RED:state==2?OK:MU;using(var br=new SolidBrush(Color.FromArgb(state==1&&(tick/15)%2==0?110:255,dc)))g.FillEllipse(br,pr.X+14,pr.Y+10.5f,7,7);
  T(g,state==1?"LIVE ANALYSIS":state==2?"SCAN COMPLETE":"READY",F(7.5f,FontStyle.Bold,true),Color.FromArgb(190,255,255,255),pr.X+28,pr.Y+8);
  using(var pn=new Pen(hov==5?Color.White:MU,1.6f)){g.DrawLine(pn,rMin.X+8,rMin.Y+19,rMin.X+22,rMin.Y+19);}
  using(var pn=new Pen(hov==4?RED2:MU,1.6f)){g.DrawLine(pn,rClose.X+8,rClose.Y+8,rClose.X+22,rClose.Y+22);g.DrawLine(pn,rClose.X+22,rClose.Y+8,rClose.X+8,rClose.Y+22);}
  using(var hl=new LinearGradientBrush(new RectangleF(0,74,W,1),Color.Transparent,Color.Transparent,0f)){var bl=new ColorBlend(3);bl.Colors=new[]{Color.FromArgb(0,255,70,80),Color.FromArgb(160,255,70,80),Color.FromArgb(0,255,70,80)};bl.Positions=new[]{0f,.5f,1f};hl.InterpolationColors=bl;g.FillRectangle(hl,0,74,W,1);}
  if(state==0)PaintStart(g);else if(state==1)PaintScan(g);else PaintDone(g);
  // bottom bar
  T(g,"SYSTEM ANALYSIS",F(7.5f,FontStyle.Bold,true),MU2,28,566);
  T(g,status,F(9.5f,FontStyle.Bold,false),Color.FromArgb(225,225,228),28,582);
  if(state>0)TR(g,(int)Math.Round(pct)+"%",F(11f,FontStyle.Bold,true),RED2,new RectangleF(W-148,576,120,22),StringAlignment.Far,StringAlignment.Center);
  if(lastErr.Length>0)TR(g,"log: "+lastErr,F(7.5f,FontStyle.Regular,true),Color.FromArgb(200,255,120,120),new RectangleF(W-520,562,492,14),StringAlignment.Far,StringAlignment.Center);
  using(var p=RR(28,610,W-56,8,4))using(var br=new SolidBrush(Color.FromArgb(24,255,255,255)))g.FillPath(br,p);
  float w=(W-56)*pct/100f;if(w>=8)using(var p=RR(28,610,w,8,4))using(var br=new LinearGradientBrush(new RectangleF(28,610,w,8),Color.FromArgb(200,0,25),Color.FromArgb(255,150,145),0f))g.FillPath(br,p);}
 void PaintStart(Graphics g){
  Card(g,28,92,540,408,22,14);
  T(g,"AUTHENTICATION",F(7.5f,FontStyle.Bold,true),RED2,56,116);
  T(g,"Start your scan",F(22f,FontStyle.Bold,false),Color.White,54,134);
  T(g,"Enter the session PIN from your reviewer, accept the checks and start.",F(9.5f,FontStyle.Regular,false),MU,56,172);
  T(g,"SESSION PIN",F(7.5f,FontStyle.Bold,true),MU2,56,208);
  using(var p=RR(pin.Left-3,pin.Top-3,pin.Width+6,pin.Height+6,12))using(var br=new SolidBrush(Color.FromArgb(16,16,18)))using(var pn=new Pen((pin.Focused||pinErr.Length>0)?Color.FromArgb(200,RED):Color.FromArgb(50,255,255,255))){g.FillPath(br,p);g.DrawPath(pn,p);}
  if(pinErr.Length>0)T(g,pinErr,F(10f,FontStyle.Bold,false),RED2,312,243);
  // consent
  var cr=rAgree;using(var p=RR(cr.X,cr.Y,cr.Width,cr.Height,16))using(var br=new SolidBrush(Color.FromArgb(hov==6?16:9,255,255,255)))using(var pn=new Pen(agree?Color.FromArgb(150,RED):Color.FromArgb(34,255,255,255))){g.FillPath(br,p);g.DrawPath(pn,p);}
  using(var p=RR(cr.X+16,cr.Y+16,22,22,6)){using(var br=new SolidBrush(agree?RED:Color.FromArgb(20,255,255,255)))g.FillPath(br,p);using(var pn=new Pen(agree?RED2:Color.FromArgb(90,255,255,255)))g.DrawPath(pn,p);}
  if(agree)using(var pn=new Pen(Color.White,2.2f)){g.DrawLines(pn,new[]{new PointF(cr.X+21,cr.Y+27),new PointF(cr.X+26,cr.Y+32),new PointF(cr.X+34,cr.Y+21)});}
  T(g,"I accept this scan of my PC. RzCore will read:",F(10f,FontStyle.Bold,false),Color.White,cr.X+50,cr.Y+17);
  string[] b={"File names, deleted files and cheat signatures in my user folders","Windows event logs and security settings (Defender, drivers)","Processes, Recent, Prefetch, execution traces and install dates","Browser history, DNS and browser memory, matched to cheat names"};
  for(int i=0;i<b.Length;i++){using(var br=new SolidBrush(RED2))g.FillEllipse(br,cr.X+54,cr.Y+51+i*21,5,5);T(g,b[i],F(9f,FontStyle.Regular,false),Color.FromArgb(205,205,210),cr.X+68,cr.Y+44+i*21);}
  TR(g,"When the scan ends, only suspicious matches are sent automatically to my reviewer's dashboard. File contents and my full history never leave my PC.",F(8.5f,FontStyle.Regular,false),MU,new RectangleF(cr.X+16,cr.Y+134,cr.Width-32,48),StringAlignment.Near,StringAlignment.Near);
  Btn(g,rGo,"Start scan",true,hov==1,true);
  // right card
  Card(g,588,92,284,408,22,14);T(g,"WHAT WILL BE CHECKED",F(7.5f,FontStyle.Bold,true),RED2,614,116);
  for(int i=0;i<STG.Length;i++){float y=136+i*40;using(var p=RR(612,y,34,34,10))using(var br=new SolidBrush(Color.FromArgb(14,255,255,255)))using(var pn=new Pen(Color.FromArgb(34,255,255,255))){g.FillPath(br,p);g.DrawPath(pn,p);}
   using(var br=new SolidBrush(RED2))g.FillEllipse(br,625.5f,y+13.5f,7,7);T(g,STG[i],F(10f,FontStyle.Bold,false),Color.White,658,y-1);T(g,STS[i],F(8f,FontStyle.Regular,false),MU,658,y+17);}
  TR(g,"Consent based - matches only, sent to the dashboard",F(8f,FontStyle.Regular,true),MU2,new RectangleF(588,512,284,44),StringAlignment.Center,StringAlignment.Center);}
 void PaintScan(Graphics g){
  Card(g,28,92,540,408,22,14);if(lt!=null&&lp!=null)LogoAnim(g,133,146,.59f);else Orb(g,298,262,1.75f,1f);
  TR(g,"Scanning your PC",F(11f,FontStyle.Bold,false),Color.FromArgb(255,150,155),new RectangleF(28,408,540,24),StringAlignment.Center,StringAlignment.Center);
  TR(g,"The report goes to your reviewer's dashboard when the scan ends.",F(8.5f,FontStyle.Regular,false),MU,new RectangleF(28,436,540,20),StringAlignment.Center,StringAlignment.Center);
  Card(g,588,92,284,408,22,14);T(g,"SCAN STAGES",F(7.5f,FontStyle.Bold,true),RED2,614,116);
  for(int i=0;i<STG.Length;i++){float y=136+i*40;bool done=i<stg,act=i==stg;
   using(var p=RR(612,y,34,34,10))using(var br=new SolidBrush(done?Color.FromArgb(30,OK):act?Color.FromArgb(36,RED):Color.FromArgb(12,255,255,255)))using(var pn=new Pen(done?Color.FromArgb(120,OK):act?Color.FromArgb(160,RED):Color.FromArgb(30,255,255,255))){g.FillPath(br,p);g.DrawPath(pn,p);}
   if(done)using(var pn=new Pen(OK,2f))g.DrawLines(pn,new[]{new PointF(621,y+17),new PointF(627,y+23),new PointF(637,y+11)});
   else if(act){using(var pn=new Pen(RED2,2f)){pn.StartCap=pn.EndCap=LineCap.Round;g.DrawArc(pn,621,y+9,16,16,tick*8,260);}}
   else using(var br=new SolidBrush(Color.FromArgb(70,255,255,255)))g.FillEllipse(br,626.5f,y+13.5f,7,7);
   T(g,STG[i],F(10f,FontStyle.Bold,false),done||act?Color.White:MU2,658,y-1);T(g,done?"Done":act?"Scanning...":"Waiting",F(8f,FontStyle.Regular,false),done?OK:act?RED2:MU2,658,y+17);}}
void PaintDone(Graphics g){
  Card(g,28,92,844,408,22,14);
  float cx=W/2f,cy=235;bool ok=sendSt==1,bad=sendSt==2;Color c=bad?RED:ok?OK:RED2;
  using(var br=new SolidBrush(Color.FromArgb(26,c)))g.FillEllipse(br,cx-64,cy-64,128,128);
  using(var pn=new Pen(Color.FromArgb(140,c),2f))g.DrawEllipse(pn,cx-64,cy-64,128,128);
  using(var pn=new Pen(c,4f)){pn.StartCap=pn.EndCap=LineCap.Round;pn.LineJoin=LineJoin.Round;
   if(ok)g.DrawLines(pn,new[]{new PointF(cx-24,cy+2),new PointF(cx-7,cy+20),new PointF(cx+26,cy-18)});
   else if(bad){g.DrawLine(pn,cx-20,cy-20,cx+20,cy+20);g.DrawLine(pn,cx+20,cy-20,cx-20,cy+20);}
   else g.DrawArc(pn,cx-34,cy-34,68,68,tick*9,250);}
  TR(g,ok?"Scan complete":bad?"Could not send the report":"Sending the report...",F(24f,FontStyle.Bold,false),Color.White,new RectangleF(28,322,844,40),StringAlignment.Center,StringAlignment.Center);
  string sub=ok?"The results are now in your reviewer's dashboard.\nYou can close this window.":bad?(sendMsg.Length>140?sendMsg.Substring(0,140):sendMsg)+"\nCheck your internet connection, then try again.":"Please keep this window open for a moment.";
  TR(g,sub,F(10f,FontStyle.Regular,false),MU,new RectangleF(120,364,660,56),StringAlignment.Center,StringAlignment.Near);
  if(bad)Btn(g,rRetry,"Retry",true,hov==2,true);
  Btn(g,CloseR(),"Close",!bad&&ok,hov==3,true);}
 // ---------------- input ----------------
 Rectangle CloseR(){return sendSt==2?new Rectangle(460,430,170,44):new Rectangle(365,430,170,44);}
 int Hit(Point p){
  if(rClose.Contains(p))return 4;if(rMin.Contains(p))return 5;
  if(state==0){if(rGo.Contains(p))return 1;if(rAgree.Contains(p))return 6;}
  if(state==2){if(sendSt==2&&rRetry.Contains(p))return 2;if(CloseR().Contains(p))return 3;}
  return 0;}
 protected override void OnMouseMove(MouseEventArgs e){int h=Hit(e.Location);if(h!=hov){hov=h;Cursor=h>0?Cursors.Hand:Cursors.Default;Invalidate();}}
 protected override void OnMouseDown(MouseEventArgs e){int h=Hit(e.Location);
  if(h==4){Close();return;}if(h==5){WindowState=FormWindowState.Minimized;return;}
  if(h==1){Start();return;}if(h==6){agree=!agree;Invalidate();return;}if(h==2){StartSend();return;}if(h==3){Close();return;}
  ReleaseCapture();SendMessage(Handle,0xA1,2,0);}
 // ---------------- scanning ----------------
 static string J(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r"," ").Replace("\n"," ")+"\"";}
 static void Post(string path,string json){ServicePointManager.SecurityProtocol=(SecurityProtocolType)3072;
  var r=(HttpWebRequest)WebRequest.Create(SRV+path);r.Method="POST";r.ContentType="application/json";r.Timeout=15000;var d=Encoding.UTF8.GetBytes(json);
  using(var s=r.GetRequestStream())s.Write(d,0,d.Length);r.GetResponse().Close();}
 void UI(string s,int p,int st){try{BeginInvoke((Action)(()=>{status=s;target=p;if(st>=0)stg=st;}));}catch{}
  if((DateTime.Now-lastPing).TotalSeconds>=2){lastPing=DateTime.Now;string pv=pinV;ThreadPool.QueueUserWorkItem(x=>{try{Post("/api/progress","{\"pin\":"+J(pv)+",\"pct\":"+p+",\"status\":"+J(s)+"}");}catch{}});}}
 void Add(int sev,string cat,string src,string t){lock(found){if(found.Count<MAXF&&seen.Add(src+t))found.Add(new[]{sev.ToString(),cat,src,t});}}
 static readonly string[] TOK={"cheat","cheats","hack","hacks","hacked","hacker","hwid","exploit","exploits","executor","crack","cracked","loader","unban","bypass","spoof","inject","injected","aimbot"};
 static bool TokHit(string n){string b=Path.GetFileNameWithoutExtension(n??"").ToLower();foreach(var t in System.Text.RegularExpressions.Regex.Split(b,"[^a-z0-9]+")){if(t.Length<3)continue;if(Array.IndexOf(TOK,t)>=0)return true;if(t.Length>5&&(t.EndsWith("cheat")||t.EndsWith("cheats")))return true;}return false;}
 static bool UserPath(string f){f=(f??"").ToLower();return f.Contains("\\users\\")||f.Contains("\\temp\\")||f.Contains("\\downloads\\")||f.Contains("\\programdata\\");}
 void Chk(string name,string full,string src,bool needExec,int adj){var r=M(name);
  if(r==null){if(needExec&&Ex(name)&&!Sk(full)){if(TokHit(name)){var v1=Ver(full);if(v1!=null)Add(1,"Suspicious Files (check manually)",src,v1);}else if(Path.GetExtension(name).ToLower()==".sys"&&UserPath(full))Add(2,"Suspicious Files (check manually)",src,full+"  [driver file in a user folder]");}return;}if(needExec&&!Ex(name))return;if(Sk(full))return;string vd=Ver(full);if(vd==null)return;Add(Math.Max(1,int.Parse(r[1])+adj),r[0],src,vd);}
 void Scan(string name,string dir,string pat,string src,int adj,int p,int st){UI("Analyzing "+name+" artifacts",p,st);try{foreach(var f in Directory.GetFiles(dir,pat)){string n=Path.GetFileName(f),fu=M(n)==null?"":src=="Recent"?Lnk(f):src=="Prefetch"?Pf(f):"";Chk(n,fu.Length>0?fu:n,src,false,adj);}}catch{}Thread.Sleep(400);}
 void Walk(){
  var drives=new List<DriveInfo>();long used=1,seenB=0,n=0;
  foreach(var d in DriveInfo.GetDrives())try{if((d.DriveType==DriveType.Fixed||d.DriveType==DriveType.Removable)&&d.IsReady){drives.Add(d);used+=d.TotalSize-d.TotalFreeSpace;}}catch{}
  foreach(var d in drives){var st=new Stack<string>();st.Push(d.RootDirectory.FullName);
   while(st.Count>0){var dir=st.Pop();
    try{var di=new DirectoryInfo(dir);
     foreach(var f in di.EnumerateFiles()){seenB+=f.Length;if(++n%3000==0)UI("Scanning "+d.Name+" ("+n.ToString("N0")+" files)",8+(int)(72*Math.Min(1.0,(double)seenB/used)),2);if(Ex(f.Name))Chk(f.Name,f.FullName,"File",true,Ar(f.Name)?-1:0);}
     foreach(var s in di.EnumerateDirectories()){if((s.Attributes&FileAttributes.ReparsePoint)!=0)continue;if(s.Name=="$Recycle.Bin"||s.Name=="System Volume Information"||s.Name=="WinSxS")continue;Chk(s.Name,s.FullName,"Folder",false,-1);st.Push(s.FullName);}
    }catch{}}}}
 void Bin(){UI("Analyzing deleted files (Recycle Bin)",84,3);
  foreach(var d in DriveInfo.GetDrives())try{if(d.DriveType!=DriveType.Fixed)continue;
   foreach(var u in Directory.GetDirectories(Path.Combine(d.RootDirectory.FullName,"$Recycle.Bin")))try{foreach(var f in Directory.GetFiles(u,"$I*")){
    var b=File.ReadAllBytes(f);if(b.Length<30)continue;string path;
    if(BitConverter.ToInt64(b,0)==2){int l=BitConverter.ToInt32(b,24);if(l<=0||28+l*2>b.Length)continue;path=Encoding.Unicode.GetString(b,28,l*2);}else path=Encoding.Unicode.GetString(b,24,Math.Min(520,b.Length-24));
    path=path.TrimEnd('\0');string when="";try{when=" ("+DateTime.FromFileTimeUtc(BitConverter.ToInt64(b,16)).ToString("yyyy-MM-dd HH:mm")+" UTC)";}catch{}
    var r=M(Path.GetFileName(path));if(r!=null&&!Sk(path))Add(int.Parse(r[1]),r[0],"Deleted",path+when);}}catch{}}catch{}}
 void Usn(){
  foreach(var d in DriveInfo.GetDrives()){try{if(d.DriveType!=DriveType.Fixed||!d.IsReady)continue;string dl=d.Name.Substring(0,2);UI("Analyzing deletion history "+dl,90,4);
   try{var qp=Process.Start(new ProcessStartInfo("fsutil","usn queryjournal "+dl){UseShellExecute=false,RedirectStandardOutput=true,CreateNoWindow=true});qp.StandardOutput.ReadToEnd();qp.WaitForExit(8000);if(qp.HasExited&&qp.ExitCode!=0)Add(2,"Anti-Forensics","Security","USN journal is not active on "+dl+" (deletion history unavailable or deleted)");}catch{}
   var ps=new ProcessStartInfo("fsutil","usn readjournal "+dl+" csv"){UseShellExecute=false,RedirectStandardOutput=true,CreateNoWindow=true};
   using(var p=Process.Start(ps)){var sw=Stopwatch.StartNew();string line;
    while(sw.Elapsed.TotalSeconds<60&&(line=p.StandardOutput.ReadLine())!=null){
     if(line.IndexOf("delete",StringComparison.OrdinalIgnoreCase)<0||M(line)==null)continue;
     foreach(var c in line.Split(',')){var nm=c.Trim('"',' ');if(M(nm)!=null&&nm.Length>2){Chk(nm,nm,"Deleted",false,0);break;}}}
    try{if(!p.HasExited)p.Kill();}catch{}}
  }catch{}}}
 // ---- browser history (Chrome/Edge/Brave/Opera/Firefox): matches only, never the full history ----
 static readonly Encoding L1=Encoding.GetEncoding(28591);
 static byte[] ReadShared(string f){using(var fs=new FileStream(f,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){var ms=new MemoryStream();fs.CopyTo(ms);return ms.ToArray();}}
 static bool Pr(char c){return c>0x20&&c<0x7f;}
 void BinScan(string f,string br){
  if(!File.Exists(f))return;byte[] b;try{if(new FileInfo(f).Length>300000000)return;b=ReadShared(f);}catch{return;}
  string s=L1.GetString(b),lo=s.ToLowerInvariant();int hits=0;
  foreach(var r in WB)for(int k=2;k<r.Length&&hits<60;k++){int idx=0;
   while(hits<60&&(idx=lo.IndexOf(r[k],idx,StringComparison.Ordinal))>=0){
    int st=-1;for(int i=idx;i>=Math.Max(0,idx-300)&&i+4<=lo.Length;i--)if(lo[i]=='h'&&string.CompareOrdinal(lo,i,"http",0,4)==0){bool okp=true;for(int j=i;j<idx;j++)if(!Pr(lo[j])||lo[j]=='"'){okp=false;break;}if(okp)st=i;break;}
    string t;
    if(st>=0){int e=idx;while(e<lo.Length&&e-st<220&&Pr(s[e])&&s[e]!='"')e++;t=s.Substring(st,e-st);}
    else{int a=idx;while(a>0&&idx-a<50&&s[a-1]>=0x20&&s[a-1]<0x7f)a--;int e=idx;while(e<s.Length&&e-a<110&&s[e]>=0x20&&s[e]<0x7f)e++;t=s.Substring(a,e-a).Trim();}
    if(t.Length>3){Add(int.Parse(r[1]),r[0],"Browser",br+": "+t);hits++;}
    idx+=r[k].Length;}}}
 void Web(){
  UI("Analyzing browser history",95,7);
  string la=Environment.GetEnvironmentVariable("LOCALAPPDATA")??"",ap=Environment.GetEnvironmentVariable("APPDATA")??"";
  string[][] roots={new[]{"Chrome",Path.Combine(la,@"Google\Chrome\User Data")},new[]{"Edge",Path.Combine(la,@"Microsoft\Edge\User Data")},new[]{"Brave",Path.Combine(la,@"BraveSoftware\Brave-Browser\User Data")},new[]{"Opera",Path.Combine(ap,@"Opera Software\Opera Stable")},new[]{"Opera GX",Path.Combine(ap,@"Opera Software\Opera GX Stable")}};
  foreach(var r in roots){try{if(!Directory.Exists(r[1]))continue;var files=new List<string>();files.Add(Path.Combine(r[1],"History"));
   foreach(var d in Directory.GetDirectories(r[1])){var n=Path.GetFileName(d);if(n=="Default"||n.StartsWith("Profile "))files.Add(Path.Combine(d,"History"));}
   foreach(var f in files){BinScan(f,r[0]);BinScan(f+"-wal",r[0]);}}catch{}}
  try{string fp=Path.Combine(ap,@"Mozilla\Firefox\Profiles");if(Directory.Exists(fp))foreach(var d in Directory.GetDirectories(fp)){BinScan(Path.Combine(d,"places.sqlite"),"Firefox");BinScan(Path.Combine(d,"places.sqlite-wal"),"Firefox");}}catch{}
  UI("Analyzing DNS cache",96,7);
  try{var ps=new ProcessStartInfo("ipconfig","/displaydns"){UseShellExecute=false,RedirectStandardOutput=true,CreateNoWindow=true};
   using(var p=Process.Start(ps)){var sw=Stopwatch.StartNew();string line;while(sw.Elapsed.TotalSeconds<20&&(line=p.StandardOutput.ReadLine())!=null){int c=line.IndexOf(':');var v=(c>=0?line.Substring(c+1):line).Trim();if(v.Length<4||v.IndexOf('.')<0||v.IndexOf(' ')>=0)continue;var r=MW(v);if(r!=null)Add(int.Parse(r[1]),r[0],"DNS",v);}try{if(!p.HasExited)p.Kill();}catch{}}}catch{}}
 // ---- execution traces: programs that ran, even if the file was deleted afterwards ----
 void RegChk(string name,string src){
  if(string.IsNullOrEmpty(name))return;string path=name;int i=name.ToLower().IndexOf(".exe");if(i>0)path=name.Substring(0,i+4);
  if(path.StartsWith("\\Device\\",StringComparison.OrdinalIgnoreCase)){int k=path.IndexOf('\\',8);if(k>0)path=path.Substring(k);}
  var r=M(path);if(r==null||Sk(path))return;
  var v=Ver(path);if(v==null)return;
  Add(int.Parse(r[1]),r[0],"Execution",v);}
 void Reg(){
  UI("Analyzing execution traces",98,8);
  try{using(var k=Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache"))if(k!=null)foreach(var n in k.GetValueNames())RegChk(n,"MuiCache");}catch{}
  try{using(var k=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store"))if(k!=null)foreach(var n in k.GetValueNames())RegChk(n,"Store");}catch{}
  foreach(var root in new[]{@"SYSTEM\CurrentControlSet\Services\bam\State\UserSettings",@"SYSTEM\CurrentControlSet\Services\bam\UserSettings"})
   try{using(var k=Registry.LocalMachine.OpenSubKey(root))if(k!=null)foreach(var sid in k.GetSubKeyNames())using(var s=k.OpenSubKey(sid))if(s!=null)foreach(var n in s.GetValueNames())RegChk(n,"BAM");}catch{}}

 // ---- verification: real file info + signature. Trusted publishers are skipped (no false alarms) ----
 static readonly string[] TRU={"microsoft","nvidia","valve","intel","advanced micro","realtek","logitech","razer","steelseries","corsair","asus","micro-star","google","mozilla","epic games","riot","discord","adobe","oracle","rockstar","cfx.re"};
 static string Sg(string f){try{var c=new System.Security.Cryptography.X509Certificates.X509Certificate2(System.Security.Cryptography.X509Certificates.X509Certificate.CreateFromSignedFile(f));string n=c.GetNameInfo(System.Security.Cryptography.X509Certificates.X509NameType.SimpleName,false)??"";var ch=new System.Security.Cryptography.X509Certificates.X509Chain();ch.ChainPolicy.RevocationMode=System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;return ch.Build(c)?n:"INVALID:"+n;}catch{return "";}}
 static bool Trusted(string s){s=s.ToLower();if(s.StartsWith("invalid:"))return false;foreach(var t in TRU)if(s.Contains(t))return true;return false;}
 static string Ver(string p){
  try{if(string.IsNullOrEmpty(p)||p.Length<4||p[1]!=':')return p;
   if(!File.Exists(p))return p+"  [NOT on disk: deleted or moved, trace only]";
   var fi=new FileInfo(p);string sg=Sg(p);if(sg.Length>0&&Trusted(sg))return null;
   string h="";try{if(fi.Length<=120L*1048576)using(var s=new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))h=BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(s)).Replace("-","").ToLower();}catch{}
   return p+"  [on disk | "+(fi.Length/1024)+" KB | created "+fi.CreationTime.ToString("yyyy-MM-dd HH:mm")+" | modified "+fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm")+" | "+(sg.Length>0?"signed: "+sg:"unsigned")+(h.Length>0?" | sha256 "+h:"")+"]";
  }catch{return p;}}
 // shortcut target (Recent)
 static string Lnk(string f){try{if(!f.EndsWith(".lnk",StringComparison.OrdinalIgnoreCase))return f;var t=Type.GetTypeFromProgID("WScript.Shell");object sh=Activator.CreateInstance(t);object sc=t.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,sh,new object[]{f});string tp=(string)sc.GetType().InvokeMember("TargetPath",BindingFlags.GetProperty,null,sc,null);return string.IsNullOrEmpty(tp)?f:tp;}catch{return f;}}
 // prefetch (.pf, Win10/11 compressed): recover the exe's real path
 [DllImport("ntdll.dll")]static extern uint RtlGetCompressionWorkSpaceSize(ushort fmt,out uint a,out uint b);
 [DllImport("ntdll.dll")]static extern uint RtlDecompressBufferEx(ushort fmt,byte[] o,int os,byte[] i,int isz,out int fs,byte[] ws);
 static string Pf(string f){try{var b=File.ReadAllBytes(f);
   if(b.Length>8&&b[0]=='M'&&b[1]=='A'&&b[2]=='M'){int us=BitConverter.ToInt32(b,4);var src=new byte[b.Length-8];Array.Copy(b,8,src,0,src.Length);uint a,c;RtlGetCompressionWorkSpaceSize(4,out a,out c);var o=new byte[us];int fs;if(RtlDecompressBufferEx(4,o,us,src,src.Length,out fs,new byte[c])!=0)return "";b=o;}
   string nm=Path.GetFileName(f),exe=nm.Substring(0,nm.LastIndexOf('-')).ToUpper();
   foreach(var x in Encoding.Unicode.GetString(b).Split('\0')){if(x.StartsWith("\\VOLUME{")&&x.ToUpper().EndsWith("\\"+exe)){string rest=x.Substring(x.IndexOf('}')+1).TrimStart('\\');
     foreach(var d in DriveInfo.GetDrives()){try{if(File.Exists(d.Name+rest))return d.Name+rest;}catch{}}return x+" (volume not mounted)";}}
  }catch{}return "";}
 // ShimCache (AppCompatCache): every exe Windows saw, even if deleted afterwards
 void Shim(){UI("Analyzing old executables (ShimCache)",98,8);try{var b=RV(Registry.LocalMachine,@"SYSTEM\CurrentControlSet\Control\Session Manager\AppCompatCache","AppCompatCache") as byte[];if(b==null||b.Length<64)return;int i=BitConverter.ToInt32(b,0);if(i!=0x30&&i!=0x34)return;
   while(i+14<b.Length){if(b[i]!='1'||b[i+1]!='0'||b[i+2]!='t'||b[i+3]!='s')break;int sz=BitConverter.ToInt32(b,i+8),pl=BitConverter.ToUInt16(b,i+12);if(pl<=0||i+14+pl>b.Length)break;RegChk(Encoding.Unicode.GetString(b,i+14,pl),"ShimCache");i+=12+sz;}}catch{}}
 // UserAssist (ROT13 names): programs launched by the user
 static string Rot(string s){var a=s.ToCharArray();for(int i=0;i<a.Length;i++){char c=a[i];if(c>='a'&&c<='z')a[i]=(char)((c-'a'+13)%26+'a');else if(c>='A'&&c<='Z')a[i]=(char)((c-'A'+13)%26+'A');}return new string(a);}
 void UA(){try{using(var ua=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist"))if(ua!=null)foreach(var g in ua.GetSubKeyNames())using(var c=ua.OpenSubKey(g+"\\Count"))if(c!=null)foreach(var n in c.GetValueNames())RegChk(Rot(n),"UserAssist");}catch{}}
 static int Mx(List<string[]> l){int m=0;foreach(var f in l)m=Math.Max(m,int.Parse(f[0]));return m;}
 string Risk(){int w=0,m=0;lock(found){foreach(var f in found){int s=int.Parse(f[0]);w+=s;m=Math.Max(m,s);}}return w>=6?"Severe":(m>=3||w>=4)?"High":w>=2?"Medium":"Low";}
 void Start(){
  if(checking)return;
  if(pin.Text.Length!=6){pinErr="PIN incorrect";status=pinErr;Invalidate();return;}
  if(!agree){MessageBox.Show("Accept the checks to continue.");return;}
  checking=true;pinErr="";status="Checking PIN...";Invalidate();string pv=pin.Text;
  ThreadPool.QueueUserWorkItem(x=>{string res="ok";
   try{Post("/api/pin-check","{\"pin\":"+J(pv)+"}");}catch(WebException e){var hr=e.Response as HttpWebResponse;res=(hr!=null&&((int)hr.StatusCode==403||(int)hr.StatusCode==404||(int)hr.StatusCode==429))?"bad":"net";}catch{res="net";}
   try{BeginInvoke((Action)(()=>{checking=false;if(res=="ok")Begin();else{pinErr=res=="bad"?"PIN incorrect":"Cannot reach the server";status=res=="bad"?pinErr:"Cannot reach "+SRV;Invalidate();}}));}catch{}});}
 void Begin(){
  if(pin.Text.Length!=6){MessageBox.Show("Enter the 6-digit PIN.");return;}
  if(!agree){MessageBox.Show("Accept the checks to continue.");return;}
  lock(found){found.Clear();seen.Clear();}mInstall="";mDays=-1;mBoots.Clear();mUn.Clear();done=false;sendSt=0;sendMsg="";skipped="";pct=0;target=0;sent=false;stg=0;pinV=pin.Text;status="Analyzing running processes";SetState(1);
  new Thread(()=>{
   string err="";
   Action<Action,string> run=(f,n)=>{try{f();}catch(Exception ex){Log(ex);err+=n+" ";}};
   run(()=>{UI("Analyzing running processes",5,0);foreach(var p in Process.GetProcesses()){try{string pf="";try{pf=p.MainModule.FileName;}catch{}Chk(p.ProcessName,pf.Length>0?pf:p.ProcessName+".exe","Process",false,0);}catch{}}},"processes");
   run(()=>Scan("Recent",Environment.GetFolderPath(Environment.SpecialFolder.Recent),"*","Recent",-1,6,1),"recent");
   run(()=>Scan("Prefetch",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"Prefetch"),"*.pf","Prefetch",0,7,1),"prefetch");
   run(Walk,"drives");run(Sig,"signatures");run(Bin,"recyclebin");run(Usn,"usn");run(Logs,"eventlogs");run(Meta,"dates");run(Security,"security");run(Web,"browser");run(Mem,"memory");run(Reg,"registry");run(Shim,"shimcache");run(UA,"userassist");
   skipped=err;done=true;
  }){IsBackground=true}.Start();}
 string Build(){
  var sb=new StringBuilder("{\"pin\":"+J(pinV)+",\"os\":"+J(Environment.OSVersion.ToString())+",\"findings\":[");
  lock(found){for(int i=0;i<found.Count;i++){var f=found[i];if(i>0)sb.Append(",");sb.Append("{\"sev\":"+f[0]+",\"cat\":"+J(f[1])+",\"src\":"+J(f[2])+",\"text\":"+J(f[3])+"}");}}
  sb.Append("],\"meta\":{\"install\":"+J(mInstall)+",\"days\":"+mDays+",\"boots\":[");
  for(int i=0;i<mBoots.Count;i++){if(i>0)sb.Append(",");sb.Append(J(mBoots[i]));}
  sb.Append("],\"uninstalls\":[");
  for(int i=0;i<mUn.Count;i++){if(i>0)sb.Append(",");sb.Append("{\"n\":"+J(mUn[i][0])+",\"t\":"+J(mUn[i][1])+"}");}
  sb.Append("]}}");return sb.ToString();}
 void StartSend(){sendSt=0;sendMsg="";string json=Build();
  ThreadPool.QueueUserWorkItem(x=>{try{Post("/api/scan",json);sendSt=1;}
   catch(WebException e){string m=e.Message;try{if(e.Response!=null)using(var sr=new StreamReader(e.Response.GetResponseStream()))m=sr.ReadToEnd();}catch{}sendMsg=m;sendSt=2;}
   catch(Exception e){sendMsg=e.Message;sendSt=2;}});}

 // ---- byte search + native memory helpers ----
 static int Find(byte[] h,int n,byte[] p){int m=p.Length;if(m==0||n<m)return -1;byte f=p[0];int i=0,last=n-m;while(i<=last){i=Array.IndexOf(h,f,i,last-i+1);if(i<0)return -1;int j=1;while(j<m&&h[i+j]==p[j])j++;if(j==m)return i;i++;}return -1;}
 [DllImport("kernel32.dll")]static extern IntPtr OpenProcess(int a,bool b,int pid);
 [DllImport("kernel32.dll")]static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
 [DllImport("kernel32.dll")]static extern int VirtualQueryEx(IntPtr h,IntPtr addr,out MBI mbi,int len);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr h);
 [DllImport("ntdll.dll")]static extern int RtlAdjustPrivilege(int p,bool e,bool t,out bool o);
 [StructLayout(LayoutKind.Sequential)]struct MBI{public IntPtr Base,AllocBase;public uint AllocProt;public IntPtr Size;public uint State,Prot,Type;}
 // ---- cheat signatures inside executables (renamed cheats) ----
 static readonly string[] SG={"keyser","susano","redengine","eulencheats","aimware","neverlose","gamesense.pub","nixware","legendware","synapse x","jjsploit","scriptware","sirhurt","macsploit","fluxus","kdmapper","xenos injector","extreme injector"};
 static readonly string[] SG3={"kdmapper","xenos injector","extreme injector","synapse x","jjsploit","scriptware","sirhurt","macsploit"};
 void Sig(){
  UI("Checking files for cheat signatures",81,2);
  string up=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);var roots=new List<string>();
  foreach(var d in new[]{"Downloads","Desktop","Documents","AppData\\Local\\Temp","AppData\\Roaming"})roots.Add(Path.Combine(up,d));
  roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
  var pats=new List<byte[]>();foreach(var g in SG){pats.Add(Encoding.ASCII.GetBytes(g));pats.Add(Encoding.Unicode.GetBytes(g));}
  string self=(Application.ExecutablePath??"").ToLower();var end=DateTime.Now.AddSeconds(120);int files=0;var buf=new byte[6<<20];
  foreach(var root in roots){if(!Directory.Exists(root))continue;var st=new Stack<KeyValuePair<string,int>>();st.Push(new KeyValuePair<string,int>(root,0));
   while(st.Count>0&&files<3000&&DateTime.Now<end){var cur=st.Pop();
    try{var di=new DirectoryInfo(cur.Key);
     foreach(var f in di.EnumerateFiles()){string ext=f.Extension.ToLower();if(ext!=".exe"&&ext!=".dll"&&ext!=".sys"&&ext!=".asi"&&ext!=".bin")continue;
      string fl=f.FullName.ToLower();if(fl==self||f.Name.ToLower().StartsWith("rzcore")||Sk(fl)||f.Length<20000||f.Length>60L*1048576)continue;
      files++;int n;try{using(var fs=new FileStream(f.FullName,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))n=fs.Read(buf,0,buf.Length);}catch{continue;}
      for(int i=0;i<n;i++){byte c=buf[i];if(c>='A'&&c<='Z')buf[i]=(byte)(c+32);}
      for(int k=0;k<pats.Count;k++)if(Find(buf,n,pats[k])>=0){string sg=SG[k/2];string vd=Ver(f.FullName);if(vd!=null)Add(Array.IndexOf(SG3,sg)>=0?3:2,"Cheat Signatures","File",vd+"  [contains \""+sg+"\"]");break;}}
     if(cur.Value<4)foreach(var d in di.EnumerateDirectories()){if((d.Attributes&FileAttributes.ReparsePoint)!=0)continue;string nl=d.Name.ToLower();if(nl=="microsoft"||nl=="google"||nl=="mozilla"||nl=="packages"||nl=="discord"||nl=="code"||nl=="npm")continue;st.Push(new KeyValuePair<string,int>(d.FullName,cur.Value+1));}
    }catch{}}}}
 // ---- browser + DNS service memory (strings left behind even after history was cleared) ----
 static readonly string[] MS={"unknowncheats.me","elitepvpers.com","mpgh.net","guidedhacking.com","cheatengine.org","aimware.net","neverlose.cc","gamesense.pub","nixware.cc","onetap.su","fluxteam","synapsex","jjsploit.","scriptware.","sirhurt.","keyser.","susano.","redengine.","eulencheats.","skript.gg"};
 void Mem(){
  UI("Checking browser and DNS memory",97,7);
  try{bool o;RtlAdjustPrivilege(20,true,false,out o);}catch{}
  var pids=new List<KeyValuePair<int,string>>();
  foreach(var ln in RunOut("tasklist","/svc /fi \"services eq Dnscache\" /fo csv /nh",8000).Split('\n')){var c=ln.Split(',');int pid;if(c.Length>1&&int.TryParse(c[1].Trim('"',' ','\r'),out pid))pids.Add(new KeyValuePair<int,string>(pid,"DNS Client service"));}
  int cnt=0;foreach(var b in new[]{"chrome","msedge","brave","opera","firefox","vivaldi"})foreach(var p in Process.GetProcessesByName(b)){if(cnt++>=14)break;pids.Add(new KeyValuePair<int,string>(p.Id,b));}
  var pats=new List<byte[]>();foreach(var m in MS){pats.Add(Encoding.ASCII.GetBytes(m));pats.Add(Encoding.Unicode.GetBytes(m));}
  var end=DateTime.Now.AddSeconds(35);var hit=new HashSet<string>();var buf=new byte[1<<20];int sz=Marshal.SizeOf(typeof(MBI));
  foreach(var kv in pids){if(DateTime.Now>end)break;IntPtr h=IntPtr.Zero;
   try{h=OpenProcess(0x0410,false,kv.Key);if(h==IntPtr.Zero)continue;long addr=0,read=0;
    while(read<300L*1048576&&DateTime.Now<end){MBI mi;if(VirtualQueryEx(h,(IntPtr)addr,out mi,sz)==0)break;long size=(long)mi.Size;if(size<=0)break;
     if(mi.State==0x1000&&mi.Prot!=0&&(mi.Prot&0x101)==0&&mi.Type==0x20000){
      for(long o=0;o<size&&read<300L*1048576;o+=buf.Length){int want=(int)Math.Min((long)buf.Length,size-o);IntPtr got;if(!ReadProcessMemory(h,(IntPtr)(addr+o),buf,want,out got)||(long)got<=0)break;int n=(int)(long)got;read+=n;
       for(int i=0;i<n;i++){byte c=buf[i];if(c>='A'&&c<='Z')buf[i]=(byte)(c+32);}
       for(int k=0;k<pats.Count;k++){string nm=MS[k/2],key=kv.Value+"|"+nm;if(hit.Contains(key))continue;
        if(Find(buf,n,pats[k])>=0){hit.Add(key);var r=MW(nm);Add(r!=null?int.Parse(r[1]):2,r!=null?r[0]:"Browser Memory","Browser","Seen in "+kv.Value+" memory: "+nm.TrimEnd('.')+" (visited recently, even if history was cleared)");}}}}
     addr+=size;if(addr<=0)break;}
   }catch{}finally{if(h!=IntPtr.Zero)CloseHandle(h);}}}
 // ---- PC timeline: Windows install date, boots, uninstalled programs ----
 string mInstall="";int mDays=-1;List<string> mBoots=new List<string>();List<string[]> mUn=new List<string[]>();
 void Meta(){
  try{object v=RV(Registry.LocalMachine,@"SOFTWARE\Microsoft\Windows NT\CurrentVersion","InstallDate");
   if(v is int){var d=new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds((int)v).ToLocalTime();mInstall=d.ToString("yyyy-MM-dd HH:mm");mDays=(int)(DateTime.Now-d).TotalDays;
    if(mDays<=14)Add(2,"PC History","Security","Windows was installed "+mDays+" day(s) ago (recent format or reinstall)");
    else if(mDays<=45)Add(1,"PC History","Security","Windows was installed "+mDays+" days ago (check if a format was done before the scan)");}}catch{}
  foreach(var x in Ev("System","(EventID=6005)",5))mBoots.Add(Tm(x));
  var cut=DateTime.UtcNow.AddHours(-48);int recent=0;
  foreach(var x in Ev("Application","(EventID=11724 or EventID=1034)",60)){
   string d0=WebUtility.HtmlDecode(Tg(x,"<Data>(.*?)</Data>"));var mm=System.Text.RegularExpressions.Regex.Match(d0,"Product: (.*?) --");string nm=(mm.Success?mm.Groups[1].Value:d0).Trim();if(nm.Length==0)continue;
   DateTime dt;if(DateTime.TryParse(Tg(x,"SystemTime='([^']+)'"),System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.AdjustToUniversal|System.Globalization.DateTimeStyles.AssumeUniversal,out dt)&&dt>cut)recent++;
   if(mUn.Count<25)mUn.Add(new[]{nm.Length>80?nm.Substring(0,80):nm,Tm(x)});
   var r=M(nm);if(r!=null)Add(int.Parse(r[1]),r[0],"EventLog","Uninstalled: "+nm+" ("+Tm(x)+")");}
  if(recent>=5)Add(2,"Anti-Forensics","EventLog",recent+" programs were uninstalled in the last 48 hours");}
 // ---- Windows event logs + security settings (matches only) ----
 static string RunOut(string exe,string args,int ms){try{var ps=new ProcessStartInfo(exe,args){UseShellExecute=false,RedirectStandardOutput=true,CreateNoWindow=true};using(var p=Process.Start(ps)){var sb=new StringBuilder();var t=new Thread(()=>{try{sb.Append(p.StandardOutput.ReadToEnd());}catch{}});t.Start();if(!t.Join(ms)){try{p.Kill();}catch{}}return sb.ToString();}}catch{return "";}}
 static List<string> Ev(string log,string cond,int max){var l=new List<string>();string o=RunOut("wevtutil","qe \""+log+"\" /q:\"*[System["+cond+"]]\" /c:"+max+" /rd:true /f:xml",25000);
  foreach(var c in o.Split(new[]{"</Event>"},StringSplitOptions.RemoveEmptyEntries))if(c.Contains("<Event"))l.Add(c);return l;}
 static string Tg(string x,string pat){var m=System.Text.RegularExpressions.Regex.Match(x,pat,System.Text.RegularExpressions.RegexOptions.Singleline);return m.Success?m.Groups[1].Value:"";}
 static string Tm(string x){var t=Tg(x,"SystemTime='([^']+)'");return t.Length>=16?t.Substring(0,16).Replace('T',' ')+" UTC":t;}
 static string Dt(string x,string n){return WebUtility.HtmlDecode(Tg(x,"<Data Name='"+n+"'>(.*?)</Data>"));}
 void Logs(){
  UI("Reading Windows event logs",91,5);
  foreach(var x in Ev("Security","(EventID=1102)",5))Add(3,"Windows Logs","EventLog","Security log was cleared at "+Tm(x));
  foreach(var lg in new[]{"System","Application"})foreach(var x in Ev(lg,"(EventID=104)",5))Add(3,"Windows Logs","EventLog",lg+" log was cleared at "+Tm(x));
  foreach(var x in Ev("System","(EventID=7045)",80)){string n=Dt(x,"ServiceName"),ip=Dt(x,"ImagePath"),ty=Dt(x,"ServiceType").ToLower(),low=ip.ToLower();var r=M(n+" "+ip);
   bool odd=ty.Contains("kernel")&&(low.Contains("\\users\\")||low.Contains("\\temp\\")||low.Contains("\\downloads\\")||low.Contains("\\appdata\\")||low.Contains("\\programdata\\"));
   if(r!=null)Add(int.Parse(r[1]),r[0],"EventLog","Service installed: "+n+" "+ip+" ("+Tm(x)+")");else if(odd)Add(2,"Suspicious Drivers","EventLog","Driver installed from a user folder: "+n+" "+ip+" ("+Tm(x)+")");}
  const string DEF="Microsoft-Windows-Windows Defender/Operational";
  foreach(var x in Ev(DEF,"(EventID=1116 or EventID=1117)",40)){string th=Dt(x,"Threat Name"),pa=Dt(x,"Path");Add(2,"Defender Detections","EventLog",(th.Length>0?th:"Threat")+" "+pa+" ("+Tm(x)+")");}
  foreach(var x in Ev(DEF,"(EventID=5001 or EventID=5010 or EventID=5012)",10))Add(2,"Security Tampering","EventLog","Windows Defender protection was turned off ("+Tm(x)+")");
  foreach(var x in Ev(DEF,"(EventID=5007)",120)){string nv=Dt(x,"New Value");if(nv.IndexOf("Exclusions",StringComparison.OrdinalIgnoreCase)>=0||nv.IndexOf("DisableRealtimeMonitoring = 0x1",StringComparison.OrdinalIgnoreCase)>=0||nv.IndexOf("DisableAntiSpyware = 0x1",StringComparison.OrdinalIgnoreCase)>=0)Add(2,"Security Tampering","EventLog","Defender setting changed: "+(nv.Length>180?nv.Substring(0,180):nv)+" ("+Tm(x)+")");}}
 static object RV(RegistryKey root,string path,string name){try{using(var k=root.OpenSubKey(path))return k==null?null:k.GetValue(name);}catch{return null;}}
 static bool Is(object v,int n){return v!=null&&(v is int)&&(int)v==n;}
 void Security(){
  UI("Checking security settings",93,6);
  var L=Registry.LocalMachine;
  if(Is(RV(L,@"SOFTWARE\Policies\Microsoft\Windows Defender","DisableAntiSpyware"),1))Add(2,"Security Tampering","Security","Windows Defender is disabled by policy");
  if(Is(RV(L,@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection","DisableRealtimeMonitoring"),1)||Is(RV(L,@"SOFTWARE\Microsoft\Windows Defender\Real-Time Protection","DisableRealtimeMonitoring"),1))Add(2,"Security Tampering","Security","Defender real-time protection is turned off");
  if(Is(RV(L,@"SYSTEM\CurrentControlSet\Services\WinDefend","Start"),4))Add(2,"Security Tampering","Security","Windows Defender service is disabled");
  if(Is(RV(L,@"SYSTEM\CurrentControlSet\Services\EventLog","Start"),4))Add(3,"Windows Logs","Security","Windows Event Log service is disabled");
  if(Is(RV(L,@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System","EnableLUA"),0))Add(1,"Security Tampering","Security","User Account Control (UAC) is turned off");
  if(Is(RV(L,@"SYSTEM\CurrentControlSet\Control\CI\Config","VulnerableDriverBlocklistEnable"),0))Add(2,"Security Tampering","Security","Vulnerable driver blocklist is turned off");
  if(Is(RV(L,@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters","EnablePrefetcher"),0))Add(1,"Anti-Forensics","Security","Prefetch is disabled (hides which programs ran)");
  foreach(var path in new[]{@"SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths",@"SOFTWARE\Microsoft\Windows Defender\Exclusions\Processes",@"SOFTWARE\Microsoft\Windows Defender\Exclusions\Extensions",@"SOFTWARE\Policies\Microsoft\Windows Defender\Exclusions\Paths"})
   try{using(var k=L.OpenSubKey(path))if(k!=null)foreach(var n in k.GetValueNames()){var r=M(n);Add(r!=null?Math.Max(2,int.Parse(r[1])):2,"Defender Exclusions","Security","Excluded from Defender: "+n);}}catch{}
  string bc=RunOut("bcdedit","/enum {current}",8000);
  foreach(var raw in bc.Split('\n')){var ln=raw.Trim().ToLower();if((ln.StartsWith("testsigning")||ln.StartsWith("nointegritychecks"))&&(ln.EndsWith("yes")||ln.EndsWith("oui")))Add(3,"Driver Signing","Security",ln.StartsWith("testsigning")?"Test-signing mode is ON (unsigned drivers can load)":"Driver integrity checks are disabled");}
  string dq=RunOut("driverquery","/fo csv /nh",25000);
  foreach(var raw in dq.Split('\n')){var ln=raw.Trim();if(ln.Length<3)continue;var r=M(ln);if(r==null)continue;var nm=ln.Split(',')[0].Trim('"',' ');Add(int.Parse(r[1]),r[0],"Driver","Loaded driver: "+nm);}}
}
