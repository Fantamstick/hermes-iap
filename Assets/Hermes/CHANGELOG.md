-----------------------
3.1.0
September 24, 2021
-----------------------
- Add support for GooglePlay deferred purchases (untested).

Upgrade Notes:

iOS builds need to explicitely add `WithDeferredPurchaseCompatibility` to the IAPBuilder when initializing the library. 