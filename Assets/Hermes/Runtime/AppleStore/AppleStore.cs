#if UNITY_IOS || UNITY_IPHONE
// Some unity files such as AppleTangle only defines UNITY_IPHONE not UNITY_IOS
// To be absolutely sure all iOS-related files are covered, we create a new IOS define.
#define IOS
#endif

#if IOS
using System;
using System.Collections.Generic;
using System.Linq;
#if DEBUG_IAP
using System.Text;
using System.Reflection;
#endif
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

namespace Hermes {
    /// <summary>
    /// Hermes In-App Purchase Manager for Apple Store.
    /// </summary>
    public class AppleStore : IStoreListener {
        IAppleConfiguration appleConfig;
        IAppleExtensions apple;
        InitStatus initStatus;
        Action<InitStatus> onInitDone;
        IStoreController storeController;
        byte[] appleTangleData;

        /// <summary>
        /// Result of product purchase or restore.
        /// </summary>
        public event Action<PurchaseResponse, Product> OnPurchased;

        /// <summary>
        /// Purchase request was deferred to parent.
        /// </summary>
        public event Action<Product> OnPurchaseDeferred;

        /// <summary>
        /// System asked to purchase from promotional purchase.
        /// </summary>
        Func<Product, UniTask<bool>> onPromotionalPurchase;
        
        /// <summary>
        /// Callback for when restore is completed.
        /// </summary>
        Action<PurchaseResponse> onRestored;

        bool deferPurchaseCompatible;
        bool promotionalPurchaseCompatible;
        
        //*******************************************************************
        // Instantiation
        //*******************************************************************
        // Prevent class from being instanced explicitly outside.
        AppleStore() {
        }
        
        internal static AppleStore CreateInstance() {
            return new AppleStore();
        }
        
        //*******************************************************************
        // INIT
        //*******************************************************************
        /// <summary>
        /// Has Hermes successfully initialized?
        /// </summary>
        public bool IsInit => initStatus == InitStatus.Ok;

        /// <summary>
        /// Initialize Hermes IAP.
        /// </summary>
        /// <param name="builder">Builder data used to create instance.</param>
        public async UniTask<InitStatus> InitAsync(IAPBuilder builder) {
            // initialize.
            Init(builder, _ => {});
    
            // wait until complete.
            await UniTask.WaitWhile(() => onInitDone != null);

            return initStatus;
        }

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
                Debug.LogWarning("Hermes is already in the process of initializing.");
                return;
            }
            
            appleTangleData = iapBuilder.AppleTangleData ?? null;
            var module = iapBuilder.PurchasingModule ?? StandardPurchasingModule.Instance();
            var builder = ConfigurationBuilder.Instance(module);

            appleConfig = builder.Configure<IAppleConfiguration>();

            // Verify if purchases are possible on this iOS device.
            if (!appleConfig.canMakePayments) {
                initStatus = InitStatus.PurchasingDisabled;
                onDone(InitStatus.PurchasingDisabled);
                return;
            }

            // Add Products to store.
            foreach (var key in iapBuilder.Products.Keys) {
                builder.AddProduct(key, iapBuilder.Products[key]);
            }

            deferPurchaseCompatible = iapBuilder.DeferredPurchaseCompatible;
            promotionalPurchaseCompatible = iapBuilder.PromotionalPurchaseCompatible;
            
            if (promotionalPurchaseCompatible) {
                appleConfig.SetApplePromotionalPurchaseInterceptorCallback(OnPromotionalPurchase);
                onPromotionalPurchase = iapBuilder.OnPromotionalPurchase;
            }
            
