﻿using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Hermes {
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
        
        public bool IsDebugLogEnabled { get; set; }
#if UNITY_IOS
        // TODO
        /// <summary>
        /// Support for promotional purchases.
        /// </summary>
        //public bool PromotionalPurchaseCompatible { get; set; }
        
        // TODO
        /// <summary>
        /// Event when promotional purchase attempted from AppStore.
        /// </summary>
        /// <returns>true: continue with purchase. false: refuse purchase.</returns>
        //public Func<Product, UniTask<bool>> OnPromotionalPurchase { get; set; }
#endif

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

        public static IAPBuilder WithDebugLog(this IAPBuilder builder) {
            builder.IsDebugLogEnabled = true;
            return builder;
        }
        
#if UNITY_IOS
        // TODO
        /// <summary>
        /// Support IAP Promotions from AppStore.
        /// </summary>
        /// <param name="onPromotionalPurchase">Callback when promotional purchase requested. Return true to continue purchase.</param>
        //public static IAPBuilder WithPromotionSupport(this IAPBuilder builder, Func<Product, UniTask<bool>> onPromotionalPurchase) {
        //    builder.PromotionalPurchaseCompatible = true;
        //    builder.OnPromotionalPurchase = onPromotionalPurchase;
        //    return builder;
        //}
#endif
    }
}
