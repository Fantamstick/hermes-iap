#if UNITY_IOS || UNITY_IPHONE
// Some unity files such as AppleTangle only defines UNITY_IPHONE not UNITY_IOS
// To be absolutely sure all iOS-related files are covered, we create a new IOS define.
#define IOS
#endif

using System;
using System.Linq;
#if DEBUG_IAP
using System.Text;
#endif
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;
#if UNITY_ANDROID
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
#endif

namespace HermesIAP {
    /// <summary>
    /// Hermes In-App Purchase Manager.
    /// </summary>
    public class HermesIAP : IStoreListener {
        public static HermesIAP Instance { get; } = new HermesIAP();
#if IOS
        IAppleConfiguration appleConfig;
#endif
        IExtensionProvider extensions;
        InitStatus initStatus;
        Action<InitStatus> onInitDone;
        IStoreController storeController;
        byte[] appleTangleData;
        byte[] googleTangleData;
        
        /// <summary>
        /// Callback for when restore is completed.
        /// </summary>
        Action<PurchaseResponse> onRestored;
        
        /// <summary>
        /// Result of product purchase.
        /// </summary>
        public event Action<PurchaseResponse, Product> OnPurchased;
#if IOS
        /// <summary>
        /// Purchase request was deferred to parent.
        /// </summary>
        public event Action<Product> OnPurchaseDeferred;
#endif
        //*******************************************************************
        // INIT
        //*******************************************************************
        // Prevent class from being instanced explicitly.
        HermesIAP() {
        }

        /// <summary>
        /// Has Hermes successfully initialized?
        /// </summary>
        public bool IsInit => initStatus == InitStatus.Ok;

        /// <summary>
        /// Initialize Hermes IAP.
        /// </summary>
        /// <param name="iapBuilder">Builder data used to create instance.</param>
        /// <param name="onDone">Callback when initialization is done.</param>
        public void Init(IAPBuilder iapBuilder, Action<InitStatus> onDone) {
            if (IsInit) {
                onDone(initStatus);
                return;
            }

            if (onInitDone != null) {
                Debug.LogError("Hermes is already in the process of initializing.");
                onDone(initStatus);
                return;
            }
            
            appleTangleData = iapBuilder.AppleTangleData ?? null;
            googleTangleData = iapBuilder.GoogleTangleData ?? null;
            
#if IOS
            IPurchasingModule module = iapBuilder.PurchasingModule ?? StandardPurchasingModule.Instance();
            ConfigurationBuilder builder = ConfigurationBuilder.Instance(module);
            
            // Verify if purchases are possible on this iOS device.
            var canMakePayments = builder.Configure<IAppleConfiguration>().canMakePayments;
            if (!canMakePayments) {
                onDone(InitStatus.PurchasingDisabled);
                return;
            }

            appleConfig = builder.Configure<IAppleConfiguration>();
#endif
#if UNITY_ANDROID
            IPurchasingModule module = Google.Play.Billing.GooglePlayStoreModule.Instance();
            ConfigurationBuilder builder = ConfigurationBuilder.Instance(module);
#endif            
            // Add Products to store.
            foreach (var key in iapBuilder.Products.Keys) {
                builder.AddProduct(key, iapBuilder.Products[key]);
            }

            onInitDone = onDone;
            UnityPurchasing.Initialize(this, builder);
        }

        void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions) {
            storeController = controller;
            this.extensions = extensions;
            initStatus = InitStatus.Ok;
#if IOS
            // Notify callback for when purchases are deferred to a parent.
            extensions.GetExtension<IAppleExtensions>().RegisterPurchaseDeferredListener(product => {
                Debug.Log("Purchase request deferred to parent.");
                OnPurchaseDeferred?.Invoke(product);
            });
#endif
            Debug.Log("IAP Manager successfully initialized");

            onInitDone(initStatus);
            onInitDone = null;
        }

