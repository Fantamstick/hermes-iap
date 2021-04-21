using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HermesIAP;
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

        HermesIAP.HermesIAP.Instance.OnPurchased += OnPurchased;
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
            {"com.fantamstick.test.iap.autorenew2", ProductType.Subscription},
            {"com.fantamstick.test.iap.autorenew3", ProductType.Subscription}
        };
        var iapBuilder = new IAPBuilder(products).WithAppleTangleData(AppleTangle.Data());
        HermesIAP.HermesIAP.Instance.Init(iapBuilder, OnInit);
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

        var request = HermesIAP.HermesIAP.Instance.PurchaseProduct(productId);
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
    public void OnClickRestore()
    {
#if UNITY_ANDROID
        AppendText("Android not support 'RESTORE'");
#else
        AppendText("clicked restore, waiting for response...");
        HermesIAP.HermesIAP.Instance.RestorePurchases(20_000, onDone: (resp) =>
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
    public async void OnClickGetExpiration()
    {
#if IOS
        var expDate = HermesIAP.HermesIAP.Instance.GetSubscriptionExpiration(productId);
        if (expDate.HasValue)
        {
            AppendText($"{productId} expiration date = {expDate.Value}");
        }
        else
        {
            AppendText($"{productId} has no expiration date");
        }
#endif
#if UNITY_ANDROID
        var ret = await HermesIAP.HermesIAP.Instance.IsSubscriptionActive(productId);
        AppendText($"{productId} subscription active:{ret}");
#endif
    }

    public async void OnClickGetInfo()
    {
        AppendText("OnClick GetInfo...");
#if IOS
        SubscriptionInfo[] receipts = HermesIAP.HermesIAP.Instance.GetPurchasedSubscriptions(productId);
        if (receipts == null || receipts.Length == 0)
        {
            AppendText($"{productId} has no info");
        }
        else
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            foreach (SubscriptionInfo info in receipts)
            {
                // https://docs.unity3d.com/ja/2019.4/Manual/UnityIAPSubscriptionProducts.html
                sb.Append(i).Append(":");
                sb.Append("getProductId=").Append(info.getProductId()).Append("\n");
                sb.Append("getPurchaseDate=").Append(info.getPurchaseDate()).Append("\n");
                sb.Append("isSubscribed=").Append(info.isSubscribed()).Append("\n");
                sb.Append("getExpireDate=").Append(info.getExpireDate()).Append("\n");
                sb.Append("isExpired=").Append(info.isExpired()).Append("\n");
                sb.Append("getCancelDate=").Append(info.getCancelDate()).Append("\n");
                sb.Append("isCancelled=").Append(info.isCancelled()).Append("\n");
                //sb.Append("getRemainingTime=").Append(info.getRemainingTime()).Append("\n");
                // sb.Append("getSkuDetails=").Append(info.getSkuDetails()).Append("\n");
                // sb.Append("getSubscriptionPeriod=").Append(info.getSubscriptionPeriod()).Append("\n");
                // sb.Append("isIntroductoryPricePeriod=").Append(info.isIntroductoryPricePeriod()).Append("\n");
                
                // 次の課金更新 あり/ なし
                sb.Append("isAutoRenewing=").Append(info.isAutoRenewing()).Append("\n");
                // sb.Append("getSubscriptionInfoJsonString=").Append(info.getSubscriptionInfoJsonString()).Append("\n");
                i++;
            }
        
            AppendText($"{productId} has {receipts.Length} info. {sb}");
            Debug.Log($"{productId} has {receipts.Length} info. {sb}");
        }
#endif
#if UNITY_ANDROID
        Product[] products = await HermesIAP.HermesIAP.Instance.GetPurchasedSubscriptions(productId);
        Debug.Log("OnClick GetInfo...");
        if (products == null || products.Length == 0)
        {
            AppendText($"{productId} has no info");
        }
        else
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < products.Length; i++)
            {
                var product = products[i];
                // https://docs.unity3d.com/ja/2019.4/Manual/UnityIAPSubscriptionProducts.html
                sb.Append(i).Append("----------------------------------\n");
                if (product == null)
                {
                    sb.Append(" product is null");
                }
                else
                {
                    sb.Append("definition.id=").Append(product.definition.id).Append("\n");
                    sb.Append("transactionID=").Append(product.transactionID).Append("\n");
//                sb.Append("availableToPurchase=").Append(product.availableToPurchase).Append("\n");
                    sb.Append("metadata=").Append(product.metadata).Append("\n");
                    if (product.hasReceipt)
                    {
                        sb.Append("receipt=").Append(product.receipt).Append("\n");
                    }
                    else
                    {
                        sb.Append("product hasn't receipt.\n");
                    }
                }

                sb.Append("\n\n");
            }
            

            AppendText($"{products.Length} info. {sb}");
        }
#endif
    }

    public async void OnclickGetAvailableProducts()
    {
        AppendText("GetAvailableProducts");

        Product[] products = HermesIAP.HermesIAP.Instance.GetAvailableProducts();
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
        var skus = HermesIAP.HermesIAP.Instance.GetSKUs();
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