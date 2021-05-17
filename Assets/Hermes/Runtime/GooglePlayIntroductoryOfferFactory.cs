#if UNITY_ANDROID
using System;
using System.Text.RegularExpressions;
using System.Xml;
using Google.Play.Billing.Internal;
using UnityEngine;

namespace HermesIAP
{
    internal class GooglePlayIntroductoryOfferFactory : IntroductoryOfferFactory
    {
        public GooglePlayIntroductoryOfferFactory(SkuDetails sku)
        {
            // https://developer.android.com/reference/com/android/billingclient/api/SkuDetails?hl=ja
#if DEBUG_IAP
            Debug.Log("---GooglePlayIntroductoryOfferFactory");
            Debug.Log(sku.JsonSkuDetails);
#endif

            // Subscription period, specified in ISO 8601 format.
            Period regularPeriod = getPeriod(sku.subscriptionPeriod);
            // The billing period of the introductory price, specified in ISO 8601 format.
            Period introductoryPeriod = getPeriod(sku.introductoryPricePeriod);
            // Trial period configured in Google Play Console, specified in ISO 8601 format.
            Period freePeriod = getPeriod(sku.freeTrialPeriod);

            // common
            // the title of the product.
            this.SetLocalizedTitle(sku.title);
            // the description of the product.
            this.SetLocalizedDescription(sku.description);
            // Returns ISO 4217 currency code for price and original price.
            this.SetISOCurrencyCode(sku.price_currency_code);

            // regular
            this.SetRegularPrice(sku.price_amount_micros / 1000000);
            this.SetLocalizedRegularPriceString(sku.price);
            this.SetRegularUnit(regularPeriod.Unit);
            this.SetRegularNumberOfUnit(regularPeriod.Number);

            if (introductoryPeriod != null)
            {
                this.SetIntroductory(true);
                // introductory
                this.SetIntroductoryUnit(introductoryPeriod.Unit);
                this.SetIntroductoryNumberOfUnits(introductoryPeriod.Number);
                // The number of subscription billing periods for which the user will be given the introductory price, such as 3.
                this.SetIntroductoryNumberOfPeriods(sku.introductoryPriceCycles);
                // Introductory price in micro-units.
                this.SetIntroductoryPrice(sku.introductoryPriceAmountMicros / 1000000);
                // Formatted introductory price of a subscription, including its currency sign, such as €3.99.
                this.SetIntroductoryPriceLocale(sku.introductoryPrice);
                this.SetLocalizedIntroductoryPriceString(sku.introductoryPrice);
            }

            if (freePeriod != null)
            {
                //  free trial
                this.SetFreeTrial(true);
                this.SetFreeTrialUnit(freePeriod.Unit);
                this.SetFreeTrialNumberOfUnits(freePeriod.Number); // (number of unit * number of period) * unit = free trial duration 
                this.SetFreeTrialNumberOfPeriods(1);
            }
            
            this.SetOriginalJson(sku.JsonSkuDetails);
        }

        class Period
        {
            public IntroductoryOffer.UnitType Unit;

            public int Days = 0;
            public int Weeks = 0;
            public int Months = 0;
            public int Yeas = 0;

            private int FullDays => Days + (Weeks * 7) + (Months * 30) + (Yeas * 365);

            public int Number
            {
                get
                {
                    switch (Unit)
                    {
                        case IntroductoryOffer.UnitType.Days:
                            return FullDays;
                        case IntroductoryOffer.UnitType.Weeks:
                            return FullDays / 7;
                        case IntroductoryOffer.UnitType.Months:
                            return FullDays / 30;
                        case IntroductoryOffer.UnitType.Years:
                            return FullDays / 365;
                        default:
                            return FullDays;
                    }
                }
            }
        }

        /// <summary>
        /// format of ISO 8601 duration
        /// https://en.wikipedia.org/wiki/ISO_8601#Durations
        /// </summary>f
        private static readonly Regex _regex = new Regex(@"(?<length>[0-9]+)(?<unit>[Y|M|W|D])");
        
        private Period getPeriod(string isoValue)
        {
            if (string.IsNullOrEmpty(isoValue))
            {
                // not setting 
                return null;
            }

            MatchCollection matches = _regex.Matches(isoValue);
            if (matches.Count == 0)
            {
                return null;
            }

            Period period = new Period();
            foreach (Match match in matches)
            {
                // [unit] = last unit text
                CalcPeriod(match, period);
            }

            return period;
        }

        private void CalcPeriod(Match match, Period period)
        {
            int len = int.Parse(match.Groups["length"].Value);
            switch (match.Groups["unit"].Value)
            {
                case "Y":
                    period.Unit = IntroductoryOffer.UnitType.Years;
                    period.Yeas = len;
                    break;
                case "M":
                    period.Unit = IntroductoryOffer.UnitType.Months;
                    period.Months = len;
                    break;
                case "W":
                    period.Unit = IntroductoryOffer.UnitType.Weeks;
                    period.Weeks = len;
                    break;
                case "D":
                    period.Unit = IntroductoryOffer.UnitType.Days;
                    period.Days = len;
                    break;
                default:
                    // not occur
                    return;
            }
        }
    }
}
#endif