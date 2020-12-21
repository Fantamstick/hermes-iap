﻿#if UNITY_IOS || UNITY_IPHONE
// Some unity files such as AppleTangle only defines UNITY_IPHONE not UNITY_IOS
// To be absolutely sure all iOS-related files are covered, we create a new IOS define.
#define IOS
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        
        /// <summary>
        /// Callback for when restore is completed.
        /// </summary>
        Action<PurchaseResponse> onRestored;
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
                Debug.Log($"{productId} has not expiration date");
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
        
        SubscriptionInfo[] GetPurchasedSubscriptions(string productId) {
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

                foreach (AppleInAppPurchaseReceipt receipts in receipt.inAppPurchaseReceipts) {
                    //Debug.Log($"Valid receipt");
                    //Debug.Log($"Original Transaction ID: {receipts.originalTransactionIdentifier}");
                    //Debug.Log($"Intro Price Period: {receipts.isIntroductoryPricePeriod}");
                    //Debug.Log($"Product ID: {receipts.productID}");
                    //Debug.Log($"Product type: {receipts.productType}");
                    //Debug.Log($"Quantity: {receipts.quantity}");
#if DEBUG_IAP
                    Debug.Log($"Original Purchase Date: {receipts.originalPurchaseDate}");
                    Debug.Log($"Purchase Date: {receipts.purchaseDate}");
                    Debug.Log($"Cancellation Date: {receipts.cancellationDate}");
                    Debug.Log($"Subsc Expiration Date: {receipts.subscriptionExpirationDate}");
                    Debug.Log($"Free trial: {receipts.isFreeTrial}");
#endif
                }
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
                // For informational purposes, we list the receipt(s)
                foreach (IPurchaseReceipt receipt in result) {
                    Debug.Log($"Valid receipt: {receipt.productID} {receipt.purchaseDate} {receipt.transactionID}");
                    var googleReceipt = receipt as GooglePlayReceipt;
                    if (googleReceipt != null) {
                        // This is Google's Order ID.
                        // Note that it is null when testing in the sandbox
                        // because Google's sandbox does not provide Order IDs.
#if DEBUG_IAP
                        Debug.Log(googleReceipt.transactionID);
                        Debug.Log(googleReceipt.purchaseState);
                        Debug.Log(googleReceipt.purchaseToken);
#endif
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
                    onRestored?.Invoke(PurchaseResponse.DuplicateTransaction);
                    break;
                case PurchaseFailureReason.PaymentDeclined:
                    OnPurchased?.Invoke(PurchaseResponse.PaymentDeclined, p);
                    onRestored?.Invoke(PurchaseResponse.PaymentDeclined);
                    break;
                case PurchaseFailureReason.ProductUnavailable:
                    OnPurchased?.Invoke(PurchaseResponse.ProductUnavailable, p);
                    onRestored?.Invoke(PurchaseResponse.ProductUnavailable);
                    break;
                case PurchaseFailureReason.PurchasingUnavailable:
                    OnPurchased?.Invoke(PurchaseResponse.PurchasingUnavailable, p);
                    onRestored?.Invoke(PurchaseResponse.ProductUnavailable);
                    break;
                case PurchaseFailureReason.SignatureInvalid:
                    OnPurchased?.Invoke(PurchaseResponse.SignatureInvalid, p);
                    onRestored?.Invoke(PurchaseResponse.SignatureInvalid);
                    break;
                case PurchaseFailureReason.UserCancelled:
                    OnPurchased?.Invoke(PurchaseResponse.UserCancelled, p);
                    onRestored?.Invoke(PurchaseResponse.UserCancelled);
                    break;
                case PurchaseFailureReason.ExistingPurchasePending:
                    OnPurchased?.Invoke(PurchaseResponse.ExistingPurchasePending, p);
                    onRestored?.Invoke(PurchaseResponse.ExistingPurchasePending);
                    break;
                case PurchaseFailureReason.Unknown:
                    OnPurchased?.Invoke(PurchaseResponse.Unknown, p);
                    onRestored?.Invoke(PurchaseResponse.Unknown);
                    break;
            }
            
            onRestored = null;
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

#if IOS
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
            
            var apple = extensions.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions(result => {
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