;NSIS Fubi Installer
; Copyright (C) 2015 Felix Kistler 
; This software is distributed under the terms of the Eclipse Public License v1.0.
; A copy of the license may be obtained at: http://www.eclipse.org/org/documents/epl-v10.html

 Name "CopyLeapDll"
 OutFile "CopyLeapDll.exe"
 RequestExecutionLevel user
 
 !include "FileFunc.nsh"
 !insertmacro GetParameters

 Function .onInit
 ${GetParameters} $0
 
 
 FileOpen $1 "leapfilepath.lst" w
 IfErrors 0 +3
  MessageBox MB_OK "Unable to open leapfilepath.lst!"
  Goto end

 SearchPath $2 'leap.dll'
 IfErrors 0 +3
  MessageBox MB_OK "Leap DLL not found!"
  Goto +2
 FileWrite $1 'File "$2"$\r$\n'
 
  FileClose $1

 end:
 Quit
 FunctionEnd

 Section
 SectionEnd