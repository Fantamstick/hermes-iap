using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Introductory Offer details.
/// </summary>
public class IntroductoryOffer
{
    //*******************************************************************
    // Common
    //*******************************************************************
    
    /// <summary>
    /// Product Title 
    /// </summary>
    public readonly string LocalizedTitle;
    
    /// <summary>
    /// Product Description
    /// </summary>
    public readonly string LocalizedDescription;

    /// <summary>
    /// Price ISO Currency Code. e.g.)JPY 通貨コード
    /// </summary>
    public readonly string ISOCurrencyCode;
    
    public readonly string OriginalJson;

    //*******************************************************************
    // Regular Setting
    //*******************************************************************

    /// <summary>
    /// Regular Price
    /// </summary>
    public readonly float RegularPrice;
    
    /// <summary>
    /// formatted price of the item, including its currency sign.
    /// </summary>
    public readonly string LocalizedRegularPriceString;
    
    /// <summary>
    /// Type of unit used for calculating length of regular subscription period
    /// </summary>
    public readonly UnitType RegularUnit;
    
    /// <summary>
    /// Number of units for each regular subscription
    /// </summary>
    public readonly int RegularNumberOfUnit;

    //*******************************************************************
    // Introductory setting
    //*******************************************************************

    public readonly bool IsIntroductory;
    
    /// <summary>
    /// Introductory Price お試し価格
    /// </summary>
    public readonly float IntroductoryPrice;
    
    public readonly string IntroductoryPriceLocale;

    /// <summary>
    /// formatted price of the item, including its currency sign.
    /// </summary>
    public readonly string LocalizedIntroductoryPriceString;

    /// <summary>
    /// Type of unit used for calculating length of introductory period.
    /// </summary>
    public readonly UnitType IntroductoryUnit;

    /// <summary>
    /// Number of periods that introductory period is valid.
    /// </summary>
    public readonly int IntroductoryNumberOfPeriods;

    /// <summary>
    /// Number of units for each introductory period.
    /// </summary>
    public readonly int IntroductoryNumberOfUnits;

    //*******************************************************************
    // FreeTrial setting
    //*******************************************************************

    public readonly bool IsFreeTrial;

    /// <summary>
    /// Type of unit used for calculating length of free trial period.
    /// </summary>
    public readonly UnitType FreeTrialUnit;

    /// <summary>
    /// Number of units for each free trial
    /// </summary>
    public readonly int FreeTrialNumberOfUnits;

    /// <summary>
    /// Number of periods that free trial period is valid.
    /// </summary>
    public readonly int FreeTrialNumberOfPeriods;
    
    public IntroductoryOffer(
        string localizedTitle,
        string localizedDescription,
        string iSOCurrencyCode,
        float regularPrice,
        string localizedRegularPriceString,
        UnitType regularUnit,
        int regularNumberOfUnit,
        bool isIntroductory,
        float introductoryPrice,
        string introductoryPriceLocale,
        string localizedIntroductoryPriceString,
        UnitType introductoryUnit,
        int introductoryNumberOfPeriods,
        int introductoryNumberOfUnits,
        bool isFreeTrial,
        UnitType freeTrialUnit,
        int freeTrialNumberOfPeriods,
        int freeTrialNumberOfUnits,
        string originalJson
    )
    {
        this.LocalizedTitle = localizedTitle;
        this.LocalizedDescription = localizedDescription;
        this.ISOCurrencyCode = iSOCurrencyCode;
        this.OriginalJson = originalJson;
        
        this.RegularPrice = regularPrice;
        this.LocalizedRegularPriceString = localizedRegularPriceString;
        this.RegularUnit = regularUnit;
        this.RegularNumberOfUnit = regularNumberOfUnit;
        
        this.IsIntroductory = isIntroductory;
        this.IntroductoryPrice = introductoryPrice;
        this.IntroductoryPriceLocale = introductoryPriceLocale;
        this.LocalizedIntroductoryPriceString = localizedIntroductoryPriceString;
        this.IntroductoryUnit = introductoryUnit;
        this.IntroductoryNumberOfPeriods = introductoryNumberOfPeriods;
        this.IntroductoryNumberOfUnits = introductoryNumberOfUnits;
        
        this.IsFreeTrial = isFreeTrial;
        this.FreeTrialUnit = freeTrialUnit;
        this.FreeTrialNumberOfPeriods = freeTrialNumberOfPeriods;
        this.FreeTrialNumberOfUnits = freeTrialNumberOfUnits;
    }
   
    public enum UnitType
    {
        Days = 0, 
        Weeks = 1, 
        Months = 2, 
        Years = 3,
    }
}
