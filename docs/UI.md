# The Shed — UI / Design System

Checklist for the design decisions. Bootstrap 5.3 already provides the spacing
scale, typography scale, breakpoints and components — we only define what is
**specific to The Shed**. Tick items off as we decide them.

Tokens live in one place: `TheShed.Client/wwwroot/css/app.css` (`:root`).

---

## 1. Color
- [x] Base palette — bg `#0f1115`, surface `#1a1d24`, border `#2a2e38`, text `#e6e6e6`, accent `#2dd4bf` (teal), danger `#ef4444`
- [ ] Accent states — hover / active / disabled
- [ ] Semantic colors — success / warning / info (or is danger enough?)
- [ ] Secondary / muted text — descriptions, placeholders
- [ ] Password strength — weak / medium / strong colors

## 2. Typography
- [ ] Font family — keep system stack or adopt one (Inter, etc.)
- [ ] Scale — override Bootstrap h1–h6 / body / small, or leave as-is
- [ ] Monospace font — for showing passwords / tokens

## 3. Spacing & layout
- [ ] Sidebar width — currently 15rem
- [ ] Content max width — full bleed or centered container
- [ ] Page padding — content margins
- [ ] Card gap — vault / entry listings

## 4. Shape & depth
- [ ] Border radius — buttons, cards, inputs (one value or a few)
- [ ] Shadows — use elevation or stay flat (dark usually flat)
- [ ] Border weight / color — `--border` exists

## 5. Components
- [ ] Buttons — primary / secondary / danger / ghost, sizes
- [ ] Inputs / forms — focus, error, labels
- [ ] Cards — vault card, entry row
- [ ] Empty states — "no vaults yet"
- [ ] Feedback — success / error toasts or alerts
- [ ] Loading — spinner / skeleton

## 6. Icons
- [ ] Icon set — adopt Bootstrap Icons (`bi-*` already partially referenced)?

---

## Decisions log
<!-- Record each decision here as it's made: token name, value, why. -->
