using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using Hermes;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using UnityEngine.UI;
#if UNITY_ANDROID
using Google.Play.Billing.Internal;
#endif

/// <summary>
/// Test scene for IAP.
/// </summary>
public class PurchaseTestScene : MonoBehaviour
{
    [SerializeField] string googlePlayProductId;
    [SerializeField] string AppleProductId;
    [Space(30)] [SerializeField] Text resultText;
    [SerializeField] Text productIdLabel;
    List<string> resultList = new List<string>();

    string productId =>
#if UNITY_ANDROID
        googlePlayProductId;
#else
        AppleProductId;
#endif

    void Start()
    {
#if UNITY_EDITOR
        Debug.LogError("Test scene only works on devices!");
#endif

        OnClearTextClicked();

        productIdLabel.text = productId;

        IAP.Instance.OnPurchased += OnPurchased;
    }

    //========================================================
    // INIT
    //========================================================
    /// <summary>
    /// Initialization button clicked.
    /// </summary>
    public void OnClickInit()
    {
        AppendText("click init, waiting for response...");

        var products = new Dictionary<string, ProductType>
        {
            {productId, ProductType.Subscription},
        };
        var iapBuilder = new IAPBuilder(products).WithAppleTangleData(AppleTangle.Data());
        IAP.Instance.Init(iapBuilder, OnInit);
    }

    /// <summary>
    /// IAP manager initialized.
    /// </summary>
    void OnInit(InitStatus status)
    {
        AppendText($"IAP init status {status}");
    }

    //========================================================
    // PURCHASE
    //========================================================
    /// <summary>
    /// Purchase button clicked.
    /// </summary>
    public void OnClickPurchase()
    {
        AppendText("click purchase, waiting for response...");

        var request = IAP.Instance.PurchaseProduct(productId);
        if (request != PurchaseRequest.Ok)
        {
            AppendText($"problem with purchase: {request}");
        }
    }

    /// <summary>
    /// Purchase process completed.
    /// </summary>
    /// <param name="resp">Response from IAP listener.</param>
    /// <param name="product">Product data.</param>
    void OnPurchased(PurchaseResponse resp, Product product)
    {
        StringBuilder sb = new StringBuilder();
        foreach (PropertyInfo property in product.GetType().GetProperties())
        {
            sb.Append(property.Name).Append("=").Append(property.GetValue(product)).Append("\n");
        }

        AppendText($"purchase result {resp} for Product: {product.transactionID}. {product.definition.id}\n");
        AppendText(sb.ToString());
        Debug.Log(sb);
    }

    //========================================================
    // RESTORE
    //========================================================
    /// <summary>
    /// Restore button clicked.
    /// </summary>
    public async UniTask OnClickRestore()
    {
#if UNITY_ANDROID
        AppendText("clicked restore, waiting for response...");
        await Hermes.IAP.Instance.RestorePurchasesAsync( onDone: (resp) => {
            AppendText($"Restore attempt, {resp} from product");
        });
        
        await OnClickGetExpiration();
#else
        // simulate restore wait period.
        await UniTask.Delay(TimeSpan.FromSeconds(1));
        
        AppendText("clicked restore, waiting for response...");
        Hermes.IAP.Instance.RestorePurchases(20_000, onDone: (resp) =>
        {
            AppendText($"Restore attempt, {resp} from product");

            OnClickGetExpiration();
        });
#endif
    }

    //========================================================
    // GET EXPIRATION
    //========================================================
    /// <summary>
    /// Get expiration button clicked.
    /// </summary>
    public async UniTask OnClickGetExpiration()
    {
#if UNITY_ANDROID
        var ret = await Hermes.IAP.Instance.IsActiveSubscription(productId);
        AppendText($"{productId} subscription active:{ret}");
#else
        var expDate = await IAP.Instance.GetSubscriptionExpiration(productId);
        if (expDate.HasValue)
        {
            AppendText($"{productId} expiration date = {expDate.Value}");
        }
        else
        {
            AppendText($"{productId} has no expiration date");
        }
#endif
    }

    public async UniTask OnClickGetInfo()
    {
        AppendText("OnClick GetInfo...");
#if UNITY_ANDROID
        Purchase[] purchases = await Hermes.IAP.Instance.GetPurchasedSubscriptions(productId);
        Debug.Log("OnClick GetInfo...");
        if (purchases == null || purchases.Length == 0)
        {
            AppendText($"{productId} has no purchases.");
        }
        else
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < purchases.Length; i++)
            {
                var purchase = purchases[i];
                sb.Append(i).Append("----------------------------------\n");
                foreach (PropertyInfo property in purchase.GetType().GetProperties())
                {
                    sb.Append(property.Name).Append("=").Append(property.GetValue(purchase)).Append("\n");
                }
                sb.Append("\n\n");
            }
            AppendText($"{productId} has {purchases.Length} purchases. {sb}");
        }
