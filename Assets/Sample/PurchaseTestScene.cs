﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Hermes;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using UnityEngine.UI;

/// <summary>
/// Test scene for IAP.
/// </summary>
public class PurchaseTestScene : MonoBehaviour 
{
    [SerializeField] string googlePlayProductId;
    [SerializeField] string AppleProductId;
    [Space(30)] [SerializeField] Text resultText;
    [SerializeField] Text productIdLabel;
    [SerializeField] Toggle receiptValidateToggle;
    readonly List<string> resultList = new List<string>();
    bool isReceiptVerified = true;
    
    string productId =>
#if UNITY_ANDROID
        googlePlayProductId;
#else
        AppleProductId;
#endif

    void Start() 
    {
#if UNITY_EDITOR || !(UNITY_IOS || UNITY_ANDROID)
        Debug.LogError("Test scene only works on iOS or Android devices!");
#endif
        OnClearTextClicked();
        
        receiptValidateToggle.onValueChanged.AddListener(OnReceiptVerificationToggled);
        productIdLabel.text = productId;
    }

    //========================================================
    // INIT
    //========================================================
    /// <summary>
    /// Initialization button clicked.
    /// </summary>
    public void OnClickInit() 
    {
        AppendText("clicked init button, waiting for response...");

        var products = new Dictionary<string, ProductType> 
        {
            {productId, ProductType.Subscription},
        };
        
        var iapBuilder = new IAPBuilder(products)
            .WithDebugLog()
            .WithAppleTangleData(AppleTangle.Data());

        try 
        {
            IAP.Instance.Init(
                iapBuilder, 
                OnInitSuccess, 
                OnInitFailure, 
                OnPurchaseSuccess, 
                OnPurchaseDeferred,
                OnPurchaseFailure);
        } 
        catch (InvalidOperationException e) 
        {
            AppendText($"Unable to init: {e.Message}");
        }
    }

    void OnInitSuccess() 
    {
        AppendText($"init success");
    }

    void OnInitFailure(InitializationFailureReason reason) 
    {
        AppendText($"init failure: {reason}");
    }

    PurchaseProcessingResult OnPurchaseSuccess(AppStore.Status status, Product product) 
    {
        AppendText($"purchase success during {status} for Product: {product.transactionID}. {product.definition.id}\n");
        
        StringBuilder sb = new StringBuilder();
        foreach (PropertyInfo property in product.GetType().GetProperties()) {
            sb.Append(property.Name).Append("=").Append(property.GetValue(product)).Append("\n");
        }

        AppendText(sb.ToString());
        
        return isReceiptVerified ? PurchaseProcessingResult.Complete : PurchaseProcessingResult.Pending;
    }

    void OnPurchaseDeferred(Product product) 
    {
        AppendText($"purchase deferred");
    }

    void OnPurchaseFailure(AppStore.Status status, PurchaseFailureReason reason) 
    {
        AppendText($"purchase failure during {status}. Reason: {reason}");
    }

    //========================================================
    // PURCHASE
    //========================================================
    /// <summary>
    /// Purchase button clicked.
    /// </summary>
    public void OnClickPurchase() 
    {
        AppendText("clicked purchase, waiting for response...");

        try 
        {
            IAP.Instance.PurchaseProduct(productId);
        } 
        catch (Exception e) 
        {
            AppendText($"Problem with purchase: {e.Message}");
        }
    }

    //========================================================
    // PURCHASE
    //========================================================
    public void OnConfirmPurchase()
    {
        AppendText("clicked confirm purchase, waiting for response...");

        try
        {
            IAP.Instance.ConfirmPendingPurchase(productId);
        } 
        catch (Exception e) 
        {
            AppendText($"Problem with confirming purchase: {e.Message}");
        }
    }
    
    //========================================================
    // RESTORE
    //========================================================
    /// <summary>
    /// Restore button clicked.
    /// </summary>
    public void OnClickRestore() 
    {
        AppendText("clicked restore, waiting for response...");

        try 
        {
            IAP.Instance.Restore(() => 
            {
                AppendText($"Restore attempt success");
            }, () => 
            {
                AppendText($"Restore attempt failed");
            });
        } 
        catch(Exception e) 
        {
            AppendText($"Restore attempt failed, {e.Message}");
        }
    }

    //========================================================
    // REFRESH
    //========================================================
    /// <summary>
    /// Refresh button clicked.
    /// </summary>
    public void OnClickRefresh() 
    {
#if UNITY_ANDROID
        AppendText("Android not support 'Refresh'");
        return;
#else
        AppendText("clicked refresh, waiting for response...");

        try 
        {
            IAP.Instance.Refresh((receipt) => 
            {
                AppendText($"Refresh attempt success with receipt: {receipt}");
            }, () => 
            {
                AppendText($"Refresh attempt failed");
            });
        } 
        catch(Exception e) 
        {
            AppendText($"Refresh attempt failed, {e.Message}");
        }
#endif
    }
    
