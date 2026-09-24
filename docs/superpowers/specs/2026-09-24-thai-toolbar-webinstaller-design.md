# Notepads Thai Localization, Red-Bordered Toolbar & Web Installer Specification

- **Date:** 2026-09-24
- **Repository:** https://github.com/phwyverysad/Notepads.git
- **Upstream Source:** https://github.com/0x7c13/Notepads.git

---

## 1. Overview & Objectives

This project enhances the open-source **Notepads** application (a modern, lightweight UWP text editor for Windows 10 & 11) with:
1. **Full Thai Localization (`th-TH`):** Complete translation covering UI manifests, editor strings, settings, and menus, matching standard Windows Thai terminology and the provided reference images. Default language set to Thai on initial launch.
2. **Toolbar & Menu Restructuring (Under Tab Bar):**
   - Relocate primary menus from the hamburger button into a dedicated menu & formatting bar positioned directly underneath the tab bar (matching the reference layout in `media_1790264184332.png`).
   - Left side: Classic Menu Bar (`ไฟล์`, `แก้ไข`, `มุมมอง`) with full flyouts matching the provided screenshots.
   - Center: Markdown & Rich Formatting Toolbar (`หัวเรื่อง H1 ˅`, `รายการ := ˅`, `B`, `I`, `S`, `🔗`, `ตาราง 田 ˅`, `A` clear formatting).
   - Right side: Markdown Preview Toggle (`👁`), Editor Mode, and Settings (`⚙`).
3. **Lightweight Web Installer & Launcher EXE (`NotepadsInstaller.exe` / `Notepads.exe`):**
   - Ultra-compact Windows executable (<1-2 MB).
   - If Notepads is already installed/present: launches Notepads instantly without prompt.
   - If Notepads is not yet installed: automatically downloads the latest release archive from GitHub Releases (`https://github.com/phwyverysad/Notepads/releases`), performs a silent extraction/registration into `%LocalAppData%\Programs\Notepads`, and launches Notepads immediately without requiring user interaction.
4. **Remote Git Repository Setup:**
   - Git remote configured to `https://github.com/phwyverysad/Notepads.git`.

---

## 2. User Interface Design & Architecture

### 2.1 Top Layout Structure
In `NotepadsMainPage.xaml`, the layout consists of:
```
+---------------------------------------------------------------------------------+
|  [Tab 1] [Tab 2] [+]                           [Compact] [Min] [Max] [Close]    |  <- SetsView Tab Bar
+---------------------------------------------------------------------------------+
|  [ไฟล์] [แก้ไข] [มุมมอง]  |  [H1 ˅] [:= ˅] [B] [I] [S] [🔗] [田 ˅] [A]  |  [👁] [⚙] |  <- Red-Bordered Toolbar
+---------------------------------------------------------------------------------+
|                                                                                 |
|                              Text Editor Area                                   |
|                                                                                 |
+---------------------------------------------------------------------------------+
|  Ln 1, Col 1 | 0 อักขระ | ข้อความธรรมดา | 100% | Windows (CRLF) | UTF-8          |  <- Status Bar
+---------------------------------------------------------------------------------+
```

### 2.2 Menu Flyouts Specification

#### 1. ไฟล์ (File)
Flyout items matching `ปุ่ม ไฟล์.png`:
- **แท็บใหม่** (`Ctrl+N`) -> Creates new tab (`NewTab()`)
- **หน้าต่างใหม่** (`Ctrl+Shift+N`) -> Creates new window (`NewWindow()`)
- **แท็บ Markdown ใหม่** -> Creates new tab preset with `.md` template/mode
- **เปิด** (`Ctrl+O`) -> Open file dialog
- **ล่าสุด >** (`Recent Files`) -> Submenu listing recently opened files
- **บันทึก** (`Ctrl+S`) -> Saves active document
- **บันทึกเป็น** (`Ctrl+Shift+S`) -> Save as dialog
- **บันทึกทั้งหมด** (`Ctrl+Alt+S`) -> Saves all open tabs
- *[Separator]*
- **การตั้งค่าหน้ากระดาษ** -> Page setup / print options
- **พิมพ์** (`Ctrl+P`) -> Print active tab
- *[Separator]*
- **ปิดแท็บ** (`Ctrl+W`) -> Closes active tab
- **ปิดหน้าต่าง** (`Ctrl+Shift+W`) -> Closes active window
- **ออก** -> Exits application

#### 2. แก้ไข (Edit)
Flyout items matching `ปุ่ม แก้ไข.png`:
- **เลิกทำ** (`Ctrl+Z`) -> Undo
- **ตัด** (`Ctrl+X`) -> Cut
- **คัดลอก** (`Ctrl+C`) -> Copy
- **วาง** (`Ctrl+V`) -> Paste
- **ลบ** (`Del`) -> Delete
- *[Separator]*
- **ล้างการจัดรูปแบบ** -> Strips markdown formatting / reset text styling
- **ค้นหาด้วย Bing** (`Ctrl+E`) -> Web search selected text with Bing
- *[Separator]*
- **ค้นหา** (`Ctrl+F`) -> Toggle Find bar
- **ค้นหาถัดไป** (`F3`) -> Find next match
- **ค้นหาก่อนหน้านี้** (`Shift+F3`) -> Find previous match
- **แทนที่** (`Ctrl+H`) -> Toggle Replace bar
- **ไปที่** (`Ctrl+G`) -> Go to line dialog
- *[Separator]*
- **เลือกทั้งหมด** (`Ctrl+A`) -> Select all
- **เวลา/วันที่** (`F5`) -> Insert current date/time string
- *[Separator]*
- **แบบอักษร** -> Opens Font selection / typography settings

