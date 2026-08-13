# The Shed — Visual Refactor Plan

## Context

The "Workshop" theme (amber/copper accent, warm dark surfaces, `--font-display`
for headings — see `docs/UI.md`) is already applied, but the app still reads
as a generic "AI + Bootstrap" admin panel. Asked what specifically was wrong
— not color, not typography, not icons — the answer was:

- **Default components**: buttons, cards, lists, badges and the modal are
  literally Bootstrap's `.btn`, `.card`, `.list-group-item`, `.btn-group`,
  `.badge`, `.modal` with the `--bs-*` variables recolored. They read as
  stock components, not designed ones.
- **Generic structure**: every page is sidebar + `.card` + `.list-group` +
  `.btn-group` — the default skeleton of any generated admin panel.

Confirmed by reading every page and component: the pattern is consistent
across all 8 surfaces (`Vaults`, `VaultDetail`/`EntriesPanel`/`EntryRow`,
`NotesPanel`, `MembersPanel`, `Health`, `Trash`, `ModalHost`) except
`Login`/`Register`, which already use their own `.auth-card`. Worst case is
`EntryRow`: up to 6 identical `outline-secondary` buttons in a row
(Favorite/Reveal/Copy/History/Files/Edit/Delete) — the most "admin panel"
element in the app, and it doesn't even have a hover state (never used
`.list-group-item-action`).

Scope explicitly requested: go beyond a component reskin — also touch page
structure. The "Workshop" concept itself (identity tied to the app name,
professional, no literal theming like pegboard/wood grain — already ruled
out earlier) is not in question; what's missing is that today it only lives
in color.

## Design direction

**One repeated motif instead of six separate ideas**: a 3px accent bar
(left edge on vertical elements, top edge on the modal) that lights up in
`--accent` (or `--danger` for destructive actions) — replacing the "boxed
element with uniform border + solid hover fill" pattern Bootstrap uses
everywhere. This is what ties nav, rows, tiles, forms and the modal to the
same language, instead of each having its own treatment.

New classes in `components.css` (sketch — exact values get tuned in
Increment 1; see Increment 0 for the file split these land in):

```css
.row-item { border-bottom: 1px solid var(--border); border-left: 3px solid transparent; padding: .75rem 1rem .75rem .75rem; }
.row-item:hover, .row-item:focus-within { border-left-color: var(--accent); background: var(--surface-2); }

.icon-btn { width: 2rem; height: 2rem; border: none; background: transparent; border-radius: var(--radius); color: var(--text-muted); }
.icon-btn:hover { background: var(--surface-2); color: var(--text); }

.overflow-menu-panel { position: absolute; right: 0; top: calc(100% + .25rem); background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius); }
.overflow-menu-panel .danger { color: var(--danger); border-top: 1px solid var(--border); }

.chip { font-family: var(--font-mono); text-transform: uppercase; font-size: .7rem; letter-spacing: .03em; border: 1px solid var(--border); border-radius: 2px; color: var(--text-muted); }

.job-form { border-left: 3px solid var(--accent); background: var(--surface); padding: 1rem 1.25rem; }
.job-form-title { font-family: var(--font-display); text-transform: uppercase; letter-spacing: .05em; font-size: .95rem; color: var(--text-muted); }

.vault-tile { border-left: 3px solid var(--border); background: var(--surface); padding: 1rem; }
.vault-tile.role-owner { border-left-color: var(--accent); }

.confirm-panel { border-top: 3px solid var(--accent); }
.confirm-panel.danger { border-top-color: var(--danger); }

.filter-tab { border: none; border-bottom: 2px solid transparent; color: var(--text-muted); }
.filter-tab.active { color: var(--accent); border-bottom-color: var(--accent); }
```

All of these still lean on Bootstrap's utilities (`d-flex`, `.row`/`.col-*`,
spacing) — that part isn't the problem and stays untouched. What changes is
that the visually distinctive pieces stop being plain `.card` /
`.list-group-item` / `.btn-group` / `.badge` / `.modal-content`.

**`EntryRow` — the biggest change.** Instead of a `.btn-group` of 6 equal
buttons: 2-3 ghost `.icon-btn`s always visible for frequent actions
(Favorite, Reveal/Hide, Copy), and the rest (History, Files, Edit, Delete)
move into an overflow menu (`bi-three-dots-vertical`) built with a native
`<details>/<summary>` — the same zero-JS pattern already used for "Manage
tags" in `EntriesPanel`, not a new technique. Delete sits below a
`border-top` in `--danger` inside the menu, so it's not next to Edit by
accident.

**Chips** (`.badge-chip` → `.chip`): stop inheriting from `.badge`.
Monospace + uppercase + `border-radius: 2px` (sharper than the general
`--radius: 4px`) — reads as a label-maker tag through typography, not
texture.

**Inline forms** (`.card` for "New entry"/"New note"/adding a member):
become `.job-form` — no boxed-on-all-sides container, left accent bar,
`--font-display` uppercase title instead of `.card-title`. Nothing changes
inside: `EditForm`/`InputText`/`DataAnnotationsValidator` stay as-is.

