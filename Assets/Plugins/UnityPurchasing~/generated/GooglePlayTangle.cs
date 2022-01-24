#if UNITY_ANDROID || UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_TVOS
// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("O5HIJ63qmW5ejEOMLwI7IxaAqILp0xuTTpn3q/mVFoX4AcPz8DLawVqyiaTKWuR8v6yraByYjYlSv95CU/oPtWg5y8ab7aLFm5/PsmbpHboK6HrHjbPMCawwmlXu/GaH2eUHXc0o8n3xlxbX2WhPxI9c3msBbvFNuxN9sS03InVSPUvFr9SrVuGSbE85gxDxJmNtXF8jIHkpG2DB00O6GwmY76iWM/mY64gd4RDGhE+Nbm7D8OmFid5f3Tj0ZQPSp/eiT3Wm5HLUV1lWZtRXXFTUV1dW30aasQ9oqBi5yeDcRjL209s8e6+WZq6xhdPji6NSx0EciqPQEHR67IQkCDDrOVxm1Fd0ZltQX3zQHtChW1dXV1NWVStFrebs1VlwkVRVV1ZX");
        private static int[] order = new int[] { 7,11,3,12,13,9,10,10,9,13,11,13,12,13,14 };
        private static int key = 86;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
#endif
