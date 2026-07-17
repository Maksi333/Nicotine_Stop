# Handoff: SnusStop — 4×2 Home-Screen Widget (two finalists: 5b & 5c)

## What this is
A focused handoff for the **4×2 Android home-screen widget** only. The rest of the app is already built — this covers just the two widget layouts to implement, frames badged **5b** and **5c** on the design canvas (section 5). Both come in light + dark; the widget follows the system theme.

## About the design files
`SnusStop UI.dc.html` is the design canvas (open in a browser). The widget frames are the 374×182 rounded cards inside the wallpaper swatches at ids `5b` and `5c`. These are HTML **design references** — recreate them as native Android app widgets (or the .NET MAUI equivalent), not copy verbatim. `android-frame.jsx` / `support.js` are canvas scaffolding, not app UI.

## Fidelity
**High-fidelity.** Colors, type, spacing, radii, and copy are final. Widget canvas: 4×2 cells, designed at 374×182 with 28px corner radius (adapt to launcher-provided corner radius where required). All values are live and must update via the platform's widget update mechanism.

## Shared elements (both widgets)
- **Brand mark, top-left: mini Puff** — NOT a logo square. 18px mint circle #BDF0D4, ink eyes/smile #17322B, cheeks #FFB1A6. In-app he bobs ±2px on a 3.2s ease-in-out loop; on the widget, use the animation only if the platform allows (AnimatedVectorDrawable etc.), otherwise static is fine. Implement as a swappable asset — final brand art replaces the primitive later.
- Type: **Nunito** 800/900; stat numbers use `tabular-nums`. Overline labels ~10px/900, 1px tracking.
- Derived values (same formulas as app): elapsed days `{{dd}}`, money saved, streak, milestone progress ("2 weeks free" bar at 86% in the mock), pouches avoided, nearest-goal % — all computed from quit datetime + pouches/day + can price ÷ pouches/can.
- Tap targets: whole widget opens the app (Home) unless noted.

## 5b — "Mini ring hero" (matches the in-app Home dashboard)
Horizontal split: left, a **118px progress ring** (10px stroke, round caps, track / fill = theme colors below) showing progress toward the next milestone, centered day count 34px/900 over "days free" 10px caption. Right, a stacked column (9px gaps) of three icon+stat rows: 💰 money saved (green, 17px/900), 🚫 pouches skipped (17px/900), 🏆 milestone countdown ("N days" 13px/900 + "until 2 weeks free" caption).
- Tap ring → app Home; tap rows → app Home.

## 5c — "Puff companion + SOS shortcut"
Three-zone row: left, **78px Puff** (mint circle, inset bottom shadow rgba(20,179,107,.22), eyes/smile/cheeks). Middle column (6px gaps): message "Day {N} — Puff's proud of you." 13.5px/900; two stat chips (💰 money — mint chip; 🔥 streak — coral-tint chip; 11px/900, 9px radius, 4×8px padding); 9px milestone progress bar; caption "🏆 N days until 2 weeks free" 9.5px/800. Right, a **64px coral SOS circle** — #FF6B5E, `0 4px 0` bottom-shadow (#D94F44 light / #B23A31 dark), "SOS" 14px/900 white.
- **Tap SOS → deep-link straight into the Craving SOS takeover** (this is the widget's reason to exist). Tap anywhere else → app Home.

## Theme tokens
Light: card #FDFEFD · ink #17322B · muted/captions #8AA398 · money green #0E8A50 · ring/bar fill #14B36B, track #E3EEE7 (5b) / #E9F3EC (bar, 5c) · mint chip #EAF7F0 · coral chip #FDEEEC, text #D94F44 · streak flame #E8590C.
Dark: card #1A2E27 · ink #ECF6F0 · muted #92AC9F · green #2BD98A (ring fill + money) · track/bar #23453A · mint chip #15382B · coral chip #3A2320, text #FF9D94 · flame #FF9D66.
Card shadow is launcher-side; mocks show 0 6px 20px ambient only for presentation.

## Which one ships?
Both were kept as finalists. If only one ships, note the trade-off: **5b** = pure glanceable progress (mirrors the in-app ring); **5c** = adds personality + the one-tap SOS deep link. If both ship, offer them as two widget options in the picker ("Progress ring" / "Puff + SOS").

## Files
- `SnusStop UI.dc.html` — canvas; widget frames at ids 5b / 5c (light + dark stacked on wallpaper swatches)
- `android-frame.jsx`, `support.js` — canvas scaffolding (not app UI)
