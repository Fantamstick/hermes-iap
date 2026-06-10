# Unity 6 移行に伴う IAP 変更点

- 対象ブランチ: `feature/update-unity-to-6`
- Unity: 2021.3.45f2 → Unity 6
- Unity IAP: 4.13.0 → 4.15.0

---

## 変更ファイル

| ファイル | 変更内容 |
|---|---|
| `Assets/Hermes/Runtime/HermesStore.cs` | インターフェース変更・`OnPurchaseFailed` 更新・`RestoreTransactions` コールバック更新 |
| `Assets/Hermes/Runtime/AppleStore/AppStore.cs` | `RefreshAppReceipt` エラーコールバック更新 |

---

## 変更詳細

### 1. `IStoreListener` → `IDetailedStoreListener` への移行（HermesStore.cs）

Unity IAP 4.x で `OnPurchaseFailed(Product, PurchaseFailureReason)` が deprecated になり、より詳細な情報を持つ `IDetailedStoreListener` が推奨された。

**変更前:**
```csharp
public abstract class HermesStore : IStoreListener

void IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
{
    onPurchaseFailureCb?.Invoke(status, failureReason);
}
```

**変更後:**
```csharp
public abstract class HermesStore : IDetailedStoreListener

void IDetailedStoreListener.OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
{
    // failureDescription.reason  : 既存コールバックへそのまま渡す
    // failureDescription.message : ストア固有のエラー詳細文字列（ログ出力に活用）
    onPurchaseFailureCb?.Invoke(status, failureDescription.reason);
}

// IStoreListener.OnPurchaseFailed は interface 要件を満たすためスタブ実装
#pragma warning disable CS0618
void IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) { }
#pragma warning restore CS0618
```

**呼び出し側への影響:** なし。`Init()` に渡す `Action<Status, PurchaseFailureReason>` のシグネチャは変更なし。

---

### 2. `OnInitializeFailed` の obsolete 警告抑制（HermesStore.cs）

Unity IAP 4.x で `OnInitializeFailed(InitializationFailureReason)` が `[Obsolete]` になった。インターフェース実装時に CS0618 警告が発生するため pragma で抑制。

```csharp
#pragma warning disable CS0618
void IStoreListener.OnInitializeFailed(InitializationFailureReason error) { ... }
#pragma warning restore CS0618
```

---

### 3. `RestoreTransactions` コールバック変更（HermesStore.cs）

Unity IAP 4.x で `Action<bool>` が deprecated になり `Action<bool, string>` に変更。第2引数はエラーメッセージ（成功時は `null`）。

**変更前:**
```csharp
void HandleRestoreTransaction(bool result) { ... }
```

**変更後:**
```csharp
void HandleRestoreTransaction(bool result, string error) { ... }
```

iOS・Google Play どちらの `RestoreTransactions` 呼び出しにも適用。

---

### 4. `RefreshAppReceipt` エラーコールバック変更（AppStore.cs・iOS のみ）

Unity IAP 4.x で `Action` が deprecated になり `Action<string>` に変更。引数はエラーメッセージ。

**変更前:**
```csharp
appleExtensions.RefreshAppReceipt(onSuccess, () => {
    DebugLog("Refresh purchases unsuccessful");
    ...
});
```

**変更後:**
```csharp
appleExtensions.RefreshAppReceipt(onSuccess, errorMessage => {
    DebugLog($"Refresh purchases unsuccessful: {errorMessage}");
    ...
});
```
