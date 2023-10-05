namespace OpenMud.Mudpiler.Net.Core.Encoding;

public interface IWorldStateEncoder
{
    void InitializeScope(string client);
    void DisposeScope(string client);
    void Encode(StateTransmitter transmitter);
}