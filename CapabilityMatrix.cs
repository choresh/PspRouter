namespace PspRouter;

public static class CapabilityMatrix
{
    public static bool Supports(string psp, RouteInput tx) => tx.Method switch
    {
        PaymentMethod.Card           => psp is "Adyen" or "Stripe",
        PaymentMethod.PayPal         => psp is "PayPal",
        PaymentMethod.KlarnaPayLater => psp is "Klarna",
        _ => false
    };
}
