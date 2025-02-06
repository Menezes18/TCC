using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

namespace Mirror.Examples.Pong
{
    public class Player : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnLivesChanged))]
        public int lives = 3;
        public float speed = 30;
        public Rigidbody2D rigidbody2d;
        [SyncVar(hook = nameof(OnScoreChanged))]
        public int score = 0;
        [SyncVar]
        public int playerId;

        // Event handlers para atualizar UI
        void OnLivesChanged(int oldValue, int newValue)
        {
            // Atualizar UI de vidas
            UpdateLivesUI(newValue);
        }

        void OnScoreChanged(int oldValue, int newValue)
        {
            // Atualizar UI de pontua��o
            UpdateScoreUI(newValue);
        }

        void UpdateLivesUI(int currentLives)
        {
            // Implemente aqui a atualiza��o da UI de vidas
            Debug.Log($"Player {playerId} lives: {currentLives}");
        }

        void UpdateScoreUI(int currentScore)
        {
            // Implemente aqui a atualiza��o da UI de pontua��o
            Debug.Log($"Player {playerId} score: {currentScore}");
        }
        // need to use FixedUpdate for rigidbody
        void FixedUpdate()
        {
            // only let the local player control the racket.
            // don't control other player's rackets
            if (isLocalPlayer)
#if UNITY_6000_0_OR_NEWER
                rigidbody2d.linearVelocity = new Vector2(0, Input.GetAxisRaw("Vertical")) * speed * Time.fixedDeltaTime;
#else
                rigidbody2d.velocity = new Vector2(0, Input.GetAxisRaw("Vertical")) * speed * Time.fixedDeltaTime;
#endif
        }

        [Server]
        public void LoseLife()
        {
            lives--;
            if (lives <= 0)
            {
                // Jogador perdeu
                RpcGameOver(false);
            }
        }

        [Server]
        public void AddScore(int points)
        {
            score += points;
            if (score >= 10) // Exemplo: vit�ria com 10 pontos
            {
                // Jogador ganhou
                RpcGameOver(true);
            }
        }

        [ClientRpc]
        void RpcGameOver(bool won)
        {
            string message = won ? "Voc� Venceu!" : "Voc� Perdeu!";
            // Mostrar mensagem de fim de jogo na UI
            Debug.Log($"Player {playerId}: {message}");
        }
    }
}
