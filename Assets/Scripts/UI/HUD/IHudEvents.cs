// Fase 4 - Item 13: Interface para eventos de HUD consumidos pelos sistemas de jogo.
public interface IHudEvents
{
    void OnFreezeTimer(float value);
    void OnMatchTimer(float value);
    void OnGameOver(string message);
}
