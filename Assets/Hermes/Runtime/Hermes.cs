namespace Hermes 
{
    public class IAP 
    {
#if UNITY_ANDROID
        public static GooglePlayStore Instance { get; } = GooglePlayStore.CreateInstance();
#elif UNITY_IOS || UNITY_IPHONE
        public static AppStore Instance { get; } = AppStore.CreateInstance();
#endif
    }
}