        /// <summary>
        /// Initialization failed.
        /// This is NOT called when device is offline.
        /// Device will continuously try to init until device is online.
        /// </summary>
        void IStoreListener.OnInitializeFailed(InitializationFailureReason error) {
            Debug.LogWarning($"IAP init error: {error}");

            switch (error) {
                case InitializationFailureReason.AppNotKnown:
                    initStatus = InitStatus.AppNotKnown;
                    break;
                case InitializationFailureReason.NoProductsAvailable:
                    initStatus = InitStatus.NoProductsAvailable;
                    break;
                case InitializationFailureReason.PurchasingUnavailable:
                    initStatus = InitStatus.PurchasingUnavailable;
                    break;
                default:
                    initStatus = InitStatus.PurchasingUnavailable;
                    break;
            }

            onInitDone(initStatus);
            onInitDone = null;
        }
    
        //*******************************************************************
        // SUBSCRIPTION
        //*******************************************************************
#if IOS 
        /// <summary>
        /// Is specified subscription active.
        /// </summary>
        /// <param name="productId">Product id.</param>
        public bool IsSubscriptionActive(string productId) {
            var expireDate = GetSubscriptionExpiration(productId);
            if (!expireDate.HasValue) {
                Debug.Log($"{productId} has no expiration date.");
                return false;
            }
            
            // instance is later than now
            DateTime nowUtc = DateTime.Now.ToUniversalTime();
            bool isActive = expireDate.Value.CompareTo(nowUtc) > 0;
            if (!isActive) {
                Debug.Log($"{expireDate.Value} is already past {nowUtc}");
                return false;
            }

            return true;
        }
    
        /// <summary>
        /// Get subscription expiration date.
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>Expiration date. Null if already expired</returns>
        public DateTime? GetSubscriptionExpiration(string productId) {
            // get most recent subscription
            return GetPurchasedSubscriptions(productId)?
                .Select(s => s.getExpireDate())
                .OrderBy(date => date.Ticks)
                .LastOrDefault();
        }
        
        public SubscriptionInfo[] GetPurchasedSubscriptions(string productId) {
            var products = GetAvailableProducts().Where(p => 
                p.definition.id == productId &&
                p.definition.type == ProductType.Subscription &&
                p.hasReceipt
            ).ToArray();

            if (products == null || products.Length == 0) {
                Debug.LogWarning($"Product id: {productId} does not exist, is not a subscription, or does not have receipt!");
                return null;
            }

            var subscriptions = products
                .Select(p => new SubscriptionManager(p, null).getSubscriptionInfo())
                .ToArray();
            
            if (subscriptions == null || subscriptions.Length == 0) {
                Debug.LogWarning($"Subscription with product id {productId} is not a valid subscription product id.");
                return null;
            }

            return subscriptions;
        }
#endif        
        
#if UNITY_ANDROID        
        
