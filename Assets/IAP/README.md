# Fantam IAP

Generic IAP package for Unity

## Requirements

- Unity 2019.4+
- Tested on iOS and GooglePlay

## Test Scene

After setting up AppStore/GooglePlay purchases, test purchases can be tested using the PurchaseTestScene on devices.

## Receipt Validation

Compatible with receipt validation. To use, define IAP_RECEIPT_VALIDATION in player settings. In order to use validation, first generate the AppleTangle and GoogleTangle files

1. Window/Unity IAP/Receipt Validation Obfuscator
2. AppleTangle and GoogleTangle files generated.
3. Define IAP_RECEIPT_VALIDATION

## Defines

Use `DEBUG_IAP` for useful debug information logging during development.
