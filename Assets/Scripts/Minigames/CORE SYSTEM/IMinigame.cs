public interface IMinigame
{
    string Name { get; }
    MinigameState CurrentState { get; }
    void Initialize();
    void StartGame();
    void EndGame();
    void ResetGame();
}
