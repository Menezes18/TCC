using System.Collections.Generic;

public interface IScoreRule{
    void SetupMiniGame();
    void StartMatch();
    void EndMatch();
    void UpdateScores();
    void AssignFinalPoints();
    Dictionary<ulong,int> GetResults();
    Dictionary<ulong,int> GetLiveScores();
}