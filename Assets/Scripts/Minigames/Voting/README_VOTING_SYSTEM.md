# Sistema de Votação de Minigames - Documentação de Uso

## Visão Geral

Este documento explica como configurar e usar o sistema de votação de minigames implementado para o TCC. O sistema permite que jogadores votem no próximo minigame através de dois modos: **votação via UI** ou **votação física por zona**.

---

## Componentes Principais

### 1. **MinigameRotationState**
Gerencia quais minigames já foram jogados na partida atual.

**Características:**
- Singleton persistente (DontDestroyOnLoad)
- Mantém lista de minigames jogados
- Reseta automaticamente na cena de vitória
- Retorna apenas minigames elegíveis (não jogados)

**Não precisa configurar manualmente** - é criado automaticamente quando necessário.

---

### 2. **VotingManager**
Sistema central de votação (server-authoritative).

**Características:**
- Seleciona até 3 minigames elegíveis aleatoriamente
- Registra votos de jogadores
- Resolve empates por sorteio aleatório
- Replica estado via Mirror SyncLists

**Não precisa configurar manualmente** - é criado automaticamente pelo LobbyController.

---

### 3. **Vote Input Providers**
Interfaces plugáveis para diferentes modos de votação.

#### **UIVoteInputProvider** (Votação via UI)
- Exibe cards clicáveis com ícones dos minigames
- Mostra contagem de votos em tempo real
- Permite trocar de voto antes do encerramento

#### **ZoneVoteInputProvider** (Votação Física)
- Cria zonas físicas no mapa (triggers)
- Jogadores votam entrando na zona
- Contagem dinâmica baseada em presença

---

## Configuração Passo a Passo

### Passo 1: Configurar o MinigameCatalog
Certifique-se de que seu `MinigameCatalog` ScriptableObject está configurado com:
- ID único para cada minigame
- Nome de exibição
- Ícone/thumbnail
- Referência de cena válida
- Cena de vitória configurada

### Passo 2: Configurar o LobbyController

No GameObject com o `LobbyController`, configure:

```
LobbyController
├── Enable Voting: ✓ (marcar para ativar votação)
├── Voting Duration: 10 (segundos de duração da votação)
└── Minigame Catalog: [Arraste seu MinigameCatalog SO aqui]
```

**Importante:** Se `Enable Voting` estiver desmarcado, o sistema volta ao comportamento antigo (seleção aleatória).

### Passo 3A: Configurar Votação via UI

1. **Criar o Canvas de Votação:**
   - Crie um Canvas na cena de Lobby
   - Adicione um Panel filho (será o container da votação)
   - Dentro do Panel, crie um layout (Horizontal/Vertical/Grid Layout)

2. **Criar o VoteCard Prefab:**
   ```
   VoteCard (Prefab)
   ├── Image (ícone do minigame)
   ├── TMP_Text (nome do minigame)
   ├── TMP_Text (contagem de votos)
   ├── Button (botão de voto)
   └── GameObject (indicador de selecionado)
   ```

3. **Adicionar UIVoteInputProvider:**
   - Crie um GameObject vazio "VotingUIProvider"
   - Adicione o componente `UIVoteInputProvider`
   - Configure:
     ```
     Voting Panel: [Arraste o Panel do passo 1]
     Cards Container: [Arraste o layout do passo 1]
     Card Prefab: [Arraste o VoteCard prefab]
     Auto Hide On Voting End: ✓
     ```

### Passo 3B: Configurar Votação Física (Alternativa)

1. **Criar o VoteZone Prefab:**
   ```
   VoteZone (Prefab)
   ├── Collider (trigger = true, ajuste o tamanho)
   ├── Visual (mesh/sprites para mostrar a área)
   ├── TMP_Text (nome do minigame - no mundo 3D)
   └── TMP_Text (contagem de votos - no mundo 3D)
   ```

2. **Adicionar ZoneVoteInputProvider:**
   - Crie um GameObject vazio "VotingZoneProvider"
   - Adicione o componente `ZoneVoteInputProvider`
   - Configure:
     ```
     Zone Prefab: [Arraste o VoteZone prefab]
     Zone Spawn Positions: [Array de Transforms marcando onde spawnar]
     Zone Spacing: 5 (espaçamento se não usar positions manuais)
     Auto Cleanup On Voting End: ✓
     ```

