# Notifications — guide for a new developer

Arabic version: [NOTIFICATIONS.ar.md](NOTIFICATIONS.ar.md)

This page explains the notification code that already exists, and how you add the next event without building a new inbox.

Read it in order. You do not need Firebase experience first.

---

## 1. What problem this solves

The API has three portals:

- **Client** — people who place orders
- **Provider** — stores that fulfill orders
- **Admin** — people who manage the platform

When something important happens, the right user should:

1. See a number on the bell icon
2. Open a list of notifications
3. Tap one item and land on the correct screen
4. Optionally get a phone or browser alert even if the app is closed

The backend stores the message and tells the frontend **which screen to open**. The frontend does the navigation.

---

## 2. Two channels, one message

Every notification uses two channels:

| Channel | What it is | Why it exists |
|---------|------------|----------------|
| Database (`Notifications` table) | The inbox row | History, list, read/unread, badge count |
| FCM (Firebase Cloud Messaging) | A push to the device | Instant alert when the app is in the background or closed |

**Rule:** save the row first, then try to push. If push fails, the business action (for example creating a provider) still succeeds. The user can still see the message when they open the app.

**FCM in one sentence:** our API asks Firebase to deliver a message to a device token. Firebase shows it on the phone or browser.

Each app install gets a long string called a **device token**. We store that token next to the logged-in user so we know where to push.

---

## 3. Two tables, linked only by user id

### `Notifications` — the inbox item

One row = one message for **one user**.

| Column | Plain meaning | Example |
|--------|----------------|---------|
| `Id` | This notification's own id | a Guid |
| `RecipientUserId` | **Who** should see it | the admin user's Guid |
| `RecipientUserType` | Which portal that person belongs to | `Admin` |
| `Title` | Short headline | `New provider registered` |
| `Body` | Longer text | `"Demo Restaurant" was added to the marketplace.` |
| `NotificationType` | **What kind of screen** to open | `new_provider_registration` |
| `RelatedEntityId` | **Which record** to open. The API calls this `notificationId` | the provider's Guid |
| `IsRead` | Has the inbox been opened? | `false`, then `true` |
| `CreatedAtUtc` | When it was created | used to sort newest first |

Two ids look similar. They are not the same:

- `RecipientUserId` = the **person**
- `notificationId` / `RelatedEntityId` = the **page** (provider, order, and so on)

### `DeviceTokens` — where to push

| Column | Plain meaning |
|--------|----------------|
| `UserId` | Owner of the device (same Guid as `RecipientUserId`) |
| `Token` | FCM registration token from the app |
| `Platform` | Optional: `android`, `ios`, or `web` |

One user can have many devices.

There is **no foreign key** from a notification to a device token. When we send, we look up tokens with:

```text
DeviceToken.UserId == Notification.RecipientUserId
```

```text
ApplicationUser.Id
        │
        ├── Notification.RecipientUserId   (who reads the inbox)
        └── DeviceToken.UserId             (which phones get the push)
```

If the user has no device token, the inbox row is still saved. They just do not get a push.

---

## 4. What is already built

| Piece | Where | What you use it for |
|-------|--------|---------------------|
| Entities | `src/Enterprise.Domain/Entities/Notification.cs`, `DeviceToken.cs` | The tables |
| Repositories | `INotificationRepository`, `IDeviceTokenRepository` on `IUnitOfWork` | Queries |
| Service | `INotificationService` in Application, implemented by `NotificationService` | The only API business code should call |
| Type names | `src/Enterprise.Application/Features/Notifications/NotificationTypes.cs` | String constants such as `new_provider_registration` |
| Inbox APIs | Controllers under `Controllers/V1/Admin`, `Client`, and `Provider` | List, count, device tokens |
| First real event | `CreateProviderCommandHandler` | Notifies other admins |

Client and Provider have the same endpoints. Their inboxes stay empty until you wire an event that targets those users.

---

## 5. How sending works (`INotificationService`)

Business handlers do **not** talk to Firebase. They call one service.

One person:

```csharp
await notificationService.NotifyAsync(
    recipientUserId,                         // who
    UserType.Admin,                          // their portal
    "New provider registered",               // title
    "\"Demo Restaurant\" was added.",        // body
    NotificationTypes.NewProviderRegistration,
    relatedEntityId: provider.Id,            // becomes notificationId
    cancellationToken);
```

Many people (same title, body, and type):

```csharp
await notificationService.NotifyManyAsync(
    recipientUserIds,
    UserType.Admin,
    title,
    body,
    NotificationTypes.NewProviderRegistration,
    provider.Id,
    cancellationToken);
```

Inside `NotificationService` the order is always:

1. Insert one `Notification` row per recipient and save
2. Load `DeviceToken` rows for those user ids
3. Send FCM (title, body, and data)
4. Delete a token if Firebase says it is invalid
5. If Firebase is not configured, log a warning and **keep the database rows**