        /// <summary>
        /// Is specified subscription active.
        /// </summary>
        /// <param name="productId">Product id.</param>
        public async UniTask<bool> IsSubscriptionActive(string productId) {
            Product[] products = await GetPurchasedSubscriptions(productId);
            if (products == null || products.Length == 0)
            {
                Debug.Log($"{productId} has no purchase result.");
                return false;
            }

            Product product = products[0];
            if (!product.hasReceipt)
            {
                Debug.Log($"{productId} has no purchase receipt.");
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Get subscription expiration date.
        /// todo: Not compatible with Google Play Developer API. This method don't get the real expired date.
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>Expiration date. Null if already expired</returns>
        public async UniTask<DateTime?> GetSubscriptionExpiration(string productId)
        {
            Product[] products = await GetPurchasedSubscriptions(productId);
            if (products == null || products.Length == 0)
            {
                Debug.Log($"{productId} has no purchase result.");
                // not existed receipt. return null.
                return null;
            }

            Product product = products[0];
            if (!product.hasReceipt)
            {
                Debug.Log($"{productId} has no purchase receipt.");
                // not existed receipt. return null.
                return null;
            }
            // existed receipt. return future date.
            return DateTime.Now.AddDays(1);
        }
        
        /// <summary>
        /// Get subscription purchases by Google Play Billing Library:`BillingClient.queryPurchase()`
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async UniTask<Product[]> GetPurchasedSubscriptions(string productId) {
           return await FetchSubscriptionPurchase(productId);
        }

        /// <summary>
        /// Get subscription purchases by Google Play Billing Library:`BillingClient.queryPurchase()`
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        private UniTask<Product[]> FetchSubscriptionPurchase(string productId)
        {
            var utcs = new UniTaskCompletionSource<Product[]>();
            if (!IsInit)
            {
                Debug.LogWarning("Cannot fetch subscription. IAPManager not successfully initialized!");
                utcs.TrySetException(new Exception("Cannot fetch subscription. IAPManager not successfully initialized!"));
                return utcs.Task;
            }
            HashSet<ProductDefinition> additionalProducts = new HashSet<ProductDefinition>()
            {
                new ProductDefinition(productId, productId, ProductType.Subscription)
            };
            Product[] result = null;
            Debug.Log("---FetchAdditionalProducts start");

            // 結果取得＝サブスクのサービス提供可能。
            // see) https://developer.android.com/google/play/billing/subscriptions
            // memo. Google Play Billing Libraryでは期限などは取れない。要 Google Play Developer API
            // purchaseTokenなら、 product.receipt のjson に入っている。
            storeController.FetchAdditionalProducts(additionalProducts, successCallback:() =>
                {
                    Debug.Log("---FetchAdditionalProducts success");
                    result = storeController.products.all;
                    utcs.TrySetResult(result);
                },
                (InitializationFailureReason reason) =>
                {
                    Debug.Log("---FetchAdditionalProducts fail");
                    Debug.LogError(reason);
                    utcs.TrySetResult(result);
                });
            return utcs.Task;
        }

#endif
        //*******************************************************************
        // PURCHASE
        //*******************************************************************
        /// <summary>
        /// Try to Purchase a product
        /// </summary>
        public PurchaseRequest PurchaseProduct(string productId) {
            if (!IsInit) {
                Debug.LogWarning("Cannot purchase product. IAPManager not successfully initialized!");
                return PurchaseRequest.NoInit;
            }

            var product = storeController.products.WithID(productId);
            if (product == null) {
                Debug.LogWarning("Cannot purchase product. Not found!");
                return PurchaseRequest.ProductUnavailable;
            }

            if (!product.availableToPurchase) {
                Debug.LogWarning("Cannot purchase product. Not available for purchase!");
                return PurchaseRequest.PurchasingUnavailable;
            }

            // try to purchase product.
            storeController.InitiatePurchase(product);

            return PurchaseRequest.Ok;
        }

        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs e) {
#if DEBUG_IAP
            Debug.Log("Processing a purchase");
#endif
#if IOS
            return ProcessIosPurchase(e);
#elif UNITY_ANDROID
            return ProcessGooglePurchase(e);
#else
            Debug.LogWarning("Receipt validation not available for current platform");
            OnPurchased?.Invoke(PurchaseResponse.Ok, e.purchasedProduct);
            return PurchaseProcessingResult.Complete;
#endif
        }
        
#if IOS
        PurchaseProcessingResult ProcessIosPurchase(PurchaseEventArgs e) {
            // receipt validation tangle data not available.
            if (appleTangleData == null) {
                OnPurchased?.Invoke(PurchaseResponse.Ok, e.purchasedProduct);
                onRestored?.Invoke(PurchaseResponse.Ok);
                onRestored = null;
                
                return PurchaseProcessingResult.Complete;
            }
            
            // validate receipt.
            try {
                var receiptData = System.Convert.FromBase64String(appleConfig.appReceipt);
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
                    Debug.Log(sb);
                }
#endif
                OnPurchased?.Invoke(PurchaseResponse.Ok, e.purchasedProduct);
                onRestored?.Invoke(PurchaseResponse.Ok);
                onRestored = null;
            } catch (IAPSecurityException err) {
                Debug.Log($"Invalid receipt or security exception: {err.Message}");
                OnPurchased?.Invoke(PurchaseResponse.InvalidReceipt, e.purchasedProduct);
                onRestored?.Invoke(PurchaseResponse.InvalidReceipt);
                onRestored = null;
            }

            return PurchaseProcessingResult.Complete;
        }
#endif

#if UNITY_ANDROID
        PurchaseProcessingResult ProcessGooglePurchase(PurchaseEventArgs e) {
            // receipt validation tangle data not available.
            if (googleTangleData == null) {
                OnPurchased?.Invoke(PurchaseResponse.Ok, e.purchasedProduct);
                return PurchaseProcessingResult.Complete;
            }
            
            // validate receipt.
            try {
                var validator = new CrossPlatformValidator(googleTangleData, appleTangleData, Application.identifier);
                // On Google Play, result has a single product ID.
                // On Apple stores, receipts contain multiple products.
                var result = validator.Validate(e.purchasedProduct.receipt);
#if DEBUG_IAP
                // For informational purposes, we list the receipt(s)
                foreach (IPurchaseReceipt receipt in result) {
                    var sb = new StringBuilder("Purchase Receipt Details:");
                    sb.Append($"\n  Product ID: {receipt.productID}");
                    sb.Append($"\n  Purchase Date: {receipt.purchaseDate}");
                    sb.Append($"\n  Transaction ID: {receipt.transactionID}");
                    
                    var googleReceipt = receipt as GooglePlayReceipt;
                    if (googleReceipt != null) {
                        // This is Google's Order ID.
                        // Note that it is null when testing in the sandbox
                        // because Google's sandbox does not provide Order IDs.
                        sb.Append($"\n  Purchase State: {googleReceipt.purchaseState}");
                        sb.Append($"\n  Purchase Token: {googleReceipt.purchaseToken}");
                    }
                    
                    Debug.Log(sb);
                }
#endif          
                OnPurchased?.Invoke(PurchaseResponse.Ok, e.purchasedProduct);
            } catch (IAPSecurityException err) {
                Debug.Log($"Invalid receipt or security exception: {err.Message}");
                OnPurchased?.Invoke(PurchaseResponse.InvalidReceipt, e.purchasedProduct);
            }

            return PurchaseProcessingResult.Complete;
        }
#endif
        void IStoreListener.OnPurchaseFailed(Product p, PurchaseFailureReason reason) {
            Debug.LogWarning($"IAP purchase error: {reason}");
            HandleRestoreFailure(reason);

            switch (reason) {
                case PurchaseFailureReason.DuplicateTransaction:
                    OnPurchased?.Invoke(PurchaseResponse.DuplicateTransaction, p);
                    break;
                case PurchaseFailureReason.PaymentDeclined:
                    OnPurchased?.Invoke(PurchaseResponse.PaymentDeclined, p);
                    break;
                case PurchaseFailureReason.ProductUnavailable:
                    OnPurchased?.Invoke(PurchaseResponse.ProductUnavailable, p);
                    break;
                case PurchaseFailureReason.PurchasingUnavailable:
                    OnPurchased?.Invoke(PurchaseResponse.PurchasingUnavailable, p);
                    break;
                case PurchaseFailureReason.SignatureInvalid:
                    OnPurchased?.Invoke(PurchaseResponse.SignatureInvalid, p);
                    break;
                case PurchaseFailureReason.UserCancelled:
                    OnPurchased?.Invoke(PurchaseResponse.UserCancelled, p);
                    break;
                case PurchaseFailureReason.ExistingPurchasePending:
                    OnPurchased?.Invoke(PurchaseResponse.ExistingPurchasePending, p);
                    break;
                case PurchaseFailureReason.Unknown:
                    OnPurchased?.Invoke(PurchaseResponse.Unknown, p);
                    break;
            }
        }

