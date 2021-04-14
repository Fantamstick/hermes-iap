![](https://img.shields.io/badge/version-v2.0.5-green)
# Hermes IAP

IAP manager package for Unity. Makes use of local receipt validation as specified in [Unity's  documentation](https://docs.unity3d.com/Manual/UnityIAPValidatingReceipts.html).

## Features

- [x] Auto-Renewable Subscriptions
- [x] Non-Renewing Subscriptions
- [ ] Consumables (untested)
- [ ] Non-Consumables  (untested)
- [ ] Deferred Purchases (untested)

### Google Play
Subscription
- [ ] Upgrade / Downgrade
- [ ] Google Play Developer API
- [ ] Confirm price changes for subscriptions
- [ ] Deferred Purchases


## Requirements

- Unity 2019.4+
- Tested on iOS and GooglePlay

## Dependencies

- Unity IAP  v2.2.2  (Jan 21, 2021)
- [Google Play Plugins for Unity v1.4.0](https://github.com/google/play-unity-plugins) (Mar 13, 2021)
    - Included [Google Play Billing Library](https://developer.android.com/google/play/billing/integrate) version 3.0.3
    - official: 
      - https://developer.android.com/google/play/billing/integrate
      - https://developer.android.com/google/play/billing/unity

## Installation as UPM Package
Add `https://github.com/Fantamstick/hermes-iap/Assets/Hermes` from the Package Manager.

or locate `manifest.json` in your Unity project's `Packages` folder and add the following dependencies:
```
"dependencies": {
  "com.hermes.iap": "https://github.com/Fantamstick/hermes-iap.git?path=Assets/Hermes#2.0.5",
  ...
}
```

## Sample

After setting up the IAP settings on AppStoreConnect and/or GooglePlay, purchases can be tested using the PurchaseTestScene on devices.

## Defines

Use `DEBUG_IAP` for useful debug information logging during development.

# License

Copyright (c) 2021 Fantamstick, Ltd
Released under the MIT license

https://github.com/Fantamstick/hermes-iap/blob/master/LICENSE.md
