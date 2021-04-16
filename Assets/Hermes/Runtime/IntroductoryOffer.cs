using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Introductory Offer details.
/// </summary>
public class IntroductoryOffer
{
    public readonly float RegularPrice;
    public readonly float IntroductoryPrice;
    public readonly string LocalizedTitle;
    public readonly string LocalizedDescription;
    public readonly string LocalizedPriceString;
    public readonly string ISOCurrencyCode;
    public readonly string PriceLocale;
    
    /// <summary>
    /// Number of units for each introductory period.
    /// </summary>
    public readonly int NumberOfUnits;
    
    /// <summary>
    /// Type of unit used for calculating length of introductory period.
    /// </summary>
    public readonly UnitType Unit;
    
    /// <summary>
    /// Number of periods that introductory period is valid.
    /// </summary>
    public readonly int NumberOfPeriods;

    public IntroductoryOffer(string productJson)
    {
        var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(productJson);
        
        this.IntroductoryPrice = ParseIntroductoryPrice(dict);
        this.RegularPrice = ParseRegularPrice(dict);
        this.NumberOfUnits = ParseNumberOfUnits(dict);
        this.NumberOfPeriods = ParseNumberOfPeriods(dict);
        this.Unit = ParseUnitType(dict);
        this.LocalizedTitle = dict["localizedTitle"];
        this.LocalizedDescription = dict["localizedDescription"];
        this.LocalizedPriceString = dict["localizedPriceString"];
        this.ISOCurrencyCode = dict["isoCurrencyCode"];
        this.PriceLocale = dict["introductoryPriceLocale"];
        this.PriceLocale = dict["introductoryPriceLocale"];
    }
    
    float ParseRegularPrice(Dictionary<string, string> json)
    {
        string price = json["localizedPrice"];
        
        return string.IsNullOrEmpty(price) ? 
            throw new InvalidOfferException() : 
            float.Parse(price);
    }
    
    float ParseIntroductoryPrice(Dictionary<string, string> json)
    {
        string price = json["introductoryPrice"];
        
        return string.IsNullOrEmpty(price) ? 
            throw new InvalidOfferException() : 
            float.Parse(price);
    }

    int ParseNumberOfUnits(Dictionary<string, string> json)
    {
        string unitNum = json["numberOfUnits"];
        
        return string.IsNullOrEmpty(unitNum) ? 
            throw new InvalidOfferException() : 
            int.Parse(unitNum);
    }

    int ParseNumberOfPeriods(Dictionary<string, string> json)
    {
        string periodNum = json["introductoryPriceNumberOfPeriods"];
        
        return string.IsNullOrEmpty(periodNum) ? 
            throw new InvalidOfferException() : 
            int.Parse(periodNum);
    }
    
    UnitType ParseUnitType(Dictionary<string, string> json)
    {
        string unit = json["unit"];
        
        return string.IsNullOrEmpty(unit) ? 
            throw new InvalidOfferException() : 
            (UnitType)int.Parse(unit);
    }

    public enum UnitType
    {
        Days = 0, 
        Weeks = 1, 
        Months = 2, 
        Years = 3,
    }
}