FCM data (the app uses this when the user taps the system notification):

| Key | Value |
|-----|--------|
| `notification_id` | The inbox row Guid |
| `notification_type` | For example `new_provider_registration` |
| `related_id` | The related entity Guid, or empty |
| `user_type` | `Admin`, `Client`, or `Provider` |

Create Provider also wraps the notify call in `try/catch`, so a notification bug cannot undo a provider that was already saved.

---

## 6. The HTTP API

`{portal}` is `admin`, `client`, or `provider`. Auth is `RequireAdmin`, `RequireClient`, or `RequireProvider`. The handlers are shared. The current user always comes from the JWT. Never accept a user id from the client for list or count.

| Method | Path | What happens |
|--------|------|----------------|
| GET | `/api/v1/{portal}/notifications` | Newest first. Then **every unread row for this user becomes read**. |
| GET | `/api/v1/{portal}/notifications/count` | `{ "unreadCount": 3 }`. Does **not** mark anything read. Use this for the bell. |
| POST | `/api/v1/{portal}/device-tokens` | Save or update `{ "token": "...", "platform": "android" }` after login |
| DELETE | `/api/v1/{portal}/device-tokens` | Remove `{ "token": "..." }` on logout |

A list item looks like this:

```json
{
  "id": "notification-row-guid",
  "title": "New provider registered",
  "body": "\"Demo Restaurant\" was added to the marketplace.",
  "notificationType": "new_provider_registration",
  "notificationId": "provider-guid",
  "isRead": true,
  "createdAtUtc": "2026-09-25T12:00:00Z"
}
```

The frontend routes from those two fields:

```text
if notificationType == "new_provider_registration":
    open the admin provider page for notificationId
```

The backend does not redirect.

---

## 7. Walkthrough: the one event that is wired today

**Event:** an admin creates a provider.

**Code:** `CreateProviderCommandHandler`, after the provider row is saved.

**Who is notified:** every **active** admin **except** the admin who clicked create (`ICurrentUserService.UserId`).

**How recipients are found:**

```csharp
var adminIds = await userAccountService.GetActiveUserIdsByTypeAsync(UserType.Admin, cancellationToken);
```

**What is stored:**

- `notificationType` = `new_provider_registration`
- `notificationId` = `provider.Id`
- Title: `New provider registered`
- Body: the company name

**What the other admin sees:**

1. Bell count goes up (`GET .../admin/notifications/count`)
2. They open the list (`GET .../admin/notifications`) and every item is marked read
3. They tap the row. The admin app opens provider management using `notificationId`
4. If their browser or phone registered a device token, they also get an FCM push with the same type and id

---

## 8. How to add the next notification

You do **not** add a new table or a new list endpoint.

### Step 1 — name the type

In `NotificationTypes.cs`:

```csharp
public const string Order = "order";
```

Use a short stable string. The frontend will switch on it forever.

### Step 2 — call the service after a successful save

Example that is **not implemented yet** (order status → the client):

```csharp
await notificationService.NotifyAsync(
    order.UserId,
    UserType.Client,
    "Order updated",
    $"Your order is now {order.Status}.",
    NotificationTypes.Order,
    order.Id,
    cancellationToken);
```

If many users should hear about it:

```csharp
await notificationService.NotifyManyAsync(userIds, UserType.Provider, title, body, type, relatedId, cancellationToken);
```

For a whole portal (all admins), `GetActiveUserIdsByTypeAsync` already exists on `IUserAccountService`.

For a store, collect the owner user id and the staff user ids that belong to that `ProviderId`, then call `NotifyManyAsync`.

### Step 3 — keep the business action safe

Wrap the notify call in `try/catch` and log the error, the same way `CreateProviderCommandHandler` does. A failed notification must not roll back the order or the provider.

### Step 4 — tell the frontend

Give them the new `notificationType` and which screen `notificationId` opens. Example: `order` + order Guid → order details.

---

## 9. Firebase setup (optional for local work)

In `appsettings.json` or `appsettings.Development.json`:

```json
"Firebase": {
  "CredentialsPath": "",
  "CredentialsJson": ""
}
```

Fill **one** of them with a Firebase service-account file path, or the JSON itself via user-secrets / environment variable `Firebase__CredentialsJson`.

If both are empty, inbox rows are still saved and push is skipped. That is the expected local setup until you have a Firebase project.

---

## 10. Checklist before you open a pull request

- The handler calls `INotificationService` only after the business save succeeded
- Recipients are real user Guids, not provider ids or order ids
- `NotificationType` is a constant, not a new magic string in the handler
- `relatedEntityId` is the id the screen needs
- Push failure cannot fail the original command
- The frontend knows the route for the new type

## Not built yet

- Order status, new order, or client registration triggers
- Marking a single notification as read (opening the list marks **all**)
- SignalR, or an email copy of the same event
