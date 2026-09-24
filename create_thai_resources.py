import os
import xml.etree.ElementTree as ET

EN_US_DIR = r"c:\Users\woran\Documents\antigravity\delightful-kepler\src\Notepads\Strings\en-US"
TH_TH_DIR = r"c:\Users\woran\Documents\antigravity\delightful-kepler\src\Notepads\Strings\th-TH"

os.makedirs(TH_TH_DIR, exist_ok=True)

# 1. Manifest.resw
manifest_tree = ET.parse(os.path.join(EN_US_DIR, "Manifest.resw"))
manifest_root = manifest_tree.getroot()

for elem in manifest_root.findall("data"):
    name = elem.get("name")
    val_elem = elem.find("value")
    if val_elem is not None and val_elem.text:
        text = val_elem.text
        if name == "NewTextDocumentDisplayName":
            val_elem.text = "เอกสารข้อความ"
        elif "FileDisplayName" in name:
            base = text.replace(" File", "").replace(" file", "")
            if base == "Markdown":
                val_elem.text = "ไฟล์ Markdown"
            elif base == "Text":
                val_elem.text = "ไฟล์ข้อความ"
            else:
                val_elem.text = f"ไฟล์ {base}"

manifest_tree.write(os.path.join(TH_TH_DIR, "Manifest.resw"), encoding="utf-8", xml_declaration=True)
print("Manifest.resw generated.")

