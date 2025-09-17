Hazards Modulares (Corrida estilo Fall Guys)

Este projeto inclui componentes prontos para adicionar obstáculos em pistas de corrida. Basta arrastar os scripts nos GameObjects e configurar pelo Inspector. Cada componente exibe uma documentação resumida no topo do Inspector.

Componentes

- Bounce Pad (Trampolim)
  - Arquivo: `Assets/Scripts/Gameplay/Minigames/Shared/Hazards/BouncePad.cs`
  - Uso: Adicione a um objeto com Collider marcado como IsTrigger. Ajuste forças horizontal/vertical e a direção (Forward local ou vetor global). O impulso é aplicado no servidor.

- Conveyor (Esteira, Server)
  - Arquivo: `Assets/Scripts/Gameplay/Minigames/Shared/Hazards/LocalConveyorZone.cs`
  - Uso: Adicione a um volume (Collider IsTrigger). Defina direção (forward local ou vetor global). Ajuste `pulseInterval` e `pulseHorizontalStrength`.
  - Funcionamento: o servidor aplica impulsos periódicos leves via Mirror, empurrando o jogador de forma sincronizada e com baixo tráfego.

- Ice Zone (Gelo)
  - Arquivo: `Assets/Scripts/Gameplay/Minigames/Shared/Hazards/IceZone.cs`
  - Uso: Adicione a uma área com Collider IsTrigger. Dois modos:
    - LocalOwner (padrão): aplica deslizamento local no cliente dono (mais suave, menos rede). A posição replica via sincronização normal do player.
    - ServerPulse: o servidor aplica pequenos impulsos periódicos (configuráveis) para manter o deslizamento consistente para todos, com pouco tráfego.
  - Parâmetros: `slideAcceleration`, `maxExtraSpeed` (LocalOwner); `pulseInterval`, `pulseHorizontalStrength`, `minServerSlideSpeed` (ServerPulse).

- Rotating Hammer (Martelo Giratório)
  - Arquivo: `Assets/Scripts/Gameplay/Minigames/Shared/Hazards/RotatingHammer.cs`
  - Uso: Monte um braço com Collider IsTrigger, defina `rotateTarget` (braço) e `pivot` (centro). Ajuste rotação e forças. Acerto é calculado no servidor.

 - Doors (Portas falsas/verdadeiras)
   - Opção A (simplificada): `FallGuysDoorRow.cs` (pai) + `FallGuysDoor.cs` (filho)
     - No pai, adicione `Fall Guys Door Row`; nas portas filhas, `Fall Guys Door` com collider sólido (não IsTrigger) e um Trigger frontal.
     - O servidor sorteia quais portas são verdadeiras; ao bater numa verdadeira, a porta cai (animação ou física). Nas falsas, o jogador é empurrado.
   - Opção B (detalhada): `ControladorFileiraPortas.cs` (pai) + `SegmentoPorta.cs` (filho)
     - Controle manual com arrays e personalização de visuais ao abrir.

Observações de Rede

- Impulsos competitivos (trampolim, martelo, porta falsa) são aplicados via servidor para consistência.
- Efeitos contínuos (esteira, gelo) rodam localmente no cliente dono para fluidez; a posição do jogador replica pelos componentes de rede já existentes.

Extensão do Player

- Método: `PlayerScript.ServerApplyImpulse(Vector3 dir, float horizontalStrength, float verticalStrength, float stunDuration, bool setStagger)`
- Permite que hazards definam impulsos customizados sem alterar o `Database` global.

Pontuação e Fluxo de Corrida

- Para linha de chegada utilize `Assets/Scripts/Gameplay/Minigames/Shared/ServerFinishLine.cs`.
- O controller do minigame pode calcular pontos por ordem de chegada utilizando `SettingsMiniGameData` (bônus 1º, 2º, 3º, 4º).
