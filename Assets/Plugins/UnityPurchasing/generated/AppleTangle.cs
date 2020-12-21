#if UNITY_ANDROID || UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_TVOS
// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class AppleTangle
    {
        private static byte[] data = System.Convert.FromBase64String("00lPSddV8Yv7PmVXjELgGH1c3hR713Ffjuje5gvD0XqBUJKvBIdM26Cp7IWir+L96vzoys+ZyMff0Y284Oyvqb64paqlr624qey8o6Clr7Xq/OjKz5nIx9/Rjby8oKnsj6m+uIUUulP/2Kltu1gF4c7PzczNb07Nys+Z0cLI2sjY5xyli1i6xTI4p0HK/MPKz5nR383NM8jJ/M/NzTP80XI4v1ciHqjDB7WD+BRu8jW0M6cEu7virby8oKnir6Oh4628vKCpr624paqlr624qeyuteytorXsvK2+uOguJx17vBPDiS3rBj2htCEredvb+lWA4bR7IUBXED+7Vz66Hrv8gw3LILH1T0ef7B/0CH1zVoPGpzPnMKtDxHjsOwdg4OyjvHrzzfxAe48D4/xND8rE58rNycnLzs78TXrWTX/BysXmSoRKO8HNzcnJzM9Ozc3MkGdvvV6Ln5kNY+ONfzQ3L7wBKm+A7KOq7Likqey4pKmi7K28vKClr63JzM9OzcPM/E7Nxs5Ozc3MKF1lxUfVRRI1h6A5y2fu/M4k1PI0nMUfibLTgKecWo1FCLiux9xPjUv/Rk159mE4w8LMXsd97driuBnwwReu2hX6sw1LmRVrVXX+jjcUGb1Ssm2eTs3MysXmSoRKO6+oyc38TT785sr5/v34/P/6ltvB//n8/vz1/v34/OyPjfxOze78wcrF5kqESjvBzc3N2vzYys+ZyM/fwY28vKCp7J6jo7jEkvxOzd3Kz5nR7MhOzcT8Ts3I/Pzdys+ZyMbfxo28vKCp7IWir+L94oxqO4uBs8SS/NPKz5nR78jU/NoF1b45kcIZs5NXPunPdplDgZHBPaWqpa+tuKWjouyNubiko76luLX9TNjnHKWLWLrFMjinQeKMajuLgbN9/JQglsj+QKR/Q9ESqb8zq5KpcP/6lvyu/cf8xcrPmcjK386Zn/3fZBCy7vkG6RkVwxqnGG7o7907bWC4pKO+pbi1/dr82MrPmcjP38GNvMNR8T/nheTWBDICeXXCFZLQGgfx/E7Id/xOz29sz87Nzs7NzvzBysW8oKnsj6m+uKWqpa+tuKWjouyNuZ6poKWtoq+p7KOi7Likpb/sr6m+8eqr7Eb/pjvBTgMSJ2/jNZ+ml6i+ra+4pa+p7L+4rbipoamiuL/i/Ayv/7s79svgmicWw+3CFna/1YN5010X0oucJ8khkrVI4Sf6bpuAmSC8oKnsnqOjuOyPjfzS28H8+vz4/sTnys3JycvOzdrSpLi4vL/24+O7lWvJxbDbjJrd0rgfe0fv94tvGaOiqOyvo6Kopbilo6K/7KOq7Lm/qbXsrb+/uaGpv+ytr6+pvLitoq+ptvxOzbr8wsrPmdHDzc0zyMjPzs1Dv02sCteXxeNefjSIhDys9FLZOeZKhEo7wc3NycnM/K79x/zFys+ZqPnv2YfZldF/WDs6UFIDnHYNlJxZUrbAaItHlxja+/8HCMOBAtilHbONZFQ1HQaqUOin3RxvdyjX5g/TyMrfzpmf/d/83crPmcjG38aNvLzsraKo7K+pvrilqqWvrbilo6LsvK6gqey/uK2iqK2+qOy4qb6hv+ytnGZGGRYoMBzFy/t8ubnt");
        private static int[] order = new int[] { 49,59,3,44,10,27,30,29,50,36,39,53,48,51,21,37,47,24,41,21,47,25,23,58,34,29,51,48,33,36,31,36,53,53,51,50,54,48,58,49,49,58,56,46,57,48,54,50,52,54,54,53,55,59,54,58,59,57,58,59,60 };
        private static int key = 204;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
#endif
