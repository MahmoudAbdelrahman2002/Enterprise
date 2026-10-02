# الإشعارات — دليل المطوّر

هذا المستند يشرح **أساس نظام الإشعارات القابل لإعادة الاستخدام** في المشروع، وكيف تضيف أحداث إشعار جديدة بدون تغيير واجهات القائمة / العدّاد.

النسخة الإنجليزية: [NOTIFICATIONS.md](NOTIFICATIONS.md)

## الفكرة العامة

| الجزء | دوره |
|-------|------|
| جدول `Notifications` | عنصر في صندوق الوارد لـ **يوزر واحد** (عنوان، نص، نوع، أيدي مرتبط، حالة القراءة) |
| جدول `DeviceTokens` | توكنات تسجيل FCM لأجهزة نفس اليوزر |
| `INotificationService` | يحفظ صفوف الوارد، ثم يحاول الإرسال عبر Firebase |
| APIs البوابات | قائمة / عدد غير المقروء / تسجيل-إلغاء توكن الجهاز |

**الرابط بين الجدولين:** الاتنين بيستخدموا **Guid بتاع اليوزر** من Identity.

- `Notification.RecipientUserId` = مين يشوف الإشعار  
- `DeviceToken.UserId` = أنهي موبايل/متصفح نبعتله الـ Push  

**مفيش** Foreign Key من Notification إلى DeviceToken. إشعار واحد ممكن يتبعت لكل أجهزة نفس اليوزر.

```text
ApplicationUser.Id
        │
        ├── Notification.RecipientUserId
        └── DeviceToken.UserId
```

## أعمدة جدول الإشعار

| العمود | المعنى |
|--------|--------|
| `Id` | Guid بتاع صف الإشعار نفسه |
| `RecipientUserId` | مين يستلم (Guid اليوزر) — **وكمان بنستخدمه عشان نلاقي الـ DeviceTokens** |
| `RecipientUserType` | Client / Admin / Provider (أمان وتصفية) |
| `Title` / `Body` | النص الظاهر (وكمان في شريط إشعار FCM) |
| `NotificationType` | فئة الـ deep link، مثلاً `new_provider_registration` |
| `RelatedEntityId` | Guid الكيان المرتبط (في الـ API اسمه `notificationId`) |
| `IsRead` | للـ badge؛ endpoint القائمة يعلّم الكل مقروء |
| `CreatedAtUtc` | للترتيب |

**متخلطش:**

- `RecipientUserId` → **الشخص**  
- `notificationId` / `RelatedEntityId` → **كيان الشاشة** (بروفايدر، أوردر، …)

## النوع المفعّل حالياً

معرّف في `Enterprise.Application/Features/Notifications/NotificationTypes.cs`:

| الثابت | القيمة | مربوط؟ | الـ Deep link |
|--------|--------|--------|----------------|
| `NewProviderRegistration` | `new_provider_registration` | نعم — إنشاء بروفايدر | إدارة البروفايدرز / تفاصيل البروفايدر في الأدمن |

## إزاي `INotificationService` بيشتغل

```csharp
await notificationService.NotifyAsync(
    recipientUserId,
    UserType.Admin,           // أو Client / Provider
    "Title",
    "Body text",
    NotificationTypes.NewProviderRegistration,
    relatedEntityId: provider.Id,
    cancellationToken);

// أو إرسال لناس كتير:
await notificationService.NotifyManyAsync(userIds, UserType.Admin, ...);
```

خطوات جوّا الخدمة:

1. إدراج صف/صفوف `Notification` ثم `SaveChanges`
2. تحميل `DeviceToken`s لليوزرز دول
3. إرسال FCM (عنوان/نص + data payload)
4. حذف التوكنات الباطلة
5. لو Firebase مش متظبط → لوج تحذير و**نحتفظ بصفوف الداتابيس** (مناسب للتطوير المحلي)

مفاتيح بيانات FCM:

| المفتاح | القيمة |
|---------|--------|
| `notification_id` | Guid صف الوارد |
| `notification_type` | مثلاً `new_provider_registration` |
| `related_id` | Guid الكيان المرتبط (أو فاضي) |
| `user_type` | `Admin` / `Client` / `Provider` |

**هاندلرز البيزنس لازم ما تفشلش** لو الـ push فشل. Create Provider ملفوف الـ notify في try/catch كحماية إضافية.

## عقد الـ API (كل البوابات)

نفس الـ handlers؛ الفرق في سياسة الـ auth (`RequireAdmin` / `RequireClient` / `RequireProvider`).

