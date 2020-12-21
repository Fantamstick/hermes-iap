namespace FantamIAP {
    public enum PurchaseResponse {
        /// <summary>
        /// Purchase OR Restore successful.
        /// </summary>
        Ok,
        PurchasingUnavailable,
        ExistingPurchasePending,
        ProductUnavailable,
        SignatureInvalid,
        UserCancelled,
        PaymentDeclined,
        DuplicateTransaction,
        InvalidReceipt,
        Unknown,
        NoInit,
        Timeout,
    }
}