# 2. Resources.resw
res_translations = {
    "FindAndReplace_FindBar.PlaceholderText": "ค้นหา",
    "FindAndReplace_NotificationMsg_NotFound": "ไม่พบข้อความ",
    "FindAndReplace_ReplaceAllButton.ToolTipService.ToolTip": "แทนที่ทั้งหมด (Ctrl+Alt+Enter)",
    "FindAndReplace_ReplaceBar.PlaceholderText": "แทนที่",
    "FindAndReplace_ReplaceButton.ToolTipService.ToolTip": "แทนที่ (Alt+R)",
    "FindAndReplace_SearchForwardButton.ToolTipService.ToolTip": "ค้นหาถัดไป (F3)",
    "FindAndReplace_SearchOptionButton.ToolTipService.ToolTip": "ตัวเลือกการค้นหา",
    "FindAndReplace_SearchOptionToggleButton_MatchCase.Text": "ตรงตามตัวพิมพ์ใหญ่-เล็ก",
    "FindAndReplace_SearchOptionToggleButton_MatchWholeWord.Text": "ค้นหาทั้งคำ",
    "MainMenu_Button_Find.Text": "ค้นหา...",
    "MainMenu_Button_New.Text": "แท็บใหม่",
    "MainMenu_Button_Open.Text": "เปิด...",
    "MainMenu_Button_Print.Text": "พิมพ์...",
    "MainMenu_Button_Replace.Text": "แทนที่...",
    "MainMenu_Button_Save.Text": "บันทึก",
    "MainMenu_Button_SaveAll.Text": "บันทึกทั้งหมด",
    "MainMenu_Button_SaveAs.Text": "บันทึกเป็น...",
    "MainMenu_Button_Settings.Text": "การตั้งค่า",
    "RevertAllChangesConfirmationDialog_CloseButtonText": "ยกเลิก",
    "RevertAllChangesConfirmationDialog_Content": 'การเปลี่ยนแปลงทั้งหมดรวมถึงข้อความ การขึ้นบรรทัดใหม่ และการเข้ารหัสของ "{0}" จะถูกย้อนกลับ!',
    "RevertAllChangesConfirmationDialog_PrimaryButtonText": "ใช่",
    "RevertAllChangesConfirmationDialog_Title": "คุณแน่ใจหรือไม่ว่าต้องการย้อนกลับการเปลี่ยนแปลงทั้งหมด?",
    "SetCloseSaveReminderDialog_CloseButtonText": "ยกเลิก",
    "SetCloseSaveReminderDialog_Content": 'บันทึกการเปลี่ยนแปลงของไฟล์ "{0}" หรือไม่?',
    "SetCloseSaveReminderDialog_PrimaryButtonText": "บันทึก",
    "SetCloseSaveReminderDialog_SecondaryButtonText": "ไม่บันทึก",
    "SetCloseSaveReminderDialog_Title": "ต้องการบันทึกการเปลี่ยนแปลงหรือไม่?",
    "Tab_ContextFlyout_CloseButtonDisplayText": "ปิดแท็บ",
    "Tab_ContextFlyout_CloseOthersButtonDisplayText": "ปิดแท็บอื่น",
    "Tab_ContextFlyout_CloseRightButtonDisplayText": "ปิดแท็บทางขวา",
    "Tab_ContextFlyout_CloseSavedButtonDisplayText": "ปิดแท็บที่บันทึกแล้ว",
    "Tab_ContextFlyout_CopyFullPathButtonDisplayText": "คัดลอกเส้นทางแบบเต็ม",
    "Tab_ContextFlyout_OpenContainingFolderButtonDisplayText": "เปิดโฟลเดอร์ที่จัดเก็บ",
    "TextEditor_ContextFlyout_CopyButtonDisplayText": "คัดลอก",
    "TextEditor_ContextFlyout_CutButtonDisplayText": "ตัด",
    "TextEditor_ContextFlyout_PasteButtonDisplayText": "วาง",
    "TextEditor_ContextFlyout_PreviewToggleDisplay_Text": "สลับมุมมองตัวอย่าง Markdown",
    "TextEditor_ContextFlyout_RedoButtonDisplayText": "ทำซ้ำ",
    "TextEditor_ContextFlyout_SelectAllButtonDisplayText": "เลือกทั้งหมด",
    "TextEditor_ContextFlyout_ShareButtonDisplayText": "แชร์",
    "TextEditor_ContextFlyout_ShareSelectedButtonDisplayText": "แชร์ส่วนที่เลือก",
    "TextEditor_ContextFlyout_UndoButtonDisplayText": "เลิกทำ",
    "TextEditor_ContextFlyout_WordWrapButtonDisplayText": "การตัดคำ",
    "TextEditor_DefaultNewFileName": "ไม่มีชื่อ.txt",
    "TextEditor_LineColumnIndicator_FullText": "บรรทัด {0}, คอลัมน์ {1} (เลือก {2} {3})",
    "TextEditor_LineColumnIndicator_ShortText": "บรรทัด {0}, คอลัมน์ {1}",
    "TextEditor_ModificationIndicator_MenuFlyoutItem_PreviewTextChanges.Text": "แสดงตัวอย่างการเปลี่ยนแปลงข้อความ",
    "TextEditor_ModificationIndicator_MenuFlyoutItem_RevertAllChanges.Text": "ย้อนกลับการเปลี่ยนแปลงทั้งหมด",
    "TextEditor_ModificationIndicator_Text": "แก้ไขแล้ว",
    "TextEditor_NotificationMsg_FileNameOrPathCopied": "คัดลอกแล้ว",
    "TextEditor_NotificationMsg_FileSaved": "บันทึกแล้ว",
    "TextEditor_NotificationMsg_ExitFullScreenHint": "กด F11 เพื่อออกจากเต็มหน้าจอ",
    "TextEditor_FileModifiedOutsideIndicator_MenuFlyoutItem_ReloadFileFromDisk.Text": "โหลดไฟล์ซ้ำจากดิสก์",
    "TextEditor_FileModifiedOutsideIndicator_ToolTip": "ไฟล์ถูกแก้ไขจากภายนอก",
    "TextEditor_FileRenamedMovedOrDeletedIndicator_ToolTip": "ไฟล์ถูกย้าย เปลี่ยนชื่อ หรือลบแล้ว!",
    "TextEditor_NotificationMsg_FileReloaded": "โหลดไฟล์ซ้ำแล้ว",
    "App_EnterCompactOverlayMode_Text": "โหมดซ้อนทับกะทัดรัด",
    "App_EnterFullScreenMode_Text": "เต็มหน้าจอ",
    "App_ExitCompactOverlayMode_Text": "ออกจากโหมดซ้อนทับกะทัดรัด",
    "App_ExitFullScreenMode_Text": "ออกจากเต็มหน้าจอ",
    "TextEditor_LineColumnIndicator_FullText_PluralSelectedWord": "อักขระ",
    "TextEditor_LineColumnIndicator_FullText_SingularSelectedWord": "อักขระ",
    "App_DragAndDrop_UIOverride_Caption_MoveTabHere": "ย้ายแท็บมาที่นี่",
    "App_DragAndDrop_UIOverride_Caption_OpenWithNotepads": "เปิดด้วย Notepads",
    "App_ShadowWindowIndicator_Description": "นี่คือหน้าต่างเงาของ Notepads ซึ่งจะปิดใช้งานภาพรวมเซสชันและการตั้งค่า",
    "TextEditor_NotificationMsg_FileAlreadyOpened": "ไฟล์นี้เปิดอยู่แล้ว!",
    "JumpList_Tasks_NewWindow_Title": "หน้าต่างใหม่",
    "JumpList_Tasks_NewWindow_Description": "เปิดหน้าต่างใหม่",
    "MainMenu_Button_New_Window.Text": "หน้าต่างใหม่",
    "MainMenu_Button_Open_Recent.Text": "ล่าสุด",
    "GoTo_GoToBar.PlaceholderText": "ไปยังบรรทัดที่",
    "GoTo_NotificationMsg_InputError_ExceedInputLimit": "หมายเลขบรรทัดเกินจำนวนบรรทัดทั้งหมด!",
    "GoTo_NotificationMsg_InputError_InvalidInput": "พิมพ์ได้เฉพาะตัวเลขเท่านั้น!",
    "GoTo_SearchButton.ToolTipService.ToolTip": "ไปยังบรรทัดที่",
    "TextEditor_ContextFlyout_WebSearchButtonDisplayText": "ค้นหาด้วย Bing",
    "TextEditor_FontZoomIndicator_FlyoutItem_RestoreDefaultZoom.Label": "คืนค่าการย่อ/ขยายเริ่มต้น",
    "TextEditor_FontZoomIndicator_FlyoutItem_ZoomIn.Label": "ขยาย",
    "TextEditor_FontZoomIndicator_FlyoutItem_ZoomOut.Label": "ย่อ",
    "GoTo_GoToBarLabel.Text": "ไปยัง:",
    "MainMenu_Button_PrintAll.Text": "พิมพ์ทั้งหมด...",
    "Print_ErrorMsg_DecimalOutOfRange": "ยอมรับทศนิยมไม่เกินหนึ่งตำแหน่ง",
    "Print_ErrorMsg_ValueOutOfRange": "ค่านอกช่วงที่กำหนด",
    "Print_FooterEntry_Title": "ส่วนท้ายกระดาษ",
    "Print_HeaderEntry_Title": "ส่วนหัวกระดาษ",
    "Print_LeftMarginEntry_Title": "ระยะขอบแนวนอน (เป็น %)",
    "Print_MarginEntry_Description": "คิดเป็น % ของความกว้างหน้ากระดาษ",
    "Print_NotificationMsg_PrintError": "เกิดข้อผิดพลาดในการพิมพ์:",
    "Print_NotificationMsg_PrintFailed": "การพิมพ์ล้มเหลว",
    "Print_NotificationMsg_PrintNotSupported": "อุปกรณ์นี้ไม่รองรับการพิมพ์",
    "Print_TopMarginEntry_Title": "ระยะขอบแนวตั้ง (เป็น %)",
    "FindAndReplace_SearchOptionToggleButton_UseRegex.Text": "ใช้นิพจน์ปกติ (Regex)",
    "MainMenu_Button_Open_Recent_ClearRecentlyOpenedSubItem_Text": "ล้างรายการล่าสุด",
    "TextEditor_EncodingIndicator_FlyoutItem_MoreEncodings": "การเข้ารหัสเพิ่มเติม",
    "TextEditor_EncodingIndicator_FlyoutItem_ReopenWithEncoding": "เปิดใหม่ด้วยการเข้ารหัส",
    "TextEditor_EncodingIndicator_FlyoutItem_SaveWithEncoding": "บันทึกด้วยการเข้ารหัส",
    "FindAndReplace_NotificationMsg_InvalidRegex": "นิพจน์ปกติไม่ถูกต้อง!",
    "TextEditor_EncodingIndicator_FlyoutItem_AutoGuessEncoding": "ตรวจหาการเข้ารหัสอัตโนมัติ",
    "TextEditor_NotificationMsg_EncodingCannotBeDetermined": "ไม่สามารถระบุการเข้ารหัสได้",
    "FindAndReplace_SearchBackwardButton.ToolTipService.ToolTip": "ค้นหาก่อนหน้า (Shift+F3)",
    "FindAndReplace_ToggleReplaceModeButton.ToolTipService.ToolTip": "สลับโหมดแทนที่",
    "TextEditor_ContextFlyout_RightToLeftReadingOrderButtonDisplayText": "ลำดับการอ่านจากขวาไปซ้าย",
    "FontStyle_Italic": "ตัวเอียง",
    "FontStyle_Normal": "ปกติ",
    "FontStyle_Oblique": "ตัวเอน",
    "FontWeight_Black": "หนามาก",
    "FontWeight_Bold": "หนา",
    "FontWeight_ExtraBlack": "ดำทึบ",
    "FontWeight_ExtraBold": "หนาพิเศษ",
    "FontWeight_ExtraLight": "บางพิเศษ",
    "FontWeight_Light": "บาง",
    "FontWeight_Medium": "ปานกลาง",
    "FontWeight_Normal": "ปกติ",
    "FontWeight_SemiBold": "กึ่งหนา",
    "FontWeight_SemiLight": "กึ่งบาง",
    "FontWeight_Thin": "บางมาก",
    "FileRenameDialog_CloseButtonText": "ยกเลิก",
    "FileRenameDialog_PrimaryButtonText": "บันทึก",
    "FileRenameDialog_Title": "เปลี่ยนชื่อ",
    "InvalidFilenameError_ContainsInvalidCharacters": "ชื่อไฟล์ต้องไม่มีอักขระที่ไม่อนุญาต",
    "InvalidFilenameError_ContainsLeadingSpaces": "ชื่อไฟล์ต้องไม่มีช่องว่างนำหน้า",
    "InvalidFilenameError_ContainsTrailingSpaces": "ชื่อไฟล์ต้องไม่มีช่องว่างต่อท้าย",
    "InvalidFilenameError_EmptyOrAllWhitespace": "ชื่อไฟล์ต้องไม่ว่างเปล่าหรือเป็นช่องว่างทั้งหมด",
    "InvalidFilenameError_InvalidOrNotAllowed": "ชื่อไฟล์ไม่ถูกต้องหรือไม่อนุญาต",
    "InvalidFilenameError_TooLong": "ชื่อไฟล์ต้องยาวไม่เกิน 255 ตัวอักษร",
    "Tab_ContextFlyout_RenameButtonDisplayText": "เปลี่ยนชื่อ",
    "TextEditor_NotificationMsg_FileRenamed": "เปลี่ยนชื่อสำเร็จ",
    "FileRenameError_EmptyFileExtension": "ยังไม่รองรับนามสกุลไฟล์ที่ว่างเปล่าในขณะนี้",
    "FileRenameError_UnsupportedFileExtension": 'ยังไม่รองรับนามสกุลไฟล์ "{0}" ในขณะนี้',
    "SessionCorruptionErrorDialog_CloseButtonText": "ปิด",
    "SessionCorruptionErrorDialog_Content": "ไม่สามารถกู้คืนข้อมูลจากเซสชันล่าสุดได้เนื่องจากข้อมูลเสียหาย โปรดสำรองไฟล์ที่ยังไม่ได้บันทึก (*.txt) ในโฟลเดอร์ของเซสชัน",
    "SessionCorruptionErrorDialog_PrimaryButtonText": "เปิดโฟลเดอร์สำรองข้อมูลเซสชัน",
    "SessionCorruptionErrorDialog_Title": "คำเตือน"
}

