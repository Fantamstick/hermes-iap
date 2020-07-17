﻿#if UNITY_IOS || UNITY_IPHONE
// Some unity files such as AppleTangle only defines UNITY_IPHONE not UNITY_IOS
// To be absolutely sure all iOS-related files are covered, we create a new IOS define.
#define IOS
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

namespace FantamIAP {
    /// <summary>
    /// In-App Purchase Manager.
    /// </summary>
    public class IAPManager : IStoreListener {
        public static IAPManager Instance { get; } = new IAPManager();
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
        // Prevent class from being instanced.
        IAPManager() {
        }

        /// <summary>
        /// Is IAP Manager initialized.
        /// </summary>
        public bool IsInit => initStatus == InitStatus.Ok;

        public void Init(IAPBuilder iapBuilder, Action<InitStatus> onDone) {
            if (IsInit) {
                onInitDone(initStatus);
                return;
            }

            appleTangleData = iapBuilder.AppleTangleData ?? null;
            googleTangleData = iapBuilder.GoogleTangleData ?? null;
            var module = iapBuilder.PurchasingModule ?? StandardPurchasingModule.Instance();
            var builder = ConfigurationBuilder.Instance(module);
#if IOS
            // Verify if purchases are possible on this iOS device.
            var canMakePayments = builder.Configure<IAppleConfiguration>().canMakePayments;
            if (!canMakePayments) {
                onDone(InitStatus.PurchasingDisabled);
                return;
            }

            appleConfig = builder.Configure<IAppleConfiguration>();
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
        }

        //*******************************************************************
        // SUBSCRIPTION
        //*******************************************************************
        /// <summary>
        /// Is specified subscription active.
        /// </summary>
        /// <param name="productId">Product id.</param>
        public bool IsSubscriptionActive(string productId) {
            var expireDate = GetSubscriptionExpiration(productId);
            if (!expireDate.HasValue) {
                return false;
            }
            
            // instance is later than now
            return expireDate.Value.CompareTo(DateTime.Now) > 0;
        }
        
        /// <summary>
        /// Get subscription expiration date.
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>Expiration date. Null if already expired</returns>
        public DateTime? GetSubscriptionExpiration(string productId) {
            return GetPurchasedSubscription(productId)?.getExpireDate();
        }
        
        SubscriptionInfo GetPurchasedSubscription(string productId) {
            var product = GetAvailableProducts().FirstOrDefault(p => p.definition.id == productId);

            if (product == null) {
                Debug.LogWarning($"Product id: ${productId} does not exist!");
                return null;
            }
            if (product.definition.type != ProductType.Subscription) {
                Debug.LogWarning($"{productId} is not a subscription!");
                return null;
            }
            if (!product.hasReceipt) {
                Debug.Log($"Subscription {productId} is not purchased");
                return null;
            }
            
            var subscription = new SubscriptionManager(product, null).getSubscriptionInfo();
            if (subscription == null) {
                Debug.LogWarning($"Subscription with receipt ${product.receipt} is not a valid subscription.");
                return null;
            }

            return subscription;
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
                return PurchaseProcessingResult.Complete;
            }
            
            // validate receipt.
            try {
                var receiptData = System.Convert.FromBase64String(appleConfig.appReceipt);
                AppleReceipt receipt = new AppleValidator(appleTangleData).Validate(receiptData);

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
                OnPurchased?.Invoke(PurchaseResponse.Ok, e.purchasedProduct);
            } catch (IAPSecurityException err) {
                Debug.Log($"Invalid receipt or security exception: {err.Message}");
                OnPurchased?.Invoke(PurchaseResponse.InvalidReceipt, e.purchasedProduct);
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

#if IOS
        //*******************************************************************
        // RESTORE
        //*******************************************************************
        /// <summary>
        /// Restore purchases
        /// (GooglePlay is automatic after Init)
        /// </summary>
        public void RestorePurchases(int timeoutMs, Action<PurchaseResponse, Product> onRestore, Action onCancel, Action onTimeout) {
            if (!IsInit) {
                Debug.LogError("Cannot restore purchases. IAPManager not successfully initialized!");
                return;
            }

            var apple = extensions.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions(result => {
                if (result) {
                    Debug.Log("Trying to restore");
                    WaitForRestorePurchases(timeoutMs, onRestore, onTimeout);
                }
                else {
                    Debug.Log("Restore cancelled");
                    onCancel();
                }
            });
        }

        async void WaitForRestorePurchases(int timeoutMs, Action<PurchaseResponse, Product> onRestore,
            Action onTimeout) {
            OnPurchased += OnPurchasedHandler;

            int waitTiem = 0;
            int waitFrame = 100;
            bool isRestored = false;
            while (!isRestored && !IsTimeout()) {
                await Task.Delay(waitFrame);
                waitTiem += waitFrame;
            }

            if (IsTimeout()) {
                Debug.Log("Restore timeout");
                onTimeout?.Invoke();
            }

            OnPurchased -= OnPurchasedHandler;

            bool IsTimeout() => waitTiem >= timeoutMs;

            void OnPurchasedHandler(PurchaseResponse resp, Product product) {
                Debug.Log("Product(s) restored");
                isRestored = true;
                onRestore?.Invoke(resp, product);
            }
        }
        
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