1.08 changes:
Fixed compatability with unity 4.1.x
Added call stack navigation to errors and logs.

1.07 changes:
Fixed issue with non-US keyboard inputs.
Added realtime syntax error highlighting feature.
Shift+Tab now works where there is no selection, or just a single line selection.
Added some intention rules for switch/case statements.
Adjusted keyword highlighting style to be easier to differentiate between text selection.

1.06 changes:
Fixed all issues with window maximization while docked.
Can double click script in project view to open files in UnIDE. Can be disabled in options.
Changed tmp file names to asset GUID or hashed filename for external files. (Also fixes drag/drop files from external sources).

1.05 changes:
Fixed bug that caused some hotkeys to not work sometimes.
Fixed bug that caused scripts to be reopened when docked and "Maximize On Play" is enabled.
Fixed bug that caused parsing not to work while docked with the game view after pressing play, before recompiling.

1.04 changes:
Made autocompletion/parsing on OSX much faster. This should no longer be an issue.
Added drag and drop feature. You can drag files from the project window, or Explorer/Finder onto UnIDE to open them.
Customizable file formats to include in the file list. (Currently must be edited through the inspector in "UnIDE/Editor/_TMP/SettingsGroupData/CGGeneral/DefaultData").
Fixed bug that caused the duplicate line command to be done twice on OSX.
Added Save, Save All, Undo, Redo icons to the project view.
Ctrl+/ now toggles comments. Ctrl+Shift+/ hotkey removed.
Shift click to expand selection.
Alt+Left/Right arrows or Home/End now have an intermediate step. They will stop at the first non-whitespace character of a line.
Save confirmation when there are unsaved changes.
Added hotkey to focus on the file search box in the project view - Ctrl+T.
Added hotkey to close current file - Ctrl+W.
Fixed Theme menu.
You can now move the UnIDE directory anywhere in your Assets directory.
Temp files are cleaned up when a file is closed.
Missing files are removed from the list of opened files.


1.03 changes:
Switched multithreaded things to use ThreadPool instead of regular threads.
Added option to force generic auto complete for all files.
Added option to disable auto complete.
Fixed bug where scripts were sometimes reloaded when they shouldnt be.
Delete key now works.
Added hotkey to move to line start - Home or Alt+LeftArrow (can use shift to modify selection).
Added hotkey to move to line end - End or Alt+RightArrow (can use shift to modify selection).
Added hotkey to move to document start - Ctrl+Home (can use shift to modify selection).
Added hotkey to move to document end - Ctrl+End (can use shift to modify selection).
Added hotkey to duplicate line - Ctrl+D
Added hotkey to delete line - Ctrl+Shift+D
Added hotkey to comment selected lines - Ctrl+/
Added hotkey to uncomment selected lines - Ctrl+Shift+/

Windows Hotkeys:
Save - Ctrl+S or Ctrl+Alt+S
Undo - Ctrl+Z or Ctrl+Alt+Z
Redo - Ctrl+Shift+Z
Select All - Ctrl+A
Copy - Ctrl+C
Cut - Ctrl+X
Paste - Ctrl+V
Move To Line Start - Home or Alt+LeftArrow
Move To Line End - End or Alt+RightArrow
Move To Doc Start - Ctrl+Home
Move To Doc End - Ctrl+End
Duplicate Line - Ctrl+D
Delete Line - Ctrl+Shift+D
Toggle Comment Lines - Ctrl+/
Focus on file search field - Ctrl+T
Close current file Ctrl+W
Search Unity Docs - F1
Find Next - F3
Find Previous - Shift+F3

OSX Hotkeys:
Save - Command+S or Command+Alt+S
Undo - Command+Z or Command+Alt+Z
Redo - Command+Shift+Z
Select All - Command+A
Copy - Command+C
Cut - Command+X
Paste - Command+V
Move To Line Start - Home or Alt+LeftArrow
Move To Line End - End or Alt+RightArrow
Move To Doc Start - Command+Home
Move To Doc End - Command+End
Duplicate Line - Command+D
Delete Line - Command+Shift+D
Toggle Comment Lines - Command+/
Focus on file search field - Ctrl+T
Close current file Ctrl+W
Search Unity Docs - F1
Find Next - F3
Find Previous - Shift+F3

There is a right click menu which gives you access to basic text editing tools, as well as plugin commands such as "Search Unity Docs" and "Go To Declaration". To close tabs you can either right click and select "Close", or middle mouse click them. 

Holding control and using the Left and Right arrow keys will move the cursor to the previous/next text "element". Pressing Up or Down arrow keys while holding control will increment the cursors line position up or down in increments of 4 lines.

Holding Shift and using the arrow keys will move the cursor while expanding the text selection to include the cursors new position.

You can add custom fonts and pick them from the settings menu. Custom fonts go into UnIDE/Editor/TextEditorFonts/YourFont/. Be sure to include YourFont.ttf as well as YourFont_B.ttf, "_B" denotes that this is the bold variation of the font.

Notes:
In order to be able to use the standard Ctrl+S (Command+S on OSX) hotkey to save your current file, you must be unity loaded into a saved scene. You can also use the alternate save hotkey in the list above at any time.

If you are experiencing slowness while editing and you are using OSX, try enabling "Force Generic Completion" in the General Settings menu, or disable completion completely. Unfortunately there seems to be a bug in the version of Mono that Unity uses that cripples multithreaded tasks on OSX.

On rare occasions the Undo hotkey (Ctrl+Z, Command+Z on OSX) may stop working. Unfortunately I dont have much control over this because of the way Unity handles hotkeys. Closing and reopening UnIDE usually fixes this though.

In Unity 3.5 when targeting mobile or flash platforms you should go into the settings menu and uncheck "Force Dynamic Font" in the "Text" settings. You may get warnings about dynamic fonts not being supported, and you can go into the offending font import settings and change their "Character" setting from Dyanmic to Unicode. This does not effect Unity 4.0 or higher users.