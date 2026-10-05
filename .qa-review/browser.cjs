const fs=require('fs');
(async()=>{
 const tabs=await fetch('http://127.0.0.1:9223/json/list').then(r=>r.json());
 const tab=tabs.find(t=>t.type==='page'); if(!tab)throw Error('No browser tab');
 const ws=new WebSocket(tab.webSocketDebuggerUrl); await new Promise((res,rej)=>{ws.onopen=res;ws.onerror=rej});
 let seq=0; const pending=new Map();
 ws.onmessage=e=>{let m=JSON.parse(e.data);if(m.id){const p=pending.get(m.id);if(p){pending.delete(m.id);m.error?p.reject(m.error):p.resolve(m.result)}}};
 const call=(method,params={})=>new Promise((resolve,reject)=>{const id=++seq;pending.set(id,{resolve,reject});ws.send(JSON.stringify({id,method,params}))});
 const mode=process.argv[2];
 if(mode==='nav'){await call('Page.navigate',{url:process.argv[3]});await new Promise(r=>setTimeout(r,1700));}
 else if(mode==='shot'){const r=await call('Page.captureScreenshot',{format:'png',captureBeyondViewport:true});fs.writeFileSync(process.argv[3],Buffer.from(r.data,'base64'));console.log('Screenshot saved');}
 else if(mode==='size'){await call('Emulation.setDeviceMetricsOverride',{width:+process.argv[3],height:+process.argv[4],deviceScaleFactor:1,mobile:process.argv[5]==='mobile'});}
 else {const expression=fs.readFileSync(0,'utf8');const r=await call('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});if(r.exceptionDetails)console.log(JSON.stringify(r.exceptionDetails));else console.log(JSON.stringify(r.result.value));}
 ws.close();
})().catch(e=>{console.error(e);process.exit(1)});
