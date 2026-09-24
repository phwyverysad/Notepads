# Notepads Thai Localization, Toolbar & Web Installer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Provide full Thai localization (`th-TH`) for Notepads, relocate and implement the menu bar (`ไฟล์`, `แก้ไข`, `มุมมอง`) and Markdown formatting toolbar (`หัวเรื่อง`, `รายการ`, `B`, `I`, `S`, `🔗`, `ตาราง`, `A`, `👁`, `⚙`) right below the tab bar matching the user's reference screenshots, compile a lightweight Web Installer & Launcher `.exe` that launches instantly or downloads/installs silently, and sync to `https://github.com/phwyverysad/Notepads.git`.

**Architecture:** 
- UWP XAML UI refactor in `NotepadsMainPage.xaml` and `NotepadsMainPage.xaml.cs`: hide `MainMenuButton` in `SetsStartHeader`, introduce a custom `TopToolBar` grid directly under `SetsView` containing the left menu bar, center markdown actions, and right preview/settings buttons.
- Localization: Create `src/Notepads/Strings/th-TH/` containing `Manifest.resw`, `Resources.resw`, and `Settings.resw` fully localized to Thai, and set default fallback culture to `th-TH`.
- Web Installer: A standalone lightweight executable project `src/Notepads.WebInstaller/` producing `NotepadsInstaller.exe` (< 1-2 MB) that checks for existing Notepads installation; if installed, launches it immediately; if not, silently downloads and installs the package from GitHub Releases and launches it.
- Git Remote: Configure remote to `https://github.com/phwyverysad/Notepads.git` and commit all enhancements.

**Tech Stack:** C# 10 / .NET Core UWP, WinUI / XAML, MSBuild, Windows SDK 10.0.26100.0, .NET Framework 4.8 / Windows Desktop Bootstrapper.

