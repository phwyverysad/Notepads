# Notepads Thai Localization, Toolbar & Web Installer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Provide full Thai localization (`th-TH`) for Notepads, relocate and implement the menu bar (`ไฟล์`, `แก้ไข`, `มุมมอง`) and Markdown formatting toolbar (`หัวเรื่อง`, `รายการ`, `B`, `I`, `S`, `🔗`, `ตาราง`, `A`, `👁`, `⚙`) right below the tab bar with responsive overflow support, build a smart Web Installer & Launcher `.exe` that launches instantly or downloads/installs silently with file argument forwarding, write comprehensive tests to verify all functionality, and sync to `https://github.com/phwyverysad/Notepads.git`.

**Architecture:** 
- UWP XAML UI refactor in `NotepadsMainPage.xaml` and `NotepadsMainPage.xaml.cs`: hide `MainMenuButton` in `SetsStartHeader`, introduce a custom `TopToolBar` grid directly under `SetsView` containing the left menu bar, center markdown actions with responsive overflow, and right preview/settings buttons.
- Localization: Create `src/Notepads/Strings/th-TH/` containing `Manifest.resw`, `Resources.resw`, and `Settings.resw` fully localized to Thai, and set default fallback culture to `th-TH`.
- Markdown Actions: Implement smart table insertion (5x5 grid picker generating formatted Markdown tables) and smart clear formatting (button `A` stripping `#`, `*`, `_`, `~`, `>` markers).
- Web Installer: A standalone lightweight executable project `src/Notepads.WebInstaller/` producing `NotepadsInstaller.exe` (< 1-2 MB) with file argument forwarding (`NotepadsInstaller.exe filename.txt`), silent background download & extraction into `%LocalAppData%\Programs\Notepads`, and instant launch.
- Testing & Verification: Automated unit tests and UI validation for string coverage, toolbar commands, markdown formatting operations, and installer lifecycle.
- Git Remote: Configure remote to `https://github.com/phwyverysad/Notepads.git` and commit all enhancements.

**Tech Stack:** C# 10 / .NET Core UWP, WinUI / XAML, MSBuild, Windows SDK 10.0.26100.0, .NET Framework 4.8 / Windows Desktop Bootstrapper.

**Spec:** [2026-09-24-thai-toolbar-webinstaller-design.md](file:///c:/Users/woran/Documents/antigravity/delightful-kepler/docs/superpowers/specs/2026-09-24-thai-toolbar-webinstaller-design.md)

---

## Global Constraints
- Target Platform Version: `10.0.26100.0` (matching local Windows SDK).
- All menu labels and accelerator texts must match the 6 reference images exactly.
- Web installer must be a single executable with zero click install experience and argument forwarding.
- All formatting tools must work on selection or cursor position.
- Tests must pass before delivering.
- Repository target: `https://github.com/phwyverysad/Notepads.git`.

---

## Task Decomposition

### Task 1: Complete Thai Localization (`th-TH`)
- [ ] **Step 1: Create `src/Notepads/Strings/th-TH/Manifest.resw`** with Thai translations.
- [ ] **Step 2: Create `src/Notepads/Strings/th-TH/Resources.resw`** with complete Thai translations for all editor strings, menus, shortcuts, and dialogs.
- [ ] **Step 3: Create `src/Notepads/Strings/th-TH/Settings.resw`** with Thai translations for all settings pages.
- [ ] **Step 4: Update `Notepads.csproj`** to include `th-TH\*.resw` and set default language to Thai.
- [ ] **Step 5: Verify build with MSBuild** ensuring PRI generation succeeds.
- [ ] **Step 6: Commit:** `git commit -m "feat(i18n): add comprehensive Thai (th-TH) localization"`

---

### Task 2: Red-Bordered Toolbar - Menu Bar Layout (`ไฟล์`, `แก้ไข`, `มุมมอง`)
- [ ] **Step 1: Hide `MainMenuButton`** in `SetsView.SetsStartHeader`.
- [ ] **Step 2: Insert Toolbar Grid Container** directly beneath `SetsView`.
- [ ] **Step 3: Implement MenuBar Controls** for `ไฟล์`, `แก้ไข`, `มุมมอง` matching all items in reference screenshots.
- [ ] **Step 4: Wire Command Handlers in Code-Behind** (`NotepadsMainPage.xaml.cs`).
- [ ] **Step 5: Build and Verify** clean compilation.
- [ ] **Step 6: Commit:** `git commit -m "feat(ui): add dedicated menu bar with File, Edit, and View below tab bar"`

---

### Task 3: Red-Bordered Toolbar - Markdown Formatting Tools (`หัวเรื่อง`, `รายการ`, `B`, `I`, `S`, `🔗`, `ตาราง`, `A`, `👁`)
- [ ] **Step 1: Implement `MarkdownToolbarControl`** with:
  - `หัวเรื่อง` DropdownButton (Title, Subtitle, Heading 1-4, Body)
  - `รายการ` DropdownButton (Bullet, Numbered, Indent, Unindent)
  - `B` Bold, `I` Italic, `S` Strikethrough, `🔗` Link
  - `ตาราง` DropdownButton with interactive 5x5 Grid Picker + Insert Table
  - `A` Smart Clear Formatting (strips markdown markers)
  - `👁` Toggle Markdown Preview
  - `⚙` Settings Button
  - Responsive Overflow (`···`) behavior for narrow windows.
- [ ] **Step 2: Connect Toolbar with Active `TextEditor`** to apply formatting to selection or insert templates.
- [ ] **Step 3: Build and Verify** formatting actions.
- [ ] **Step 4: Commit:** `git commit -m "feat(ui): implement markdown formatting toolbar with heading, list, table, and inline tools"`

---

### Task 4: Lightweight Web Installer & Launcher EXE (`NotepadsInstaller.exe`)
- [ ] **Step 1: Create `NotepadsInstaller` project** (.NET Framework 4.8 / Windows Desktop Native, < 1-2 MB).
- [ ] **Step 2: Implement Instant Launch Check** with argument forwarding (`args`).
- [ ] **Step 3: Implement Silent Web Installer Logic** (downloads from GitHub Releases, extracts to `%LocalAppData%\Programs\Notepads`, creates shortcut, launches immediately).
- [ ] **Step 4: Build `NotepadsInstaller.exe`** and verify size < 2 MB.
- [ ] **Step 5: Commit:** `git commit -m "feat(installer): create lightweight silent web installer and portable launcher"`

---

### Task 5: Comprehensive Automated Testing & Verification
- [ ] **Step 1: Write Unit & Integration Tests** for:
  - String resource verification (verifying all keys exist in `th-TH` matching `en-US`).
  - Markdown formatting helpers (Bold, Italic, Strikethrough, Headings, Lists, Table generation, Clear Formatting).
  - Installer file launch and path detection logic.
- [ ] **Step 2: Run all tests** and ensure 100% pass rate.
- [ ] **Step 3: Commit:** `git commit -m "test: add comprehensive test suite for Thai resources, markdown formatting, and installer"`

---

### Task 6: Packaging, GitHub Remote Configuration & Push
- [ ] **Step 1: Package Release ZIP** (`dist/Notepads-x64.zip`) and copy `NotepadsInstaller.exe` to `dist/`.
- [ ] **Step 2: Configure git remote** to `https://github.com/phwyverysad/Notepads.git`.
- [ ] **Step 3: Push changes** to the remote repository.
- [ ] **Step 4: Document Walkthrough** with test results and user instructions.
