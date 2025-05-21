using System.Collections.Generic;

public interface IScoreRule{
    void SetupMiniGame();
    void StartMatch();
    void UpdateScores();
    void AssignFinalPoints();
    Dictionary<ulong,int> GetResults();
}