#### 3. มุมมอง (View)
Flyout items matching `ปุ่ม มุมมอง.png`:
- **ย่อ/ขยาย >** -> Submenu: ซูมเข้า (`Ctrl+Plus`), ซูมออก (`Ctrl+Minus`), คืนค่าการซูม (`Ctrl+0`)
- **แถบสถานะ** (Checkmark) -> Toggles StatusBar visibility
- **การตัดคำ** (Checkmark) -> Toggles Word Wrap (On/Off)
- **Markdown >** -> Submenu: สลับมุมมองแสดงผล Markdown (`Ctrl+M`), แสดงตัวอย่างเคียงข้าง (Side-by-side)

### 2.3 Formatting Toolbar Specification

#### 1. หัวเรื่อง (Heading H1 ˅)
Flyout matching `ปุ่ม หัวเรื่อง.png`:
- **ชื่อ** -> `# Title`
- **คำบรรยาย** -> `## Subtitle`
- **ส่วนหัว** -> `### Heading 1`
- **หัวเรื่องย่อย** -> `#### Heading 2`
- **ส่วน** -> `##### Heading 3`
- **ส่วนย่อย** -> `###### Heading 4`
- **เนื้อความ** -> Normal body text (removes `#` prefixes)

#### 2. รายการ (List := ˅)
Flyout matching `ปุ่ม รายการ.png`:
- **รายการสัญลักษณ์แสดงหัวข้อย่อย** -> Inserst `- ` bullet points
- **รายการลำดับเลข** -> Inserts `1. ` numbered items
- **เพิ่มการเยื้อง** -> Indent selected lines (Tab / 4 spaces)
- **ลดการเยื้อง** -> Unindent selected lines (Shift+Tab)

#### 3. สไตล์ตัวอักษรและลิงก์ (Inline Formatting)
- **B (ตัวหนา):** Wraps selection with `**` (e.g. `**ข้อความ**`)
- **I (ตัวเอียง):** Wraps selection with `*` (e.g. `*ข้อความ*`)
- **S (ขีดทับ):** Wraps selection with `~~` (e.g. `~~ข้อความ~~`)
- **🔗 (ลิงก์):** Wraps selection with `[ข้อความ](url)`
- **A (ล้างการจัดรูปแบบ):** Removes enclosing markdown markup

#### 4. ตาราง (Table 田 ˅)
Flyout matching `ปุ่มตาราง.png`:
- **Grid Picker (5x5):** Interactive grid selector to choose column & row count (1x1 up to 5x5) and insert markdown table template.
- **แทรกตาราง:** Prompt or default 3x3 table insertion.
- **แก้ไขตาราง >:** Submenu for adding/removing rows and columns.

#### 5. ปุ่มขวา (Preview & Settings)
- **👁 (Markdown Preview):** Toggles live markdown preview view for active tab.
- **⚙ (การตั้งค่า):** Opens Notepads Settings panel.

---

## 3. Localization Architecture (`th-TH`)

Create `src/Notepads/Strings/th-TH/`:
1. `Manifest.resw`: App name, file type associations (.txt, .md, etc.), tile descriptions in Thai.
2. `Resources.resw`: Editor strings, status bar labels, dialog prompts, context menu items, button tooltips, notifications in Thai.
3. `Settings.resw`: Personalization, Text & Editor, Advanced settings, About page labels in Thai.
4. Set default culture to `th-TH` in initialization while retaining user preference switching via Settings.

---

## 4. Web Installer & Launcher Architecture

### 4.1 Component Details
- **Project:** `src/Notepads.WebInstaller/NotepadsInstaller.csproj`
- **Output:** `NotepadsInstaller.exe` / `Notepads.exe` (Single standalone executable, < 1 MB).
- **Runtime:** .NET Framework 4.8 / Windows Desktop native compatible.
- **Execution Flow:**
  1. **Detection:**
     - Query Windows Package Manager / Appx (`Notepads` package family).
     - Check local installation path (`%LocalAppData%\Programs\Notepads\Notepads.exe`).
  2. **Launch Branch (Already Installed):**
     - Execute `explorer.exe shell:AppsFolder\...` or start `Notepads.exe`.
     - Installer terminates immediately.
  3. **Install Branch (Not Installed):**
     - Display a lightweight, non-blocking splash with progress bar.
     - Download package from:
       `https://github.com/phwyverysad/Notepads/releases/latest/download/Notepads-x64.zip`
     - Extract silently to `%LocalAppData%\Programs\Notepads`.
     - Register execution alias / Appx package silently via PowerShell / Windows API.
     - Launch Notepads immediately.

---

## 5. Verification Plan

1. **Build Verification:**
   - Solution compiles cleanly via MSBuild (`x64` Debug and Release).
   - Resources compile with PRI resource indexing.
2. **UI Verification:**
   - Red-bordered toolbar renders directly under tabs.
   - All dropdowns (`ไฟล์`, `แก้ไข`, `มุมมอง`, `หัวเรื่อง`, `รายการ`, `ตาราง`) match the user's reference screenshots.
   - Clicking formatting buttons inserts markdown syntax correctly into active editor.
3. **Localization Verification:**
   - All text in UI displays correctly in Thai (`th-TH`).
   - Switching language works as expected.
4. **Installer Verification:**
   - `NotepadsInstaller.exe` runs silently, downloads, installs, and launches Notepads.
   - Running again launches installed app instantaneously.
5. **Git Repository Verification:**
   - Remote added: `https://github.com/phwyverysad/Notepads.git`.
   - All commits pushed cleanly to the remote repository.
