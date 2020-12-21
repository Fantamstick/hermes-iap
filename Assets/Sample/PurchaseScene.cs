using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PurchaseScene : MonoBehaviour
{
    [SerializeField] private Button initButton;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Button restoreButton;
    [SerializeField] private Text resultText;
    private List<string> resultList = new List<string>();
    
    public void OnClickInit()
    {
        resultList.Add("click init");
        UpdateText();
    }

    public void OnClickPurchase()
    {
        resultList.Add("click purchase");
        UpdateText();
    }

    public void OnClickRestore()
    {
        resultList.Add("click restore");
        UpdateText();
    }

    public void OnClearText()
    {
        resultList.Clear();
        UpdateText();
    }

    void UpdateText()
    {
        foreach (var result in resultList)
        {
            resultText.text += result + "¥n";
        }
    }
}
