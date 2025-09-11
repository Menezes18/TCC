using UnityEngine;

public interface IPlayerAbility
{
    string Id { get; }
    bool CanExecute(PlayerContext ctx);
    void Execute(PlayerContext ctx);
}
