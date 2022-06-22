using System;
using System.Text;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

public class GooglePlayStore : HermesStore
{
    //*******************************************************************
    // Instantiation
    //*******************************************************************
    // Prevent class from being instanced explicitly outside.
    GooglePlayStore() { }

    internal static GooglePlayStore CreateInstance()
    {
        return new GooglePlayStore();
    }
    
    protected override IStoreConfiguration GetStoreConfiguration(ConfigurationBuilder builder) 
    {
        return builder.Configure<IGooglePlayConfiguration>();
    }

    protected override IStoreExtension GetStoreExtensions(IExtensionProvider provider) 
    {
        return provider.GetExtension<IGooglePlayStoreExtensions>();
    }
    
    //*******************************************************************
    // PURCHASE
    //*******************************************************************
    protected override PurchaseProcessingResult OnProcessPurchase(PurchaseEventArgs purchaseEvent) 
    {
        DebugLog($"Purchased: {purchaseEvent.purchasedProduct.definition.id}");

        if ((extensions as IGooglePlayStoreExtensions).IsPurchasedProductDeferred(purchaseEvent.purchasedProduct)) 
        {
            //The purchase is Deferred.
            //Therefore, we do not unlock the content or complete the transaction.
            //ProcessPurchase will be called again once the purchase is Purchased.
            return PurchaseProcessingResult.Pending;
        }

        if (tangleData != null) 
        {
            // validate receipt.
            try 
            {
                DebugLog($" ***** ProcessGooglePurchase   validate receipt.");
                
                var validator = new CrossPlatformValidator(tangleData, null, Application.identifier);
                // On Google Play, result has a single product ID.
                // On Apple stores, receipts contain multiple products.
                var result = validator.Validate(purchaseEvent.purchasedProduct.receipt);

                if (iapBuilder.IsDebugLogEnabled) 
                {
                    foreach (IPurchaseReceipt receipt in result) 
                    {
                        var sb = new StringBuilder("Purchase Receipt Details:");
                        sb.Append($"\n  Product ID: {receipt.productID}");
                        sb.Append($"\n  Purchase Date: {receipt.purchaseDate}");
                        sb.Append($"\n  Transaction ID: {receipt.transactionID}");

                        if (receipt is GooglePlayReceipt googleReceipt)
                        {
                            // This is Google's Order ID.
                            // Note that it is null when testing in the sandbox
                            // because Google's sandbox does not provide Order IDs.
                            sb.Append($"\n  Purchase State: {googleReceipt.purchaseState}");
                            sb.Append($"\n  Purchase Token: {googleReceipt.purchaseToken}");
                        }

                        DebugLog(sb);
                    }
                }

                return onPurchaseSuccessCb != null ? 
                    onPurchaseSuccessCb(status, purchaseEvent.purchasedProduct) : 
                    PurchaseProcessingResult.Pending;
                
            } 
            catch (IAPSecurityException err) 
            {
                Debug.Log($"Invalid receipt or security exception: {err.Message}");
                onPurchaseFailureCb?.Invoke(status, PurchaseFailureReason.SignatureInvalid);
            } 
            catch (Exception err) 
            {
                Debug.Log($"Invalid receipt: {err.Message}");
                onPurchaseFailureCb?.Invoke(status, PurchaseFailureReason.Unknown);
            }
        }
        else 
        {
            // no tangle data.
            return onPurchaseSuccessCb != null ? 
                onPurchaseSuccessCb(status, purchaseEvent.purchasedProduct) : 
                PurchaseProcessingResult.Pending;
        }

        return PurchaseProcessingResult.Pending;
    }
}
