using System.Collections.Generic;

public interface IScoreRule
{
    void StartMatch();
    void UpdateScores();
    void AssignFinalPoints();
    Dictionary<ulong,int> GetResults();
}