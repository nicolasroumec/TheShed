# The Shed — UI / Design System

Checklist for the design decisions. Bootstrap 5.3 already provides the spacing
scale, typography scale, breakpoints and components — we only define what is
**specific to The Shed**. Tick items off as we decide them.

Tokens live in one place: `TheShed.Client/wwwroot/css/app.css` (`:root`).

---

## Concept — "Workshop"

The Shed = your workshop out back. Solid, crafted, warm — not a cold corporate
vault. Each vault is a labeled drawer on a workbench; secrets read like
workshop labels (monospace, label-maker tape). Visible joints (strong borders),
flat surfaces, no gradients or glass. Identity comes from **treatment** (stamped
uppercase headings, monospace secrets, rust-red danger), not a custom webfont —
so there is nothing extra to load.

---

## 1. Color (Workshop palette)
- [x] Surfaces — bg `#16130f`, surface `#211c16`, surface-2 `#2c251c` (hover/raised), border `#3a3128`
- [x] Text — text `#ece6db` (bone), text-muted `#a89c88` (worn label)
- [x] Accent — `#e0922f` (amber/copper), hover `#f0a949`, active `#c47a1f`
- [x] Semantic — success `#6f9e4a`, warning `#d9a441`, danger `#c4503a` (rust). No `info`: accent covers it.
- [x] Password strength — alias the semantics: weak=danger, medium=warning, strong=success

## 2. Typography
- [x] Font family — system sans stack (no webfont). Identity via treatment, not a custom face.
- [x] Headings — "stamped" look: uppercase + letter-spacing on h1/h2, heavier weight
- [x] Monospace — system mono stack (`ui-monospace, "Cascadia Code", Consolas`) for passwords/secrets
- [ ] Scale — keep Bootstrap's; only revisit if something looks off

## 3. Spacing & layout
- [x] Sidebar width — 15rem
- [x] Content max width — centered, ~72rem
- [x] Page padding — 1.5rem
- [x] Card gap — 1rem (Bootstrap `g-3`)

## 4. Shape & depth
- [x] Border radius — `--radius: 4px` (sturdy, square-ish; not pill, not zero)
- [x] Shadows — none. Depth via surface → surface-2 + borders.
- [x] Border weight — 1px, strong color (`--border`); accent border on hover/focus

## 5. Components
- [x] Buttons — primary = solid amber + dark text; secondary = bordered ghost; danger = rust. Square-ish.
- [x] Inputs — surface bg, 1px border, focus = amber border + subtle glow
- [x] Cards — vault card = labeled drawer; hover → surface-2 + amber border
- [ ] Empty states — workshop voice (e.g. "The shed's empty — hang your first vault.")
- [ ] Feedback — success / error toasts or alerts (decide when first needed)
- [ ] Loading — spinner / skeleton (decide when first needed)

## 6. Icons
- [x] Bootstrap Icons (`bi-*`) — utilitarian line icons, fits the workshop look

---

## Decisions log

- **Direction: Workshop** — strongest identity, matches the name; not another blue PM.
- **No webfont** — character from treatment (stamped headings, mono secrets); nothing to load. Upgrade to a display font later if wanted.
- **Rust danger `#c4503a`** instead of generic red — oxidized iron, fits the workshop.
- **Depth by color, not shadow** — `bg → surface → surface-2`; flat and solid.
- **Single accent (amber)** — only actionable things get color.

## Token block (target `:root`)

```css
:root {
  /* Surfaces */
  --bg:        #16130f;
  --surface:   #211c16;
  --surface-2: #2c251c;
  --border:    #3a3128;

  /* Text */
  --text:       #ece6db;
  --text-muted: #a89c88;

  /* Accent (amber/copper) */
  --accent:        #e0922f;
  --accent-hover:  #f0a949;
  --accent-active: #c47a1f;

  /* Semantic */
  --success: #6f9e4a;
  --warning: #d9a441;
  --danger:  #c4503a;

  /* Password strength (aliases) */
  --pw-weak:   var(--danger);
  --pw-medium: var(--warning);
  --pw-strong: var(--success);

  /* Shape */
  --radius: 4px;

  /* Monospace for secrets */
  --font-mono: ui-monospace, "Cascadia Code", Consolas, monospace;
}
```
