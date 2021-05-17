using System;

namespace HermesIAP
{
    internal abstract class IntroductoryOfferFactory
    {
        #region  fields
        
        //*******************************************************************
        // Common
        //*******************************************************************
        
        protected string LocalizedTitle;
        protected string LocalizedDescription;
        protected string ISOCurrencyCode;
        //*******************************************************************
        // Regular Setting
        //*******************************************************************
        /// <summary>
        /// Regular Price 通常価格
        /// </summary>
        protected float RegularPrice;
    
        /// <summary>
        /// formatted price of the item, including its currency sign.
        /// </summary>
        protected string LocalizedRegularPriceString;
    
        /// <summary>
        /// Regular Price 
        /// </summary>
        protected IntroductoryOffer.UnitType RegularUnit;
    
        /// <summary>
        /// Number of units for each regular subscription
        /// </summary>
        protected int RegularNumberOfUnit;
        //*******************************************************************
        // Introductory setting
        //*******************************************************************

        private  bool IsIntroductory = false;
        
        protected float IntroductoryPrice;
        
        protected string IntroductoryPriceLocale;

        protected string LocalizedIntroductoryPriceString;

        /// <summary>
        /// Type of unit used for calculating length of introductory period.
        /// </summary>
        protected IntroductoryOffer.UnitType IntroductoryUnit;

        /// <summary>
        /// Number of units for each introductory period.
        /// </summary>
        protected int IntroductoryNumberOfUnits;

        /// <summary>
        /// Number of periods that introductory period is valid.
        /// </summary>
        protected int IntroductoryNumberOfPeriods;

        //*******************************************************************
        // FreeTrial setting
        //*******************************************************************
   
        private bool IsFreeTrial = false;
        
        /// <summary>
        /// Type of unit used for calculating length of introductory period.
        /// </summary>
        protected IntroductoryOffer.UnitType FreeTrialUnit;

        /// <summary>
        /// Number of units for each free trial
        /// </summary>
        protected int FreeTrialNumberOfUnits;

        /// <summary>
        /// Number of periods that free trial period is valid.
        /// </summary>
        protected int FreeTrialNumberOfPeriods;

        private string OriginalJson;
        
        #endregion
        public IntroductoryOffer Make()
        {
            return new IntroductoryOffer(
                this.LocalizedTitle,
                this.LocalizedDescription,
                this.ISOCurrencyCode,
                this.RegularPrice,
                this.LocalizedRegularPriceString,
                this.RegularUnit,
                this.RegularNumberOfUnit,
                this.IsIntroductory,
                this.IntroductoryPrice,
                this.IntroductoryPriceLocale,
                this.LocalizedIntroductoryPriceString,
                this.IntroductoryUnit,
                this.IntroductoryNumberOfPeriods,
                this.IntroductoryNumberOfUnits,
                this.IsFreeTrial,
                this.FreeTrialUnit,
                this.FreeTrialNumberOfPeriods,
                this.FreeTrialNumberOfUnits,
                this.OriginalJson);
        }

        #region setter
        
        //*******************************************************************
        // Common
        //*******************************************************************
        
        protected void SetLocalizedTitle(string value)
        {
            this.LocalizedTitle = value;
        }

        protected void SetLocalizedDescription(string value)
        {
            this.LocalizedDescription = value;
        }

        protected void SetISOCurrencyCode(string value)
        {
            this.ISOCurrencyCode = value;
        }
        
        //*******************************************************************
        // Regular Setting
        //*******************************************************************
        protected void SetRegularPrice(string value)
        {
            this.RegularPrice = TryParseFloat(value);
            
        }
        protected void SetRegularPrice(float value)
        {
            this.RegularPrice = value;
        }

        protected void SetLocalizedRegularPriceString(string value)
        {
            this.LocalizedRegularPriceString = value;
        }
        
