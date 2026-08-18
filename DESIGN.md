# Design — Nestglow

Locked design system inspired by Hallmark (`hallmark-main`).
Genre · atmospheric · Theme · Lumen Night Foundry + Garden honey chord.

## System
- Genre · atmospheric
- Theme · catalog: Lumen (drop: Night Foundry) · garden honey as secondary warmth
- Axes · dark cool-violet paper / classical-serif lowercase display / warm-brass accent
- Anti-patterns avoided · purple-gradient hero · pure black · Inter-only · glassmorphism · gradient text

## Tokens (Unity: `NestglowTheme.cs`)
```
paper       #05080D   cool-violet night
paper-2     #0C0F16
paper-3     #161B23
ink         #F0F2F6
ink-2       #C0C4CB
muted       #7B808A
accent      #F5B352   molten brass
accent-2    #EB6B66   coral chord
honey       #E6B847   garden honey
glow        brass @ 42%
```

## Typography voice
- Display · lowercase **nestglow** (serif energy; runtime TextMesh until custom fonts land)
- Labels · UPPERCASE micro labels: ЭНЕРГИЯ · ЦЕЛЬ · УРОВЕНЬ
- Body/hints · quiet ink-2, not pure white
- No gradient-filled headlines

## UI voice
- Chips · floating pills on paper-2 (Hallmark N5 energy)
- Primary CTA · full pill, brass fill / brass-tinted plate
- Board · elevated paper-3 wells, hairline rule rim — not thick gold chrome
- Atmosphere · ≤2 warm blooms + moon apparatus; vignette soft

## Motion
- ease-out cubic-bezier(0.16, 1, 0.3, 1)
- Soft pulse on light sources (~4s Lumen rhythm scaled for gameplay)
- Merge: short emit burst, not bounce spam

## Source
- `hallmark-main/skills/hallmark/references/genres/atmospheric.md`
- `hallmark-main/skills/hallmark/references/themes/lumen.md`
- `hallmark-main/site/examples/lumen-01/tokens.css`
- `hallmark-main/site/examples/garden-01/tokens.css` (honey / botanical warmth)
