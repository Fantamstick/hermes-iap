using System.Collections.Generic;
using FantamIAP;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using UnityEngine.UI;

/// <summary>
/// Test scene for IAP.
/// </summary>
public class PurchaseScene : MonoBehaviour
{
    [SerializeField] string googlePlayProductId;
    [SerializeField] string AppleProductId;
    [Space(30)]
    [SerializeField] Text resultText;
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
        OnClearTextClicked();

        productIdLabel.text = productId;
        
        IAPManager.Instance.OnPurchased += OnPurchased;
    }

    //========================================================
    // INIT
    //========================================================
    public void OnClickInit()
    {
        UpdateText("click init, waiting for response...");
        
        var products = new Dictionary<string, ProductType>
        {
            {productId, ProductType.Subscription}
        };

        var iapBuilder = new IAPBuilder(products).WithAppleTangleData(AppleTangle.Data());
        IAPManager.Instance.Init(iapBuilder, OnInit);
    }

    void OnInit(InitStatus status)
    {
        UpdateText($"IAP init status {status}");
    }
    
    //========================================================
    // PURCHASE
    //========================================================
    public void OnClickPurchase()
    {
        UpdateText("click purchase, waiting for response...");
        
        var request = IAPManager.Instance.PurchaseProduct(productId);
        if (request != PurchaseRequest.Ok)
        {
            UpdateText($"problem with purchase: {request}");
        }
    }

    void OnPurchased(PurchaseResponse resp, Product product)
    {
        UpdateText($"purchase result {resp} for Product: {product.transactionID}. {product.definition.id}");
    }
    
    //========================================================
    // RESTORE
    //========================================================
    public void OnClickRestore()
    {
        UpdateText("clicked restore, waiting for response...");
        IAPManager.Instance.RestorePurchases(20_000, onDone: (resp) =>
        {
            UpdateText($"Restore attempt, {resp} from product");

            OnClickGetExpiration();
        });
    }

    //========================================================
    // GET EXPIRATION
    //========================================================
    public void OnClickGetExpiration()
    {
        var expDate = IAPManager.Instance.GetSubscriptionExpiration(productId);
        if (expDate.HasValue)
        {
            UpdateText($"{productId} expiration date = {expDate.Value}");
        }
        else
        {
            UpdateText($"{productId} has no expiration date");
        }
    }

    //========================================================
    // UTILITY
    //========================================================
    void UpdateText(string str = default)
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

    public void OnClearTextClicked()
    {
        resultList.Clear();
        UpdateText();
    }
}