res_tree = ET.parse(os.path.join(EN_US_DIR, "Resources.resw"))
res_root = res_tree.getroot()

for elem in res_root.findall("data"):
    name = elem.get("name")
    val_elem = elem.find("value")
    if name in res_translations:
        val_elem.text = res_translations[name]

res_tree.write(os.path.join(TH_TH_DIR, "Resources.resw"), encoding="utf-8", xml_declaration=True)
print("Resources.resw generated.")

# 3. Settings.resw
set_translations = {
    "AboutPage_DependenciesAndReferences_Title.Text": "การพึ่งพาและการอ้างอิง",
    "AboutPage_Disclaimer_Content.Text": 'ซอฟต์แวร์นี้จัดทำขึ้น "ตามสภาพ" โดยไม่มีการรับประกันใดๆ ไม่ว่าโดยชัดแจ้งหรือโดยปริยาย ผู้เขียนหรือผู้ถือลิขสิทธิ์จะไม่รับผิดชอบต่อการเรียกร้อง ค่าเสียหาย หรือความรับผิดชอบอื่นใดอันเกิดจากซอฟต์แวร์นี้',
    "AboutPage_Disclaimer_Title.Text": "ข้อจำกัดความรับผิดชอบ",
    "AboutPage_NotepadsShortDescription.Text": "โปรแกรมแก้ไขข้อความฟรีและโอเพ่นซอร์ส ออกแบบและพัฒนาโดย Jackie (Jiaqi) Liu",
    "AboutPage_Notepads_AuthorContactsTitle.Text": "ติดต่อผู้พัฒนา:",
    "AboutPage_Notepads_IssueAndFeatureRequestsTitle.Text": "รายงานปัญหาและขอฟีเจอร์เพิ่มเติม:",
    "AboutPage_Notepads_SourceCodeTitle.Text": "ซอร์สโค้ดพร้อมใช้งานบน GitHub:",
    "AboutPage_Notepads_WebsiteTitle.Text": "ดูข้อมูลเพิ่มเติมได้ที่เว็บไซต์:",
    "AboutPage_PrivacyStatementTitle.Text": "คำแถลงความเป็นส่วนตัว",
    "AboutPage_Title.Content": "เกี่ยวกับ",
    "AdvancedPage_StatusBarSettings_ShowHideStatusBarToggleSwitch.OffContent": "แสดงแถบสถานะ",
    "AdvancedPage_StatusBarSettings_ShowHideStatusBarToggleSwitch.OnContent": "แสดงแถบสถานะ",
    "AdvancedPage_StatusBarSettings_Title.Text": "การตั้งค่าแถบสถานะ",
    "AdvancedPage_Title.Content": "ขั้นสูง",
    "PersonalizationPage_AccentColorSettings_Title.Text": "สีเน้น",
    "PersonalizationPage_AccentColorSettings_UseWindowsAccentColorToggleSwitch.OffContent": "ใช้สีเน้นของ Windows",
    "PersonalizationPage_AccentColorSettings_UseWindowsAccentColorToggleSwitch.OnContent": "ใช้สีเน้นของ Windows",
    "PersonalizationPage_BackgroundTintOpacitySettings_Description.Text": "ความทึบของสีพื้นหลังเอฟเฟกต์ Acrylic หมายเหตุ: เอฟเฟกต์ Acrylic จะปิดลงเมื่อเปิดโหมดประหยัดพลังงานหรือปิดเอฟเฟกต์ความโปร่งใสใน Windows",
    "PersonalizationPage_BackgroundTintOpacitySettings_Title.Text": "ความทึบของสีพื้นหลัง",
    "PersonalizationPage_ThemeModeSettings_DarkModeRadioButton.Content": "มืด",
    "PersonalizationPage_ThemeModeSettings_LightModeRadioButton.Content": "สว่าง",
    "PersonalizationPage_ThemeModeSettings_Title.Text": "โหมดชุดรูปแบบ",
    "PersonalizationPage_ThemeModeSettings_WindowsModeRadioButton.Content": "ใช้ตามโหมดของ Windows",
    "PersonalizationPage_Title.Content": "การปรับแต่งส่วนบุคคล",
    "TextAndEditorPage_DecodingSettings_AnsiRadioButton.Content": "ANSI (Windows code page)",
    "TextAndEditorPage_DecodingSettings_Description.Text": "การถอดรหัสสำรองจะถูกใช้เมื่อไม่สามารถตรวจหาการเข้ารหัสของไฟล์ได้",
    "TextAndEditorPage_DecodingSettings_Title.Text": "การถอดรหัสสำรอง",
    "TextAndEditorPage_DecodingSettings_Utf8RadioButton.Content": "UTF-8",
    "TextAndEditorPage_EncodingSettings_Description.Text": "มีผลเฉพาะเอกสารใหม่เท่านั้น",
    "TextAndEditorPage_EncodingSettings_Title.Text": "การเข้ารหัสเริ่มต้น",
    "TextAndEditorPage_FontSettings_Title.Text": "แบบอักษรและขนาดเริ่มต้น",
    "TextAndEditorPage_LineEndingSettings_Description.Text": "มีผลเฉพาะเอกสารใหม่เท่านั้น",
    "TextAndEditorPage_LineEndingSettings_Title.Text": "การขึ้นบรรทัดใหม่เริ่มต้น",
    "TextAndEditorPage_TabKeySettings_DefaultRadioButton.Content": "ค่าเริ่มต้น (\\t)",
    "TextAndEditorPage_TabKeySettings_Description.Text": "การตั้งค่าการทำงานของปุ่ม Tab จะมีผลกับแท็บใหม่ที่พิมพ์โดยผู้ใช้",
    "TextAndEditorPage_TabKeySettings_EightSpacesRadioButton.Content": "8 ช่องว่าง",
    "TextAndEditorPage_TabKeySettings_FourSpacesRadioButton.Content": "4 ช่องว่าง",
    "TextAndEditorPage_TabKeySettings_Title.Text": "พฤติกรรมของปุ่ม Tab",
    "TextAndEditorPage_TabKeySettings_TwoSpacesRadioButton.Content": "2 ช่องว่าง",
    "TextAndEditorPage_TextWrappingSettings_Title.Text": "การตัดคำ",
    "TextAndEditorPage_TextWrappingSettings_ToggleSwitch.OffContent": "ตัดคำ",
    "TextAndEditorPage_TextWrappingSettings_ToggleSwitch.OnContent": "ตัดคำ",
    "TextAndEditorPage_Title.Content": "ข้อความและตัวแก้ไข",
    "AboutPage_ChangelogUrl_Title.Text": "บันทึกการเปลี่ยนแปลง",
    "AdvancedPage_SessionSnapshotSettings_Description.Text": "เมื่อเปิดใช้งาน Notepads จะจดจำเซสชันปัจจุบันสำหรับการเปิดครั้งถัดไป และสำรองข้อมูลเป็นระยะเพื่อป้องกันการสูญหายของข้อมูล Notepads จะไม่แจ้งเตือนให้บันทึกไฟล์เมื่อปิดแอปหากเปิดใช้งานฟีเจอร์นี้",
    "AdvancedPage_SessionSnapshotSettings_OnOffToggleSwitch.OffContent": "เปิดใช้งานการบันทึกเซสชัน",
    "AdvancedPage_SessionSnapshotSettings_OnOffToggleSwitch.OnContent": "เปิดใช้งานการบันทึกเซสชัน",
    "AdvancedPage_SessionSnapshotSettings_Title.Text": "การตั้งค่าการบันทึกเซสชัน",
    "TextAndEditorPage_SpellingSettings_HighlightMisspelledWordsToggleSwitch.OffContent": "เน้นคำที่สะกดผิด",
    "TextAndEditorPage_SpellingSettings_HighlightMisspelledWordsToggleSwitch.OnContent": "เน้นคำที่สะกดผิด",
    "TextAndEditorPage_SpellingSettings_Title.Text": "การสะกดคำ",
    "AdvancedPage_AlwaysOpenNewWindow_Description.Text": "เมื่อเปิดใช้งาน Notepads จะเปิดไฟล์ในหน้าต่างใหม่เสมอแทนที่จะสร้างแท็บใหม่",
    "AdvancedPage_LaunchPreferenceSettings_AlwaysOpenNewWindowToggleSwitch.OffContent": "เปิดหน้าต่างใหม่เสมอ",
    "AdvancedPage_LaunchPreferenceSettings_AlwaysOpenNewWindowToggleSwitch.OnContent": "เปิดหน้าต่างใหม่เสมอ",
    "AdvancedPage_LaunchPreferenceSettings_Title.Text": "การกำหนดลักษณะการเปิดโปรแกรม",
    "TextAndEditorPage_LineHighlighterSettings_ToggleSwitch.OffContent": "เน้นบรรทัดปัจจุบัน",
    "TextAndEditorPage_LineHighlighterSettings_ToggleSwitch.OnContent": "เน้นบรรทัดปัจจุบัน",
    "TextAndEditorPage_SearchEngineSettings_CustomSearchUrlRadioButton.Text": "เครื่องมือค้นหาที่กำหนดเอง",
    "TextAndEditorPage_SearchEngineSettings_CustomSearchUrlRadioButton_CustomUrlErrorReport.Text": "*ระบุ URL ในรูปแบบ https://www.example.com/search?q={0}",
    "TextAndEditorPage_SearchEngineSettings_Description.Text": "การตั้งค่าเครื่องมือค้นหาเริ่มต้นเมื่อทำการค้นหาบนเว็บ",
    "TextAndEditorPage_SearchEngineSettings_Title.Text": "เครื่องมือค้นหาเริ่มต้น",
    "TextAndEditorPage_DecodingSettings_AutoGuessRadioButton.Content": "ตรวจหาการเข้ารหัสอัตโนมัติ (แนะนำ)",
    "TextAndEditorPage_DisplaySettings_Title.Text": "การแสดงผล",
    "TextAndEditorPage_LineNumbersSettings_Description.Text": "แสดงหมายเลขบรรทัดในเอกสาร",
    "TextAndEditorPage_LineNumbersSettings_ToggleSwitch.OffContent": "แสดงหมายเลขบรรทัด",
    "TextAndEditorPage_LineNumbersSettings_ToggleSwitch.OnContent": "แสดงหมายเลขบรรทัด",
    "TextAndEditorPage_FontStyleSettings_Title.Text": "ลักษณะแบบอักษรเริ่มต้น",
    "TextAndEditorPage_FontWeightSettings_Title.Text": "ความหนาแบบอักษรเริ่มต้น",
    "AdvancedPage_LanguagePreferenceSettings_Description.Text": "เลือกภาษาเพื่อแทนที่ภาษาเริ่มต้นของระบบใน Notepads จำเป็นต้องรีสตาร์ทเพื่อนำการเปลี่ยนแปลงไปใช้",
    "AdvancedPage_LanguagePreferenceSettings_RestartPrompt.Text": "*รีสตาร์ท Notepads เพื่อให้การเปลี่ยนแปลงมีผลสมบูรณ์",
    "AdvancedPage_LanguagePreferenceSettings_Title.Text": "การกำหนดลักษณะภาษา",
    "AdvancedPage_SmartCopySettings_Description.Text": "เมื่อเปิดใช้งาน Notepads จะตัดช่องว่าง แท็บ และบรรทัดว่างที่หัวและท้ายข้อความที่เลือกอย่างชาญฉลาดก่อนคัดลอกไปยังคลิปบอร์ด",
    "AdvancedPage_SmartCopySettings_EnableSmartCopyToggleSwitch.OffContent": "เปิดใช้งาน Smart Copy",
    "AdvancedPage_SmartCopySettings_EnableSmartCopyToggleSwitch.OnContent": "เปิดใช้งาน Smart Copy",
    "AdvancedPage_SmartCopySettings_Title.Text": "การตั้งค่า Smart Copy",
    "AdvancedPage_LanguagePreferenceSettings_SystemDefaultText": "ค่าเริ่มต้นของระบบ",
    "AdvancedPage_LaunchPreferenceSettings_ExitWhenLastTabClosedToggleSwitch.OffContent": "ปิดแอปเมื่อปิดแท็บสุดท้าย",
    "AdvancedPage_LaunchPreferenceSettings_ExitWhenLastTabClosedToggleSwitch.OnContent": "ปิดแอปเมื่อปิดแท็บสุดท้าย"
}

set_tree = ET.parse(os.path.join(EN_US_DIR, "Settings.resw"))
set_root = set_tree.getroot()

for elem in set_root.findall("data"):
    name = elem.get("name")
    val_elem = elem.find("value")
    if name in set_translations:
        val_elem.text = set_translations[name]

set_tree.write(os.path.join(TH_TH_DIR, "Settings.resw"), encoding="utf-8", xml_declaration=True)
print("Settings.resw generated.")
