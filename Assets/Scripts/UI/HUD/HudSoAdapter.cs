// Adapter que traduz IHudEvents para chamadas do HUDSO (mantém compatibilidade com ScriptableObject existente).
public class HudSoAdapter : IHudEvents
{
    private readonly HUDSO _hud;
    public HudSoAdapter(HUDSO hud) { _hud = hud; }
    public void OnFreezeTimer(float value) { _hud?.FreezeTimerUpdated(value); }
    public void OnMatchTimer(float value) { _hud?.MatchTimerUpdate(value); }
    public void OnGameOver(string message) { _hud?.GameOver(message); }
}
