#if UNITY_ANDROID || UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_TVOS
// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class AppleTangle
    {
        private static byte[] data = System.Convert.FromBase64String("YVo7aE90smWt4FBGLzSnZaMXrqURFhUQFBcSfjMpFxEUFhQdFhUQFApkgtNjaVssehQ7IidxOQcgPBQyI8hZHaevdwT3HOCVm75rLk/bD9g7oaehP70ZYxPWjb9kqgjwlbQ2/FNTCkVUVEhBCkdLSQtFVFRIQUdFFDUiJ3EgLjcuZVRUSEEEbUpHChUIBEdBVlBNQk1HRVBBBFRLSE1HXUhBBG1KRwoVAhQAIidxIC83OWVUrz2t+t1vSNEjjwYUJsw8Gtx0LfdbZYy83fXuQrgATzX0h5/APw7nO6QwD/RNY7BSLdrQT6kKZILTY2lbq1elROI/fy0LtpbcYGzURBy6MdEhJCemJSskFKYlLiamJSUkwLWNLRSmIJ8UpieHhCcmJSYmJSYUKSItBEtCBFBMQQRQTEFKBEVUVEhNR0WRHonQKyokti+VBTIKUPEYKf9GMlZFR1BNR0EEV1BFUEFJQUpQVwoUO7X/OmN0zyHJel2gCc8ShnNocchGSEEEV1BFSkBFVkAEUEFWSVcERSu5GdcPbQw+7NrqkZ0q/Xo48u8ZUExLVk1QXRUyFDAiJ3EgJzcpZVSa0Fe/yvZAK+9daxD8hhrdXNtP7A6ibKLTKSUlISEkFEYVLxQtIidxdkFITUVKR0EES0oEUExNVwRHQVZ9gyEtWDNkcjU6UPeTrwcfY4fxS4+HVbZjd3Hliwtll9zfx1TpwodoXhSmJVIUKiIncTkrJSXbICAnJiWVFHzIfiAWqEyXqzn6QVfbQ3pBmIz4WgYR7gHx/SvyT/CGAAc104WIQ6sskATT74gIBEtUkhslFKiTZ+siFCsiJ3E5NyUl2yAhFCclJdsUOUpABEdLSkBNUE1LSlcES0IEUVdBkz+Zt2YANg7jKzmSabh6R+xvpDNt/FK7FzBBhVOw7QkmJyUkJYemJSwPIiUhISMmJTI6TFBQVFceCwtTEr1oCVyTyai/+NdTv9ZS9lMUa+UiJ3E5KiAyIDAP9E1jsFIt2tBPqe09VtF5KvFbe7/WASeecatpeSnVAhQAIidxIC83OWVUVEhBBGdBVlAERUpABEdBVlBNQk1HRVBNS0oEVP0SW+Wjcf2DvZ0WZt/88VW6WoV2UE1CTUdFUEEERl0ERUpdBFRFVlAAxs/1k1T7K2HFA+7VSVzJw5EzM7G6XiiAY69/8DITF+/gK2nqME31GQJDBK4XTtMppuv6z4cL3XdOf0CmJSQiLQ6ibKLTR0AhJRSl1hQOIkARBzFvMX05l7DT0ri663Se5Xx0KSItDqJsotMpJSUhISQnpiUlJHhUSEEEZ0FWUE1CTUdFUE1LSgRlURcSfhRGFS8ULSIncSAiNyZxdxU3BGdlFKYlBhQpIi0Oomyi0yklJSULFKXnIiwPIiUhISMmJhSlkj6ll10ERVdXUUlBVwRFR0dBVFBFSkdBICI3JnF3FTcUNSIncSAuNy5lVFQyFDAiJ3EgJzcpZVRUSEEEdktLUORHF1PTHiMIcs/+KwUq/p5XPWuRLHoUpiU1IidxOQQgpiUsFKYlIBRUSEEEdktLUARnZRQ6MykUEhQQFk1CTUdFUE1LSgRlUVBMS1ZNUF0VdI6u8f7A2PQtIxOUUVEF");
        private static int[] order = new int[] { 25,7,31,51,49,36,9,44,51,16,17,32,55,14,25,41,23,46,41,42,22,36,50,44,38,52,38,28,38,54,36,54,43,59,38,35,48,48,42,46,45,58,46,53,58,56,46,50,50,49,51,52,52,56,54,55,57,59,58,59,60 };
        private static int key = 36;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
#endif