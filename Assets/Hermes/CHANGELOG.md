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