| Method | Path | السلوك |
|--------|------|--------|
| GET | `/api/v1/{portal}/notifications` | قائمة من الأحدث، ثم **تعليم كل الغير مقروء كمقروء** |
| GET | `/api/v1/{portal}/notifications/count` | `{ unreadCount }` — من غير تعليم مقروء |
| POST | `/api/v1/{portal}/device-tokens` | Upsert `{ token, platform? }` |
| DELETE | `/api/v1/{portal}/device-tokens` | حذف `{ token }` |

شكل عنصر القائمة:

```json
{
  "id": "...",
  "title": "New provider registered",
  "body": "\"Demo Restaurant\" was added to the marketplace.",
  "notificationType": "new_provider_registration",
  "notificationId": "provider-guid-here",
  "isRead": true,
  "createdAtUtc": "..."
}
```

الفرونت مسؤول عن التوجيه:

```text
switch (notificationType) {
  case "new_provider_registration":
    open admin provider details / management(notificationId)
}
```

## إعداد Firebase

في `appsettings.json` / Development:

```json
"Firebase": {
  "CredentialsPath": "",
  "CredentialsJson": ""
}
```

حط إما مسار ملف service-account JSON، أو الـ JSON نفسه عبر user-secrets / env (`Firebase__CredentialsJson`). لو الاتنين فاضي، الإشعارات تتحفظ في الداتابيس والـ FCM بيتعدّى.

## مثال شغّال (متنفّذ فعلاً)

**الـ Trigger:** `CreateProviderCommandHandler` بعد حفظ صف البروفايدر.

**المستلمون:** كل يوزرز الـ Admin الـ **active** **ماعدا** الأدمن اللي عمل الإنشاء (`ICurrentUserService.UserId`).

**الـ Payload:**

- النوع: `new_provider_registration`
- الأيدي المرتبط: `provider.Id`
- العنوان/النص: جمل إنجليزي بتوصف اسم الشركة الجديدة

## وصفة: إضافة إشعار مستقبلي

**مش محتاج** جداول جديدة ولا endpoints جديدة للقائمة/العدّاد.

1. **ضيف ثابت نوع** (لو جديد) في `NotificationTypes.cs`:

   ```csharp
   public const string Order = "order";
   ```

2. **بعد حفظ البيزنس بنجاح**، نادي الخدمة:

   ```csharp
   // مثال — تغيير حالة الأوردر → إشعار للعميل (لسه مش مربوط)
   await notificationService.NotifyAsync(
       order.UserId,
       UserType.Client,
       "Order updated",
       $"Your order is now {order.Status}.",
       NotificationTypes.Order,
       order.Id,
       cancellationToken);
   ```

3. **إرسال لناس كتير (fan-out)** لما أكتر من شخص محتاج يعرف:

   ```csharp
   var adminIds = await userAccountService.GetActiveUserIdsByTypeAsync(UserType.Admin, ct);
   await notificationService.NotifyManyAsync(adminIds, UserType.Admin, ...);
   ```

   لمستخدمي البروفايدر: جيب أيدي المالك + الستاف المرتبطين بـ `ProviderId`، بعدين `NotifyManyAsync`.

4. **بلّغ الفرونت** بخريطة الـ routes للـ `notificationType` الجديد.

5. اختياري: لفّ نداء الـ notify في try/catch عشان باج الإشعار ما يرجعش ترانزاكشن البيزنس (الخدمة أصلاً بتعزل أخطاء FCM).

## Helper موجود جاهز

`IUserAccountService.GetActiveUserIdsByTypeAsync(UserType)` — بيرجع Guid اليوزرز الـ active لبوابة معيّنة (مستخدم لـ fan-out الأدمنز).

## خريطة الملفات

| الطبقة | المكان |
|--------|--------|
| Entities | `Enterprise.Domain/Entities/Notification.cs`, `DeviceToken.cs` |
| Repos | `INotificationRepository`, `IDeviceTokenRepository` على `IUnitOfWork` |
| Service | `INotificationService` → `Enterprise.Infrastructure/Notifications/NotificationService.cs` |
| Types | `Enterprise.Application/Features/Notifications/NotificationTypes.cs` |
| MediatR | `Enterprise.Application/Features/Notifications/...` |
| Controllers | `.../Controllers/V1/{Admin\|Client\|Provider}/*NotificationsController.cs` |
| Trigger | `CreateProviderCommandHandler` |

## برا النطاق (حالياً)

- تريجرز حالة الأوردر / أوردر جديد / تسجيل عميل  
- تعليم إشعار واحد بس كمقروء  
- SignalR أو إيميل لنفس الحدث  
