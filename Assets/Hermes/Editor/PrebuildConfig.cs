using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#if UNITY_ANDROID
using UnityEditor.Purchasing;
using UnityEngine;
using UnityEngine.Purchasing;
#endif

/// <summary>
/// Prebuild Hermes configuration.
/// </summary>
public class PrebuildConfig : IPreprocessBuildWithReport {
    public int callbackOrder => 0;
    
    public void OnPreprocessBuild(BuildReport report) {
#if UNITY_ANDROID
#if AMAZON
        UnityPurchasingEditor.TargetAndroidStore(AppStore.AmazonAppStore);
        Debug.Log("Success: IAP Targeting Amazon Store");
#else
        UnityPurchasingEditor.TargetAndroidStore(AppStore.GooglePlay);
        Debug.Log("Success: IAP Targeting GooglePlay Store");
#endif
#endif
    }
}
