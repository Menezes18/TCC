# Manual Release Gate

Record build identifier, commit, date, tester, machines, logs/screenshots, and pass/fail notes for every row. These checks intentionally remain manual because localhost KCP cannot reproduce Steam callbacks, distinct accounts, overlays, NAT/WAN behavior, or human-visible presentation quality.

## Steam multiplayer

- [ ] Host creates a Steam lobby, remains admitted to its own lobby, and sees the correct party-owner state.
- [ ] Three remote Steam accounts join (4 total); repeat with five remotes (6 total) as a scalability rehearsal.
- [ ] Invite and overlay join paths work from each supported entry point.
- [ ] A client leaves during lobby, briefing, active minigame, results, and scene transition; remaining peers stay synchronized.
- [ ] A disconnected client reconnects where the design permits it; duplicate identity/admission attempts are rejected cleanly.
- [ ] Host disconnect behavior matches the current product decision (explicit session end or documented migration behavior).
- [ ] Run at least one session across different networks, including one higher-latency connection, and retain client plus host logs.

## Gameplay and presentation

- [ ] All four players complete Ready/briefing and enter each enabled minigame scene.
- [ ] Death animation is fully visible to owner and observers before respawn or spectate; model, controller, camera, and HUD restore correctly.
- [ ] Permanent elimination enters spectate and cycles valid living targets; leaving/rejoining does not retain stale spectator state.
- [ ] Voting options/counts/winner agree on all peers; results totals and customization agree on all peers.
- [ ] Host and remote clients can use representative interactions, including ball push, without ownership or disconnect errors.
- [ ] Complete a full rotation through final victory and return to menu/lobby.

## Entry-level target hardware

- [ ] Run the portable Performance Audit package on at least one entry-level gaming laptop/PC at `Very Low` or `Performant`, 1080p, D3D11.
- [ ] Run the same scene/settings/frame-count combination twice before treating a delta as a regression.
- [ ] Inspect representative gameplay (not menus) for frame pacing, GPU time, memory growth, thermal throttling, and visible quality defects.
- [ ] Capture Frame Debugger evidence for expensive scenes when draw-call, batching, overdraw, light, shadow, or shader changes are proposed.

Automated success is necessary but does not replace this checklist.
