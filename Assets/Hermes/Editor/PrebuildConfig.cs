using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Purchasing;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Prebuild Hermes configuration.
/// </summary>
public class PrebuildConfig : IPreprocessBuildWithReport {
    public int callbackOrder => 0;
    
    public void OnPreprocessBuild(BuildReport report) {
#if AMAZON
        UnityPurchasingEditor.TargetAndroidStore(AppStore.AmazonAppStore);
        Debug.Log("Success: IAP Targeting Amazon Store");
#elif UNITY_ANDROID
        UnityPurchasingEditor.TargetAndroidStore(AppStore.GooglePlay);
        Debug.Log("Success: IAP Targeting GooglePlay Store");
#elif UNITY_IOS
        // No need to do anything.
        Debug.Log("Success: IAP Targeting Apple Store");
#else
        throw new NotSupportedException($"Target {Application.platform} not supported");
#endif
    }
}
