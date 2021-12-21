#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Google.Play.Billing.Internal;
using HermesGoogle = Hermes.GoogleUtil;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;
#if DEBUG_IAP
using System.Text;
using System.Reflection;
#endif

namespace Hermes {
    /// <summary>
    /// Hermes In-App Purchase Manager for GooglePlay
    /// </summary>
    public class GooglePlayStore : IStoreListener {
        IAppleConfiguration appleConfig;
        Google.Play.Billing.IGooglePlayStoreExtensions googlePlay;
        InitStatus initStatus;
        Action<InitStatus> onInitDone;
        IStoreController storeController;
        byte[] googleTangleData;
        
        /// <summary>
        /// Callback for when restore is completed.
        /// </summary>
        Action<PurchaseResponse> onRestored;

        /// <summary>
        /// Result of product purchase.
        /// </summary>
        public event Action<PurchaseResponse, Product> OnPurchased;
        
        /// <summary>
        /// Purchase request was deferred to parent.
        /// </summary>
        public event Action<Product> OnPurchaseDeferred;
        
        /// <summary>
        /// Has Hermes successfully initialized?
        /// </summary>
        public bool IsInit => initStatus == InitStatus.Ok;

        bool deferPurchaseCompatible;
        
        //*******************************************************************
        // Instantiation
        //*******************************************************************
        // Prevent class from being instanced explicitly outside.
        GooglePlayStore() {
        }
        
        internal static GooglePlayStore CreateInstance() {
            return new GooglePlayStore();
        }
        
        //*******************************************************************
        // INIT
        //*******************************************************************
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

            googleTangleData = iapBuilder.GoogleTangleData ?? null;
            IPurchasingModule module = Google.Play.Billing.GooglePlayStoreModule.Instance();
            ConfigurationBuilder builder = ConfigurationBuilder.Instance(module);

            // Add Products to store.
            foreach (var key in iapBuilder.Products.Keys) {
                builder.AddProduct(key, iapBuilder.Products[key]);
            }

            deferPurchaseCompatible = iapBuilder.DeferredPurchaseCompatible;
            
            onInitDone = onDone;
            UnityPurchasing.Initialize(this, builder);
        }
        
