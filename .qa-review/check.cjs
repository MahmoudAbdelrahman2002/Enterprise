const fs = require('fs');
(async () => {
  const tabs = await fetch('http://127.0.0.1:9223/json/list').then(r => r.json());
  const tab = tabs.find(t => t.type === 'page' && t.url.includes('127.0.0.1:4200')) || tabs.find(t => t.type === 'page');
  const ws = new WebSocket(tab.webSocketDebuggerUrl);
  await new Promise((resolve, reject) => { ws.onopen = resolve; ws.onerror = reject; });
  let seq = 0;
  const pending = new Map();
  const responses = [];
  ws.onmessage = e => { const m = JSON.parse(e.data); if (m.method === 'Network.responseReceived' && m.params.response.url.includes('/api/')) responses.push({ url: m.params.response.url, status: m.params.response.status }); if (m.id && pending.has(m.id)) { const p = pending.get(m.id); pending.delete(m.id); m.error ? p.reject(m.error) : p.resolve(m.result); } };
  const call = (method, params = {}) => new Promise((resolve, reject) => { const id = ++seq; pending.set(id, { resolve, reject }); ws.send(JSON.stringify({ id, method, params })); });
  if (process.argv[2] === 'viewport') await call('Emulation.setDeviceMetricsOverride', { width: +process.argv[3], height: +process.argv[4], deviceScaleFactor: 1, mobile: false });
  await call('Runtime.enable');
  await call('Network.enable');
  if (process.argv[2] === 'file') {
    const doc = await call('DOM.getDocument');
    const node = await call('DOM.querySelector', { nodeId: doc.root.nodeId, selector: process.argv[3] });
    await call('DOM.setFileInputFiles', { nodeId: node.nodeId, files: [process.argv[4]] });
    await new Promise(r => setTimeout(r, 3500));
  }
  const expression = fs.readFileSync(0, 'utf8');
  if (expression.trim()) { const r = await call('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true }); console.log(JSON.stringify(r.exceptionDetails || r.result.value)); }
  if (process.argv[2] === 'keys' || process.argv[6]) {
    const steps = [];
    for (const key of (process.argv[6] || process.argv[3]).split(',')) {
      const actual = key === 'ShiftTab' ? 'Tab' : key;
      const code = actual === 'Tab' ? 9 : actual === 'Escape' ? 27 : 13;
      await call('Input.dispatchKeyEvent', { type: 'keyDown', key: actual, code: actual, windowsVirtualKeyCode: code, modifiers: key === 'ShiftTab' ? 8 : 0 });
      await call('Input.dispatchKeyEvent', { type: 'keyUp', key: actual, code: actual, windowsVirtualKeyCode: code });
      const r = await call('Runtime.evaluate', { expression: '({text:document.activeElement.innerText,tag:document.activeElement.tagName,aria:document.activeElement.getAttribute("aria-label"),dialogs:document.querySelectorAll("[role=dialog]").length})', returnByValue: true });
      steps.push({ key, focus: r.result.value });
    }
    console.log(JSON.stringify({ keyboard: steps }));
  }
  if (process.argv[5]) { const r = await call('Page.captureScreenshot', { format: 'png', captureBeyondViewport: true }); fs.writeFileSync(process.argv[5], Buffer.from(r.data, 'base64')); }
  if (responses.length) console.log(JSON.stringify({ responses }));
  ws.close();
})().catch(e => { console.error(e); process.exit(1); });
