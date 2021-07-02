#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Google.Play.Billing.Internal;
using UnityEngine;

namespace Hermes.GoogleUtil
{
    /// <summary>
    /// BillingClient.queryPurchase() 用
    /// logic copy from Google.Play.Billing.GooglePlayStoreImpl, Google.Play.Billing.Internal.JniUtils
    /// # can't use internal/private scope sources....
    /// </summary>
    public class GoogleUtil 
    {
        private AndroidJavaObject _billingClient;
        private volatile bool _deferredPurchasesEnabled = false;
        private BillingClientStateListener _billingClientStateListener;
        private volatile bool _billingClientReady;
        
        public GoogleUtil() { }

        public async UniTask<List<Purchase>> QueryPurchase()
        {
            List<Purchase> purchases = null;
            try
            {
                this.InstantiateBillingClientAndMakeConnection();
                await UniTask.WaitWhile(() => !_billingClientReady);

                purchases = ExecuteQueryPurchase();
            }
            finally
            {
                this.EndConnection();
            }
            
            return purchases;
        }

        private List<Purchase> ExecuteQueryPurchase()
        {
            var subsPurchasesResult = _billingClient.Call<AndroidJavaObject>("queryPurchases",
                SkuType.Subs.ToString());
            if (this.GetResponseCodeFromQueryPurchasesResult(subsPurchasesResult) != BillingResponseCode.Ok)
            {
                return null;
            }

            List<Purchase> purchases = this.ParseQueryPurchasesResult(subsPurchasesResult).ToList();
            return purchases;
        }

        private void InstantiateBillingClientAndMakeConnection()
        {
            _billingClientStateListener = new BillingClientStateListener();
            _billingClientStateListener.OnBillingServiceDisconnected += () =>
            {
                Debug.Log("Service disconnected");
                EndConnection();
                //InstantiateBillingClientAndMakeConnection();
            };
            _billingClientStateListener.OnBillingSetupFinished += (billingResult) => MarkBillingClientStartConnectionCallComplete(billingResult);
            
            
            // Set ready flag to false as this action could be triggered when in-app billing service is disconnected.
            _billingClientReady = false;

            var context = JniUtils.GetApplicationContext();

            _billingClient = new AndroidJavaObject("com.android.billingclient.api.BillingClientImpl",
                "3.0.0-unity");
            
            _billingClient.Call(
                "initialize",
                context,
                null,
                _deferredPurchasesEnabled);

            _billingClient.Call("startConnection", _billingClientStateListener);
        }
        
        private void MarkBillingClientStartConnectionCallComplete(AndroidJavaObject billingResult)
        {
            var responseCode = this.GetResponseCodeFromBillingResult(billingResult);
            if (responseCode == BillingResponseCode.Ok)
            {
                _billingClientReady = true;
            }
            else
            {
                Debug.Log(
                    $"Failed to connect to service with error code '{responseCode}' and debug message: '{JniUtils.GetDebugMessageFromBillingResult(billingResult)}'.");
            }
        }

        private void EndConnection()
        {
            if (!IsGooglePlayInAppBillingServiceAvailable())
            {
                return;
            }
            
            _billingClient.Call("endConnection");
            _billingClientReady = false;
        }
        
        private bool IsGooglePlayInAppBillingServiceAvailable()
        {
            if (_billingClientReady)
            {
                return true;
            }

            Debug.Log("Service is unavailable.");
            return false;
        }
        
        private BillingResponseCode GetResponseCodeFromBillingResult(AndroidJavaObject billingResult)
        {
            var responseCode = billingResult.Call<int>("getResponseCode");
            var billingResponseCode = BillingResponseCode.Error;
            try
            {
                billingResponseCode =
                    (BillingResponseCode) Enum.Parse(typeof(BillingResponseCode), responseCode.ToString());
            }
            catch (ArgumentNullException)
            {
                Debug.Log("Missing response code, return BillingResponseCode.Error.");
            }
            catch (ArgumentException)
            {
                Debug.Log($"Unknown response code {responseCode}, return BillingResponseCode.Error.");
            }

            return billingResponseCode;
        }
        
        private BillingResponseCode GetResponseCodeFromQueryPurchasesResult(AndroidJavaObject javaPurchasesResult)
        {
            var billingResult =
                javaPurchasesResult.Call<AndroidJavaObject>("getBillingResult");
            return GetResponseCodeFromBillingResult(billingResult);
        }
        
        private IEnumerable<Purchase> ParseQueryPurchasesResult(AndroidJavaObject javaPurchasesResult)
        {
            var billingResult =
                javaPurchasesResult.Call<AndroidJavaObject>("getBillingResult");
            var responseCode = GetResponseCodeFromBillingResult(billingResult);
            if (responseCode != BillingResponseCode.Ok)
            {
                Debug.Log(
                    $"Failed to retrieve purchase information! Error code {responseCode}, debug message: { billingResult.Call<string>("getDebugMessage")}.");
                return Enumerable.Empty<Purchase>();
            }

            return ParseJavaPurchaseList(
                javaPurchasesResult.Call<AndroidJavaObject>("getPurchasesList"));
        }
        
        private IEnumerable<Purchase> ParseJavaPurchaseList(AndroidJavaObject javaPurchasesList)
        {
            var parsedPurchasesList = new List<Purchase>();
            var size = javaPurchasesList.Call<int>("size");
            for (var i = 0; i < size; i++)
            {
                var javaPurchase = javaPurchasesList.Call<AndroidJavaObject>("get", i);
                var originalJson = javaPurchase.Call<string>("getOriginalJson");
                var signature = javaPurchase.Call<string>("getSignature");
                Purchase purchase;
                if (Purchase.FromJson(originalJson, signature, out purchase))
                {
                    parsedPurchasesList.Add(purchase);
                }
#if DEBUG_IAP                    
                else
                {
                    Debug.Log($"Failed to parse purchase {originalJson} ");
                }
#endif           
            }

            return parsedPurchasesList;
        }
    }
}
#endif