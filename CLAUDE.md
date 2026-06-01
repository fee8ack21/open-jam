# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Open Jam is a Taiwanese digital marketplace for creators. This repository currently contains the `Auth` service — a split-screen authentication UI (login, register, forgot password, reset password) with Traditional Chinese copy.

## Commands

All commands should be run from `src/Auth/`:

```bash
# Run dev server (http://localhost:5169 or https://localhost:7280)
dotnet run

# Build
dotnet build

# Publish (release)
dotnet publish -c Release
```

Docker:
```bash
# Build image (run from src/ directory)
docker build -f Auth/Dockerfile -t open-jam-auth .
```

There are no automated tests in this project.

## Architecture

### Backend (ASP.NET Core 8 MVC)

`Program.cs` is minimal — just MVC + static files, no database or auth middleware yet. `HomeController` serves static Razor views for each route (`/login`, `/register`, `/error`). The routes map 1:1 to the JS screens below.

### Frontend (jQuery + vanilla JS modules)

The UI is entirely client-side rendered. Razor views are shell HTML files that load scripts in order and call `Auth.start('<screen>')`. No build step — scripts are served as-is from `wwwroot/js/`.

**Script load order (all pages):**
1. `icons.js` — exposes `window.icon(name, opts)` (SVG string builder) and `window.esc()` (HTML escaper) and `window.brandMark(size)`
2. `fields.js` — exposes form field builders: `window.fieldHTML`, `window.passwordFieldHTML`, `window.checkboxHTML`, `window.strengthHTML`, `window.scorePassword`, `window.isEmail`
3. `BrandPanel.js` — exposes `window.brandPanelHTML(headline, sub)` for the left decorative panel
4. `LegalModal.js` — exposes `window.legalModalHTML(which)` and `window.LEGAL_DOCS`
5. `auth-core.js` — the main controller: state machine, screen renderers, event delegation, `Auth.start(screen)`

`AuthApp.js` is an alternate single-page version of the controller (all screens on one URL, `go()` instead of `window.location.href`). It is not currently wired up in the Razor views.

### Rendering pattern

All UI is built by concatenating HTML strings. There is no templating framework or virtual DOM. The single `render()` call rebuilds `#root` from scratch on every state change. Event handling uses jQuery document-level delegation with `data-*` attributes (`data-go`, `data-submit`, `data-input`, `data-checkbox`, `data-pwtoggle`, `data-legal-open/close/ack`, `data-resend`, `data-tweaks-close`, `data-font`, `data-accent`).

### State

`auth-core.js` holds a plain `state` object in its IIFE closure:
- `screen` — current screen key (maps to a screen renderer function in `SCREENS`)
- `form` / `errors` / `show` — form field values, validation errors, password visibility
- `loading` — disables the submit button and shows a spinner
- `font` / `accent` — user display preferences, persisted to `localStorage` as `ojAuthPrefs`
- `legal` — which legal doc modal is open (`'terms'` | `'privacy'` | `null`)
- `read` — tracks whether the user has opened each legal doc
- `left` / `justSent` — countdown timer for resend-email UI

Email is passed between pages via `sessionStorage` key `ojAuthEmail`.

### CSS design system

`site.css` defines CSS custom properties for the color palette (`--c-violet`, `--c-pink`, `--c-orange`, `--c-lime`, `--c-cyan`, `--c-yellow`), typography (`--june-display`, `--june-font`, `--june-mono`), and spacing/radius tokens. The neobrutalist visual style uses hard offsets (`box-shadow: Xpx Xpx 0 var(--border-strong)`) with hover lift effects. Font family switches via `.font-bricolage` / `.font-unbounded` class on `.auth-shell`.

### Tweaks panel

A developer/preview panel (bottom-right) for switching accent gradient and display font. Activated via `postMessage` with `{ type: '__activate_edit_mode' }` from a parent frame. The panel posts `__edit_mode_available` on load and `__edit_mode_dismissed` on close.
