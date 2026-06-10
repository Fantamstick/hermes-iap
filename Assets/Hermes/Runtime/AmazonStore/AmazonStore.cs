using System;
using System.Collections.Generic;
using System.Text;
using Hermes;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

public class AmazonStore : HermesStore
{
    //*******************************************************************
    // Instantiation
    //*******************************************************************
    // Prevent class from being instanced explicitly outside.
    AmazonStore() { }

    internal static AmazonStore CreateInstance()
    {
        return new AmazonStore();
    }

    protected override IStoreConfiguration GetStoreConfiguration(ConfigurationBuilder builder)
    {
        return builder.Configure<IAmazonConfiguration>();
    }

    protected override IStoreExtension GetStoreExtensions(IExtensionProvider provider)
    {
        return provider.GetExtension<IAmazonExtensions>();
    }

    //*******************************************************************
    // PURCHASE
    //*******************************************************************
    protected override PurchaseProcessingResult OnProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        DebugLog($"Purchased: {purchaseEvent.purchasedProduct.definition.id}");

        if (tangleData != null)
        {
            // validate receipt.
            try
            {
                DebugLog($" ***** ProcessAmazonPurchase   validate receipt.");

                var validator = new CrossPlatformValidator(tangleData, null, Application.identifier);
                var result = validator.Validate(purchaseEvent.purchasedProduct.receipt);

                if (iapBuilder.IsDebugLogEnabled)
                {
                    foreach (IPurchaseReceipt receipt in result)
                    {
                        var sb = new StringBuilder("Purchase Receipt Details:");
                        sb.Append($"\n  Product ID: {receipt.productID}");
                        sb.Append($"\n  Purchase Date: {receipt.purchaseDate}");
                        sb.Append($"\n  Transaction ID: {receipt.transactionID}");
                        DebugLog(sb);
                    }
                }

                return CallPurchaseSuccessCb(status, purchaseEvent.purchasedProduct);
            }
            catch (IAPSecurityException err)
            {
                Debug.LogWarning($"Invalid receipt or security exception: {err.Message}");
                CallPurchaseFailCb(status, PurchaseFailureReason.SignatureInvalid);
            }
            catch (Exception err)
            {
                Debug.LogWarning($"Invalid receipt: {err.Message}");
                CallPurchaseFailCb(status, PurchaseFailureReason.Unknown);
            }
        }
        else
        {
            // no tangle data.
            return CallPurchaseSuccessCb(status, purchaseEvent.purchasedProduct);
        }

        // Amazon Store does not support Pending — returning Pending causes infinite redelivery.
        return PurchaseProcessingResult.Complete;
    }

    //*******************************************************************
    // AMAZON SPECIFIC
    //*******************************************************************
    /// <summary>
    /// Gets the current Amazon user ID for use with other Amazon services.
    /// </summary>
    /// <returns>Amazon user ID, or null if not initialized.</returns>
    public string GetAmazonUserId()
    {
        if (!IsInitAndReady)
        {
            DebugLog("Unable to read amazonUserId. Hermes is not initialized yet.");
            return null;
        }

        return (extensions as IAmazonExtensions).amazonUserId;
    }

    /// <summary>
    /// Notifies Amazon that a product cannot be fulfilled.
    /// Calls Amazon's notifyFulfillment(transactionID, FulfillmentResult.UNAVAILABLE).
    /// Use this when a purchased product is no longer available to deliver.
    /// </summary>
    /// <param name="transactionID">Transaction ID of the product that cannot be fulfilled.</param>
    public void NotifyUnableToFulfillUnavailableProduct(string transactionID)
    {
        if (!IsInitAndReady)
        {
            throw new InvalidOperationException("Unable to notify fulfillment. Hermes is not initialized.");
        }

        (extensions as IAmazonExtensions).NotifyUnableToFulfillUnavailableProduct(transactionID);
    }

    /// <summary>
    /// Writes a JSON description of the product catalog to the device's SD card
    /// for use with Amazon's local Sandbox testing app.
    /// </summary>
    /// <param name="products">Products to include in the sandbox JSON.</param>
    public void WriteSandboxJSON(HashSet<ProductDefinition> products)
    {
        if (configuration == null)
        {
            throw new InvalidOperationException("Unable to write sandbox JSON. Hermes is not initialized.");
        }

        (configuration as IAmazonConfiguration).WriteSandboxJSON(products);
    }
}