        void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions) {
            DebugLog("IAP Manager successfully initialized");
            
            storeController = controller;
            googlePlay = extensions.GetExtension<Google.Play.Billing.IGooglePlayStoreExtensions>();

            if (deferPurchaseCompatible) {
                // enable deferred purchases
                IPurchasingModule googlePlayModule = Google.Play.Billing.GooglePlayStoreModule.Instance();
                var configurationBuilder = ConfigurationBuilder.Instance(googlePlayModule);
                configurationBuilder.Configure<Google.Play.Billing.IGooglePlayConfiguration>().EnableDeferredPurchase();

                // Notify callback for when purchases are deferred to a parent.
                googlePlay.SetDeferredPurchaseListener(product => {
                    DebugLog("Purchase request deferred to parent.");
                    OnPurchaseDeferred?.Invoke(product);
                });
            }

            initStatus = InitStatus.Ok;

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
        /// <summary>
        /// Get the Product from the product's ID as registered from the platform's store.
        /// </summary>
        /// <param name="productID">Product's ID</param>
        /// <returns>Product information.</returns>
        public Product GetProduct(string productID) {
            var product = storeController.products.WithID(productID);
            if (product == null) {
                Debug.LogWarning($"{productID} is not a valid Product ID.");
                return null;
            }

            return product;
        }

        /// <summary>
        /// Is specified subscription active.
        /// </summary>
        /// <param name="productID">Product ID.</param>
        public async UniTask<bool> IsActiveSubscription(string productID) {
            Purchase[] purchases = await GetPurchasedSubscriptions(productID);
            if (purchases == null || purchases.Length == 0) {
                DebugLog($"{productID} has no purchase information.");
                return false;
            }

            Purchase purchase = purchases[0];
            if (purchase.TransactionId == null) {
                DebugLog($"{productID} has no purchase transactionId.");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Gets introductory offer details.
        /// Includes Free Trial.
        /// </summary>
        /// <param name="productID">Product ID</param>
        /// <returns>Offer details if exists.</returns>
        public async UniTask<IntroductoryOffer> GetIntroductoryOfferDetailsAsync(string productID) {
            Product[] products = await GetAvailableProductsAsync();
            if (products == null || products.Length == 0) 
                return null;

            var availableProducts = products.Where(pd => !pd.hasReceipt).ToArray();
            if (availableProducts == null || availableProducts.Length == 0 ) 
                return null;
            
            Product[] p = availableProducts.Where(a => a.definition.id == productID).ToArray();
            if (p.Length == 0) 
                return null;

            SkuDetails sku = GetSKU(productID);
            if (sku == null) 
                return null;
            
            return new GooglePlayIntroductoryOfferFactory(sku).Make();
        }
        
        /// <summary>
        /// Get subscription expiration date.
        /// todo: [GooglePlay] Not compatible with Google Play Developer API. This method doesn't get the real expired date.
        /// </summary>
        /// <param name="productID">Product ID</param>
        /// <param name="nextExpiredHours">next expired datetime. default: 24h </param>
        /// <returns>Expiration datetime. Null if already expired</returns>
        public async UniTask<DateTime?> GetSubscriptionExpiration(string productID, int nextExpiredHours = 24) {
            if (await IsActiveSubscription(productID)) {
                // existed receipt. return future date.
                return DateTime.Now.AddHours(nextExpiredHours);
            }
            
            return null;
        }

        /// <summary>
        /// Get subscription purchases by Google Play Billing Library:`BillingClient.queryPurchase()`
        /// </summary>
        /// <param name="productID">productId. if NULL, then return all purchases.</param>
        /// <returns></returns>
        public async UniTask<Purchase[]> GetPurchasedSubscriptions(string productID = null) {
            List<Purchase> purchases = await new HermesGoogle.GoogleUtil().QueryPurchases();
            if (purchases == null) 
                return null;
            
            if (productID == null) 
                return purchases.ToArray();
            
            return purchases.Where(p => p.ProductId == productID).ToArray();
        }

        public bool IsPurchasedProduct(Product product) {
            return product.hasReceipt;
        }

        /// <summary>
        /// Fetch additional products from the controlled store.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        async UniTask<Product[]> FetchSubscriptionPurchase(string productId) {
            if (!IsInit) {
                Debug.LogWarning("Cannot fetch subscription. IAPManager not successfully initialized!");
                return null;
            }

            HashSet<ProductDefinition> additionalProducts = new HashSet<ProductDefinition>() {
                new ProductDefinition(productId, productId, ProductType.Subscription)
            };
            
            Product[] result = null;
            bool isWaiting = true;
            DebugLog($"---FetchAdditionalProducts start!! productId = {productId}");

            storeController.FetchAdditionalProducts(additionalProducts, successCallback: () => {
                DebugLog($"---FetchAdditionalProducts success!! productId = {productId}");
                result = storeController.products.all.Where(p => p.definition.id == productId).ToArray();
#if DEBUG_IAP
                StringBuilder sb = new StringBuilder();
                foreach (var p in result) {
                    sb.Append("\n\n----------------\n");
                    foreach (PropertyInfo property in p.GetType().GetProperties()) {
                        sb.Append(property.Name).Append("=").Append(property.GetValue(p)).Append("\n");
                    }
                }

                DebugLog(sb);
#endif
                isWaiting = false;
            },
            (InitializationFailureReason reason) => {
                DebugLog($"---FetchAdditionalProducts fail!! productId = {productId}");
                DebugLog(reason);
                isWaiting = false;
            });

            await UniTask.WaitWhile(() => isWaiting);
            
            return result;
        }
        
        //*******************************************************************
        // PURCHASE
        //*******************************************************************
        public PurchaseRequest PurchaseProduct(string productID) {
            if (!IsInit) {
                Debug.LogWarning("Cannot purchase product. IAPManager not successfully initialized!");
                return PurchaseRequest.NoInit;
            }

            var product = GetProduct(productID);
            if (product == null) 
                return PurchaseRequest.ProductUnavailable;

            if (!product.availableToPurchase) {
                Debug.LogWarning($"Cannot purchase product {productID}. Not available for purchase!");
                return PurchaseRequest.PurchasingUnavailable;
            }

            // try to purchase product.
            storeController.InitiatePurchase(product);
            
            return PurchaseRequest.Ok;
        }

        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs e) {
            DebugLog("Processing a purchase");
            return ProcessGooglePurchase(e);
        }
        
        PurchaseProcessingResult ProcessGooglePurchase(PurchaseEventArgs e) {
            // receipt validation tangle data not available.
            if (googleTangleData == null) {
                DebugLog($" ***** ProcessGooglePurchase   googleTangleData IS NULL");
                OnPurchased?.Invoke(PurchaseResponse.Ok, e.purchasedProduct);
                return PurchaseProcessingResult.Complete;
            }

            // validate receipt.
            try {
                DebugLog($" ***** ProcessGooglePurchase   validate receipt.");
                var validator = new CrossPlatformValidator(googleTangleData, null, Application.identifier);
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
                    if (googleReceipt != null)
                    {
                        // This is Google's Order ID.
                        // Note that it is null when testing in the sandbox
                        // because Google's sandbox does not provide Order IDs.
                        sb.Append($"\n  Purchase State: {googleReceipt.purchaseState}");
                        sb.Append($"\n  Purchase Token: {googleReceipt.purchaseToken}");
                    }

                    DebugLog(sb);
                }
#endif
                OnPurchased?.Invoke(PurchaseResponse.Ok, e.purchasedProduct);
            } catch (IAPSecurityException err) {
                DebugLog($"Invalid receipt or security exception: {err.Message}");
                OnPurchased?.Invoke(PurchaseResponse.InvalidReceipt, e.purchasedProduct);
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
        /// Get all available products from cache.
        /// </summary>
        public Product[] GetAvailableProducts() {
            if (!IsInit) {
                Debug.LogWarning("Cannot get products. IAPManager not successfully initialized!");
                return null;
            }
            
            // (p.availableToPurchase always return True)
            return storeController.products.all.ToArray();
        }
        
        /// <summary>
        /// Get all available products with RESTORE GooglePlay
        /// </summary>
        public async UniTask<Product[]> GetAvailableProductsAsync() {
            DebugLog($"GetAvailableProductsAsync start");
            if (!IsInit) {
                Debug.LogWarning("Cannot get products. IAPManager not successfully initialized!");
                return null;
            }
            
            await RestorePurchasesAsync();
            // (p.availableToPurchase always return True)
            return storeController.products.all.ToArray();
        }
        
        //*******************************************************************
        // RESTORE
        //*******************************************************************
        /// <summary>
        /// Restore purchases
        /// (GooglePlay is automatic after Init)
        /// </summary>
        public async UniTask RestorePurchasesAsync(Action<PurchaseResponse> onDone = null) {
            if (!IsInit) {
                Debug.LogWarning("Cannot restore purchases. IAPManager not successfully initialized!");
                onDone(PurchaseResponse.NoInit);
                return;
            }
            
            bool isProcessing = true;

            googlePlay.RestoreTransactions(result => {
                DebugLog($"googlePlay.RestoreTransactions result = {result}");
                
                // still waiting for result.
                if (onDone != null) {
                    if (result) {
                        onDone(PurchaseResponse.Ok);
                    } else {
                        DebugLog("Restore process rejected.");
                        onDone(PurchaseResponse.Unknown);
                    }
                }

                isProcessing = false;
            });

            await UniTask.WaitWhile(() => isProcessing);
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


        /// <summary>
        /// Get SKU information jsons from Google Play
        /// </summary>
        /// <returns>Dictionary key:productId / value:SKU json</returns>
        public Dictionary<string, string> GetSKUsJson() {
            Dictionary<string, string> SKUs = googlePlay.GetProductJSONDictionary();
#if DEBUG_IAP
            foreach (var pair in SKUs) {
                DebugLog($"------- \n Key={pair.Key}\n Value={pair.Value}");
            }
#endif
            return SKUs;
        }

        /// <summary>
        /// Get SKU information from Google Play
        /// </summary>
        /// <returns>Dictionary key:productId / value:SKU detail</returns>
        public Dictionary<string, SkuDetails> GetSKUs() {
            Dictionary<string, string> SKUs = googlePlay.GetProductJSONDictionary();
            Dictionary<string, SkuDetails> SKUDetails = new Dictionary<string, SkuDetails>();
            
            foreach (var pair in SKUs) {
                SkuDetails detail = new SkuDetails();
                SkuDetails.FromJson(pair.Value, out detail);
                SKUDetails.Add(pair.Key, detail);
                DebugLog($"------- \n pair.Key={pair.Key}\ndetail.JsonSkuDetails={detail.JsonSkuDetails}");
            }

            return SKUDetails;
        }

        /// <summary>
        /// Get SKU information from Google Play
        /// </summary>
        /// <param name="productId">target product id</param>
        /// <returns></returns>
        public SkuDetails GetSKU(string productId) {
            Dictionary<string, string> SKUs = GetSKUsJson();
            if (!SKUs.ContainsKey(productId)) {
                DebugLog($"{productId} doesn't exist in SKU dictionary.");
                return null;
            }

            SkuDetails detail = new SkuDetails();
            SkuDetails.FromJson(SKUs[productId], out detail);
            DebugLog(detail.JsonSkuDetails);
            
            return detail;
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