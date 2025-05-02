using UnityEngine;

public interface IObserverPontos
{
    abstract void Atualizacao(ISubjectPontos subject, int[] pontos, string[] jogadores);
}