**Spec:** [2026-09-24-thai-toolbar-webinstaller-design.md](file:///c:/Users/woran/Documents/antigravity/delightful-kepler/docs/superpowers/specs/2026-09-24-thai-toolbar-webinstaller-design.md)

---

## Global Constraints
- Target Platform Version: `10.0.26100.0` (matching local Windows SDK).
- All menu labels and accelerator texts must match the 6 reference images exactly:
  - `ปุ่ม ไฟล์.png` (แท็บใหม่, หน้าต่างใหม่, แท็บ Markdown ใหม่, เปิด, ล่าสุด, บันทึก, บันทึกเป็น, บันทึกทั้งหมด, การตั้งค่าหน้ากระดาษ, พิมพ์, ปิดแท็บ, ปิดหน้าต่าง, ออก)
  - `ปุ่ม แก้ไข.png` (เลิกทำ, ตัด, คัดลอก, วาง, ลบ, ล้างการจัดรูปแบบ, ค้นหาด้วย Bing, ค้นหา, ค้นหาถัดไป, ค้นหาก่อนหน้านี้, แทนที่, ไปที่, เลือกทั้งหมด, เวลา/วันที่, แบบอักษร)
  - `ปุ่ม มุมมอง.png` (ย่อ/ขยาย, แถบสถานะ, การตัดคำ, Markdown)
  - `ปุ่ม หัวเรื่อง.png` (ชื่อ, คำบรรยาย, ส่วนหัว, หัวเรื่องย่อย, ส่วน, ส่วนย่อย, เนื้อความ)
  - `ปุ่ม รายการ.png` (รายการสัญลักษณ์แสดงหัวข้อย่อย, รายการลำดับเลข, เพิ่มการเยื้อง, ลดการเยื้อง)
  - `ปุ่มตาราง.png` (5x5 grid picker, แทรกตาราง, แก้ไขตาราง)
- Web installer must be a single executable with zero click install experience.
- Repository target: `https://github.com/phwyverysad/Notepads.git`.

---

## Task Decomposition

### Task 1: Complete Thai Localization (`th-TH`)

**Files:**
- Create: `src/Notepads/Strings/th-TH/Manifest.resw`
- Create: `src/Notepads/Strings/th-TH/Resources.resw`
- Create: `src/Notepads/Strings/th-TH/Settings.resw`
- Modify: `src/Notepads/Notepads.csproj` (add resw references to Content items)
- Modify: `src/Notepads/Settings/AppSettingsService.cs` (set default language fallback to Thai)

**Interfaces:**
- Produces: Complete resource strings for `th-TH` culture loaded by `ResourceLoader`.

- [ ] **Step 1: Create `src/Notepads/Strings/th-TH/Manifest.resw`** with Thai translations for app display name, file extension descriptions, and tile descriptions.
- [ ] **Step 2: Create `src/Notepads/Strings/th-TH/Resources.resw`** with Thai translations for all editor actions, dialogs, notifications, status bar strings, and flyout menu labels matching the reference screenshots.
- [ ] **Step 3: Create `src/Notepads/Strings/th-TH/Settings.resw`** with Thai translations for all settings tabs, options, descriptions, and about info.
- [ ] **Step 4: Update `Notepads.csproj`** to include `th-TH\*.resw` in the project item group.
- [ ] **Step 5: Verify build with MSBuild** to ensure PRI resource generation succeeds without errors.
- [ ] **Step 6: Commit:** `git commit -m "feat(i18n): add comprehensive Thai (th-TH) localization"`

---

### Task 2: Red-Bordered Toolbar - Menu Bar Layout (`ไฟล์`, `แก้ไข`, `มุมมอง`)

**Files:**
- Modify: `src/Notepads/Views/MainPage/NotepadsMainPage.xaml`
- Modify: `src/Notepads/Views/MainPage/NotepadsMainPage.xaml.cs`

**Interfaces:**
- Consumes: `SetsView` active tab context, `TextEditor` instance methods (`Save()`, `Open()`, `Print()`, etc.)
- Produces: `MenuBar` docked directly under `SetsView` containing `ไฟล์`, `แก้ไข`, `มุมมอง`.

- [ ] **Step 1: Hide `MainMenuButton`** in `SetsView.SetsStartHeader` (`Visibility="Collapsed"`).
- [ ] **Step 2: Insert Toolbar Grid Container** directly beneath `SetsView` (Row 1 or within the SetsView Content layout).
- [ ] **Step 3: Implement MenuBar Controls** for `ไฟล์`, `แก้ไข`, `มุมมอง`:
  - `ไฟล์` DropDownButton/MenuBarItem:
    - แท็บใหม่ (`Ctrl+N`), หน้าต่างใหม่ (`Ctrl+Shift+N`), แท็บ Markdown ใหม่, เปิด (`Ctrl+O`), ล่าสุด (FlyoutSubItem), บันทึก (`Ctrl+S`), บันทึกเป็น (`Ctrl+Shift+S`), บันทึกทั้งหมด (`Ctrl+Alt+S`), การตั้งค่าหน้ากระดาษ, พิมพ์ (`Ctrl+P`), ปิดแท็บ (`Ctrl+W`), ปิดหน้าต่าง (`Ctrl+Shift+W`), ออก.
  - `แก้ไข` DropDownButton/MenuBarItem:
    - เลิกทำ (`Ctrl+Z`), ตัด (`Ctrl+X`), คัดลอก (`Ctrl+C`), วาง (`Ctrl+V`), ลบ (`Del`), ล้างการจัดรูปแบบ, ค้นหาด้วย Bing (`Ctrl+E`), ค้นหา (`Ctrl+F`), ค้นหาถัดไป (`F3`), ค้นหาก่อนหน้านี้ (`Shift+F3`), แทนที่ (`Ctrl+H`), ไปที่ (`Ctrl+G`), เลือกทั้งหมด (`Ctrl+A`), เวลา/วันที่ (`F5`), แบบอักษร.
  - `มุมมอง` DropDownButton/MenuBarItem:
    - ย่อ/ขยาย (Submenu: ซูมเข้า, ซูมออก, คืนค่าการซูม), แถบสถานะ (Toggle), การตัดคำ (Toggle), Markdown (Submenu).
- [ ] **Step 4: Wire Command Handlers in Code-Behind** (`NotepadsMainPage.xaml.cs`) to route each menu action to active editor commands.
- [ ] **Step 5: Build and Verify** that XAML compiles without warnings or errors.
- [ ] **Step 6: Commit:** `git commit -m "feat(ui): add dedicated menu bar with File, Edit, and View below tab bar"`

---

### Task 3: Red-Bordered Toolbar - Markdown Formatting Tools (`หัวเรื่อง`, `รายการ`, `B`, `I`, `S`, `🔗`, `ตาราง`, `A`, `👁`)

**Files:**
- Create: `src/Notepads/Controls/MarkdownToolbar/MarkdownToolbarControl.xaml`
- Create: `src/Notepads/Controls/MarkdownToolbar/MarkdownToolbarControl.xaml.cs`
- Modify: `src/Notepads/Views/MainPage/NotepadsMainPage.xaml`
- Modify: `src/Notepads/Views/MainPage/NotepadsMainPage.xaml.cs`

**Interfaces:**
- Consumes: `TextEditor.GetSelectedText()`, `TextEditor.SetSelectedText()`, `MarkdownExtensionView`
- Produces: Markdown syntax manipulation actions and interactive 5x5 table insertion picker.

- [ ] **Step 1: Implement `MarkdownToolbarControl`** with:
  - `หัวเรื่อง` DropdownButton: ชื่อ (`#`), คำบรรยาย (`##`), ส่วนหัว (`###`), หัวเรื่องย่อย (`####`), ส่วน (`#####`), ส่วนย่อย (`######`), เนื้อความ (remove heading).
  - `รายการ` DropdownButton: รายการสัญลักษณ์แสดงหัวข้อย่อย (`- `), รายการลำดับเลข (`1. `), เพิ่มการเยื้อง, ลดการเยื้อง.
  - `B` (Bold: `**text**`).
  - `I` (Italic: `*text*`).
  - `S` (Strikethrough: `~~text~~`).
  - `🔗` (Hyperlink: `[text](url)`).
  - `ตาราง` DropdownButton: 5x5 interactive hover grid selector + แทรกตาราง + แก้ไขตาราง.
  - `A` (Clear Formatting / Strip markdown markers).
  - `👁` (Toggle Markdown Preview).
  - `⚙` (Settings Button).
- [ ] **Step 2: Connect Toolbar with Active `TextEditor`**:
  - Implement helper methods to wrap selected text or prefix lines in the active `RichEditBox` / text stream.
- [ ] **Step 3: Build and Verify** formatting actions with MSBuild.
- [ ] **Step 4: Commit:** `git commit -m "feat(ui): implement markdown formatting toolbar with heading, list, table, and inline tools"`

---

### Task 4: Lightweight Web Installer & Launcher EXE (`NotepadsInstaller.exe`)

**Files:**
- Create: `src/Notepads.WebInstaller/NotepadsInstaller.csproj`
- Create: `src/Notepads.WebInstaller/Program.cs`
- Create: `src/Notepads.WebInstaller/InstallerForm.cs`
- Modify: `src/Notepads.sln` (add `Notepads.WebInstaller` to solution)

**Interfaces:**
- Consumes: GitHub Releases API / release download URL (`https://github.com/phwyverysad/Notepads/releases`)
- Produces: Standalone `NotepadsInstaller.exe` (< 1-2 MB) with silent download, silent install, and immediate launch.

- [ ] **Step 1: Create `NotepadsInstaller` project** (.NET Framework 4.8 / Windows Forms / console launcher, native to all Windows 10/11 machines with zero prerequisites).
- [ ] **Step 2: Implement Instant Launch Check:**
  - Check if Notepads is already installed in `%LocalAppData%\Programs\Notepads\Notepads.exe` or registered as Appx package.
  - If found: launch immediately with passed arguments and exit cleanly within milliseconds.
- [ ] **Step 3: Implement Silent Web Installer Logic:**
  - If Notepads is not found: show a sleek compact progress window, download package from GitHub Releases or fallback payload, extract to `%LocalAppData%\Programs\Notepads`, register shortcut/alias, and launch Notepads immediately.
- [ ] **Step 4: Build `NotepadsInstaller.exe`** and verify executable size is under 2 MB.
- [ ] **Step 5: Commit:** `git commit -m "feat(installer): create lightweight silent web installer and portable launcher"`

---

### Task 5: Packaging & Verification

**Files:**
- Build outputs: `src/Notepads/bin/x64/Release/`
- Build outputs: `dist/Notepads-x64.zip`
- Build outputs: `dist/NotepadsInstaller.exe`

- [ ] **Step 1: Compile Notepads x64 Release** package.
- [ ] **Step 2: Package Release ZIP** (`dist/Notepads-x64.zip`) containing all required binaries, assets, and pri resources.
- [ ] **Step 3: Verify Web Installer end-to-end** using local package test and launch verification.
- [ ] **Step 4: Commit:** `git commit -m "chore(build): produce release package and installer binaries"`

---

### Task 6: GitHub Remote Configuration & Push

**Files:**
- Remote: `https://github.com/phwyverysad/Notepads.git`

- [ ] **Step 1: Check and configure git remote origin** to point to `https://github.com/phwyverysad/Notepads.git`.
- [ ] **Step 2: Push changes** to the remote repository.
- [ ] **Step 3: Document walkthrough and user instructions.**
