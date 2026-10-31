/* RzCore logo animation (same as RzCore.exe): black logo -> red water rises -> magnifier inspects the logo -> back -> drains -> repeat. Needs lt.png + lp.png */
(function(){
var KP=[[0,0],[-110,115],[-250,135],[-345,115],[-215,80],[-80,40],[0,0]],sm=function(x){x=Math.max(0,Math.min(1,x));return x*x*(3-2*x)};
function tint(im,fn){var c=document.createElement('canvas');c.width=560;c.height=412;var x=c.getContext('2d');x.drawImage(im,0,0);fn(x);return c}
window.rzAnim=function(cv){var ctx=cv.getContext('2d'),lt=new Image(),lp=new Image(),n=0,still=window.matchMedia&&matchMedia('(prefers-reduced-motion:reduce)').matches;
 function go(){if(++n<2)return;
  var bt=tint(lt,function(x){x.globalCompositeOperation='source-in';x.fillStyle='#121214';x.fillRect(0,0,560,412)}),bl=tint(lp,function(x){x.globalCompositeOperation='source-in';x.fillStyle='#121214';x.fillRect(0,0,560,412)}),
  lit=tint(lt,function(x){x.globalCompositeOperation='source-atop';x.fillStyle='rgba(255,90,90,.28)';x.fillRect(0,0,560,412)}),t0=performance.now();
  function frame(now){var tick=still?165:Math.floor((now-t0)/40),t=(tick%330)/330,f,p=-1,ox=0,oy=0,rot=0,sc=1;
   f=t<.06?0:t<.44?sm((t-.06)/.38):t<.92?1:1-sm((t-.92)/.08);if(t>.52&&t<.86)p=(t-.52)/.34;
   if(p>=0){var sg=p*6,i=Math.min(5,Math.floor(sg)),fr=sg-i;fr=(fr+sm(fr))/2;ox=KP[i][0]+(KP[i+1][0]-KP[i][0])*fr;oy=KP[i][1]+(KP[i+1][1]-KP[i][1])*fr;rot=7*Math.sin(p*Math.PI*6);sc=1+.12*Math.sin(p*Math.PI)}
   ctx.clearRect(0,0,560,412);ctx.drawImage(bt,0,0);ctx.drawImage(bl,0,0);
   for(var w=0;w<2&&f>0;w++){var sy=(w?430:418)-460*f,ph=tick*(w?.19:.13)+w*2,am=w?6:9;ctx.save();ctx.beginPath();
    for(var x=-10;x<=570;x+=10){var y=sy+am*Math.sin(x*.028+ph);x==-10?ctx.moveTo(x,y):ctx.lineTo(x,y)}ctx.lineTo(570,480);ctx.lineTo(-10,480);ctx.closePath();ctx.clip();
    ctx.globalAlpha=w?1:.5;ctx.drawImage(lt,0,0);if(p<0)ctx.drawImage(lp,0,0);ctx.restore()}
   if(p>=0){var lx=442.7+ox,ly=73+oy,lr=57*sc;ctx.save();ctx.beginPath();ctx.arc(lx,ly,lr,0,7);ctx.clip();ctx.fillStyle='rgba(6,4,6,.85)';ctx.fillRect(lx-lr,ly-lr,lr*2,lr*2);
    ctx.translate(lx,ly);ctx.scale(1.45,1.45);ctx.translate(-lx,-ly);ctx.drawImage(lit,0,0);ctx.restore();
    ctx.save();ctx.translate(ox,oy);ctx.translate(442.7,73);ctx.rotate(rot*Math.PI/180);ctx.scale(sc,sc);ctx.translate(-442.7,-73);ctx.drawImage(lp,0,0);ctx.restore()}
   if(!still)requestAnimationFrame(frame)}
  requestAnimationFrame(frame)}
 lt.onload=lp.onload=go;lt.src='lt.png';lp.src='lp.png'};
window.rzTilt=function(el){var st=document.createElement('style');st.textContent='.t3idle{animation:idle3 8s ease-in-out infinite}@keyframes idle3{0%,100%{transform:perspective(1000px) rotateY(-10deg) rotateX(4deg)}50%{transform:perspective(1000px) rotateY(10deg) rotateX(-3deg)}}@media(prefers-reduced-motion:reduce){.t3idle{animation:none}}';document.head.appendChild(st);
 el.style.transformStyle='preserve-3d';el.classList.add('t3idle');
 el.addEventListener('pointermove',function(e){var r=el.getBoundingClientRect(),x=(e.clientX-r.left)/r.width-.5,y=(e.clientY-r.top)/r.height-.5;el.classList.remove('t3idle');el.style.transform='perspective(1000px) rotateY('+x*22+'deg) rotateX('+(-y*18)+'deg)'});
 el.addEventListener('pointerleave',function(){el.style.transform='';el.classList.add('t3idle')})};
})();
