namespace Hermes {
    /// <summary>
    /// IAP Init status
    /// </summary>
    public enum InitStatus {
        None,
        Initializing,
        Ok,
        PurchasingDisabled,
        PurchasingUnavailable,
        NoProductsAvailable,
        AppNotKnown,
        Unknown,
    }
}
