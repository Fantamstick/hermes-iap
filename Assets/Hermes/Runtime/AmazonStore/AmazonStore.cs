using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using System.Text;
#if DEBUG_IAP
using System.Reflection;
#endif
using System.Threading.Tasks;
using HermesIAP;

namespace Hermes {
    /// <summary>
    /// Hermes In-App Purchase Manager for Amazon Store.
    /// </summary>
    public class AmazonStore : IStoreListener {
        IAppleConfiguration appleConfig;
        IExtensionProvider extensions;
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

        //*******************************************************************
        // Instantiation
        //*******************************************************************
        // Prevent class from being instanced explicitly outside.
        AmazonStore() {
        }
        
        internal static AmazonStore CreateInstance() {
            return new AmazonStore();
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
        /// <param name="iapBuilder">Builder data used to create instance.</param>
        /// <param name="onDone">Callback when initialization is done.</param>
        public void Init(IAPBuilder iapBuilder, Action<InitStatus> onDone) {
            if (IsInit) {
                onDone(initStatus);
                return;
            }
    
            googleTangleData = iapBuilder.GoogleTangleData ?? null;
            var module = iapBuilder.PurchasingModule ?? StandardPurchasingModule.Instance();
            var builder = ConfigurationBuilder.Instance(module);
    
            // Add Products to store.
            foreach (var key in iapBuilder.Products.Keys) {
                builder.AddProduct(key, iapBuilder.Products[key]);
            }
    
            onInitDone = onDone;

            UnityPurchasing.Initialize(this, builder);
        }
    
        void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions) {
            Debug.Log("IAP Manager successfully initialized");
            
            storeController = controller;
            this.extensions = extensions;
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

            Debug.LogWarning($"IAP init error: {error}");
            onInitDone(initStatus);
            onInitDone = null;
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
    
        
        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs args) {
            if (googleTangleData == null) {
                Debug.Log($" ***** ProcessGooglePurchase   googleTangleData IS NULL");
                OnPurchased?.Invoke(PurchaseResponse.Ok, args.purchasedProduct);
                return PurchaseProcessingResult.Complete;
            }
    
            // validate receipt.
            try {
                Debug.Log($" ***** ProcessGooglePurchase   validate receipt.");
                var validator = new CrossPlatformValidator(googleTangleData, null, Application.identifier);
                // On Google Play, result has a single product ID.
                // On Apple stores, receipts contain multiple products.
                var result = validator.Validate(args.purchasedProduct.receipt);
    #if DEBUG_IAP
                // For informational purposes, we list the receipt(s)
                foreach (IPurchaseReceipt receipt in result) {
                    var sb = new StringBuilder("Purchase Receipt Details:");
                    sb.Append($"\n  Product ID: {receipt.productID}");
                    sb.Append($"\n  Purchase Date: {receipt.purchaseDate}");
                    sb.Append($"\n  Transaction ID: {receipt.transactionID}");
                    Debug.Log(sb);
                }
    #endif
                OnPurchased?.Invoke(PurchaseResponse.Ok, args.purchasedProduct);
            } catch (IAPSecurityException err) {
                Debug.Log($"Invalid receipt or security exception: {err.Message}");
                OnPurchased?.Invoke(PurchaseResponse.InvalidReceipt, args.purchasedProduct);
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
    }
}

