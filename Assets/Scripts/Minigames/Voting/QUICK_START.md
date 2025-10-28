# Quick Start - Sistema de Votação

## 🚀 Início Rápido (5 minutos)

### Passo 1: Configurar LobbyController
1. Abra a cena de Lobby (`RASCUNHO`)
2. Selecione o GameObject com `LobbyController`
3. No Inspector:
   - Marque ✓ **Enable Voting**
   - Configure **Voting Duration**: `10` (segundos)
   - Arraste seu `MinigameCatalog` para **Minigame Catalog**

### Passo 2: Escolher Modo de Votação

#### **Opção A: Votação via UI (Recomendado para começar)**

1. **Criar UI Panel:**
   - Botão direito na Hierarchy → UI → Panel
   - Renomeie para "VotingPanel"
   - Adicione um componente `Horizontal Layout Group` ou `Grid Layout Group`

2. **Criar VoteCard Prefab:**
   - Crie um GameObject vazio
   - Adicione componentes UI:
     - `Image` (para o ícone)
     - 2x `TextMeshPro - Text` (nome e contagem)
     - `Button`
     - `GameObject` vazio (indicador de seleção)
   - Adicione o script `VoteCard`
   - Configure as referências no Inspector
   - Salve como Prefab em `Assets/Prefabs/`

3. **Adicionar Provider:**
   - Crie GameObject vazio "VotingUIProvider"
   - Adicione script `UIVoteInputProvider`
   - Configure:
     - **Voting Panel**: O Panel criado no passo 1
     - **Cards Container**: O mesmo Panel (com layout)
     - **Card Prefab**: O prefab criado no passo 2
     - Marque ✓ **Auto Hide On Voting End**

#### **Opção B: Votação Física**

1. **Criar VoteZone Prefab:**
   - Crie um Cube no mundo
   - Adicione um `Box Collider`
   - Marque ✓ **Is Trigger**
   - Adicione TextMeshPro 3D para nome e contagem
   - Adicione script `VoteZone`
   - Configure referências
   - Salve como Prefab

2. **Adicionar Provider:**
   - Crie GameObject vazio "VotingZoneProvider"
   - Adicione script `ZoneVoteInputProvider`
   - Configure:
     - **Zone Prefab**: O prefab criado no passo 1
     - **Zone Spawn Positions**: Crie 3 Empty GameObjects e adicione aqui
     - **Zone Spacing**: `5` (se não usar positions)

### Passo 3: Testar

1. **Play no Editor:**
   - Entre no Play Mode
   - Inicie o jogo normalmente
   - Quando o timer acabar, votação deve iniciar
   - Vote e veja a contagem atualizar

2. **Build Multiplayer:**
   - Faça build do jogo
   - Conecte 2+ clientes
   - Observe votação sincronizada

---

## ⚙️ Configurações Opcionais

### Desabilitar Votação Temporariamente
No `LobbyController`, desmarque **Enable Voting**. O sistema volta à seleção aleatória.

### Ajustar Duração da Votação
Altere **Voting Duration** no `LobbyController` (valor em segundos).

### Usar Ambos os Modos
Você pode ter UIVoteInputProvider E ZoneVoteInputProvider ativos ao mesmo tempo. Ambos funcionarão em paralelo.

---

## 🐛 Troubleshooting Comum

### Votação não inicia?
- ✅ Verificar se `Enable Voting` está marcado
- ✅ Verificar se `MinigameCatalog` está atribuído
- ✅ Olhar console por erros

### Cards/Zonas não aparecem?
- ✅ Verificar se o Provider está ativo na cena
- ✅ Verificar se o Prefab está atribuído
- ✅ Verificar se o Prefab tem todos os componentes

### Votos não contam?
- ✅ Verificar se está em modo multiplayer (NetworkServer ativo)
- ✅ Verificar se `PlayerData` tem steamId válido

---

## 📚 Documentação Completa

Para informações detalhadas, consulte:
- `README_VOTING_SYSTEM.md` - Documentação completa do sistema
- `IMPLEMENTATION_SUMMARY.md` - Resumo técnico da implementação

---

## 💡 Dicas

- **Teste com 1 minigame primeiro** para ver o comportamento de auto-seleção
- **Use Context Menu** no `VotingSystemExample` para testar manualmente
- **Monitore os logs** com filtro `[VOTING]` para entender o fluxo
- **Comece com UI** (mais simples) e depois teste zonas físicas

---

Pronto! Sistema configurado e funcionando! 🎉
