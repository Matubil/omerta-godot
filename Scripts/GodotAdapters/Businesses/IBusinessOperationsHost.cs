namespace Omerta.GodotAdapters.Businesses;

public interface IBusinessOperationsHost
{
    bool CanInteractWithOperations { get; }

    string OperationsPrompt { get; }

    void InteractWithOperations();
}
