/*
using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using System.Text;
#if DEBUG_IAP
using System.Reflection;
#endif
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Hermes {
    /// <summary>
    /// Hermes In-App Purchase Manager for Amazon Store.
    /// </summary>
    public class AmazonStore : IStoreListener {
        IAppleConfiguration appleConfig;
        IAmazonExtensions amazon;
        InitStatus initStatus;
        Action<InitStatus> onInitDone;
        IStoreController storeController;
        byte[] googleTangleData;

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
                Debug.LogError("Hermes is already in the process of initializing.");
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
            amazon = extensions.GetExtension<IAmazonExtensions>();
            initStatus = InitStatus.Ok;
            
            onInitDone?.Invoke(initStatus);
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
            onInitDone?.Invoke(initStatus);
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
                Debug.Log($" ***** ProcessAmazonPurchase   validate receipt.");
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
    }
}
*/

