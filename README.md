![](https://img.shields.io/badge/version-v7.0.0-green)
# Hermes IAP

IAP manager package for Unity. Makes use of local receipt validation as specified in [Unity's  documentation](https://docs.unity3d.com/Manual/UnityIAPValidatingReceipts.html).

## Features

### iOS
- [x] Auto-Renewable Subscriptions
- [ ] Non-Renewing Subscriptions
- [x] Consumables
- [x] Non-Consumables
- [x] Deferred Purchases (iOS)

### Google Play
- [x] Auto-Renewable Subscriptions (Partial support) *1
- [ ] Non-Renewing Subscriptions
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

- Unity: 2021.3.43f1
- Tested on iOS, GooglePlay and Amazon Store

## Dependencies

- Unity IAP  v4.13.0  (July 7, 2025)


## Installation as UPM Package
Add `https://github.com/Fantamstick/hermes-iap/Assets/Hermes` from the Package Manager.

or locate `manifest.json` in your Unity project's `Packages` folder and add the following dependencies:
```
"dependencies": {
  "com.fantamstick.hermesiap": "https://github.com/Fantamstick/hermes-iap.git?path=Assets/Hermes#7.0.0",
  ...
}
```

## Sample

After setting up the IAP settings on AppStoreConnect and/or GooglePlay, purchases can be tested using the PurchaseTestScene on devices.

## Defines

- `DEBUG_IAP` for useful debug information logging during development.
- `UNITY_PURCHASING` *required* for Android builds.

# Build

## Android

Player Settings > Punlishing Settings

- Check `Custom Main Gradle Template`,`Custom Launcher Gradle Template`,`Custom Gradle Properties Template`
- Add `compileSdkVersion 35` to `launcherTemplate.gradle`,`mainTemplate.gradle`
- Add 
```
android.aapt2FromMavenOverride=/Users/ユーザー名/Library/Android/sdk/build-tools/36.0.0/aapt2 // ローカルのAndoridSDKのpath
android.suppressUnsupportedCompileSdk=35
org.gradle.jvmargs=-Xmx4096m -Dfile.encoding=UTF-8
android.useAndroidX=true
```
to `gradleTemplate.properties`

# License

Copyright (c) 2021 Fantamstick, Ltd
Released under the MIT license

https://github.com/Fantamstick/hermes-iap/blob/master/LICENSE.md
