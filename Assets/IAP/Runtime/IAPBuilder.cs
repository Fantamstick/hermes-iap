using System.Collections.Generic;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace FantamIAP {
    public class IAPBuilder {
        /// <summary>
        /// IAP purchases.
        /// </summary>
        public readonly Dictionary<string, ProductType> Products;
        /// <summary>
        /// Purchasing module.
        /// </summary>
        public IPurchasingModule PurchasingModule { get; set; }
        /// <summary>
        /// Apple tangle data for IAP receipt validation.
        /// </summary>
        public byte[] AppleTangleData { get; set; }
        /// <summary>
        /// Google tangle data for IAP receipt validation.
        /// </summary>
        public byte[] GoogleTangleData { get; set; }
    
        public IAPBuilder(Dictionary<string, ProductType> products) {
            this.Products = products;
        }
    }

    public static class IAPBuilderExtensions {
        /// <summary>
        /// Add Apple tangle data for IAP receipt validation.
        /// </summary>
        public static IAPBuilder WithAppleTangleData(this IAPBuilder builder, byte[] tangleData) {
            builder.AppleTangleData = tangleData;
            return builder;
        }
        /// <summary>
        /// Add Google tangle data for IAP receipt validation.
        /// </summary>
        public static IAPBuilder WithGoogleTangleData(this IAPBuilder builder, byte[] tangleData) {
            builder.GoogleTangleData = tangleData;
            return builder;
        }
        /// <summary>
        /// Add Purchasing module.
        /// </summary>
        public static IAPBuilder WithPurchasingModule(this IAPBuilder builder, IPurchasingModule module) {
            builder.PurchasingModule = module;
            return builder;
        }
    }
}
