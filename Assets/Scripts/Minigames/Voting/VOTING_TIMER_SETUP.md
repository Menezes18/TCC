# Configuração do Timer de Votação e Mouse

## ✅ Implementado

### Funcionalidades Adicionadas:

1. **Liberação do Mouse Durante Votação**
   - Mouse é desbloqueado automaticamente quando a votação inicia
   - Cursor fica visível para clicar nos cards
   - Mouse é bloqueado novamente quando a votação termina

2. **Timer de Votação na UI**
   - Mostra quanto tempo resta para votar
   - Atualiza em tempo real
   - Se esconde automaticamente quando a votação termina

---

## 🎨 Como Configurar a UI do Timer

### Passo 1: Criar o Timer UI

1. **Criar GameObject na Canvas:**
   ```
   Canvas (sua canvas existente)
   └── VotingTimerPanel (novo)
       └── TimerText (TextMeshPro)
   ```

2. **Configurar VotingTimerPanel:**
   - Adicione um `Image` (opcional, para fundo)
   - Posicione no topo da tela (ou onde preferir)

3. **Configurar TimerText:**
   - Use TextMeshPro - Text
   - Configure fonte, tamanho, cor
   - Alinhe ao centro

### Passo 2: Adicionar o Script VotingTimerUI

1. Selecione o GameObject `VotingTimerPanel`
2. Adicione o componente `VotingTimerUI`
3. Configure no Inspector:
   ```
   Timer Text: [Arraste o TextMeshPro aqui]
   Timer Panel: [Arraste o VotingTimerPanel aqui]
   HUDSO: [Arraste seu HUDSO ScriptableObject aqui]
   Timer Format: "Tempo de Votação: {0:0}s"
   Auto Hide When Zero: ✓
   ```

### Resultado

- Timer aparece quando votação começa
- Mostra contagem regressiva
- Desaparece quando votação termina
- Mouse é gerenciado automaticamente

---

## 🎮 Comportamento do Mouse

### Durante a Votação:
```csharp
Cursor.lockState = CursorLockMode.None;
Cursor.visible = true;
```

### Após a Votação:
```csharp
Cursor.lockState = CursorLockMode.Locked;
Cursor.visible = false;
```

---

## 📝 Arquivos Modificados/Criados

### Novos Arquivos:
- `VotingTimerUI.cs` - Componente UI do timer

### Arquivos Modificados:
- `HUDSO.cs` - Adicionado evento `EventOnVotingTimerUpdated`
- `LobbyController.cs` - Hook para atualizar timer e gerenciar cursor
- `UIVoteInputProvider.cs` - Libera/bloqueia cursor ao iniciar/terminar

---

## 🧪 Testar

1. Entre em Play Mode
2. Inicie o jogo (votação deve começar após o timer inicial)
3. Observe:
   - ✅ Timer aparece no topo da tela
   - ✅ Mouse fica livre para clicar
   - ✅ Contagem regressiva funciona
   - ✅ Após votar ou tempo acabar, mouse trava novamente

---

## ⚙️ Personalização

### Mudar Formato do Timer:
No componente `VotingTimerUI`, altere `Timer Format`:
- `"Vote agora! {0:0}s"` 
- `"{0:0.0} segundos restantes"`
- `"⏱️ {0:0}s"`

### Mudar Posição:
Ajuste a posição do `VotingTimerPanel` na Canvas como preferir.

### Adicionar Animações:
Você pode adicionar animações ao mostrar/esconder o painel modificando o script `VotingTimerUI`.

---

Pronto! Sistema de timer e controle de mouse implementado! 🎉