3. **Definir Posições de Spawn (Opcional mas Recomendado):**
   - Crie Empty GameObjects na posição desejada para cada zona
   - Adicione-os ao array `Zone Spawn Positions`
   - Se não fizer isso, as zonas serão criadas automaticamente em linha

---

## Fluxo de Execução

### Durante o Jogo:

1. **Lobby inicia** → LobbyController detecta que é hora de iniciar
2. **Timer de votação inicia** (se `Enable Voting = true`)
3. **VotingManager seleciona até 3 minigames elegíveis**
4. **Input Provider exibe as opções** (UI ou zonas físicas)
5. **Jogadores votam** durante o tempo configurado
6. **Votação encerra:**
   - Minigame mais votado é escolhido
   - Empates são resolvidos aleatoriamente
   - Minigame é marcado como "jogado"
7. **Cena do minigame é carregada**

### Na Cena de Vitória:
- Sistema **automaticamente reseta** a lista de jogados
- Todos os minigames voltam a ser elegíveis

---

## Casos Especiais Tratados

### ✅ Só 1 minigame elegível
- Sistema seleciona automaticamente
- Não abre votação
- Ainda marca como jogado

### ✅ 0 minigames elegíveis
- Sistema força reset da rotação
- Todos voltam a ser elegíveis
- Log de warning é gerado

### ✅ Empate na votação
- Sistema escolhe aleatoriamente entre empatados
- Log indica que houve desempate

### ✅ Jogador desconecta
- Voto é removido automaticamente
- Contagem atualiza em tempo real

### ✅ Jogador troca de voto
- Voto anterior é removido
- Novo voto é contabilizado
- Funciona tanto em UI quanto em zonas físicas

---

## Debugging

### Logs Importantes:

```
🗳️ [VOTING] Started voting round with X options
🗳️ [VOTING] Player {id} voted for option {index}
🏆 [VOTING] Winner determined: {name} with {votes} votes
🎲 [VOTING] TIE! {count} options tied - Random winner: {name}
⚠️ [VOTING] No eligible minigames found! Forcing rotation reset
🔄 [ROTATION] Minigame '{id}' marked as played
🔄 [VOTING] MinigameRotationState reset after victory scene
```

### Verificações Comuns:

1. **Votação não inicia?**
   - Verificar se `Enable Voting` está marcado no LobbyController
   - Verificar se `MinigameCatalog` está atribuído
   - Verificar logs de erro no console

2. **Votos não contam?**
   - Verificar se `PlayerData` tem `steamId` válido
   - Verificar se VotingManager está ativo no servidor
   - Verificar se NetworkServer está ativo

3. **Zonas físicas não aparecem?**
   - Verificar se ZoneVoteInputProvider está no servidor
   - Verificar se Zone Prefab tem Collider como trigger
   - Verificar logs de spawn

4. **Minigames repetindo?**
   - Verificar se MinigameRotationState.Reset() é chamado na vitória
   - Verificar logs de "marked as played"

---

## Alternando Entre Modos

Você pode ter **ambos** os providers na cena simultaneamente:
- Cada um se inscreve nos eventos do VotingManager
- Ambos recebem as mesmas opções
- Contagem é compartilhada

Ou use apenas um:
- Desabilite/remova o GameObject do provider não usado
- Sistema funciona da mesma forma

---

## Extensibilidade

### Criar um novo modo de votação:

1. Implemente a interface `IVoteInputProvider`
2. Inscreva-se nos eventos do VotingManager:
   - `OnVotingStarted`
   - `OnVoteCountsUpdated`
   - `OnVotingEnded`
3. Chame `VotingManager.Instance.RegisterVote(playerId, optionIndex)` quando o jogador votar
4. Pronto!

---

## Notas Finais

- **Server-authoritative:** Toda lógica de votação roda no servidor
- **Network-replicated:** Estado é sincronizado via Mirror
- **Fail-safe:** Sistema volta para seleção aleatória se algo falhar
- **Logs extensivos:** Facilita debugging e acompanhamento do fluxo

Para dúvidas ou problemas, verifique os logs no console Unity com filtro `[VOTING]`, `[ROTATION]`, ou `[LOBBY]`.