    //========================================================
    // GET EXPIRATION
    //========================================================
    /// <summary>
    /// Get expiration button clicked.
    /// </summary>
    public void OnClickGetExpiration() 
    {
        AppendText("clicked get expiration, waiting for response...");

        try 
        {
            var subInfo = IAP.Instance.GetSubscriptionInfo(productId);
            if (subInfo == null) 
            {
                AppendText($"No sub info for product: {productId}");
            } 
            else 
            {
                AppendText($"Expiration: {subInfo.getExpireDate()}");
            }
        }
        catch(Exception e) 
        {
            AppendText($"get expiration attempt failed, {e.Message}");
        }
    }

    public void OnClickGetInfo() {
        AppendText("clicked GetInfo, waiting for response...");

        try 
        {
            var subInfo = IAP.Instance.GetSubscriptionInfo(productId);
            if (subInfo == null) 
            {
                AppendText($"No sub info for product: {productId}");
            }
            else 
            {
                StringBuilder sb = new StringBuilder();

                // https://docs.unity3d.com/ja/2019.4/Manual/UnityIAPSubscriptionProducts.html
                sb.Append("getProductId=").Append(subInfo.getProductId()).Append("\n");
                sb.Append("getPurchaseDate=").Append(subInfo.getPurchaseDate()).Append("\n");
                sb.Append("isSubscribed=").Append(subInfo.isSubscribed()).Append("\n");
                sb.Append("getExpireDate=").Append(subInfo.getExpireDate()).Append("\n");
                sb.Append("isExpired=").Append(subInfo.isExpired()).Append("\n");
                sb.Append("getCancelDate=").Append(subInfo.getCancelDate()).Append("\n");
                sb.Append("isCancelled=").Append(subInfo.isCancelled()).Append("\n");
                sb.Append("isAutoRenewing=").Append(subInfo.isAutoRenewing()).Append("\n");
                AppendText($"{productId} info. {sb}");
            }
        } 
        catch(Exception e) 
        {
            AppendText($"click get info attempt failed, {e.Message}");
        }
    }

    public void OnClickGetAvailableProducts() 
    {
        AppendText("clicked GetAllProducts, waiting for response...");

        try 
        {
            var products = IAP.Instance.GetAllProducts();

            foreach (Product product in products) 
            {
                AppendText(
                    $"{product.definition.id}: availableToPurchase={product.availableToPurchase}, hasReceipt={product.hasReceipt}");
            }
        }
        catch (Exception e) 
        {
            AppendText($"click available products failed, {e.Message}");
        }
    }

    //========================================================
    // TOGGLE RECEIPT VERIFICATION SUCCESS
    //========================================================
    void OnReceiptVerificationToggled(bool isOn)
    {
        isReceiptVerified = isOn;
        AppendText($"Auto receipt validation ON? {isOn}");
    }
    
    //========================================================
    // GET INTRODUCTORY OFFER DETAILS
    //========================================================
    public void OnClickIntroOffer() {
        string[] ids = new string[] {
            productId
        };
        
        foreach (var id in ids) {
            AppendText($"--{id}");
#if UNITY_ANDROID            
            var offer = IAP.Instance.GetIntroductoryOfferDetails(id);
#else
            var offer = IAP.Instance.GetIntroductoryOfferDetails(id);
#endif
            if (offer != null) {
                
                // AppendText($"Regular Price: {offer.RegularPrice}");
                // AppendText($"Intro Price: {offer.IntroductoryPrice}");
                // AppendText($"Duration: {offer.NumberOfUnits} {offer.Unit}");
                // AppendText($"Periods: {offer.NumberOfPeriods}");
                // AppendText($"Free Trial? {offer.IsFreeTrial}");
                
                AppendText($"Regular Price: {offer.RegularPrice}");
                AppendText($"Regular Duration {offer.RegularNumberOfUnit} {offer.RegularUnit}");

                AppendText($"Introductory?: {offer.IsIntroductory}");
                AppendText($"Intro Price: {offer.IntroductoryPrice} ({offer.LocalizedIntroductoryPriceString})");
                AppendText($"Intro Duration: {offer.IntroductoryNumberOfUnits} {offer.IntroductoryUnit}");
                AppendText($"Intro Periods: {offer.IntroductoryNumberOfPeriods}");
                
                AppendText($"Free Trial? {offer.IsFreeTrial}");
                AppendText($"Free Trial Duration {offer.FreeTrialNumberOfUnits} {offer.FreeTrialUnit}");
                AppendText($"Free Trial Periods {offer.FreeTrialNumberOfPeriods}");
            } else {
                AppendText($"{productId} has no introductory offer available");
            }
        }

    }

    //========================================================
    // UTILITY
    //========================================================
    /// <summary>
    /// Append text to log on screen.
    /// </summary>
    void AppendText(string str = default) {
        if (!string.IsNullOrEmpty(str)) {
            resultList.Add(str);
            Debug.Log(str);
        }

        resultText.text = "";
        foreach (var result in resultList) {
            resultText.text = result + "\n" + resultText.text;
        }
    }

    /// <summary>
    /// Clear log text.
    /// </summary>
    public void OnClearTextClicked() {
        resultList.Clear();
        AppendText();
    }
}