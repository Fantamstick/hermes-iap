using System.Collections.Generic;
using Newtonsoft.Json;
#if DEBUG_IAP
using UnityEngine;
using System.Text;
#endif

namespace HermesIAP
{
    internal class IOSIntroductoryOfferFactory : IntroductoryOfferFactory
    {
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
            this.SetLocalizedTitle(json["localizedTitle"]);
            this.SetLocalizedDescription(json["localizedDescription"]);
            this.SetISOCurrencyCode(json["isoCurrencyCode"]);

            // regular
            this.SetRegularPrice(json["localizedPrice"]);

            float introductoryPrice = TryParseFloat(json["introductoryPrice"]);
            if (introductoryPrice != 0f)
            {
                // introductory
                this.SetIntroductory(true);
                this.SetIntroductoryPrice(introductoryPrice);
                this.SetIntroductoryPriceLocale(json["introductoryPriceLocale"]);
                this.SetLocalizedIntroductoryPriceString(json["localizedPriceString"]);
                this.SetIntroductoryUnit(json["unit"]);
                this.SetIntroductoryNumberOfUnits(json["numberOfUnits"]);
                this.SetIntroductoryNumberOfPeriods(json["introductoryPriceNumberOfPeriods"]);
            }
            else
            {
                // free trial
                this.SetFreeTrial(true);
                this.SetIntroductoryPrice(introductoryPrice);
                this.SetIntroductoryPriceLocale(json["introductoryPriceLocale"]);
                this.SetLocalizedIntroductoryPriceString(json["localizedPriceString"]);
                this.SetFreeTrialUnit(json["unit"]);
                this.SetFreeTrialNumberOfUnits(json["numberOfUnits"]);
                this.SetFreeTrialNumberOfPeriods(json["introductoryPriceNumberOfPeriods"]);
            }
            this.SetOriginalJson(productJson);
        }
    }
}