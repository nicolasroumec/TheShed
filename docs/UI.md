# The Shed — UI / Design System

Checklist for the design decisions. Bootstrap 5.3 already provides the spacing
scale, typography scale, breakpoints and components — we only define what is
**specific to The Shed**. Tick items off as we decide them.

Tokens live in one place: `TheShed.Client/wwwroot/css/theme.css` (`:root`).
Page shell (sidebar/top bar) is in `layout.css`; reusable component classes are in
`components.css`; `app.css` is just the document base (reset, headings, Blazor's own
bootstrap/error UI).

---

## Concept — "Workshop"

The Shed = your workshop out back. Solid, crafted, warm — not a cold corporate
vault. Each vault is a labeled drawer on a workbench; secrets read like
workshop labels (monospace, label-maker tape). Visible joints (strong borders),
flat surfaces, no gradients or glass. Identity comes from **treatment**
(stamped uppercase headings, monospace secrets, rust-red danger, a repeated
accent-bar motif on every component) rather than literal theming — no
pegboard textures, no wood grain.

---

## 1. Color (Workshop palette)
- [x] Surfaces — bg `#16130f`, surface `#211c16`, surface-2 `#2c251c` (hover/raised), border `#3a3128`
- [x] Text — text `#ece6db` (bone), text-muted `#a89c88` (worn label)
- [x] Accent — `#e0922f` (amber/copper), hover `#f0a949`, active `#c47a1f`
- [x] Semantic — success `#6f9e4a`, warning `#d9a441`, danger `#c4503a` (rust). No `info`: accent covers it.
- [x] Password strength — alias the semantics: weak=danger, medium=warning, strong=success

## 2. Typography
- [x] Font family — "Big Shoulders Display" (headings) + "Inter" (body), via Bunny Fonts.
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
- [x] Empty states — workshop voice ("The shed's empty — hang your first vault.")
- [x] Feedback — inline `alert alert-danger` in the form or section that failed, no toasts;
      success uses the same shape with `alert-success` when a screen first needs one. The
      `--bs-*-bg-subtle` / `-text-emphasis` / `-border-subtle` tokens back both.
- [x] Loading — one `<Loading />` component (Bootstrap `.spinner-border` + label). No skeletons.

Bootstrap's own component classes (`.card`, `.list-group-item`, `.btn-group`,
`.badge`, `.modal-content`) read as generic once merely recolored — a
"reskinned admin panel" look. Replaced everywhere by a small set of custom
classes in `components.css`, built around **one repeated motif**: a 3px
accent bar (left edge on vertical elements, top edge on the modal) instead
of a uniform border + solid hover fill.

- [x] `.row-item` — replaces `.list-group-item`. Flush against the page, left
      accent bar + `surface-2` fill on hover/focus.
- [x] `.icon-btn` (+ `.danger` modifier) — ghost icon button for the frequent,
      always-visible row actions (favorite/reveal/copy/restore/remove),
      instead of a `.btn-group` of identical outline buttons.
- [x] `.overflow-menu` (native `<details>`) + `.overflow-menu-panel` — the
      rarer row actions (history/files/edit/delete) live behind a menu
      instead of widening the row; destructive actions get a `border-top` +
      `--danger`.
- [x] `.chip` (+ `.strength-*`/`.reused` modifiers) — replaces `.badge-chip`
      and `.badge`. Monospace, uppercase, `border-radius: 2px` (sharper than
      the general `--radius: 4px`) — a printed label, not a rounded UI badge.
- [x] `.job-form` — replaces `.card` for inline create/edit forms (entries,
      notes, vaults, members). Left accent bar, `--font-display` uppercase
      title instead of `.card-title`.
- [x] `.vault-tile` — replaces the vault listing's `.card`. Owner's own
      vaults keep the accent border always; others light up on hover.
- [x] `.confirm-panel` — the confirm modal's `.modal-content`, same motif
      horizontally (top stripe, accent or danger by `ConfirmVariant`).
- [x] `.filter-tab` — the entries tag filter; underline tab strip instead of
      a row of solid toggle buttons.

## 6. Icons
- [x] Bootstrap Icons (`bi-*`) — utilitarian line icons, fits the workshop look. Loaded from
      the jsDelivr CDN in `index.html`; vendor it into `wwwroot/lib/` if offline use matters.

---

## Decisions log

- **Direction: Workshop** — strongest identity, matches the name; not another blue PM.
- **No webfont** — character from treatment (stamped headings, mono secrets); nothing to load. Upgrade to a display font later if wanted.
- **Rust danger `#c4503a`** instead of generic red — oxidized iron, fits the workshop.
- **Depth by color, not shadow** — `bg → surface → surface-2`; flat and solid.
- **Single accent (amber)** — only actionable things get color.
- **Bootstrap's own theme vars point at our tokens** — its dark theme ships cold greys
  (`#212529` / `#343a40` / `#6c757d`), so `:root` in `theme.css` remaps `--bs-body-bg`,
  `--bs-border-color` and the semantic colors. Stock components follow the palette without
  per-page CSS. The `.btn-*` variants are the exception: Bootstrap compiles literal hex into
  them, so the ones we use are restated by hand.
- **Webfont added after all** — Big Shoulders Display (headings) + Inter (body), via Bunny
  Fonts, loaded in `index.html`. Supersedes the original "no webfont" decision above; kept
  for the record since it explains why treatment alone was the starting point.
- **One repeated accent-bar motif, not per-component tweaks** — replaced Bootstrap's
  component classes surface by surface (§5) with a single recurring shape (3px accent bar)
  so the pieces read as one family instead of six unrelated redesigns.

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
