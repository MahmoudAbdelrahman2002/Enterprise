# تدفق الدفع بـ Stripe (ادفع أولاً → ثم أنشئ الأوردر)

## Current implementation — 4 October 2026

`ICheckoutGateway` uses an explicitly configured Stripe client. Missing checkout configuration or rejected creation returns a localized HTTP 503 and retains the basket. Development inherits the configured test secret instead of overriding it with an empty value. Identical basket/redirect/locale parameters generate the same idempotency key. Stripe line-item product metadata includes the internal product ID.

Fulfillment accepts a paid session through a verified signed completed/async-success webhook or authenticated client confirmation that retrieves the session from Stripe and checks ownership. It retrieves all session line items with expanded products, checks their quantities, metadata, unit prices and total, and writes immutable order snapshots. It consumes paid quantities in the same transaction, preserves later basket additions and deletes only an empty basket. The checkout-session unique key prevents duplicate orders and repeated provider notifications.

Current development does not configure a webhook signing secret. Set the genuine target endpoint signing secret through environment configuration/user secrets for autonomous delivery. Authenticated return confirmation provides the existing fallback. Never invent a signing secret or treat a simulated webhook as a real paid purchase.

Validation: offline signed-webhook lifecycle tests and a separately selected real Stripe test-mode API checkout test passed; the real test session was expired without submitting payment. Production payments and external webhook delivery still require environment validation.

