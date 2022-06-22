using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hermes;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

public class AppStore : HermesStore
{
    //*******************************************************************
    // Instantiation
    //*******************************************************************
    // Prevent class from being instanced explicitly outside.
    AppStore() { }
    
    internal static AppStore CreateInstance() 
    {
        return new AppStore();
    }
    
    protected override IStoreConfiguration GetStoreConfiguration(ConfigurationBuilder builder) 
    {
        return builder.Configure<IAppleConfiguration>();
    }

    protected override IStoreExtension GetStoreExtensions(IExtensionProvider provider) 
    {
        return provider.GetExtension<IAppleExtensions>();
    }

    //*******************************************************************
    // PURCHASE
    //*******************************************************************
    /*
    * This value is true if the user can authorize payments in the App Store; otherwise false.
    * The value of canMakePayments is false when users set content and privacy controls to limit a child's ability to purchase content.
    * It can also be false if the device has a mobile device management (MDM) profile that doesn’t allow payments.
    */
    public bool CanMakePayments() 
    {
        if (configuration == null) 
        {
            DebugLog("Unable to read canMakePayments value. Hermes is not initialized yet.");
            return false;
        }

        return (configuration as IAppleConfiguration).canMakePayments;;
    }
    
    protected override PurchaseProcessingResult OnProcessPurchase(PurchaseEventArgs purchaseEvent) {
        DebugLog($"Purchased: {purchaseEvent.purchasedProduct.definition.id}");

        // receipt validation tangle data not available.
        if (tangleData != null) 
        {
            // validate receipt.
            try 
            {
                var receiptData = System.Convert.FromBase64String((configuration as IAppleConfiguration).appReceipt);
                AppleReceipt receipt = new AppleValidator(tangleData).Validate(receiptData);

                if (iapBuilder.IsDebugLogEnabled) {
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
                }
                
                return onPurchaseSuccessCb != null ? 
                    onPurchaseSuccessCb(status, purchaseEvent.purchasedProduct) : 
                    PurchaseProcessingResult.Pending;
                
            } catch (IAPSecurityException err) {
                Debug.Log($"Invalid receipt or security exception: {err.Message}");
                onPurchaseFailureCb?.Invoke(status, PurchaseFailureReason.SignatureInvalid);
            } catch (Exception err) {
                Debug.Log($"Invalid receipt: {err.Message}");
                onPurchaseFailureCb?.Invoke(status, PurchaseFailureReason.Unknown);
            }
        } else {
            // no tangle data.
            return onPurchaseSuccessCb != null ? 
                onPurchaseSuccessCb(status, purchaseEvent.purchasedProduct) : 
                PurchaseProcessingResult.Pending;
        }
        
        // purchase failed. 
        return PurchaseProcessingResult.Pending;
    }

    /// <summary>
    /// Gets introductory offer details.
    /// Includes Free Trial.
    /// </summary>
    /// <param name="productID">Product ID</param>
    /// <param name="groupProductIDs">Group products that productID belongs to.
    /// If empty or null, assume productID is in its own group.</param>
    /// <returns>Offer details if exists.</returns>
    public IntroductoryOffer GetIntroductoryOfferDetailsAsync(string productID, string[] groupProductIDs = null) 
    {
        Dictionary<string,string> products = (extensions as IAppleExtensions).GetProductDetails();
        
        if (products == null || !products.ContainsKey(productID)) 
        {
            return null;
        }

        // Get product details.
        IntroductoryOffer offer = null;
        
        try 
        {
            offer = new IOSIntroductoryOfferFactory(products[productID]).Make();
        } 
        catch (InvalidOfferException) 
        {
            return null;
        } 
        catch(Exception e) 
        {
            // Invalid JSON
            Debug.LogWarning($"Invalid product data detected! {e.Message}");
            return null;
        }

        try 
        {
            var receiptData = System.Convert.FromBase64String((configuration as IAppleConfiguration).appReceipt);
            AppleReceipt receipt = new AppleValidator(tangleData).Validate(receiptData);
            if (receipt == null || receipt.inAppPurchaseReceipts == null) 
            {
                // no previous subscription purchased. 
                return offer;
            }

            if (groupProductIDs == null || groupProductIDs.Length == 0) 
            {
                groupProductIDs = new string[] {productID};
            }
            
            var prevCampaignPurchase = receipt.inAppPurchaseReceipts
                .FirstOrDefault(r => 
                    groupProductIDs.Contains(r.productID) &&
                    (r.isFreeTrial != 0 || r.isIntroductoryPricePeriod != 0));
                
            if(prevCampaignPurchase != null) 
            {
                // user already used free trial or introductory offer. 
                return null;
            }   
        } catch {
            // unable to validate receipt or unable to access.
            return null;
        }

        return offer;
    }
    
    //*******************************************************************
    // REFRESH
    //*******************************************************************
    /// <summary>
    /// Refresh and get the latest receipt.
    /// </summary>
    /// <param name="onCompleted">Refresh complete with latest receipt. </param>
    /// <param name="onFailed">Refresh failed.</param>
    public void Refresh(Action<string> onCompleted, Action onFailed) {
        if (!IsInitAndReady) {
            throw new InvalidOperationException("Unable to restore purchase. Hermes is not initialized.");
        }

        DebugLog("Refreshing purchases...");
        status = Status.Refresh;

        (extensions as IAppleExtensions).RefreshAppReceipt(latestReceipt => {
            // This handler is invoked if the request is successful.
            // Receipt will be the latest app receipt.
            DebugLog("Successfully refreshed purchases");
            status = Status.Idle;
            onCompleted?.Invoke(latestReceipt);
        }, () => {
            // This handler will be invoked if the request fails,
            // such as if the network is unavailable or the user
            // enters the wrong password.
            DebugLog("Refresh purchases unsuccessful");
            status = Status.Idle;
            onFailed?.Invoke();
        });
    }
}