#else
        SubscriptionInfo receipt = IAP.Instance.GetSubscriptionInfo(productId);
        if (receipt == null)
        {
            AppendText($"{productId} has no info");
        }
        else
        {
            StringBuilder sb = new StringBuilder();

            // https://docs.unity3d.com/ja/2019.4/Manual/UnityIAPSubscriptionProducts.html
            sb.Append("getProductId=").Append(receipt.getProductId()).Append("\n");
            sb.Append("getPurchaseDate=").Append(receipt.getPurchaseDate()).Append("\n");
            sb.Append("isSubscribed=").Append(receipt.isSubscribed()).Append("\n");
            sb.Append("getExpireDate=").Append(receipt.getExpireDate()).Append("\n");
            sb.Append("isExpired=").Append(receipt.isExpired()).Append("\n");
            sb.Append("getCancelDate=").Append(receipt.getCancelDate()).Append("\n");
            sb.Append("isCancelled=").Append(receipt.isCancelled()).Append("\n");
            //sb.Append("getRemainingTime=").Append(receipt.getRemainingTime()).Append("\n");
            // sb.Append("getSkuDetails=").Append(receipt.getSkuDetails()).Append("\n");
            // sb.Append("getSubscriptionPeriod=").Append(receipt.getSubscriptionPeriod()).Append("\n");
            // sb.Append("isIntroductoryPricePeriod=").Append(receipt.isIntroductoryPricePeriod()).Append("\n");
            
            // 次の課金更新 あり/ なし
            sb.Append("isAutoRenewing=").Append(receipt.isAutoRenewing()).Append("\n");
            // sb.Append("getSubscriptionInfoJsonString=").Append(receipt.getSubscriptionInfoJsonString()).Append("\n");
            
            Debug.Log($"{productId} info. {sb}");
        }
#endif
    }

    public async void OnClickGetAvailableProducts()
    {
        AppendText("GetAvailableProducts");
#if UNITY_ANDROID
        Product[] products = await Hermes.IAP.Instance.GetAvailableProductsAsync();
#else
        Product[] products = Hermes.IAP.Instance.GetAvailableProducts();
#endif
        if (products.Length == 0)
        {
            AppendText("no products.");
        }

        foreach (Product product in products)
        {
            AppendText(
                $"{product.definition.id}: availableToPurchase={product.availableToPurchase}, hasReceipt={product.hasReceipt}");
        }
    }

    public void OnClickSKU()
    {
#if UNITY_ANDROID
        AppendText("Get SKU");
        var skus = Hermes.IAP.Instance.GetSKUs();
        foreach (var pair in skus)
        {
            AppendText("----------------");
            AppendText(pair.Key);
            AppendText("--");
            AppendText(pair.Value.JsonSkuDetails);
        }
#else
        AppendText("IOS not support 'get SKU'");
#endif
    }

    //========================================================
    // GET INTRODUCTORY OFFER DETAILS
    //========================================================
    public async void OnClickIntroOffer()
    {
        string[] ids = new string[]
        {
            productId
        };
        foreach (var id in ids)
        {
            AppendText($"--{id}");
#if UNITY_ANDROID            
            var offer = await Hermes.IAP.Instance.GetIntroductoryOfferDetailsAsync(id);
#else
            var offer = await Hermes.IAP.Instance.GetIntroductoryOfferDetailsAsync(id);
#endif
            if (offer != null)
            {
                
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
            }
            else
            {
                AppendText($"{productId} has no introductory offer available");
            }
        }

    }

    //========================================================
    // UTILITY
    //========================================================
    /// <summary>
    /// Append text to log.
    /// </summary>
    void AppendText(string str = default)
    {
        if (!string.IsNullOrEmpty(str))
        {
            resultList.Add(str);
            Debug.Log(str);
        }

        resultText.text = "";
        foreach (var result in resultList)
        {
            resultText.text += result + "\n";
        }
    }

    /// <summary>
    /// Clear log text.
    /// </summary>
    public void OnClearTextClicked()
    {
        resultList.Clear();
        AppendText();
    }
}