**Tag filter** in `EntriesPanel` (currently a row of toggle `.btn.btn-sm`):
becomes `.filter-tab` — text + underline on the active one, not a row of
solid stacked buttons.

**Sidebar/nav — stays a column**, not moved to an icon rail or repositioned.
The responsive mechanics (`.sidebar`, `.top-row`, `.nav-backdrop`, the
641px breakpoint) are already tuned from previous increments, and the
reported problem isn't placement — it's that the active/hover state is the
generic solid-fill block of any admin sidebar. Only the internal treatment
changes: left accent bar (same motif as everywhere else) instead of a solid
fill, labels in `--font-display` uppercase to match h1/h2 and the
`navbar-brand`.

## Out of scope

- Backend, business logic, Argon2/AES-256/JWT.
- `EditForm`, `InputText`, `InputTextArea`, `InputCheckbox`,
  `DataAnnotationsValidator`, `ValidationMessage`.
- Bootstrap's grid and utility classes.
- `Login.razor`/`Register.razor` (already have their own `.auth-card`).
- The sidebar's responsive positioning/mechanics.
- No new dependencies, no CSS build step, no generic reusable Blazor
  components (`<Card>`, `<Chip>`) — the overflow menu repeats 4-5 times but
  is `<details>` + 3 classes, not enough to justify a parameterized
  abstraction.

## Increments

Same granularity as the `VaultDetail` split (5 increments) and the
`window.confirm` replacement (2 increments): stop after each one for
review/commit.

- [x] ~~**0 — Split `app.css`**~~ — `app.css` is 411 lines and about to grow by
      7-8 new component classes; split before adding them instead of after.
      Plain file split, no build tool (none exists in this project):
      `theme.css` (`:root` tokens + the `--bs-*` remap), `layout.css`
      (page shell: sidebar, top-row, nav-backdrop, breakpoints, mobile
      stacking rules), `components.css` (restated `.btn-*`/`.form-control`/
      `.modal` overrides, plus every new class from this plan going
      forward). `app.css` stays as the thin base: html/body reset,
      headings, loading spinner, `#blazor-error-ui`. Reference all four
      via `<link>` tags in `index.html`, in that order (cascade matters:
      theme → layout → components → app overrides). Pure move, no visual
      changes — verify by diffing rendered output, not by eye.
- [x] ~~**1 — Foundations + `EntryRow`**~~ (most visible case, validates the
      direction before it spreads) — `components.css` (new classes),
      `EntryRow.razor` (restructure actions into icon-btn + overflow menu),
      `docs/UI.md` (new "Component vocabulary" section + decisions log).
      **Stop here before continuing** — this is the piece that changes the
      most and decides whether the rest follows the same pattern.
- [ ] **2 — `EntriesPanel.razor`** — wrapping list, tag filter to
      `.filter-tab`, create/edit form to `.job-form`.
- [ ] **3 — `Vaults.razor`** — tiles to `.vault-tile`, create form to
      `.job-form`, split to code-behind (`Vaults.razor.cs` — already on the
      pending list in `docs/TODO.md`).
- [ ] **4 — `NotesPanel.razor` + `MembersPanel.razor`** — rows to
      `.row-item`, actions to `.icon-btn`, forms (note and add-member) to
      `.job-form`; the `.card`/`.card-body` currently wrapping all of
      `MembersPanel` also gets uncapped (title + loose text, no box).
- [ ] **5 — `EntryHistoryPanel.razor` + `EntryAttachmentsPanel.razor`** —
      align buttons to `.icon-btn` (already don't use `.list-group-item`,
      low risk).
- [ ] **6 — `Health.razor` + `Trash.razor`** — same `.row-item`/`.chip`/
      `.icon-btn` pattern (Trash: only 2 actions, stay as direct
      icon-buttons, no overflow menu). Split to code-behind (both pending
      in TODO).
- [ ] **7 — `ModalHost.razor`** — `.confirm-panel` with a top stripe per
      variant (accent/danger). Low risk, high impact — used across the
      whole app.
- [ ] **8 — `NavMenu.razor` + `MainLayout.razor`** — accent bar + labels in
      `--font-display` uppercase; split both to code-behind (pending in
      TODO).
- [ ] **9 — Final sweep** (optional) — remove dead rules in `app.css`
      (`.badge-chip`, `.vault-card`, mobile overrides for
      `.list-group-item .btn-group` if no longer applicable), polish
      `docs/UI.md` (§5 checklist, and fix the "no webfont" line, which went
      stale once Big Shoulders Display/Inter were added).

## Verification

No automated UI tests in this project — each increment is verified by hand
in the browser (clean build + exercise the touched surface: row hover/focus,
overflow menu opens/closes and its actions work, forms still validate and
save, the modal still confirms/cancels) before moving to the next one.
Repeat at mobile width (<640px) since almost everything touched has
stacking rules tuned in Increment 14 — confirm nothing breaks.
