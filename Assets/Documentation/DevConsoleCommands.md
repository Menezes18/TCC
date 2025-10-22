# Dev Console – Comandos

Atalho para abrir/fechar: tecla ` (backquote) ou F1

O console é injetado automaticamente em runtime e funciona para host/cliente. Alguns comandos exigem permissões de servidor (host).

- help
  Lista os comandos disponíveis.

- clear
  Limpa o histórico do console.

- start
  Inicia/prepara a partida atual (cliente solicita ao servidor; host executa direto).

- end  [host]
  Encerra a partida atual imediatamente.

- restart  [host]
  Reinicia o jogo: zera pontos e rotação de minigames.

- scoreboard
  Mostra os pontos totais do scoreboard.

- live
  Mostra o placar ao vivo do minigame atual (GetLiveScores).

- results
  Mostra os ganhos do último minigame (StoreLastResults).

- status
  Lista jogadores com estado (morto/vivo), frozen, cor e cena atual.

- freeze on|off  [host]
  Congela/descongela todos os jogadores.

- timer <segundos>  [host]
  Define o Match Timer para o valor informado.

- tp <all|nome|steamId> <x> <y> <z> [yRot]  [host]
  Teleporta um jogador (por nome parcial/SteamID) ou todos para uma posição.

- team
  Lista times A/B no Soccer (quando ativo).

- points add <alvo> <delta>  [host]
  Adiciona pontos a um jogador (alvo: all | nome | steamId).

- points set <alvo> <valor>  [host]
  Define os pontos de um jogador (internamente calcula delta).

- unityconsole on|off|toggle
  Mostra/oculta o console nativo da Unity (só aparece em Development Build).

- console open|close|toggle
  Mostra/fecha este console in‑game por comando.

Observações
- “host” indica comandos restritos ao servidor/host.
- Nomes de jogadores são “contains” case‑insensitive; para precisão use o SteamID.
- O console captura logs do Unity e marca: [ERR], [WRN], [EXC].
