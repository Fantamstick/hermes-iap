using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

namespace FantamIAP {
    /// <summary>
    /// In-App Purchase Manager.
    /// </summary>
    public class IAPManager: IStoreListener {
        static IAPManager instance = default;
        public static IAPManager Instance => instance ?? new IAPManager();

        /// <summary>
        /// Result of product purchase.
        /// </summary>
        public event Action<PurchaseResponse, Product> OnPurchased;
#if UNITY_IOS
        /// <summary>
        /// Purchase request was deferred to parent.
        /// </summary>
        public event Action<Product> OnPurchaseDeferred; 
#endif
        /// <summary>
        /// Is IAP Manager initialized.
        /// </summary>
        public bool IsInit => initStatus == InitStatus.Ok;
        
        IStoreController storeController;
        IExtensionProvider extensions;
        InitStatus initStatus;
#if UNITY_IOS
        IAppleConfiguration appleConfig;
#endif
        //*******************************************************************
        // INIT
        //*******************************************************************
        // Prevent class from being instanced.
        IAPManager() { }

        public async UniTask<InitStatus> InitAsync(Dictionary<string, ProductType> products) {
            return await InitAsync(products, StandardPurchasingModule.Instance());
        }
        
        public async UniTask<InitStatus> InitAsync(Dictionary<string, ProductType> products, IPurchasingModule purchasingModel) {
            var builder = ConfigurationBuilder.Instance(purchasingModel);
#if UNITY_IOS
            // Verify if purchases are possible on this iOS device.
            bool canMakePayments = builder.Configure<IAppleConfiguration>().canMakePayments;
            if (!canMakePayments) {
                initStatus = InitStatus.PurchasingDisabled;
                return initStatus;
            }
            
            appleConfig = builder.Configure<IAppleConfiguration>();
#endif
            // Add Products to store.
            foreach (var key in products.Keys) {
                builder.AddProduct(key, products[key]);
            }

            UnityPurchasing.Initialize(this, builder);

            // Wait for Unity Purchase to initialize.
            await UniTask.WaitWhile(() => initStatus == InitStatus.None);

            return initStatus;
        }
        
