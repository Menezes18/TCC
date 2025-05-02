using UnityEngine;

public interface ISubjectPontos
{
    void Adicionar(IObserverPontos observer);

    void Retira(IObserverPontos observer);

    void Notifica();
}
