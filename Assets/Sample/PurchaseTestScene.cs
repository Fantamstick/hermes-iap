using System.Collections.Generic;
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
            {productId, ProductType.Subscription}
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
        AppendText($"purchase result {resp} for Product: {product.transactionID}. {product.definition.id}");
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
        HermesIAP.HermesIAP.Instance.RestorePurchases(20_000, onDone: (resp) =>
        {
            AppendText($"Restore attempt, {resp} from product");

            OnClickGetExpiration();
        });
    }

    //========================================================
    // GET EXPIRATION
    //========================================================
    /// <summary>
    /// Get expiration button clicked.
    /// </summary>
    public void OnClickGetExpiration()
    {
        var expDate = HermesIAP.HermesIAP.Instance.GetSubscriptionExpiration(productId);
        if (expDate.HasValue)
        {
            AppendText($"{productId} expiration date = {expDate.Value}");
        }
        else
        {
            AppendText($"{productId} has no expiration date");
        }
    }

    //========================================================
    // GET INTRODUCTORY OFFER DETAILS
    //========================================================
    public void OnClickIntroOffer()
    {
        var offerDict = HermesIAP.HermesIAP.Instance.GetIntroductoryOfferDetails(productId);
        if (offerDict != null)
        {
            AppendText($"Regular Price: {offerDict.RegularPrice}");
            AppendText($"Intro Price: {offerDict.IntroductoryPrice}");
            AppendText($"Duration: {offerDict.NumberOfUnits} {offerDict.Unit}");
            AppendText($"Periods: {offerDict.NumberOfPeriods}");
        }
        else
        {
            AppendText($"{productId} has no introductory offer available");
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