References: [Stripe idempotent requests](https://docs.stripe.com/api/idempotent_requests), [Checkout line items](https://docs.stripe.com/api/checkout/sessions/line_items), [Checkout fulfillment](https://docs.stripe.com/checkout/fulfillment).


المستند ده بيشرح **بالتفصيل** إزاي تـhandle الدفع في المشروع:
السلة موجودة → إنشاء جلسة Stripe → العميل يدفع → Webhook → إنشاء `Order` → مسح السلة.

**القاعدة:** مفيش `Order` قبل تأكيد الدفع من Stripe Webhook.

مرتبط بـ: [`ORDER_BUSINESS_FLOW.md`](ORDER_BUSINESS_FLOW.md)

---

## 1. الصورة الكبيرة

```text
[سلة جاهزة]
      │
      ▼
POST /payments/checkout-session
      │  (يقرأ السلة، يتحقق، ينشئ Stripe Session)
      ▼
الفرونت يفتح session.Url
      │
      ├─ إلغاء ──► CancelUrl ──► السلة زي ما هي
      │
      └─ دفع ناجح
            │
            ▼
Stripe يبعت Webhook: checkout.session.completed
            │
            ▼
الباكند: ينشئ Order + OrderItems من السلة
            │
            ▼
يمسح السلة
            │
            ▼
المزود يشوف الطلب (Paid)
```

| المرحلة | في الداتابيز | في Stripe |
|---------|-------------|-----------|
| قبل الدفع | سلة فقط | Session مفتوحة |
| أثناء الدفع | سلة فقط | Session / Payment |
| بعد Webhook ناجح | Order + Items، السلة اتمسحت | Session مكتملة |

---

## 2. المتطلبات مرة واحدة (Setup)

1. حساب Stripe (Test mode للتجربة).
2. مفاتيح:
   - `SecretKey` → باكند فقط
   - `PublishableKey` → فرونت فقط (لو محتاج)
   - `WebhookSecret` → باكند للتحقق من التوقيع
3. باكدج: `Stripe.net` في مشروع **Infrastructure** فقط.
4. إعدادات مثال:

```json
"Stripe": {
  "SecretKey": "sk_test_...",
  "PublishableKey": "pk_test_...",
  "WebhookSecret": "whsec_...",
  "Currency": "usd",
  "SuccessUrl": "http://localhost:4200/payment/success?session_id={CHECKOUT_SESSION_ID}",
  "CancelUrl": "http://localhost:4200/payment/cancel"
}
```

5. في DI:

```csharp
StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
services.AddScoped<ICheckoutGateway, StripePaymentService>();
```

6. Clean Architecture:
   - Application: `ICheckoutGateway` + Commands
   - Infrastructure: `StripePaymentService`
   - Api: Controllers (checkout + webhook)

---

## 3. الخطوة أ — العميل يجهّز السلة (موجود)

1. Login كـ Client.
2. إضافة منتجات: `POST /api/v1/client/{providerId}/cart/items`
3. مراجعة: `GET /api/v1/client/{providerId}/cart`
4. السلة فيها: `UserId`, `ProviderId`, `Items (ProductId + Quantity)`.

لسه مفيش دفع ولا أوردر.

---

## 4. الخطوة ب — إنشاء جلسة الدفع (Checkout Session)

### 4.1 Endpoint

```http
POST /api/v1/client/{providerId}/payments/checkout-session
Authorization: Bearer <client-token>
```

**مش** بينشئ Order. بيرجع بس رابط الدفع.

### 4.2 Response مثال

```json
{
  "sessionId": "cs_test_...",
  "url": "https://checkout.stripe.com/c/pay/cs_test_..."
}
```

الفرونت:

```text
redirect → url
```

### 4.3 منطق الـ Handler بالتفصيل

1. **Authentication**
   - `userId = currentUserService.UserId`
   - لو null → 401

2. **Load cart**
   - `GetCartByProviderIdAndUserIdAsync(providerId, userId)`
   - Include: Items + Product

3. **Validation**
   - السلة موجودة وفيها عناصر
   - كل منتج `Active`
   - كل منتج تابع لنفس المزود
   - الكمية > 0

4. **Build line items**
   - لكل cart item:
     - Name من ترجمة المنتج
     - UnitPrice من `product.Price`
     - Quantity من السطر
   - Stripe يحتاج المبلغ بالوحدة الصغرى:
     - `14.00` → `1400` (cents)

5. **Create Stripe Session عبر ICheckoutGateway**
   - `Mode = payment`
   - `LineItems = ...`
   - `SuccessUrl / CancelUrl`
   - `Metadata`:
     - `userId`
     - `providerId`
     - `cartId`
   - Metadata ضرورية عشان الـ Webhook يعرف أنهي سلة يحوّلها لأوردر

6. **Return**
   - `sessionId` + `url`
   - **لا Create Order هنا**
   - **لا Clear Cart هنا**

### 4.4 حالة النظام بعد الخطوة ب

- DB: السلة موجودة
- Stripe: Session مفتوحة
- Order: غير موجود

---

## 5. الخطوة ج — العميل يدفع عند Stripe

1. يفتح صفحة Stripe Checkout.
2. يدخل بيانات الكارت (Test: `4242 4242 4242 4242`).
3. نتيجتين:
   - **نجاح** → Redirect إلى SuccessUrl ومعاها `session_id`
   - **إلغاء** → CancelUrl والسلة تفضل زي ما هي

### تحذير مهم

SuccessUrl **مش** دليل نهائي إن الدفع اتحسب.
المرجع الصح = **Webhook**.

---

## 6. الخطوة د — Webhook (تأكيد الدفع)

### 6.1 Endpoint

```http
POST /api/v1/webhooks/stripe
```

- بدون JWT
- يقرأ **raw body**
- يتحقق من `Stripe-Signature` بـ `WebhookSecret`

### 6.2 خطوات الـ Controller

1. اقرأ الطلب كـ string (raw).
2. خد header: `Stripe-Signature`.
3. `EventUtility.ConstructEvent(json, signature, webhookSecret)`.
4. لو التوقيع غلط → 400.
5. لو الحدث:

| Event | التصرف |
|-------|--------|
| `checkout.session.completed` | أكمل إنشاء الأوردر |
| غير ذلك | تجاهل / 200 |

6. استخرج من Session:
   - `session.Id`
   - `session.PaymentStatus` لازم `paid`
   - `metadata.userId`
   - `metadata.providerId`
   - `metadata.cartId`
   - `amount_total` (اختياري للمراجعة)

7. نادِ Command داخلي: مثلًا `CompletePaidCheckoutCommand`.

8. رجّع `200 OK` لـ Stripe  
   (لو فشلت، Stripe هيعيد الإرسال).

### 6.3 اختبار محلي

```bash
stripe listen --forward-to https://localhost:<port>/api/v1/webhooks/stripe
```

---

## 7. الخطوة هـ — إنشاء الأوردر بعد الدفع الناجح

ده قلب البيزنس. يتنفّذ داخل `CompletePaidCheckoutCommand`
(وممكن يعيد استخدام نفس منطق `CreateOrder` داخليًا).

### 7.1 Idempotency (مهم جدًا)

الـ Webhook ممكن يتبعت أكتر من مرة.

```text
لو Order موجود بنفس StripeCheckoutSessionId
→ رجّع نجاح بدون إنشاء Order جديد
```

لذلك خزّن على Order مثلًا:

- `StripeCheckoutSessionId`
- `PaymentStatus = Paid`
- `PaidAtUtc`

### 7.2 Read the paid session and basket identity

Validate cart/user/provider metadata. A remaining basket must match that identity. A basket removed during payment does not erase the paid purchase.

### 7.3 Read immutable paid line items

Fetch all Stripe session line items with `data.price.product` expanded. Validate the internal product metadata, quantities, prices and summed total. Create the order from those paid values rather than re-reading mutable catalogue prices or basket quantities.

### 7.4 Create Order (لقطة / Snapshot)

```text
Order
  UserId
  ProviderId
  Status = New              // جاهز للمزود
  PaymentStatus = Paid
  TotalAmount
  StripeCheckoutSessionId
  OrderDateUtc = UtcNow
  Notes? (لو خزّنتها قبل الدفع)

OrderItem (لكل سطر سلة)
  ProductId
  ProductName   // نسخة وقت الشراء
  UnitPrice     // نسخة وقت الشراء
  Quantity
```

**مفيش FK بين Order و ShoppingCart.**

### 7.5 Consume paid basket quantities

Subtract only the quantities represented by the paid session. Preserve extra quantities/products added during checkout and delete the basket only when it is empty. This runs in the same database transaction as order persistence.

### 7.6 بعد النجاح

- العميل يقدر يشوف الأوردر
- المزود يشوف الطلب المدفوع ويبدأ التنفيذ

---

## 8. الخطوة و — الفرونت بعد الرجوع من Stripe

على Success page:

1. اقرأ `session_id` من Query.
2. نادِ:

```http
GET /api/v1/client/orders/by-session/{sessionId}
```

3. حالات:
   - الأوردر موجود → اعرض التفاصيل
   - لسه مش موجود (webhook متأخر) → polling قصير أو زر Refresh
   - اختياري: endpoint مزامنة يستعلم Stripe ويعمل Complete لو الدفع paid والأوردر ناقص

---

## 9. Endpoints النهائية المقترحة

| Method | Path | من؟ | الوظيفة |
|--------|------|-----|---------|
| POST | `/api/v1/client/{providerId}/payments/checkout-session` | Client | إنشاء جلسة دفع من السلة |
| POST | `/api/v1/webhooks/stripe` | Stripe | تأكيد الدفع وإنشاء الأوردر |
| GET | `/api/v1/client/orders/by-session/{sessionId}` | Client | جلب الأوردر بعد الدفع |
| GET | `/api/v1/client/orders` | Client | قائمة أوردرات العميل |
| GET | `/api/v1/client/orders/{orderId}` | Client | تفاصيل أوردر |
| GET | `/api/v1/provider/orders` | Provider | طلبات المطعم |
| PATCH | `/api/v1/provider/orders/{orderId}/status` | Provider | تحديث حالة التحضير |

---

## 10. توزيع الكود في الحل

| Layer | ماذا تضع |
|-------|----------|
| Domain | `Order`, `OrderItem`, `PaymentStatus` |
| Application | `ICheckoutGateway`, `CreateCheckoutSessionCommand`, `CompletePaidCheckoutCommand` |
| Infrastructure | `StripePaymentService`, EF configs, migration |
| Api | `ClientPaymentsController`, `StripeWebhooksController` |

`CreateOrderCommand` = منطق تحويل السلة → أوردر  
ويُستدعى من **CompletePaidCheckout** بعد الدفع، مش من الكلاينت مباشرة.

---

## 11. حالات الفشل وكيف تتعامل معاها

| الحالة | التصرف |
|--------|--------|
| سلة فاضية عند إنشاء Session | 400 / validation error |
| منتج غير متاح | ارفض إنشاء Session |
| العميل لغى الدفع | السلة تفضل؛ مفيش Order |
| Webhook بتوقيع غلط | 400 |
| Webhook مكرر | Idempotency بالـ sessionId |
| Webhook نجح والسلة اتمسحت قبل الإنشاء | لو Order موجود OK؛ غير كده تحقيق |
| SuccessUrl فتح والـ webhook متأخر | polling / sync endpoint |

---

## 12. مثال end-to-end

1. Client يضيف 2× Pepperoni (سعر 14.00) → إجمالي السلة 28.00  
2. `POST .../payments/checkout-session`  
3. Stripe Session بمبلغ `2800` cents + metadata (`userId`, `providerId`, `cartId`)  
4. Client يدفع بنجاح  
5. Webhook `checkout.session.completed`  
6. Backend ينشئ:
   - Order (`New`, `Paid`, Total 28.00)
   - OrderItem (Pepperoni, 14.00, qty 2)
7. السلة تتمسح  
8. Provider يشوف الطلب ويغيّر الحالة: New → Preparing → Ready

---

## 13. Checklist تنفيذ

- [ ] Stripe settings + ApiKey في DI
- [ ] `ICheckoutGateway` + `StripePaymentService`
- [ ] `POST checkout-session` من السلة (من غير Order)
- [ ] حقول الدفع على Order (`StripeCheckoutSessionId`, `PaymentStatus`, ...)
- [ ] Webhook + التحقق من التوقيع
- [ ] `CompletePaidCheckout` + Idempotency
- [ ] إنشاء OrderItems كـ snapshot
- [ ] مسح السلة في نفس الـ transaction
- [ ] `GET order by sessionId`
- [ ] اختبار بـ `stripe listen` + كارت تجريبي

---

## مستندات مرتبطة

- بيزنس الأوردر: [`ORDER_BUSINESS_FLOW.md`](ORDER_BUSINESS_FLOW.md)
- سلة العميل: `ClientCartsController`
- المعمارية: [`DEVELOPER_WORKFLOW.md`](DEVELOPER_WORKFLOW.md)
