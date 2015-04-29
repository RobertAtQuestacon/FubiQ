;NSIS Fubi Installer
; Copyright (C) 2014 Felix Kistler 
; This software is distributed under the terms of the Eclipse Public License v1.0.
; A copy of the license may be obtained at: http://www.eclipse.org/org/documents/epl-v10.html

 Name "CopyCVDlls"
 OutFile "CopyCVDlls.exe"
 RequestExecutionLevel user
 
 !include "FileFunc.nsh"
 !insertmacro GetParameters

 Function .onInit
 ${GetParameters} $0
 
 
 StrCmp $0 '' 0 +3
  MessageBox MB_OK "OpenCV version not specified!"
  Goto end

 FileOpen $1 "cvfilepaths.lst" w
 IfErrors 0 +3
  MessageBox MB_OK "Unable to open cvfilepaths.lst!"
  Goto end

 SearchPath $2 'opencv_core"$0".dll'
 IfErrors 0 +3
  MessageBox MB_OK "OpenCV Core DLL not found!"
  Goto +2
 FileWrite $1 'File "$2"$\r$\n'
 
 SearchPath $2 'opencv_highgui"$0".dll'
 IfErrors 0 +3
  MessageBox MB_OK "OpenCV Highgui DLL not found!"
  Goto +2
 FileWrite $1 'File "$2"$\r$\n'
 
 SearchPath $2 'opencv_imgproc"$0".dll'
 IfErrors 0 +3
  MessageBox MB_OK "OpenCV Imgproc DLL not found!"
  Goto +2
 FileWrite $1 'File "$2"$\r$\n'
 
 SearchPath $2 'opencv_imgcodecs"$0".dll'
 IfErrors 0 +3
  MessageBox MB_OK "OpenCV Imgcodecs DLL not found (Only needed for OpenCV 3.x)!"
  Goto +2
 FileWrite $1 'File "$2"$\r$\n'

 FileClose $1

 end:
 Quit
 FunctionEnd

 Section
 SectionEnd