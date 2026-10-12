namespace Omerta.GodotAdapters.Businesses;

public interface IBusinessMerchantHost
{
    bool CanInteractWithMerchant { get; }

    string MerchantPrompt { get; }

    void InteractWithMerchant();

    void NotifyMerchantAvailability(bool available);
}