        //*******************************************************************
        // PRODUCTS
        //*******************************************************************
        /// <summary>
        /// Get all available products.
        /// </summary>
        public Product[] GetAvailableProducts() {
            if (!IsInit) {
                Debug.LogWarning("Cannot get products. IAPManager not successfully initialized!");
                return null;
            }

            // only return all products that are available for purchase.
            return storeController.products.all.Where(p => p.availableToPurchase).ToArray();
        }

        //*******************************************************************
        // RESTORE
        //*******************************************************************
        /// <summary>
        /// Restore purchases
        /// (GooglePlay is automatic after Init)
        /// </summary>
        public void RestorePurchases(int timeoutMs, Action<PurchaseResponse> onDone) {
            if (!IsInit) {
                Debug.LogWarning("Cannot restore purchases. IAPManager not successfully initialized!");
                onDone(PurchaseResponse.NoInit);
                return;
            }

            onRestored = onDone;
#if IOS
            var apple = extensions.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions(result => {
#else
            var googlePlay = extensions.GetExtension<Google.Play.Billing.IGooglePlayStoreExtensions>();
            googlePlay.RestoreTransactions(result => {
#endif
                // still waiting for result.
                if (onRestored != null) {
                    if (result) {
                        Debug.Log("Waiting for restore...");
                        WaitForRestorePurchases(timeoutMs);
                    } else {
                        Debug.Log("Restore process rejected.");
                        onDone(PurchaseResponse.Unknown);
                        onRestored = null;
                    }
                }
            });
        }

        async void WaitForRestorePurchases(int timeoutMs) {
            int waitTime = 0;
            int waitFrame = 100;
            while (!IsTimeout() && onRestored != null) {
                await Task.Delay(waitFrame);
                waitTime += waitFrame;
            }

            if (IsTimeout() && onRestored != null) {
                Debug.Log("Restore timeout");
                onRestored(PurchaseResponse.Timeout);
                onRestored = null;
            }

            bool IsTimeout() => waitTime >= timeoutMs;
        }

        void HandleRestoreFailure(PurchaseFailureReason reason) {
            switch (reason) {
                case PurchaseFailureReason.DuplicateTransaction:
                    onRestored?.Invoke(PurchaseResponse.DuplicateTransaction);
                    break;
                case PurchaseFailureReason.PaymentDeclined:
                    onRestored?.Invoke(PurchaseResponse.PaymentDeclined);
                    break;
                case PurchaseFailureReason.ProductUnavailable:
                    onRestored?.Invoke(PurchaseResponse.ProductUnavailable);
                    break;
                case PurchaseFailureReason.PurchasingUnavailable:
                    onRestored?.Invoke(PurchaseResponse.ProductUnavailable);
                    break;
                case PurchaseFailureReason.SignatureInvalid:
                    onRestored?.Invoke(PurchaseResponse.SignatureInvalid);
                    break;
                case PurchaseFailureReason.UserCancelled:
                    onRestored?.Invoke(PurchaseResponse.UserCancelled);
                    break;
                case PurchaseFailureReason.ExistingPurchasePending:
                    onRestored?.Invoke(PurchaseResponse.ExistingPurchasePending);
                    break;
                case PurchaseFailureReason.Unknown:
                    onRestored?.Invoke(PurchaseResponse.Unknown);
                    break;
            }
            
            onRestored = null;
        }
        
#if IOS
        //*******************************************************************
        // REFRESH
        //*******************************************************************
        /// <summary>
        /// Refresh purchases
        /// </summary>
        public void RefreshPurchases(Action<bool> onDone = null) {
            if (!IsInit) {
                Debug.LogError("Cannot refresh purchases. IAPManager not successfully initialized!");
                return;
            }

            var apple = extensions.GetExtension<IAppleExtensions>();
            apple.RefreshAppReceipt(successCallback: (success) => {
                Debug.Log("Successfully refreshed purchases");
                onDone?.Invoke(true);
            }, errorCallback: () => {
                Debug.Log("Refreshed purchase error");
                onDone?.Invoke(false);
            });
        }
#endif
    }
}