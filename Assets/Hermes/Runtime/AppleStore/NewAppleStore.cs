using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Hermes;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

public class NewAppleStore : IStoreListener {
    static NewAppleStore instance;
    public static NewAppleStore Instance => instance ??= new NewAppleStore();

    public enum Status {
        Idle,
        Purchase,
        Restore,
        Refresh,
    }
    
    public bool IsInitAndReady => appleExtensions != null;

    Status status = Status.Idle;
    IStoreController controller;
    ConfigurationBuilder builder;
    IAPBuilder iapBuilder;
    IAppleExtensions appleExtensions;
    IAppleConfiguration appleConfiguration;
    byte[] appleTangleData;
    
    // IAP internal callback delegates
    Action onInitSuccessCb;
    Action<InitializationFailureReason> onInitFailureCb;
    Func<Status, Product, PurchaseProcessingResult> onPurchaseSuccessCb;
    Action<Product> onPurchaseDeferredCb;
    Action<Status, PurchaseFailureReason> onPurchaseFailureCb;
    
    /*
     * This value is true if the user can authorize payments in the App Store; otherwise false.
     * The value of canMakePayments is false when users set content and privacy controls to limit a child's ability to purchase content.
     * It can also be false if the device has a mobile device management (MDM) profile that doesn’t allow payments.
     */
    public bool CanMakePayments() {
        if (appleConfiguration == null) {
            DebugLog("Unable to read canMakePayments value. Hermes is not initialized yet.");
            return false;
        }

        return appleConfiguration.canMakePayments;;
    }
    
    // Prevent class from being instanced explicitly outside.
    NewAppleStore() { }

    //*******************************************************************
    // INIT
    //*******************************************************************
    /// <summary>
    /// Initialize Hermes IAP. Call only once in the lifetime of the app.
    /// Initialization may never complete if device is offline.
    /// IAP will continuously retry to initialize.
    /// </summary>
    /// <param name="iapBuilder">Builder data used to create instance.</param>
    /// <param name="onInitSuccess">Callback when initialization is done.</param>
    /// <param name="onInitFailure"></param>
    /// <param name="onPurchaseSuccess"></param>
    /// <param name="onPurchaseDeferred"></param>
    /// <param name="onPurchaseFailure"></param>
    public void Init(
        IAPBuilder iapBuilder, 
        Action onInitSuccess, 
        Action<InitializationFailureReason> onInitFailure, 
        Func<Status, Product, PurchaseProcessingResult> onPurchaseSuccess, 
        Action<Product> onPurchaseDeferred,
        Action<Status, PurchaseFailureReason> onPurchaseFailure) {
        
        if (builder != null) {
            throw new InvalidOperationException("Init should only be called once.");
        }

        this.iapBuilder = iapBuilder;
        
        var module = iapBuilder.PurchasingModule ?? StandardPurchasingModule.Instance();
        builder = ConfigurationBuilder.Instance(module);
        
        appleConfiguration = builder.Configure<IAppleConfiguration>();
        
        // Add Products to store.
        foreach (var key in iapBuilder.Products.Keys) {
            builder.AddProduct(key, iapBuilder.Products[key]);
        }
 
        onInitSuccessCb = onInitSuccess;
        onInitFailureCb = onInitFailure;
        onPurchaseSuccessCb = onPurchaseSuccess;
        onPurchaseDeferredCb = onPurchaseDeferred;
        onPurchaseFailureCb = onPurchaseFailure;

        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions) {
        this.controller = controller;
        appleExtensions = extensions.GetExtension<IAppleExtensions>();
        
        appleExtensions.RegisterPurchaseDeferredListener(product => {
            Debug.Log("Purchase request deferred");
            onPurchaseDeferredCb?.Invoke(product);
        });

        Debug.Log("Hermes successfully initialized");
        onInitSuccessCb?.Invoke();
    }
    
    public void OnInitializeFailed(InitializationFailureReason error) {
        Debug.LogWarning($"Hermes could not be initialized! {error}");
        onInitFailureCb?.Invoke(error);
    }

