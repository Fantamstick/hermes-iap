using System;
using System.Collections.Generic;
using System.IO;
using Hermes;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public abstract class HermesStore : IStoreListener 
{
    public enum Status 
    {
        Idle,
        /// <summary>
        /// User is purchasing a product.
        /// </summary>
        Purchase,
        /// <summary>
        /// User is restoring a product.
        /// </summary>
        Restore,
        /// <summary>
        /// User is refreshing a product.
        /// </summary>
        Refresh,
    }
    
    /// <summary>
    /// IAP initialized successfully and ready
    /// </summary>
    public bool IsInitAndReady => extensions != null;

    protected Status status = Status.Idle;
    protected IStoreController controller;
    protected IStoreExtension extensions;
    protected IStoreConfiguration configuration;
    ConfigurationBuilder builder;
    protected IAPBuilder iapBuilder;
    protected byte[] tangleData;
    
    // IAP internal callback events
    event Action onInitSuccessCb;
    event Action<InitializationFailureReason> onInitFailureCb;
    event Func<Status, Product, PurchaseProcessingResult> onPurchaseSuccessCb;
    event Action<Product> onPurchaseDeferredCb;
    event Action<Status, PurchaseFailureReason> onPurchaseFailureCb;

    // Allow parent store to call events
    protected void CallInitSuccessCb() => onInitSuccessCb?.Invoke();
    protected void CallInitFailCb(InitializationFailureReason reason) => onInitFailureCb?.Invoke(reason);
    protected void CallPurchaseDeferredCb(Product product) => onPurchaseDeferredCb?.Invoke(product);
    protected void CallPurchaseFailCb(Status status, PurchaseFailureReason reason)
    {
        onPurchaseFailureCb?.Invoke(status, reason);
    }
    protected PurchaseProcessingResult CallPurchaseSuccessCb(Status status, Product product)
    {
        return onPurchaseSuccessCb(status, product);
    }
    
    protected abstract IStoreConfiguration GetStoreConfiguration(ConfigurationBuilder builder);
    protected abstract IStoreExtension GetStoreExtensions(IExtensionProvider provider);
    protected abstract PurchaseProcessingResult OnProcessPurchase(PurchaseEventArgs purchaseEvent);
    
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
        Action<Status, PurchaseFailureReason> onPurchaseFailure) 
    {
        if (onInitSuccess != null)
        {
            onInitSuccessCb += onInitSuccess;
        }
        if (onInitFailure != null)
        {
            onInitFailureCb += onInitFailure;
        }
        if (onPurchaseSuccess != null)
        {
            onPurchaseSuccessCb += onPurchaseSuccess;
        }
        if (onPurchaseDeferred != null)
        {
            onPurchaseDeferredCb += onPurchaseDeferred;
        }
        if (onPurchaseFailure != null)
        {
            onPurchaseFailureCb += onPurchaseFailure;
        }

        if (builder != null) 
        {
            DebugLog("Init already called. Added callback events.");
            return;
        }

        this.iapBuilder = iapBuilder;
        
        var module = iapBuilder.PurchasingModule ?? StandardPurchasingModule.Instance();
        builder = ConfigurationBuilder.Instance(module);
        
        configuration = GetStoreConfiguration(builder);
        
        // Add Products to store.
        foreach (var key in iapBuilder.Products.Keys) 
        {
            builder.AddProduct(key, iapBuilder.Products[key]);
        }

        UnityPurchasing.Initialize(this, builder);
    }

    void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions) 
    {
        this.controller = controller;
        this.extensions = GetStoreExtensions(extensions);

        if (this.extensions is IAppleExtensions appleExtensions) 
        {
            appleExtensions.RegisterPurchaseDeferredListener(product => 
            {
                Debug.Log("Purchase request deferred");
                onPurchaseDeferredCb?.Invoke(product);
            });
        }

        Debug.Log("Hermes successfully initialized");
        onInitSuccessCb?.Invoke();
    }
    
    void IStoreListener.OnInitializeFailed(InitializationFailureReason error) {
        Debug.LogWarning($"Hermes could not be initialized! {error}");
        onInitFailureCb?.Invoke(error);
    }

    //*******************************************************************
    // PRODUCT
    //*******************************************************************
    /// <summary>
    /// Get Product from product ID.
    /// </summary>
    public Product GetProduct(string productId) 
    {
        if (!IsInitAndReady) 
        {
            throw new InvalidOperationException("Cannot get Product. IAPManager not successfully initialized.");
        }
            
        return controller.products.WithID(productId);
    }
    
    /// <summary>
    /// Get all Products.
    /// </summary>
    public IEnumerable<Product> GetAllProducts() 
    {
        if (!IsInitAndReady) 
        {
            throw new InvalidOperationException("Cannot get products. IAPManager not successfully initialized.");
        }

        return controller.products.all;
    }
    
    //*******************************************************************
    // PURCHASE
    //*******************************************************************
    /// <summary>
    /// Try to Purchase a product
    /// <param name="productId">Product ID</param>
    /// </summary>
    public void PurchaseProduct(string productId) 
    {
        if (!IsInitAndReady) 
        {
            throw new InvalidOperationException("Cannot purchase product. Hermes not successfully initialized.");
        }

        // get product for purchase.
        var product = GetProduct(productId);
        if (product == null) 
        {
            Debug.LogWarning($"Product with ID: {productId} not found!");
            return;
        }

        status = Status.Purchase;
        
        // try to purchase product.
        controller.InitiatePurchase(product);
    }
    
    PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs purchaseEvent) 
    {
        DebugLog($"Purchased: {purchaseEvent.purchasedProduct.definition.id}");

        return OnProcessPurchase(purchaseEvent);
    }

    void IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) 
    {
        DebugLog($"Hermes could not purchase! {failureReason}");
        onPurchaseFailureCb?.Invoke(status, failureReason);
    }

    public void ConfirmPendingPurchase(string productId) 
    {
        if (!IsInitAndReady) 
        {
            throw new InvalidOperationException("Unable to confirm purchase. Hermes is not initialized.");
        }

        Product product = GetProduct(productId);
        if (product == null)
        {
            throw new InvalidDataException("Invalid Product ID");
        }
        
        controller.ConfirmPendingPurchase(product);
    }
    
    //*******************************************************************
    // SUBSCRIPTION
    //*******************************************************************
    public bool IsSubscribedTo(string subscriptionId) 
    {
        var subscription = controller.products.WithStoreSpecificID(subscriptionId);
        if (subscription.receipt == null) 
        {
            // subscription either never existed OR it expired before the IAP initialization.
            return false;
        }

        var subscriptionManager = new SubscriptionManager(subscription, null);
        var info = subscriptionManager.getSubscriptionInfo();
        return info.isSubscribed() == Result.True;
    }
    
    /// <summary>
    /// Check for subscription validity at time of IAP initialization.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Is subscription valid and not expired?</returns>
    public SubscriptionInfo GetSubscriptionInfo(string productId) 
    {
        Product product = GetProduct(productId);
        if (product == null) 
        {
            Debug.LogWarning($"Product with ID: {productId} not found!");
            return null;
        }
        
        var subscriptionManager = new SubscriptionManager(product, null);
        
        try 
        {
            var subInfo = subscriptionManager.getSubscriptionInfo();
            
            if (subInfo != null && subInfo.isAutoRenewing() == Result.Unsupported) 
            {
                // check for correct subscription type.
                Debug.LogError("Hermes does not support non-renewable subscriptions.");
                return null;
            }
            
            return subInfo;
        } 
        catch (Exception e) 
        {
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

        if (extensions is IAppleExtensions appleExtensions) 
        {
            appleExtensions.RestoreTransactions(HandleRestoreTransaction);
        }
        else if (extensions is IGooglePlayStoreExtensions googlePlayExtensions) 
        {
            googlePlayExtensions.RestoreTransactions(HandleRestoreTransaction);
        }

        void HandleRestoreTransaction(bool result) 
        {
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
        }
    }

    /// <summary>
    /// Print debug log
    /// </summary>
    /// <param name="text">the data to be output.</param>
    protected void DebugLog(object text) {
        if (iapBuilder.IsDebugLogEnabled) {
            Debug.Log($"[Hermes]{text}");
        }
    }
}
