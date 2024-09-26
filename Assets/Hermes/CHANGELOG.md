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