        void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions) {
            this.storeController = controller;
            this.extensions = extensions;
            this.initStatus = InitStatus.Ok;
#if UNITY_IOS
            // Notify callback for when purchases are deferred to a parent.
            extensions.GetExtension<IAppleExtensions>().RegisterPurchaseDeferredListener(product => {
                Debug.Log("Purchase request deferred to parent.");
                OnPurchaseDeferred?.Invoke(product);
            });
#endif
        }
        
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
        }
        
        //*******************************************************************
        // SUBSCRIPTION
        //*******************************************************************
        /// <summary>
        /// Is specified subscription active.
        /// </summary>
        /// <param name="receipt">Product receipt.</param>
        public bool IsSubscriptionActive(string receipt) {
            var product = GetAvailableProducts().FirstOrDefault(p => p.receipt == receipt);
            if (product == null) {
                Debug.LogWarning($"Subscript with receipt: ${receipt} does not exist!");
                return false;
            }
            
            if (!ValidSubscriptionFormat(receipt)) {
                Debug.LogWarning($"Subscript with receipt: ${receipt} is not a valid subscription!");
                return false;
            }

            var subscription = new SubscriptionManager(product, intro_json: null).getSubscriptionInfo();
            DateTime expireDate = subscription.getExpireDate();

            // instance is later than now
            return expireDate.CompareTo(DateTime.Now) > 0;
        }
        
        bool ValidSubscriptionFormat(string receipt) {
            const string JsonKey = "json";
            const string StoreKey = "Store";
            const string PayloadKey = "Payload";
            const string devPayloadKey = "developerPayload";
            
            var receiptWrapper = JsonUtility.FromJson<Dictionary<string, object>>(receipt);
            if (!receiptWrapper.ContainsKey(StoreKey) || !receiptWrapper.ContainsKey(PayloadKey)) {
                Debug.Log("The subscription product receipt does not contain enough information");
                return false;
            }

            var payload = (string)receiptWrapper[PayloadKey];
            if (payload != null ) {
                var store = (string)receiptWrapper[StoreKey];
                switch (store) {
                    case GooglePlay.Name: 
                        var payloadWrapper = JsonUtility.FromJson<Dictionary<string, object>>(payload);
                        if (!payloadWrapper.ContainsKey(JsonKey)) {
                            Debug.Log($"The product receipt does not contain enough information, the '${JsonKey}' field is missing");
                            return false;
                        }
                        
                        var origJsonPayloadWrapper = JsonUtility.FromJson<Dictionary<string, object>>((string)payloadWrapper[JsonKey]);
                        if (origJsonPayloadWrapper == null || !origJsonPayloadWrapper.ContainsKey(devPayloadKey)) {
                            Debug.Log($"The product receipt does not contain enough information, the '{devPayloadKey}' field is missing");
                            return false;
                        }
                        
                        var devPayloadJson = (string)origJsonPayloadWrapper[devPayloadKey];
                        var devPayloadWrapper = JsonUtility.FromJson<Dictionary<string, object>>(devPayloadJson);
                        if (devPayloadWrapper == null || !devPayloadWrapper.ContainsKey("is_free_trial") || !devPayloadWrapper.ContainsKey("has_introductory_price_trial")) {
                            Debug.Log("The product receipt does not contain enough information, the product is not purchased using 1.19 or later");
                            return false;
                        }
                        
                        return true;
                    
                    case AppleAppStore.Name:
                    case AmazonApps.Name:
                    case MacAppStore.Name: 
                        return true;
                    
                    default:
                        return false;
                }
            }
            
            return false;
        }
        
        //*******************************************************************
        // PURCHASE
        //*******************************************************************
        /// <summary>
        /// Try to Purchase a product
        /// </summary>
        public void PurchaseProduct(string productId) {
            if (!IsInit) {
                Debug.LogError("Cannot purchase product. IAPManager not successfully initialized!");
                return;
            }

            var product = storeController.products.WithID(productId);
            if (product == null) {
                Debug.LogWarning("Cannot purchase product. Not found!");
                return;
            }

            if (!product.availableToPurchase) {
                Debug.LogWarning("Cannot purchase product. Not available for purchase!");
                return;
            }
            
            // try to purchase product.
            storeController.InitiatePurchase(product);
        }

        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs e) {
            try {
#if UNITY_IOS
                var receiptData = System.Convert.FromBase64String(appleConfig.appReceipt);
                AppleReceipt receipt = new AppleValidator(AppleTangle.Data()).Validate(receiptData);

                foreach (AppleInAppPurchaseReceipt receipts in receipt.inAppPurchaseReceipts) {
                    Debug.Log($"Valid receipt");
                    Debug.Log($"Original Transaction ID: ${receipts.originalTransactionIdentifier}");
                    Debug.Log($"Intro Price Period: ${receipts.isIntroductoryPricePeriod}");
                    Debug.Log($"Product ID: ${receipts.productID}");
                    Debug.Log($"Product type: ${receipts.productType}");
                    Debug.Log($"Quantity: ${receipts.quantity}");
                    Debug.Log($"Original Purchase Date: ${receipts.originalPurchaseDate}");
                    Debug.Log($"Purchase Date: ${receipts.purchaseDate}");
                    Debug.Log($"Cancellation Date: ${receipts.cancellationDate}");
                    Debug.Log($"Subsc Expiration Date: ${receipts.subscriptionExpirationDate}");
                    Debug.Log($"Free trial: {receipts.isFreeTrial}");
                }
#elif UNITY_ANDROID
                var validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
                // On Google Play, result has a single product ID.
                // On Apple stores, receipts contain multiple products.
                var result = validator.Validate(e.purchasedProduct.receipt);
                // For informational purposes, we list the receipt(s)
                foreach (IPurchaseReceipt receipt in result) {
                    Debug.Log($"Valid receipt: {receipt.productID} {receipt.purchaseDate} {receipt.transactionID}");
                    var googleReceipt = receipt as GooglePlayReceipt;
                    if (googleReceipt != null) {
                        // This is Google's Order ID.
                        // Note that it is null when testing in the sandbox
                        // because Google's sandbox does not provide Order IDs.
                        Debug.Log(googleReceipt.transactionID);
                        Debug.Log(googleReceipt.purchaseState);
                        Debug.Log(googleReceipt.purchaseToken);
                    }
                }
#endif
                OnPurchased?.Invoke(PurchaseResponse.Ok, e.purchasedProduct);
            } catch (IAPSecurityException err) {
                Debug.Log($"Invalid receipt or security exception: {err.Message}");
                OnPurchased?.Invoke(PurchaseResponse.InvalidReceipt, e.purchasedProduct);
            } 

            return PurchaseProcessingResult.Complete;
        }

        void IStoreListener.OnPurchaseFailed(Product p, PurchaseFailureReason reason) {
            Debug.LogWarning($"IAP purchase error: {reason}");
            
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
                default:
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
                Debug.LogError("Cannot get products. IAPManager not successfully initialized!");
                return null;
            }

            // only return all products that are available for purchase.
            return storeController.products.all.Where(p => p.availableToPurchase).ToArray();
        }
        
#if UNITY_IOS
        //*******************************************************************
        // RESTORE
        //*******************************************************************
        /// <summary>
        /// Restore purchases
        /// (GooglePlay is automatic after Init)
        /// </summary>
        public void RestorePurchases() {
            if (!IsInit) {
                Debug.LogError("Cannot restore purchases. IAPManager not successfully initialized!");
                return;
            }

            var apple = extensions.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions(result => {
                Debug.Log("RestorePurchases continuing: " + result);
            });
        }
#endif
    }
}