    //*******************************************************************
    // PRODUCT
    //*******************************************************************
    public bool CanPurchaseProduct(string productID) {
        return controller.products.WithID(productID).availableToPurchase;
    }
    
    /// <summary>
    /// Get all available products.
    /// </summary>
    public Product[] GetAvailableProducts() {
        if (!IsInitAndReady) {
            throw new InvalidOperationException("Cannot get products. IAPManager not successfully initialized.");
        }
            
        // only return all products that are available for purchase.
        return controller.products.all.Where(p => p.availableToPurchase).ToArray();
    }
    
    //*******************************************************************
    // PURCHASE
    //*******************************************************************
    /// <summary>
    /// Try to Purchase a product
    /// <param name="productID">Product ID</param>
    /// </summary>
    public void PurchaseProduct(string productID) {
        if (!IsInitAndReady) {
            throw new InvalidOperationException("Cannot purchase product. Hermes not successfully initialized.");
        }

        // get product for finding subscription.
        var product = controller.products.WithID(productID);
        if (product == null) {
            Debug.LogWarning($"Product with ID: {productID} not found!");
            return;
        }

        status = Status.Purchase;
        
        // try to purchase product.
        controller.InitiatePurchase(product);
    }
    
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent) {
        DebugLog($"Purchased: {purchaseEvent.purchasedProduct.definition.id}");

        // receipt validation tangle data not available.
        if (appleTangleData != null) {
            // validate receipt.
            try {
                var receiptData = System.Convert.FromBase64String(appleConfiguration.appReceipt);
                AppleReceipt receipt = new AppleValidator(appleTangleData).Validate(receiptData);
#if DEBUG_IAP
                foreach (AppleInAppPurchaseReceipt receipts in receipt.inAppPurchaseReceipts) {
                    var sb = new StringBuilder("Purchase Receipt Details:");
                    sb.Append($"\n  Original Transaction ID: {receipts.originalTransactionIdentifier}");
                    sb.Append($"\n  Intro Price Period: {receipts.isIntroductoryPricePeriod}");
                    sb.Append($"\n  Product ID: {receipts.productID}");
                    sb.Append($"\n  Product type: {receipts.productType}");
                    sb.Append($"\n  Quantity: {receipts.quantity}");
                    sb.Append($"\n  Original Transaction ID: {receipts.originalTransactionIdentifier}");
                    sb.Append($"\n  Original Purchase Date: {receipts.originalPurchaseDate}");
                    sb.Append($"\n  Purchase Date: {receipts.purchaseDate}");
                    sb.Append($"\n  Cancellation Date: {receipts.cancellationDate}");
                    sb.Append($"\n  Subsc Expiration Date: {receipts.subscriptionExpirationDate}");
                    sb.Append($"\n  Free trial: {receipts.isFreeTrial}");
                    DebugLog(sb);
                }
#endif
                return onPurchaseSuccessCb != null ? 
                    onPurchaseSuccessCb(status, purchaseEvent.purchasedProduct) : 
                    PurchaseProcessingResult.Pending;
                
            } catch (IAPSecurityException err) {
                Debug.Log($"Invalid receipt or security exception: {err.Message}");
                onPurchaseFailureCb?.Invoke(status, PurchaseFailureReason.SignatureInvalid);
            } catch (Exception err) {
                Debug.Log($"Invalid receipt: {err.Message}");
                onPurchaseFailureCb?.Invoke(status, PurchaseFailureReason.Unknown);
            }
        } else {
            // no tangle data.
            return onPurchaseSuccessCb != null ? 
                onPurchaseSuccessCb(status, purchaseEvent.purchasedProduct) : 
                PurchaseProcessingResult.Pending;
        }
        
        // purchase failed. 
        return PurchaseProcessingResult.Pending;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) {
        DebugLog($"Hermes could not purchase! {failureReason}");
        onPurchaseFailureCb?.Invoke(status, failureReason);
    }

    public void ConfirmPendingPurchase(Product product) {
        if (!IsInitAndReady) {
            throw new InvalidOperationException("Unable to confirm purchase. Hermes is not initialized.");
        }
        
        if (!iapBuilder.HasReceiptServer) {
            Debug.LogError("Invalid call! Receipt server isn't enabled. Use IAPBuilder.WithReceiptServer");
            return;
        }

        controller.ConfirmPendingPurchase(product);
    }
    
    //*******************************************************************
    // SUBSCRIPTION
    //*******************************************************************
    /// <summary>
    /// Check for subscription validity at time of IAP initialization.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Is subscription valid and not expired?</returns>
    public SubscriptionInfo GetSubscriptionInfo(string productId) {
        if (!IsInitAndReady) {
            throw new InvalidOperationException("Unable to check subscription. Hermes is not initialized.");
        }
        
        DebugLog($"Finding subscription info for Product ID: {productId}");
        
        Dictionary<string, string> introDict = appleExtensions.GetIntroductoryPriceDictionary();
        
        foreach (ProductDefinition productDefinition in builder.products) {
            if (productId != productDefinition.id)
                continue;
            
            // get product for finding subscription.
            var product = controller.products.WithID(productId);
            if (product == null)
                continue;

            // only check subscriptions with receipts.
            if (!product.hasReceipt)
                return null;
            
            introDict.TryGetValue(productId, out string introJSON);

            return GetSubscriptionInfo(product, introJSON);
        }

        DebugLog($"Unable to find subscription: {productId}");
        
        return null;
    }
    
    SubscriptionInfo GetSubscriptionInfo(Product product, string introJSON) {
        var subscriptionManager = new SubscriptionManager(product, introJSON);

        try {
            var subInfo = subscriptionManager.getSubscriptionInfo();
            if (subInfo != null && subInfo.isAutoRenewing() == Result.Unsupported) {
                // check for correct subscription type.
                Debug.LogError("Hermes does not support non-renewable subscriptions.");
                return null;
            }

            return subInfo;
        } catch (Exception e) {
            DebugLog($"Unable to acquire subscription info: {e.Message}");
            return null;
        }
    }
    
    //*******************************************************************
    // RESTORE
    //*******************************************************************
    /// <summary>
    /// Restore transaction.
    /// </summary>
    /// <param name="onCompleted">restore process is completed</param>
    /// <param name="onFailed">restore process failed</param>
    public void Restore(Action onCompleted, Action onFailed) {
        if (!IsInitAndReady) {
            throw new InvalidOperationException("Unable to restore purchase. Hermes is not initialized.");
        }
        
        DebugLog("Restoring purchases...");
        status = Status.Restore;

        appleExtensions.RestoreTransactions(result => {
            status = Status.Idle;
            DebugLog($"Restore transaction complete with result: {result}");
            
            if (result) {
                // This does not mean anything was restored,
                // merely that the restoration process succeeded.
                onCompleted?.Invoke();
            } else {
                // Restoration failed.
                onFailed?.Invoke();
            }
        });
    }
    
    //*******************************************************************
    // REFRESH
    //*******************************************************************
    /// <summary>
    /// Refresh and get the latest receipt.
    /// </summary>
    /// <param name="onCompleted">Refresh complete with latest receipt. </param>
    /// <param name="onFailed">Refresh failed.</param>
    public void Refresh(Action<string> onCompleted, Action onFailed) {
        if (!IsInitAndReady) {
            throw new InvalidOperationException("Unable to restore purchase. Hermes is not initialized.");
        }

        DebugLog("Refreshing purchases...");
        status = Status.Refresh;

        appleExtensions.RefreshAppReceipt(latestReceipt => {
            // This handler is invoked if the request is successful.
            // Receipt will be the latest app receipt.
            DebugLog("Successfully refreshed purchases");
            status = Status.Idle;
            onCompleted?.Invoke(latestReceipt);
        }, () => {
            // This handler will be invoked if the request fails,
            // such as if the network is unavailable or the user
            // enters the wrong password.
            DebugLog("Refresh purchases unsuccessful");
            status = Status.Idle;
            onFailed?.Invoke();
        });
    }
    
    /// <summary>
    /// Print debug log
    /// </summary>
    /// <param name="text">the data to be output.</param>
    void DebugLog(object text) {
        if (iapBuilder.IsDebugLogEnabled) {
            Debug.Log($"[Hermes]{text}");
        }
    }
}
