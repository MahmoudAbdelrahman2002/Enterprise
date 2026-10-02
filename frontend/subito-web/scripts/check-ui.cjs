/* Render checks use API fixtures, never live checkout or management writes.
 * Run with: node scripts/check-ui.cjs (requires playwright in .ui-check).
 */
const {chromium} = require('../.ui-check/node_modules/playwright');
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../dist/subito-web/browser');
const artifacts = path.resolve(__dirname, '../.ui-check/artifacts');
fs.mkdirSync(artifacts, {recursive: true});
const sid='11111111-1111-1111-1111-111111111111', pid='22222222-2222-2222-2222-222222222222';
const productId='33333333-3333-3333-3333-333333333333', oid='44444444-4444-4444-4444-444444444444';
const names={en:['Everyday essentials','The Corner Store','Reusable shopping bag'],ar:['احتياجاتك اليومية','متجر الزاوية','حقيبة تسوق قابلة لإعادة الاستخدام'],it:['Articoli quotidiani','Il negozio all’angolo','Borsa riutilizzabile per la spesa']};
const permissions=['Providers.Read','Providers.Update','Providers.Delete','Services.Read','Services.Create','Services.Update','Services.Delete','Clients.Read','Clients.Update','Roles.Read','Roles.Create','Roles.Delete','Admins.Read','Admins.Create','Admins.Update','Admins.Delete','ProviderCategory.Read','ProviderCategory.Create','ProviderCategory.Update','ProviderCategory.Delete','ProviderProduct.Read','ProviderProduct.Create','ProviderProduct.Update','ProviderProduct.Delete','ProviderOrder.Read','ProviderOrder.Update','ProviderRoles.Read','ProviderRoles.Create','ProviderRoles.Delete','ProviderStaff.Read','ProviderStaff.Create','ProviderStaff.Update','ProviderStaff.Delete'];
permissions.push('Providers.Create');
const server=http.createServer((req,res)=>{
  let file=path.join(root,decodeURIComponent(req.url.split('?')[0]));
  if(!file.startsWith(root)) {res.writeHead(403);res.end();return;}
  if(!fs.existsSync(file)||fs.statSync(file).isDirectory()) file=path.join(root,'index.html');
  const mime={'.js':'text/javascript','.css':'text/css','.html':'text/html','.json':'application/json','.png':'image/png','.woff2':'font/woff2'};
  res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream'});fs.createReadStream(file).pipe(res);
});
(async()=>{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 const base=`http://127.0.0.1:${server.address().port}`;
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 const report=[]; const errors=[];
 try {
  for(const lang of ['en','ar','it']) for(const width of [390,768,1024,1440]) {
   const context=await browser.newContext({viewport:{width,height:1000},reducedMotion:'reduce'});
   await context.addInitScript(({lang,permissions})=>{
    localStorage.setItem('subito.lang',lang);
    if(!localStorage.getItem('qa.anonymous')) for(const portal of ['client','provider','admin']) localStorage.setItem(`subito.${portal}`,JSON.stringify({
     accessToken:'head.'+btoa(JSON.stringify({permission:permissions}))+'.sig',refreshToken:'test',accessTokenExpiresAtUtc:'2099-01-01T00:00:00Z',user:{id:'test',email:'qa@example.com',firstName:'Test',lastName:'User',roles:[],userType:1}
    }));
   },{lang,permissions});
   let quantity=2, cleared=false, failCart=false, failPayment=false;
   await context.route('**/api/v1/**',async route=>{
    const request=route.request(), url=new URL(request.url()); const p=url.pathname.replace('/api/v1','');
    const l=request.headers()['accept-language']||lang, n=names[l]||names.en;
    const service={id:sid,name:n[0],description:n[0],imageUrl:'/assets/logo-mark.png',code:'test',displayOrder:1,isActive:true};
    const store={id:pid,companyName:n[1],serviceId:sid,serviceName:n[0],phoneNumber:'+39 123 456 789',imageUrl:'/assets/logo.png',isActive:true,email:'store@example.com',firstName:'Store',lastName:'Owner'};
    const product={id:productId,providerId:pid,categoryId:sid,name:n[2],description:n[2],price:12.5,imageUrl:'/assets/logo-mark.png',sku:'BAG-01',status:1};
    const cart={id:'cart',providerId:pid,providerName:n[1],items:cleared?[]:[{id:'item',productId,productName:n[2],productImage:'/assets/logo-mark.png',quantity,price:12.5}],totalPrice:quantity*12.5};
    const order={id:oid,providerId:pid,orderDateUtc:'2026-10-01T08:00:00Z',totalAmount:25,status:0,items:[{id:'item',productName:n[2],productId,quantity:2,unitPrice:12.5,lineTotal:25}]};
    const paged=items=>({items,totalCount:items.length,totalPages:1,pageNumber:1});
    let data=[];
    if(failCart && (p.endsWith('/cart')||p.endsWith('/carts'))) return route.fulfill({status:500,json:{success:false,message:'Fixture failure',data:null}});
    if(failPayment && p.includes('/orders/') && (p.includes('session')||p.includes('confirm'))) return route.fulfill({status:404,json:{success:false,message:'Not ready',data:null}});
    if(p.endsWith('/payments')) data={sessionId:'test',url:base+'/mock-stripe',metadata:{}};
    else if(p.endsWith('/cart') && request.method()==='DELETE') {cleared=true;data=null;}
    else if(p.includes('/cart/items')&&request.method()==='PUT') {quantity=JSON.parse(request.postData()).quantity;data=null;}
    else if(p.includes('/cart/items')&&request.method()==='DELETE') {cleared=true;data=null;}
    else if(p.endsWith('/cart')) data=cart;
    else if(p.endsWith('/carts')) data=cleared?[]:[cart];
    else if(p.includes('unread')) data={unreadCount:2};
    else if(p.endsWith('/notifications')) data=[{id:'notice',title:n[1],body:n[0],isRead:false,notificationType:'new_provider_registration',createdAtUtc:'2026-10-01T08:00:00Z'}];
    else if(p.endsWith('/profile')) data={id:'qa',email:'qa@example.com',firstName:'Test',lastName:'User'};
    else if(p==='/provider/store') data=store;
    else if(p.includes('/orders/')) data=order;
    else if(p.endsWith('/orders')) data=paged([order]);
    else if(p.endsWith('/permissions')) data=[{module:'Catalog',moduleLabel:n[0],permissions:[{name:permissions[0],action:'Read'}]}];
    else if(p.endsWith('/roles')) data=paged([{id:'role',name:'Manager',usersCount:2,isSystem:false,permissions}]);
    else if(p.endsWith('/staff')||p.endsWith('/users')||p.endsWith('/clients')) data=paged([{id:'user',firstName:'Test',lastName:'User',email:'qa@example.com',roleName:'Manager',isActive:true}]);
    else if(p.endsWith('/services')||p==='/admin/services/lookup') data=paged([service]);
    else if(p.endsWith('/providers')) data=paged([store]);
    else if(p==='/client/providers/'+pid||p==='/admin/providers/'+pid) data=store;
    else if(p==='/client/products/'+productId) data=product;
    else if(p.includes('/categories')) data=paged([{id:sid,name:n[0],displayOrder:1,isActive:true}]);
    else if(p.endsWith('/products')) data=paged([product]);
    await route.fulfill({status:200,json:{success:true,statusCode:200,message:'OK',errors:[],data}});
   });
   const page=await context.newPage(); page.on('pageerror',e=>errors.push(`${lang}/${width}: ${e.message}`));
   const routes=['/',`/services/${sid}/providers`,`/stores/${pid}`,`/products/${productId}`,'/cart',`/cart/${pid}`,'/orders',`/orders/${oid}`,'/payment/success?session_id=test','/payment/cancel','/profile','/notifications','/provider','/provider/products','/provider/categories','/provider/orders',`/provider/orders/${oid}`,'/provider/roles','/provider/staff','/provider/store','/provider/profile','/provider/notifications','/admin','/admin/providers',`/admin/providers/${pid}`,'/admin/services','/admin/clients','/admin/roles','/admin/users','/admin/profile','/admin/notifications'];
   for(const route of routes) {
    await page.goto(base+route); await page.waitForFunction(()=>document.querySelector('h1') && !document.body.innerText.includes('dashboard.providerTitle')); await page.waitForTimeout(140);
    const metrics=await page.evaluate(()=>({overflow:document.documentElement.scrollWidth>innerWidth+1,dir:document.documentElement.dir,labels:[...document.querySelectorAll('.field input:not([type=file]),.field select,.field textarea')].filter(el=>!el.labels?.length&&!el.getAttribute('aria-label')).length,icons:document.querySelectorAll('app-icon').length}));
    if(metrics.overflow) {
      await page.screenshot({path:path.join(artifacts,'overflow.png'),fullPage:true});
      console.log(await page.evaluate(()=>[...document.querySelectorAll('body *')].filter(el=>el.getBoundingClientRect().right>innerWidth+1).slice(0,15).map(el=>({tag:el.tagName,cls:el.className,width:el.getBoundingClientRect().width,right:el.getBoundingClientRect().right}))));
    }
    assert.equal(metrics.overflow,false,`Overflow: ${lang} ${width} ${route}`);
    assert.equal(metrics.dir,lang==='ar'?'rtl':'ltr'); assert.equal(metrics.labels,0,`Missing labels: ${route}`);
    if(lang==='ar') for(const icon of await page.locator('app-icon.directional').all()) assert.notEqual(await icon.evaluate(el=>getComputedStyle(el).transform),'none');
    report.push({lang,width,route,...metrics});
    if(['/',`/stores/${pid}`,`/cart/${pid}`,'/provider/products','/admin/providers'].includes(route)) await page.screenshot({path:path.join(artifacts,`${lang}-${width}-${route==='/'?'home':route.split('/')[1]+(route.split('/')[2]==='products'?'-products':'')}.png`),fullPage:true});
    if(['/provider/products','/provider/categories','/provider/roles','/provider/staff','/admin/services','/admin/roles','/admin/users','/admin/providers'].includes(route)) {
     await page.locator('.toolbar button').last().click(); await page.locator('form').waitFor();
     assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1),false,`Form overflow: ${route}`);
     assert.equal(await page.evaluate(()=>[...document.querySelectorAll('form input:not([type=checkbox]),form select,form textarea')].filter(el=>!el.labels?.length&&!el.getAttribute('aria-label')).length),0,`Form labels: ${route}`);
    }
   }
   if(lang==='en'&&width===1440) {
    await page.goto(base+'/'); await page.locator('#global-search').fill('essentials'); await page.waitForURL('**/?q=essentials');
    await page.locator('#global-search').fill(''); await page.waitForURL(base+'/');
    await page.goto(base+`/cart/${pid}`); await page.getByRole('button',{name:'Checkout',exact:true}).click(); await page.waitForURL(base+'/mock-stripe');
    await page.goto(base+`/cart/${pid}`); await page.getByRole('button',{name:'Increase quantity'}).click(); await page.waitForFunction(()=>document.querySelector('.qty__value')?.textContent==='3');
    const increase=page.getByRole('button',{name:'Increase quantity'}); await increase.focus(); assert.equal(await increase.getAttribute('data-tooltip'),'Increase quantity');
    assert.ok((await increase.boundingBox()).width>=44); await increase.blur();
    await page.getByRole('button',{name:'Clear basket'}).click(); await page.getByRole('dialog').waitFor();
    assert.equal(await page.evaluate(()=>document.activeElement?.textContent.trim()),'Cancel');
    await page.keyboard.press('Tab'); assert.match(await page.evaluate(()=>document.activeElement.textContent),/Delete/);
    await page.keyboard.press('Tab'); assert.equal(await page.evaluate(()=>document.activeElement.textContent.trim()),'Cancel');
    await page.keyboard.press('Escape'); await page.getByRole('dialog').waitFor({state:'hidden'});
    assert.equal(await page.evaluate(()=>document.activeElement.textContent.trim()),'Clear basket');
    await page.getByRole('button',{name:'Clear basket'}).click(); await page.getByRole('dialog').getByRole('button',{name:'Delete',exact:true}).click(); await page.getByText('Your cart is empty').waitFor();
    failCart=true; await page.goto(base+'/cart'); await page.getByRole('button',{name:'Try again'}).waitFor(); failCart=false; await page.getByRole('button',{name:'Try again'}).click();
    await page.goto(base+'/payment/success'); await page.getByText('We couldn’t confirm your order yet').waitFor();
    failPayment=true; await page.goto(base+'/payment/success?session_id=test'); await page.getByText('We couldn’t confirm your order yet').waitFor();
    failPayment=false; await page.getByRole('button',{name:'Try again'}).click(); await page.getByRole('link',{name:'View order'}).waitFor();
   }
   await page.evaluate(()=>{localStorage.setItem('qa.anonymous','true');for(const p of ['client','provider','admin'])localStorage.removeItem(`subito.${p}`);});
   for(const route of ['/auth/login','/auth/register','/provider/login','/provider/forgot-password','/admin/login','/admin/forgot-password']) {
    await page.goto(base+route); await page.locator('h1').waitFor(); await page.waitForTimeout(100);
    assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1),false,`Auth overflow: ${lang} ${width} ${route}`);
    assert.equal(await page.evaluate(()=>[...document.querySelectorAll('input')].filter(el=>!el.labels?.length).length),0,`Auth labels: ${route}`);
    report.push({lang,width,route});
   }
   await context.close(); console.log(`Checked ${lang} at ${width}px`);
  }
  assert.deepEqual(errors,[]); fs.writeFileSync(path.join(artifacts,'report.json'),JSON.stringify({checks:report.length,errors,report},null,2));
  console.log(`PASS: ${report.length} route/language/viewport checks, no page errors, basket mutations and dialog keyboard checks.`);
 } finally {await browser.close();server.close();}
})().catch(e=>{console.error(e);server.close();process.exitCode=1;});