            onInitDone = onDone;
            UnityPurchasing.Initialize(this, builder);
        }

        /// <summary>
        /// Callback called by IAP when a purchase is attempted from promotional purchase.
        /// </summary>
        void OnPromotionalPurchase(Product product) {
            OnPromotionalPurchaseAsync(product).Forget();
            
            async UniTask OnPromotionalPurchaseAsync(Product p) {
                while (!IsInit) {
                    // wait for initialization before continuing.
                    await UniTask.DelayFrame(1);
                }
                
                // ask if user can purchase.
                var continuePurchase = await onPromotionalPurchase(p);
                if (continuePurchase) {
                    // Continue promotional purchase process after promotional purchase callback.
                    apple.ContinuePromotionalPurchases();
                }
            }
        }

        void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions) {
            storeController = controller;
            apple = extensions.GetExtension<IAppleExtensions>();
            initStatus = InitStatus.Ok;

            // Notify callback for when purchases are deferred to a parent.
            if (deferPurchaseCompatible) {
                apple.RegisterPurchaseDeferredListener(product => {
                    DebugLog("Purchase request deferred to parent.");
                    OnPurchaseDeferred?.Invoke(product);
                });
            }

            if (promotionalPurchaseCompatible) {
                foreach (var item in controller.products.all) {
                    if (item.availableToPurchase) {                
                        // Set all these products to be visible in the user's App Store
                        apple.SetStorePromotionVisibility(item, AppleStorePromotionVisibility.Show);
                    }
                }
            }

            DebugLog("IAP Manager successfully initialized");

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
                    initStatus = InitStatus.Unknown;
                    break;
            }

            onInitDone(initStatus);
            onInitDone = null;
        }
    
        //*******************************************************************
        // SUBSCRIPTION
        //*******************************************************************
        /// <summary>
        /// Get the Product from the product's ID as registered from the platform's store.
        /// </summary>
        /// <param name="productID">Product's ID</param>
        /// <returns>Product information.</returns>
        public Product GetProduct(string productID) {
            if (storeController == null) {
                Debug.LogWarning($"IAP not initialized correctly.");
                return null;
            }
            
            var product = storeController.products.WithID(productID);
            if (product == null) {
                Debug.LogWarning($"{productID} is not a valid Product ID.");
                return null;
            }

            return product;
        }
        
        /// <summary>
        /// Get most recent auto-renewable subscription information from the product ID as registered in the platform's store.
        /// </summary>
        /// <param name="productID">Product ID</param>
        /// <returns>Subscription information.</returns>
        public SubscriptionInfo GetSubscriptionInfo(string productID) {
            var product = GetProduct(productID);
            if (product == null) 
                return null;
            
            Dictionary<string, string> introDict = apple.GetIntroductoryPriceDictionary();
            
            var introJSON = storeController.products.all
                .Where(p => p.hasReceipt && introDict.ContainsKey(p.definition.storeSpecificId))
                .Select(p => introDict[p.definition.storeSpecificId])
                .FirstOrDefault();

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
        
        /// <summary>
        /// Get auto-renewable subscription expiration date.
        /// </summary>
        /// <param name="productID">Product ID</param>
        /// <returns>returns null if user has not purchased or product is invalid.</returns>
        public async UniTask<DateTime?> GetSubscriptionExpiration(string productID) {
            var expirationDate = GetSubscriptionInfo(productID)?.getExpireDate();
            
            return await UniTask.FromResult(expirationDate);
        }
        
        public bool IsPurchasedProduct(Product product) {
            return product.hasReceipt;
        }
        
        /// <summary>
        /// Is auto-renewable subscription's active.
        /// </summary>
        /// <param name="productId">Product ID.</param>
        /// <returns>returns true if the store receipt's expiration date is after the device's current time</returns>
        public async UniTask<bool> IsActiveSubscription(string productId) {
            var subInfo = GetSubscriptionInfo(productId);

            return subInfo != null ? 
                await UniTask.FromResult(subInfo.isSubscribed() == Result.True) : 
                await UniTask.FromResult(false);
        }

        /// <summary>
        /// Gets introductory offer details.
        /// Includes Free Trial.
        /// </summary>
        /// <param name="productID">Product ID</param>
        /// <param name="groupProductIDs">Group products that productID belongs to.
        /// If empty or null, assume productID is in its own group.</param>
        /// <returns>Offer details if exists.</returns>
        public UniTask<IntroductoryOffer> GetIntroductoryOfferDetailsAsync(string productID, string[] groupProductIDs = null) {
            // Determine if product exists.
            var products = apple.GetProductDetails();
            if (products == null || !products.ContainsKey(productID)) {
                // No product available.
                return UniTask.FromResult<IntroductoryOffer>(null);
            }

            // Get product details.
            IntroductoryOffer offer = null;
            try {
                offer = new IOSIntroductoryOfferFactory(products[productID]).Make();
            } catch (InvalidOfferException) {
                // Invalid offer.
                return UniTask.FromResult<IntroductoryOffer>(null);
            } catch(Exception e) {
                // Invalid json!
                Debug.LogWarning($"Invalid product data detected! {e.Message}");
                return UniTask.FromResult<IntroductoryOffer>(null);
            }

            try {
                var receiptData = System.Convert.FromBase64String(appleConfig.appReceipt);
                AppleReceipt receipt = new AppleValidator(appleTangleData).Validate(receiptData);
                if (receipt == null || receipt.inAppPurchaseReceipts == null) {
                    // no previous subscription purchased. 
                    return UniTask.FromResult(offer);
                }

                if (groupProductIDs == null || groupProductIDs.Length == 0) {
                    groupProductIDs = new string[] {productID};
                }
                
                var prevCampaignPurchase = receipt.inAppPurchaseReceipts
                    .FirstOrDefault(r => 
                        groupProductIDs.Contains(r.productID) &&
                        (r.isFreeTrial != 0 || r.isIntroductoryPricePeriod != 0));
                    
                if(prevCampaignPurchase != null) {
                    // user already used free trial or introductory offer. 
                    return UniTask.FromResult<IntroductoryOffer>(null);
                }   
            } catch {
                // unable to validate receipt or unable to access.
                return UniTask.FromResult<IntroductoryOffer>(null);
            }

            return UniTask.FromResult(offer);
        }

        //*******************************************************************
        // PURCHASE
        //*******************************************************************
        /// <summary>
        /// Try to Purchase a product
        /// <param name="productID">Product ID</param>
        /// </summary>
        public PurchaseRequest PurchaseProduct(string productID) {
            if (!IsInit) {
                Debug.LogWarning("Cannot purchase product. IAPManager not successfully initialized!");
                return PurchaseRequest.NoInit;
            }

            var product = GetProduct(productID);
            if (product == null) 
                return PurchaseRequest.ProductUnavailable;

            if (!product.availableToPurchase) {
                Debug.LogWarning($"Cannot purchase product {productID}. Not available for purchase.");
                return PurchaseRequest.PurchasingUnavailable;
            }

            // try to purchase product.
            storeController.InitiatePurchase(product);

            return PurchaseRequest.Ok;
        }

        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs e) {
            DebugLog("Processing a purchase");
            
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
                    DebugLog(sb);
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
            } catch {
                OnPurchased?.Invoke(PurchaseResponse.InvalidReceipt, e.purchasedProduct);
                onRestored?.Invoke(PurchaseResponse.InvalidReceipt);
                onRestored = null;
            }

            return PurchaseProcessingResult.Complete;
        }

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
        /// <returns>Refresh process successfully</returns>
        public void RestorePurchases(int timeoutMs, Action<PurchaseResponse> onDone) {
            if (!IsInit) {
                Debug.LogWarning("Cannot restore purchases. IAPManager not successfully initialized!");
                onDone(PurchaseResponse.NoInit);
                return;
            }

            onRestored = onDone;

            apple.RestoreTransactions(result => {
                // still waiting for result.
                if (onRestored != null) {
                    if (result) {
                        DebugLog("Waiting for restore...");
                        WaitForRestorePurchases(timeoutMs);
                    } else {
                        DebugLog("Restore process rejected.");
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
                DebugLog("Restore timeout");
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

        //*******************************************************************
        // REFRESH
        //*******************************************************************
        /// <summary>
        /// Refresh purchases asynchronously
        /// </summary>
        public async UniTask<bool> RefreshPurchasesAsync() {
            if (!IsInit)
                return false;

            bool? refreshResult = null;
            RefreshPurchases(OnRefreshed);
        
            // wait until purchase complete.
            await UniTask.WaitUntil(() => refreshResult.HasValue);
        
            return refreshResult.Value;

        
            void OnRefreshed(bool isSuccessful) {
                refreshResult = isSuccessful;
            }
        }
        
        /// <summary>
        /// Refresh purchases
        /// </summary>
        /// <returns>Refresh process successfully</returns>
        public void RefreshPurchases(Action<bool> onDone = null) {
            if (!IsInit) {
                Debug.LogWarning("Cannot refresh purchases. IAPManager not successfully initialized!");
                return;
            }

            apple.RefreshAppReceipt(successCallback: (success) => {
                DebugLog("Successfully refreshed purchases");
                onDone?.Invoke(true);
            }, errorCallback: () => {
                DebugLog("Refreshed purchase error");
                onDone?.Invoke(false);
            });
        }
        
        /// <summary>
        /// Print debug log (when DEBUG_IAP define is active)
        /// </summary>
        /// <param name="text">the data to be output.</param>
        static void DebugLog(object text) {
#if DEBUG_IAP
            Debug.Log(text);
#endif
        }
    }
}
#endif