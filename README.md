![](https://img.shields.io/badge/version-v3.1.0-green)
# Hermes IAP

IAP manager package for Unity. Makes use of local receipt validation as specified in [Unity's  documentation](https://docs.unity3d.com/Manual/UnityIAPValidatingReceipts.html).

## Features

### iOS
- [x] Auto-Renewable Subscriptions
- [x] Non-Renewing Subscriptions
- [x] Consumables
- [x] Non-Consumables
- [x] Deferred Purchases (iOS)

### Google Play
- [x] Auto-Renewable Subscriptions (Partial support) *1
- [x] Non-Renewing Subscriptions (Partial support) *1
- [x] Non-Consumables
- [ ] Upgrade / Downgrade
- [ ] Google Play Developer API
- [ ] Confirm price changes for subscriptions
- [x] Deferred Purchases (untested)

*1: Partial support means subscriptions are confirmed on purchase, but subsequent subscription cancellations or updates are not automatically done. Complete subscription support requires a backend to call GooglePlay servers to confirm and update purchase status based on developer needs.

### Amazon Store
- [ ] Auto-Renewable Subscriptions
- [ ] Non-Renewing Subscriptions
- [ ] Consumables
- [x] Non-Consumables

## Requirements

- Unity 2019.4+
- Tested on iOS, GooglePlay and Amazon Store

## Dependencies

- Unity IAP  v3.1.0  (April 19, 2021)
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
  "com.fantamstick.hermesiap": "https://github.com/Fantamstick/hermes-iap.git?path=Assets/Hermes#3.0.1",
  ...
}
```

## Sample

After setting up the IAP settings on AppStoreConnect and/or GooglePlay, purchases can be tested using the PurchaseTestScene on devices.

## Defines

- `DEBUG_IAP` for useful debug information logging during development.
- `UNITY_PURCHASING` *required* for Android builds.

# License

Copyright (c) 2021 Fantamstick, Ltd
Released under the MIT license

https://github.com/Fantamstick/hermes-iap/blob/master/LICENSE.md