        protected void SetRegularUnit(string value)
        {
            this.RegularUnit = TryParseUnit(value);
        }
        protected void SetRegularUnit(IntroductoryOffer.UnitType value)
        {
            this.RegularUnit = value;
        }

        protected void SetRegularNumberOfUnit(string value)
        {
            this.RegularNumberOfUnit = TryParseInt(value);
        }
        protected void SetRegularNumberOfUnit(int value)
        {
            this.RegularNumberOfUnit = value;
        }
        
        //*******************************************************************
        // Introductory setting
        //*******************************************************************

        protected void SetIntroductory(bool value)
        {
            this.IsIntroductory = value;
        }
        
        protected void SetIntroductoryPrice(string value)
        {
            this.IntroductoryPrice = TryParseFloat(value);
        }

        protected void SetIntroductoryPrice(float value)
        {
            this.IntroductoryPrice = value;
        }

        protected void SetIntroductoryPriceLocale(string value)
        {
            this.IntroductoryPriceLocale = value;
        }

        protected void SetLocalizedIntroductoryPriceString(string value)
        {
            this.LocalizedIntroductoryPriceString = value;
        }
    
        /// <summary>
        /// set Type of unit used for calculating length of introductory period.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected void SetIntroductoryUnit(string value)
        {
            this.IntroductoryUnit =  TryParseUnit(value);
        }
        
        /// <summary>
        /// set Type of unit used for calculating length of introductory period.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected void SetIntroductoryUnit(IntroductoryOffer.UnitType value)
        {
            this.IntroductoryUnit = value;
        }

        /// <summary>
        /// set Number of periods that introductory period is valid.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected void SetIntroductoryNumberOfPeriods(string value)
        {
            this.IntroductoryNumberOfPeriods = TryParseInt(value);
        }
        
        /// <summary>
        /// set Number of periods that introductory period is valid.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected void SetIntroductoryNumberOfPeriods(int value)
        {
            this.IntroductoryNumberOfPeriods = value;
        }
        
        /// <summary>
        /// set Number of units for each introductory period.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected void SetIntroductoryNumberOfUnits(string value)
        {
            this.IntroductoryNumberOfUnits = TryParseInt(value);
        }

        /// <summary>
        /// set Number of units for each introductory period.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected void SetIntroductoryNumberOfUnits(int value)
        {
            this.IntroductoryNumberOfUnits = value;
        }

        //*******************************************************************
        // FreeTrial setting
        //*******************************************************************
        
        protected void SetFreeTrial(bool value)
        {
            this.IsFreeTrial = value;
        }

        protected void SetFreeTrialUnit(string value)
        {
            this.FreeTrialUnit = TryParseUnit(value);
        }
        protected void SetFreeTrialUnit(IntroductoryOffer.UnitType value)
        {
            this.FreeTrialUnit = value;
        }
        
        protected void SetFreeTrialNumberOfPeriods(string value)
        {
            this.FreeTrialNumberOfPeriods = TryParseInt(value);
        }
        protected void SetFreeTrialNumberOfPeriods(int value)
        {
            this.FreeTrialNumberOfPeriods = value;
        }
        protected void SetFreeTrialNumberOfUnits(string value)
        {
            this.FreeTrialNumberOfUnits = TryParseInt(value);
        }
        protected void SetFreeTrialNumberOfUnits(int value)
        {
            this.FreeTrialNumberOfUnits = value;
        }

        protected void SetOriginalJson(string value)
        {
            this.OriginalJson = value;
        }
        
        
        #endregion
        
        protected float TryParseFloat(string value)
        {
            return string.IsNullOrEmpty(value) ? throw new InvalidOfferException() : float.Parse(value);
        }

        protected int TryParseInt(string value)
        {
            return string.IsNullOrEmpty(value) ? throw new InvalidOfferException() : int.Parse(value);
        }

        protected IntroductoryOffer.UnitType TryParseUnit(string value)
        {
            return (IntroductoryOffer.UnitType) this.TryParseInt(value);
        }
    }
}