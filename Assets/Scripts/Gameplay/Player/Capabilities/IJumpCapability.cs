// Fase 4 - Item 12: Interface de capacidade de Pulo
public interface IJumpCapability
{
    bool CanJump(PlayerContext ctx);
    void ExecuteJump(PlayerContext ctx);
}
