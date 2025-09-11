// Fase 4 - Item 12: Interface de capacidade de Rolagem
public interface IRollCapability
{
    bool CanRoll(PlayerContext ctx);
    void ExecuteRoll(PlayerContext ctx);
}
