using System.Collections.Generic;
using Newtonsoft.Json;
#if DEBUG_IAP
using UnityEngine;
using System.Text;
#endif

namespace Hermes
{
    /// <summary>
    /// Builds an introductory offer from the product details dictionary of Unity IAP.
    /// Supports both StoreKit 1 and StoreKit 2 formats.
    ///
    /// StoreKit 1: always has "introductoryPrice", "introductoryPriceLocale" etc. (null when there is no offer).
    /// StoreKit 2: the introductory offer keys exist only when the offer exists AND the user is eligible for it
    ///             (Unity IAP only exports the offer when Product.SubscriptionInfo.isEligibleForIntroOffer is true).
    ///             "introductoryType" holds the payment mode: freeTrial, payAsYouGo or payUpFront.
    ///             "introductoryPriceLocale" does not exist.
    /// </summary>
    internal class IOSIntroductoryOfferFactory : IntroductoryOfferFactory
    {
        const string FreeTrial = "freeTrial";
        const string PayAsYouGo = "payAsYouGo";
        const string PayUpFront = "payUpFront";

        public IOSIntroductoryOfferFactory(string productJson)
        {
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(productJson);

#if DEBUG_IAP
            var sb = new StringBuilder("introductory offer json: \n");
            foreach (var key in json.Keys)
            {
                sb.Append($"{key}:{json[key]}\n");
            }

            Debug.Log(sb);
#endif
            // common
            this.SetLocalizedTitle(Get(json, "localizedTitle"));
            this.SetLocalizedDescription(Get(json, "localizedDescription"));
            this.SetISOCurrencyCode(Get(json, "isoCurrencyCode"));

            // regular
            this.SetRegularPrice(Get(json, "localizedPrice"));

            var introductoryType = Get(json, "introductoryType");
            var introductoryPriceText = Get(json, "introductoryPrice");

            bool isFreeTrial;
            if (IsMode(introductoryType, FreeTrial))
            {
                // StoreKit 2 free trial.
                isFreeTrial = true;
            }
            else if (IsMode(introductoryType, PayAsYouGo) || IsMode(introductoryType, PayUpFront))
            {
                // StoreKit 2 introductory price.
                isFreeTrial = false;
            }
            else
            {
                // StoreKit 1, StoreKit 2 without an offer (or the user is not eligible), or an unknown payment mode.
                // No price means no introductory offer. TryParseFloat throws InvalidOfferException.
                isFreeTrial = TryParseFloat(introductoryPriceText) == 0f;
            }

            var introductoryPrice = string.IsNullOrEmpty(introductoryPriceText)
                ? 0f
                : TryParseFloat(introductoryPriceText);

            // StoreKit 2 has no introductoryPriceLocale. Use the currency code instead.
            var priceLocale = Get(json, "introductoryPriceLocale") ?? Get(json, "isoCurrencyCode") ?? string.Empty;
            var numberOfPeriods = Get(json, "introductoryPriceNumberOfPeriods");
            if (string.IsNullOrEmpty(numberOfPeriods))
            {
                // A single period if the number is not provided.
                numberOfPeriods = "1";
            }

            if (!isFreeTrial)
            {
                // introductory
                this.SetIntroductory(true);
                this.SetIntroductoryPrice(introductoryPrice);
                this.SetIntroductoryPriceLocale(priceLocale);
                this.SetLocalizedIntroductoryPriceString(Get(json, "localizedPriceString"));
                this.SetIntroductoryUnit(Get(json, "unit"));
                this.SetIntroductoryNumberOfUnits(Get(json, "numberOfUnits"));
                this.SetIntroductoryNumberOfPeriods(numberOfPeriods);
            }
            else
            {
                // free trial
                this.SetFreeTrial(true);
                this.SetIntroductoryPrice(introductoryPrice);
                this.SetIntroductoryPriceLocale(priceLocale);
                this.SetLocalizedIntroductoryPriceString(Get(json, "localizedPriceString"));
                this.SetFreeTrialUnit(Get(json, "unit"));
                this.SetFreeTrialNumberOfUnits(Get(json, "numberOfUnits"));
                this.SetFreeTrialNumberOfPeriods(numberOfPeriods);
            }
            this.SetOriginalJson(productJson);
        }

        /// <summary>
        /// Compares the payment mode ignoring case. The casing differs between Apple's rawValue ("freeTrial")
        /// and the names used in Unity IAP ("FreeTrial"), so accept both.
        /// </summary>
        static bool IsMode(string value, string mode)
        {
            return string.Equals(value, mode, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets a value, or null if the key does not exist. StoreKit 2 omits keys that StoreKit 1 always had.
        /// </summary>
        static string Get(Dictionary<string, string> json, string key)
        {
            return json != null && json.TryGetValue(key, out var value) ? value : null;
        }
    }
}
