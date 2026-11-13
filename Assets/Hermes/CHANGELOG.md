-----------------------
7.0.11
-----------------------
- Fix purchase failure / cancel callback not fired with Unity IAP 5.x
    - Unity IAP 5.x legacy bridge only calls IStoreListener.OnPurchaseFailed(Product, PurchaseFailureReason)
    - Invoke onPurchaseFailureCb from that overload as well (once per purchase)
- Notify PurchaseFailureReason.ProductUnavailable when the product to purchase is not found

-----------------------
7.0.10
-----------------------
- Fix free trial / introductory offer not shown on iOS with Unity IAP 5.x (StoreKit 2)
    - IOSIntroductoryOfferFactory no longer fails on missing keys, and reads "introductoryType" (paymentMode)
    - StoreKit 2: trust Unity IAP's eligibility (isEligibleForIntroOffer), no receipt check
    - StoreKit 1: read the receipt without signature validation, keep the offer when the receipt is unavailable
- Update dependency: com.unity.purchasing 5.4.1

-----------------------
6.0.0
September 26, 2024
-----------------------
- Update Unity In App Purchase plugin: 4.12.2
    - using Google Billing Library 6.0.1
    - support Apple privacy manifest
- Update Unity version: 2021.3.43f1
- Update Android targetSDK 34

-----------------------
4.1-beta
December 21, 2021
-----------------------
- Add iOS promotion purchase support
- add initialization error "Initializing" if calling init twice before finished

-----------------------
3.1.0
September 24, 2021
-----------------------
- Add support for GooglePlay deferred purchases (untested).

Upgrade Notes:

iOS builds need to explicitely add `WithDeferredPurchaseCompatibility` to the IAPBuilder when initializing the library. 