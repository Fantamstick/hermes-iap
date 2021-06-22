namespace Hermes {
    public class IAP {
#if AMAZON
        public static AmazonStore Instance { get; } = AmazonStore.CreateInstance();
#elif UNITY_ANDROID
        public static GooglePlayStore Instance { get; } = GooglePlayStore.CreateInstance();
#elif UNITY_IOS || UNITY_IPHONE
        public static AppleStore Instance { get; } = AppleStore.CreateInstance();
#endif
    }
}


