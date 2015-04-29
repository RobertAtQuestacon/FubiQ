;SIS Fubi Installer
; Copyright (C) 2014-2015 Felix Kistler 
; This software is distributed under the terms of the Eclipse Public License v1.0.
; A copy of the license may be obtained at: http://www.eclipse.org/org/documents/epl-v10.html

;--------------------------------
;The installer uses the Modern UI interface, Copyright © 2002-2009 Joost Verburg
; License:
; This software is provided 'as-is', without any express or implied
; warranty. In no event will the authors be held liable for any damages
; arising from the use of this software.
; 
; Permission is granted to anyone to use this software for any purpose,
; including commercial applications, and to alter it and redistribute
; it freely, subject to the following restrictions:
; 
; 1. The origin of this software must not be misrepresented; 
;    you must not claim that you wrote the original software.
;    If you use this software in a product, an acknowledgment in the
;    product documentation would be appreciated but is not required.
; 2. Altered versions must be plainly marked as such,
;    and must not be misrepresented as being the original software.
; 3. This notice may not be removed or altered from any distribution. 
;--------------------------------


;--------------------------------
;Important configuration

 ;Fubi Version
 !define VERSIONMAJOR 0
 !define VERSIONMINOR 13
 !define VERSIONBUILD 1
 
 ;Where to put the uninstall registry information
 !define FUBI_UNINST_REG "Software\Microsoft\Windows\CurrentVersion\Uninstall\FUBI"
 ;And other information as the startmenu folder
 !define FUBI_REG "Software\FUBI"
 
 ;OpenCV config
 !define FUBI_USE_OPENCV ;define to use OpenCV
 !define FUBI_OPENCV_VERSION 2411 ;define the OpenCV version Fubi has been compiled against
 
 ;.Net version used in Fubi C# wrapper and GUI (as displayed in $WINDIR\Microsoft.NET\Framework)
 !define FUBI_NETVersion "4.0.30319"
 
 ;Download locations for dependencies
 !define FUBI_NETDownload "http://www.microsoft.com/download/details.aspx?id=17718"
 !define FUBI_VSCDownload "http://www.microsoft.com/en-us/download/details.aspx?id=40784"
 !define FUBI_KinectSDKDownload "http://www.microsoft.com/en-us/kinectforwindows/develop"
 !define FUBI_Kinect1SDKDownload "http://www.microsoft.com/en-us/download/details.aspx?id=40278"
 !define FUBI_Kinect1DTKDownload "http://www.microsoft.com/en-us/download/details.aspx?id=40276"
 !define FUBI_OpenNIDownload "http://code.google.com/p/simple-openni/downloads/list?can=1"
 
;--------------------------------
;Includes

  !include "FileFunc.nsh"
  !include "MUI2.nsh"
  !include "LogicLib.nsh"
  !include "nsDialogs.nsh"
  !include "Sections.nsh"
  
;--------------------------------
;General setup

  ;Name and file
  Name "FUBI"
  OutFile "Fubi_${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}_setup.exe"

  ;Default installation folder
  InstallDir "$PROGRAMFILES\FUBI"  

  ;Request admin privileges
  RequestExecutionLevel admin
  
  ; Compression method (lzma resulted in the smallest setup file)
  SetCompressor /SOLID lzma

;--------------------------------
;Interface Settings

  !define MUI_ABORTWARNING  
  !define MUI_COMPONENTSPAGE_SMALLDESC
  !define MUI_ICON "..\bin\Fubi_Logo.ico"
  !define MUI_UNICON "..\bin\Fubi_Logo.ico"
  !define MUI_STARTMENUPAGE_DEFAULTFOLDER "FUBI"
  !define MUI_STARTMENUPAGE_REGISTRY_ROOT "HKCU"
  !define MUI_STARTMENUPAGE_REGISTRY_KEY "${FUBI_REG}"
  !define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "Start Menu Folder"
  LangString DEPENDENCIES_PAGE_TITLE ${LANG_ENGLISH} "Fubi Dependencies"
  LangString DEPENDENCIES_PAGE_SUBTITLE ${LANG_ENGLISH} "Checking for Fubi dependencies"
  Var StartMenuFolder
  
;Section functions
!define disableSection "!insertmacro DisableSection"
!macro DisableSection sec
  Push "${sec}"
  Call DisableSectionFun
!macroend
Function DisableSectionFun
  Exch $0
  !insertmacro ClearSectionFlag $0 ${SF_SELECTED}
  !insertmacro SetSectionFlag $0 ${SF_RO}
  Pop $0   
FunctionEnd
  
;--------------------------------
;Pages and functions

  !insertmacro MUI_PAGE_LICENSE "license.txt"
  Page custom Dependencies_Page checkMissingDependencies
  !insertmacro MUI_PAGE_COMPONENTS
  !insertmacro MUI_PAGE_DIRECTORY
  !insertmacro MUI_PAGE_STARTMENU Application $StartMenuFolder
  !insertmacro MUI_PAGE_INSTFILES
    
  !insertmacro MUI_UNPAGE_CONFIRM
  !insertmacro MUI_UNPAGE_INSTFILES
  
 Var CustomDialog
 Var wnd
 Var yCounter
 Var font
 Var envString
 Var regKey
 Var requiredDllMissing
 Var uninstallPathPreviousVersion
 Var uninstallDirPreviousVersion
 Var selectedSensorSection
 
  
;--------------------------------
;Languages
 
  !insertmacro MUI_LANGUAGE "English"

;--------------------------------
;Installer Sections

Section "!Main" FubiMain
  SectionIn RO ;Mandatory component
  
  ; Uninstall previous version silently, but wait for it to finish
  StrCmp $uninstallPathPreviousVersion "" +5
	StrCmp $uninstallDirPreviousVersion "" +3
		ExecWait "$uninstallPathPreviousVersion /S _?=$uninstallDirPreviousVersion"
		Goto +2
		; Not possible, so uninstall in non-silent mode
		ExecWait "$uninstallPathPreviousVersion"	

  ;Fubi binaries, configurations and important dependencies
  SetOutPath "$INSTDIR\bin"
  File ..\bin\FubiNET.dll
  File ..\bin\Fubi_WPF_GUI.exe
  File ..\bin\Fubi_WPF_GUI.exe.config
  File ..\bin\FubiRecognizers.xsd
  File ..\bin\glut32.dll
  File ..\bin\LegPostureRecognizers.xml
  File ..\bin\MouseControlRecognizers.xml
  File ..\bin\RecognizerTest_2013.exe
  File ..\bin\Fubi_Logo.ico
  File ..\bin\SampleFingerRecognizers.xml
  File ..\bin\SampleRecognizers.xml
  File ..\bin\SamplesConfig.xml
  File ..\bin\KeyMouseBindings.xml
  ;OpenCV dependencies (trick for finding them in the system path)
  !ifdef FUBI_USE_OPENCV
   !execute "makensis.exe CopyCVDlls.nsi"
   !execute "CopyCVDlls.exe ${FUBI_OPENCV_VERSION}"
   !include /nonfatal "cvfilepaths.lst"
  !endif
  
  ;DTW training files
  SetOutPath "$INSTDIR\bin\trainingData"
  File ..\bin\trainingData\*.xml
  
  ;temp folder for saved images
  SetOutPath "$INSTDIR\bin\savedImages"
  
  ;documentation
  SetOutPath "$INSTDIR\doc"
  File /x log.txt ..\doc\*.*
  SetOutPath "$INSTDIR\doc\FubiAPI"
  File /nonfatal ..\doc\FubiAPI\*.*
  SetOutPath "$INSTDIR\doc\FubiXML"
  File /nonfatal ..\doc\FubiXML\*.*
  
  ;includes
  SetOutPath "$INSTDIR\include"
  File ..\include\*.h  
  SetOutPath "$INSTDIR\include\GL"
  File ..\include\GL\glut.h
  
  ;libraries
  SetOutPath "$INSTDIR\lib"
  File ..\lib\*.lib
  
  ;Licenses and Readme
  SetOutPath "$INSTDIR"
  File ..\license-epl.html
  File ..\Readme.txt
  File ..\Changelog.txt
  File OpenCV.License.txt
  File OpenNI2.License.txt
  
  ;Shortcuts
  SetOutPath "$INSTDIR\bin" ;This is important as it will become the working directory!
  CreateShortCut "$DESKTOP\Fubi GUI.lnk" "$INSTDIR\bin\Fubi_WPF_GUI.exe" "" "$INSTDIR\bin\Fubi_Logo.ico"
  !insertmacro MUI_STARTMENU_WRITE_BEGIN Application
   CreateDirectory "$SMPROGRAMS\$StartMenuFolder"
   CreateShortCut "$SMPROGRAMS\$StartMenuFolder\Fubi GUI.lnk" "$INSTDIR\bin\Fubi_WPF_GUI.exe" "" "$INSTDIR\bin\Fubi_Logo.ico"
   CreateShortCut "$SMPROGRAMS\$StartMenuFolder\Fubi RecognizerTest.lnk" "$INSTDIR\bin\RecognizerTest_2013.exe"
   CreateShortCut "$SMPROGRAMS\$StartMenuFolder\API Documentation.lnk" "$INSTDIR\doc\FubiAPI\index.html"
   CreateShortCut "$SMPROGRAMS\$StartMenuFolder\XML Documentation.lnk" "$INSTDIR\doc\FubiXML\index.html"
   CreateShortCut "$SMPROGRAMS\$StartMenuFolder\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
  !insertmacro MUI_STARTMENU_WRITE_END
SectionEnd

SectionGroup "Fubi supported sensors" FubiSensors
Section "NO SENSOR" FubiNoSensor
  SetOutPath "$INSTDIR\bin"
  File ..\bin\Fubi.dll
SectionEnd

Section /o "OpenNI v1" FubiOpenNI1
  SetOutPath "$INSTDIR\bin"
  File /oname=Fubi.dll ..\bin\Fubi_OpenNI1.dll
SectionEnd

Section /o "OpenNI v2" FubiOpenNI2
  SetOutPath "$INSTDIR\bin"
  File /oname=Fubi.dll ..\bin\Fubi_OpenNI2.dll
  ;OpenNI dll, config and drivers
  File ..\bin\OpenNI.ini
  File ..\bin\OpenNI2.dll
  
  SetOutPath "$INSTDIR\bin\OpenNI2\Drivers"
  File ..\bin\OpenNI2\Drivers\*.*
   
  ;Copy in NiTE dependencies as well
  ReadEnvStr $envString "NITE2_REDIST"
  IfErrors NoNite 0
    CopyFiles /SILENT "$envString\*.*" "$INSTDIR\bin"
  NoNite: 
SectionEnd

Section /o "Kinect SDK v1" FubiKinectSDK
  SetOutPath "$INSTDIR\bin"
  File /oname=Fubi.dll ..\bin\Fubi_KinectSDK1.dll
  ; Copy in FaceTrack stuff
  ReadEnvStr $envString "FTSDK_DIR"
  IfErrors NoFTSDK 0
   CopyFiles /SILENT $envStringRedist\x86\FaceTrack*.dll $INSTDIR\bin
  NoFTSDK:
SectionEnd

Section /o "Kinect SDK v2" FubiKinectSDK2
  SetOutPath "$INSTDIR\bin"
  File /oname=Fubi.dll ..\bin\Fubi_KinectSDK2.dll
  ; Copy in Kinect2 face stuff
  ReadEnvStr $envString "KINECTSDK20_DIR"
  IfErrors NoKSDK2 0
   CopyFiles /SILENT $envStringRedist\Face\x86\Kinect20.Face.dll $INSTDIR\bin
   CopyFiles /SILENT $envStringRedist\Face\x86\NuiDatabase\*.* $INSTDIR\bin\NuiDatabase
  NoKSDK2:
SectionEnd

Section /o "OpenNI1+2 & KinectSDK1" FubiPreKinectSDK2
  SetOutPath "$INSTDIR\bin"
  File /oname=Fubi.dll ..\bin\Fubi_PreKinectSDK2.dll
  
  ;OpenNI dll, config and drivers
  File ..\bin\OpenNI.ini
  File ..\bin\OpenNI2.dll
  
  SetOutPath "$INSTDIR\bin\OpenNI2\Drivers"
  File ..\bin\OpenNI2\Drivers/\*.*
   
  ;Copy in NiTE dependencies as well
  ReadEnvStr $envString "NITE2_REDIST"
  IfErrors NoNite 0
    CopyFiles /SILENT "$envString\*.*" "$INSTDIR\bin"
  NoNite: 
  
  ; Copy in FaceTrack stuff
  ReadEnvStr $envString "FTSDK_DIR"
  IfErrors NoFTSDK 0
   CopyFiles /SILENT $envStringRedist\x86\FaceTrack*.dll $INSTDIR\bin
  NoFTSDK:
SectionEnd

Section /o "KinectSDK2 & Leap" FubiKinectSDK2Leap
  SetOutPath "$INSTDIR\bin"
  File /oname=Fubi.dll ..\bin\Fubi_KinectSDK2_Leap.dll
  ;Leap.dll (trick for finding them in the system path)
  !execute "makensis.exe CopyLeapDll.nsi"
  !execute "CopyLeapDll.exe"
  !include /nonfatal "leapfilepath.lst"
  
   ; Copy in Kinect2 face stuff
  ReadEnvStr $envString "KINECTSDK20_DIR"
  IfErrors NoKSDK2 0
   CopyFiles /SILENT $envStringRedist\Face\x86\Kinect20.Face.dll $INSTDIR\bin
   CopyFiles /SILENT $envStringRedist\Face\x86\NuiDatabase\*.* $INSTDIR\bin\NuiDatabase
  NoKSDK2:
SectionEnd

Section /o "All Sensors" FubiAll
  SetOutPath "$INSTDIR\bin"
  File /oname=Fubi.dll ..\bin\Fubi_All.dll
  ;Leap.dll (trick for finding them in the system path)
  !execute "makensis.exe CopyLeapDll.nsi"
  !execute "CopyLeapDll.exe"
  !include /nonfatal "leapfilepath.lst"
  
  ;OpenNI dll, config and drivers
  File ..\bin\OpenNI.ini
  File ..\bin\OpenNI2.dll
  
  SetOutPath "$INSTDIR\bin\OpenNI2\Drivers"
  File ..\bin\OpenNI2\Drivers/\*.*
   
  ;Copy in NiTE dependencies as well
  ReadEnvStr $envString "NITE2_REDIST"
  IfErrors NoNite 0
    CopyFiles /SILENT "$envString\*.*" "$INSTDIR\bin"
  NoNite: 
  
  ; Copy in FaceTrack stuff
  ReadEnvStr $envString "FTSDK_DIR"
  IfErrors NoFTSDK 0
   CopyFiles /SILENT $envStringRedist\x86\FaceTrack*.dll $INSTDIR\bin
  NoFTSDK:
  
   ; Copy in Kinect2 face stuff
  ReadEnvStr $envString "KINECTSDK20_DIR"
  IfErrors NoKSDK2 0
   CopyFiles /SILENT $envStringRedist\Face\x86\Kinect20.Face.dll $INSTDIR\bin
   CopyFiles /SILENT $envStringRedist\Face\x86\NuiDatabase\*.* $INSTDIR\bin\NuiDatabase
  NoKSDK2:
SectionEnd
SectionGroupEnd

Section "Fubi Sources" FubiSources
  ;VS solution
  SetOutPath "$INSTDIR"
  File ..\FUBI.sln
  File ..\FUBI_2013.sln
  File ..\Fubi.workspace
  
  ;Main Fubi sources and VS project
  SetOutPath "$INSTDIR\Fubi"
  File ..\Fubi\*.cpp
  File ..\Fubi\*.h
  File ..\Fubi\Fubi.cbp
  File ..\Fubi\Fubi.vcxproj
  File ..\Fubi\Fubi.vcxproj.filters
  File ..\Fubi\Fubi.vcxproj.user
  File ..\Fubi\Fubi_2013.vcxproj
  File ..\Fubi\Fubi_2013.vcxproj.filters
  File ..\Fubi\Fubi_2013.vcxproj.user
  File ..\Fubi\rapidxml.hpp
  File ..\Fubi\rapidxml_print.hpp
  
  ;Include templated FubiConfigs
  File ..\Fubi\FubiConfig_OpenNI1.h
  File ..\Fubi\FubiConfig_OpenNI2.h
  File ..\Fubi\FubiConfig_KinectSDK.h
  File ..\Fubi\FubiConfig_KinectSDK2.h
  File ..\Fubi\FubiConfig_PreKinectSDK2.h
  File ..\Fubi\FubiConfig_KinectSDK2Leap.h
  File ..\Fubi\FubiConfig_All.h
  ;Overwrite FubiConfig.h depending on the selected sensor combination
  StrCmp $selectedSensorSection ${FubiOpenNI1} 0 +3 
   File /oname=FubiConfig.h ..\Fubi\FubiConfig_OpenNI1.h
   Goto FubiConfigFinished
  StrCmp $selectedSensorSection ${FubiOpenNI2} 0 +3 
   File /oname=FubiConfig.h ..\Fubi\FubiConfig_OpenNI2.h
   Goto FubiConfigFinished
  StrCmp $selectedSensorSection ${FubiKinectSDK} 0 +3 
   File /oname=FubiConfig.h ..\Fubi\FubiConfig_KinectSDK.h
   Goto FubiConfigFinished
  StrCmp $selectedSensorSection ${FubiKinectSDK2} 0 +3 
   File /oname=FubiConfig.h ..\Fubi\FubiConfig_KinectSDK2.h
   Goto FubiConfigFinished
  StrCmp $selectedSensorSection ${FubiPreKinectSDK2} 0 +3 
   File /oname=FubiConfig.h ..\Fubi\FubiConfig_PreKinectSDK2.h
   Goto FubiConfigFinished
  StrCmp $selectedSensorSection ${FubiKinectSDK2Leap} 0 +3 
   File /oname=FubiConfig.h ..\Fubi\FubiConfig_KinectSDK2Leap.h
   Goto FubiConfigFinished
  StrCmp $selectedSensorSection ${FubiAll} 0 +3 
   File /oname=FubiConfig.h ..\Fubi\FubiConfig_All.h
   Goto FubiConfigFinished  
  FubiConfigFinished:
  
  ;Gestures recognizers defined in code
  SetOutPath "$INSTDIR\Fubi\GestureRecognizer"
  File ..\Fubi\GestureRecognizer\*.cpp
  File ..\Fubi\GestureRecognizer\*.h
  
  ;C# wrapper
  SetOutPath "$INSTDIR\FubiNET"
  File ..\FubiNET\*.cs
  File ..\FubiNET\FubiNET.csproj
  
  ;Installer
  SetOutPath "$INSTDIR\Installer"
  File ..\Installer\*.nsi
  File ..\Installer\*.txt
  File ..\Installer\*.bat
  
  ;C++ sample
  SetOutPath "$INSTDIR\Samples\RecognizerTest"
  File ..\Samples\RecognizerTest\main.cpp
  File ..\Samples\RecognizerTest\RecognizerTest.cbp
  File ..\Samples\RecognizerTest\RecognizerTest.vcxproj
  File ..\Samples\RecognizerTest\RecognizerTest.vcxproj.filters
  File ..\Samples\RecognizerTest\RecognizerTest.vcxproj.user
  File ..\Samples\RecognizerTest\RecognizerTest_2013.vcxproj
  File ..\Samples\RecognizerTest\RecognizerTest_2013.vcxproj.filters
  File ..\Samples\RecognizerTest\RecognizerTest_2013.vcxproj.user  
  
  ;C# sample
  SetOutPath "$INSTDIR\Samples\Fubi_WPF_GUI"
  File ..\Samples\Fubi_WPF_GUI\*.xaml
  File ..\Samples\Fubi_WPF_GUI\*.cs
  File ..\Samples\Fubi_WPF_GUI\Fubi_WPF_GUI.csproj
  File ..\Samples\Fubi_WPF_GUI\Fubi_WPF_GUI.csproj.user
  File ..\Samples\Fubi_WPF_GUI\app.config
  ;Separate folder for xml generator code
  SetOutPath "$INSTDIR\Samples\Fubi_WPF_GUI\FubiXMLGenerator"
  File ..\Samples\Fubi_WPF_GUI\FubiXMLGenerator\*.xaml
  File ..\Samples\Fubi_WPF_GUI\FubiXMLGenerator\*.cs
  ;UpDownCtrls
  SetOutPath "$INSTDIR\Samples\Fubi_WPF_GUI\UpDownCtrls"
  File ..\Samples\Fubi_WPF_GUI\UpDownCtrls\*.xaml
  File ..\Samples\Fubi_WPF_GUI\UpDownCtrls\*.cs
  ;Images for GUI
  SetOutPath "$INSTDIR\Samples\Fubi_WPF_GUI\Images"
  ;File /nonfatal ..\Samples\Fubi_WPF_GUI\Images\*.jpg
  File /nonfatal ..\Samples\Fubi_WPF_GUI\Images\*.png
  File /nonfatal ..\Samples\Fubi_WPF_GUI\Images\*.ico
  ;Properties
  SetOutPath "$INSTDIR\Samples\Fubi_WPF_GUI\Properties"
  File ..\Samples\Fubi_WPF_GUI\Properties\*.cs
  File ..\Samples\Fubi_WPF_GUI\Properties\*.resx
  File ..\Samples\Fubi_WPF_GUI\Properties\*.settings
  ;GUI themes
  SetOutPath "$INSTDIR\Samples\Fubi_WPF_GUI\Themes"
  File ..\Samples\Fubi_WPF_GUI\Themes\*.xaml
SectionEnd

Section "-InfoForUnistall"
 ;Create uninstaller
 WriteUninstaller "$INSTDIR\Uninstall.exe"
  
 ;Registry information for add/remove programs
 WriteRegStr HKLM "${FUBI_UNINST_REG}" "DisplayName" "FUBI"
 WriteRegStr HKLM "${FUBI_UNINST_REG}" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
 WriteRegStr HKLM "${FUBI_UNINST_REG}" "QuietUninstallString" "$\"$INSTDIR\Uninstall.exe$\" /S"
 WriteRegStr HKLM "${FUBI_UNINST_REG}" "InstallLocation" "$INSTDIR"
 WriteRegStr HKLM "${FUBI_UNINST_REG}" "DisplayIcon" "$INSTDIR\bin\Fubi_Logo.ico"
 WriteRegStr HKLM "${FUBI_UNINST_REG}" "Publisher" "Uni Augsburg"
 WriteRegStr HKLM "${FUBI_UNINST_REG}" "HelpLink" "http://www.hcm-lab.de/fubi.html"
 WriteRegStr HKLM "${FUBI_UNINST_REG}" "DisplayVersion" "${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}"
 WriteRegDWORD HKLM "${FUBI_UNINST_REG}" "VersionMajor" ${VERSIONMAJOR}
 WriteRegDWORD HKLM "${FUBI_UNINST_REG}" "VersionMinor" ${VERSIONMINOR}
 WriteRegDWORD HKLM "${FUBI_UNINST_REG}" "VersionBuild" ${VERSIONBUILD}
 ;There is no option for modifying or repairing the install
 WriteRegDWORD HKLM "${FUBI_UNINST_REG}" "NoModify" 1
 WriteRegDWORD HKLM "${FUBI_UNINST_REG}" "NoRepair" 1
 ;Calculate the size of the installation directory
 ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
 IntFmt $0 "0x%08X" $0
 ;Now set the INSTALLSIZE in the registry so Add/Remove Programs can accurately report the size
 WriteRegDWORD HKLM "${FUBI_UNINST_REG}" "EstimatedSize" "$0"
SectionEnd

;--------------------------------
;Uninstaller Section

Section "Uninstall"
  ;Delete shortcuts
  !insertmacro MUI_STARTMENU_GETFOLDER Application $StartMenuFolder
  Delete "$DESKTOP\Fubi GUI.lnk"
  Delete "$SMPROGRAMS\$StartMenuFolder\Fubi GUI.lnk"
  Delete "$SMPROGRAMS\$StartMenuFolder\Fubi RecognizerTest.lnk"
  Delete "$SMPROGRAMS\$StartMenuFolder\Uninstall.lnk"
  Delete "$SMPROGRAMS\$StartMenuFolder\API Documentation.lnk"
  Delete "$SMPROGRAMS\$StartMenuFolder\XML Documentation.lnk"
  RMDir "$SMPROGRAMS\$StartMenuFolder"
  
  ;Simply remove the whole installation dir (may delete additional user data, but this shouldn't be here anyway)
  RMDir /r "$INSTDIR"
  
  ;Delete the whole registry folder for uninstall information with all entries 
  DeleteRegKey HKLM "${FUBI_UNINST_REG}"
  
  ;And the key for other information
  DeleteRegKey HKCU "${FUBI_REG}"
SectionEnd


Function Dependencies_Page
  !insertmacro MUI_HEADER_TEXT $(DEPENDENCIES_PAGE_TITLE) $(DEPENDENCIES_PAGE_SUBTITLE)
  nsDialogs::Create 1018
  Pop $CustomDialog
  ${If} $CustomDialog == error
    Abort
  ${Endif}
  
  IntOp $requiredDllMissing 0 +
  IntOp $yCounter 0 +
  CreateFont $font "" 8 0
  
  IfFileExists "$WINDIR\Microsoft.NET\Framework\v${FUBI_NETVersion}" NETFrameworkInstalled 0
   IntOp $requiredDllMissing $requiredDllMissing + 1
   ${NSD_CreateLabel} 0 "$yCounteru" 100% 9u "Did not find .Net Framework! For running FUBI GUI, please install .Net Framework v${FUBI_NETVersion} from:"
   Pop $wnd
   IntOp $yCounter  $yCounter + 9
   SendMessage $wnd ${WM_SETFONT} $font 0
   SetCtlColors $wnd "0x990000" transparent   
   ${NSD_CreateLink} 0 "$yCounteru" 100% 9u ${FUBI_NETDownload}
   Pop $wnd
   ${NSD_OnClick} $wnd openNetLink
   IntOp $yCounter $yCounter + 9
   Goto NETDone
  NETFrameworkInstalled:
   ${NSD_CreateLabel} 0 "$yCounteru" 100% 9u ".Net Framework found!"
   Pop $wnd
   SendMessage $wnd ${WM_SETFONT} $font 0
   SetCtlColors $wnd "0x009900" transparent  
   IntOp $yCounter $yCounter + 9
  NETDone:
   IntOp $yCounter $yCounter + 2
   
  ReadRegDword $regKey HKLM "SOFTWARE\Microsoft\VisualStudio\10.0\VC\VCRedist\x86" "Installed"
  IfErrors VSCErrors VSCInstalled
  VSCErrors:
   IntOp $requiredDllMissing $requiredDllMissing + 1
   ${NSD_CreateLabel} 0 "$yCounteru" 100% 9u "Did not find Visual C++ Redist! Please install from the following link:"
   Pop $wnd
   IntOp $yCounter  $yCounter + 9
   SendMessage $wnd ${WM_SETFONT} $font 0
   SetCtlColors $wnd "0x990000" transparent   
   ${NSD_CreateLink} 0 "$yCounteru" 100% 9u ${FUBI_VSCDownload}
   Pop $wnd
   ${NSD_OnClick} $wnd openVSCLink
   IntOp $yCounter $yCounter + 9
   Goto VSCDone
  VSCInstalled:
   ${If} $regKey <> 1
    Goto VSCErrors
   ${Endif}
   ${NSD_CreateLabel} 0 "$yCounteru" 100% 9u "Visual C++ Redist found!"
   Pop $wnd
   SendMessage $wnd ${WM_SETFONT} $font 0
   SetCtlColors $wnd "0x009900" transparent  
   IntOp $yCounter $yCounter + 9
  VSCDone:
   IntOp $yCounter $yCounter + 2   
   
   !insertmacro SectionFlagIsSet ${FubiOpenNI2} ${SF_RO} NiteInActive NiteActive
   NiteInActive:
    ${NSD_CreateLabel} 0 "$yCounteru" 100% 9u "Did not find NiTE2! You can copy the libs from SimpleOpenNI 1.96 to the FUBI bin folder:"
    Pop $wnd
    IntOp $yCounter $yCounter + 9
	SendMessage $wnd ${WM_SETFONT} $font 0
    SetCtlColors $wnd "0x999900" transparent
    ${NSD_CreateLink} 0 "$yCounteru" 100% 9u ${FUBI_OpenNIDownload}
    Pop $wnd
	${NSD_OnClick} $wnd openOpenNILink
    IntOp $yCounter $yCounter + 9
	Goto NiteDone
   NiteActive:    
	${NSD_CreateLabel} 0 "$yCounteru" 100% 9u "NiTE2 middleware found!"
    Pop $wnd
    SendMessage $wnd ${WM_SETFONT} $font 0
    SetCtlColors $wnd "0x009900" transparent  
    IntOp $yCounter $yCounter + 9
   NiteDone:
   IntOp $yCounter $yCounter + 2
  
   !insertmacro SectionFlagIsSet ${FubiOpenNI1} ${SF_RO} OpenNIInActive OpenNIActive
   OpenNIInActive:
    ${NSD_CreateLabel} 0 "$yCounteru" 100% 9u "Did not find OpenNI/NiTE 1.x! You can install it using OpenNI_NITE_Installer-win32-0.27:"
    Pop $wnd
    IntOp $yCounter  $yCounter + 9
    SendMessage $wnd ${WM_SETFONT} $font 0
    SetCtlColors $wnd "0x999900" transparent
    ${NSD_CreateLink} 0 "$yCounteru" 100% 9u ${FUBI_OpenNIDownload}
    Pop $wnd
    ${NSD_OnClick} $wnd openOpenNILink
    IntOp $yCounter $yCounter + 9
	Goto OpenNIDone
   OpenNIActive:
	${NSD_CreateLabel} 0 "$yCounteru" 100% 9u "OpenNI 1.x and NiTE1 middleware found!"
    Pop $wnd
    SendMessage $wnd ${WM_SETFONT} $font 0
    SetCtlColors $wnd "0x009900" transparent  
    IntOp $yCounter $yCounter + 9
   OpenNIDone:
   IntOp $yCounter $yCounter + 2

   !insertmacro SectionFlagIsSet ${FubiKinectSDK} ${SF_RO} KinectInactive KinectActive
   KinectInactive:
    ${NSD_CreateLabel} 0 "$yCounteru" 100% 9u "Did not find Kinect SDK! You can install Kinect SDK 1.8 and Developer Toolkit 1.7 from:"
    Pop $wnd
    IntOp $yCounter  $yCounter + 9
    SendMessage $wnd ${WM_SETFONT} $font 0
    SetCtlColors $wnd "0x999900" transparent   
    ${NSD_CreateLink} 0 "$yCounteru" 100% 9u ${FUBI_Kinect1SDKDownload}
    Pop $wnd
    ${NSD_OnClick} $wnd openKinect1SDKLink
    IntOp $yCounter $yCounter + 9
	${NSD_CreateLink} 0 "$yCounteru" 100% 9u ${FUBI_Kinect1DTKDownload}
    Pop $wnd
    ${NSD_OnClick} $wnd openKinect1DTKLink
    IntOp $yCounter $yCounter + 9
	Goto KinectDone
   KinectActive:
	${NSD_CreateLabel} 0 "$yCounteru" 100% 9u "Kinect SDK including Face Tracking lib found!"
    Pop $wnd
    SendMessage $wnd ${WM_SETFONT} $font 0
    SetCtlColors $wnd "0x009900" transparent  
    IntOp $yCounter $yCounter + 9
   KinectDone:
   IntOp $yCounter $yCounter + 2

   !insertmacro SectionFlagIsSet ${FubiKinectSDK2} ${SF_RO} Kinect2Inactive Kinect2Active
   Kinect2Inactive:
    ${NSD_CreateLabel} 0 "$yCounteru" 100% 9u "Did not find Kinect2 SDK! You can install Kinect for Windows SDK 2.0 from:"
    Pop $wnd
    IntOp $yCounter  $yCounter + 9
    SendMessage $wnd ${WM_SETFONT} $font 0
    SetCtlColors $wnd "0x990000" transparent   
    ${NSD_CreateLink} 0 "$yCounteru" 100% 9u ${FUBI_KinectSDKDownload}
    Pop $wnd
    ${NSD_OnClick} $wnd openKinectSDKLink
    IntOp $yCounter $yCounter + 9
	Goto Kinect2Done
   Kinect2Active:
	${NSD_CreateLabel} 0 "$yCounteru" 100% 9u "Kinect SDK 2 found!"
    Pop $wnd
    SendMessage $wnd ${WM_SETFONT} $font 0
    SetCtlColors $wnd "0x009900" transparent  
    IntOp $yCounter $yCounter + 9
   Kinect2Done:
   IntOp $yCounter $yCounter + 2
   
  nsDialogs::Show  
FunctionEnd

Function checkMissingDependencies
 ${If} $requiredDllMissing  > 0
  MessageBox MB_YESNO "You have missing dependencies $\nIf you continue, Fubi might not work! $\nDo you really want to continue?" /SD IDYES IDNO CancelInstall
   Goto IgnoreDeps
  CancelInstall:
   Abort
  IgnoreDeps:
 ${Endif}
FunctionEnd

;Functions for opening links
Function openNetLink
  Pop $0
  ExecShell "open" ${FUBI_NETDownload}
FunctionEnd
Function openVSCLink
  Pop $0
  ExecShell "open" ${FUBI_VSCDownload}
FunctionEnd
Function openKinectSDKLink
  Pop $0
  ExecShell "open" ${FUBI_KinectSDKDownload}
FunctionEnd
Function openKinect1SDKLink
  Pop $0
  ExecShell "open" ${FUBI_Kinect1SDKDownload}
FunctionEnd
Function openKinect1DTKLink
  Pop $0
  ExecShell "open" ${FUBI_Kinect1DTKDownload}
FunctionEnd
Function openOpenNILink
  Pop $0
  ExecShell "open" ${FUBI_OpenNIDownload}
FunctionEnd

Function .onInit
	; Check for older installed version
	ReadRegStr $uninstallPathPreviousVersion HKLM "${FUBI_UNINST_REG}" "UninstallString"
	IfErrors continueInstallation 0
	StrCmp $uninstallPathPreviousVersion "" continueInstallation
	ReadRegStr $uninstallDirPreviousVersion HKLM "${FUBI_UNINST_REG}" "InstallLocation"
	IfErrors 0 0
	; Compare versions
	IntOp $R5 0 + ; installed version older
	ReadRegDWORD $R2 HKLM "${FUBI_UNINST_REG}" "VersionMajor"
	ReadRegDWORD $R3 HKLM "${FUBI_UNINST_REG}" "VersionMinor"
	ReadRegDWORD $R4 HKLM "${FUBI_UNINST_REG}" "VersionBuild"
	IfErrors 0 0
	${If} $R2 == ${VERSIONMAJOR}
		${If} $R3 == ${VERSIONMINOR}
			${If} $R4 == ${VERSIONBUILD}
				IntOp $R5 2 + ; installed version equal
			${ElseIf} $R4 > ${VERSIONBUILD}
				IntOp $R5 1 + ; installed version newer
			${EndIf}
		${ElseIf} $R3 > ${VERSIONMINOR}
			IntOp $R5 1 + ; installed version newer
		${EndIf}
	${ElseIf} $R2 > ${VERSIONMAJOR}
		IntOp $R5 1 + ; installed version newer
	${EndIf}
	; Handle installed version
	${If} $R5 == 1 ; installed version newer
		MessageBox MB_OK|MB_ICONSTOP \
			"A newer version of FUBI (V.$R2.$R3.$R4) is already installed at $uninstallDirPreviousVersion.$\n \
			Please uninstall this version manually and restart the installation if you really want to downgrade."
	${ElseIf} $R5 == 2 ; installed version equal
		MessageBox MB_YESNO|MB_ICONEXCLAMATION \
			"The same version of FUBI is already installed at $uninstallDirPreviousVersion. $\n \
			Do you want repair or modify this installation by reinstalling?" \
			/SD IDNO IDYES continueInstallation
	${Else} ; installed version older (default case)
		MessageBox MB_YESNO|MB_ICONINFORMATION \
			"To install FUBI, the previous version needs to be removed first.$\n \
			Do you want to continue?" \
			/SD IDYES IDYES continueInstallation
	${EndIf}
	; Abort the installation
	Abort
continueInstallation:
  ;Deactivate sensors group (selects all sensor options!)
  !insertmacro SetSectionFlag ${FubiSensors} ${SF_RO}
  ;Correct sensor selection by deselecting all sensors
  !insertmacro UnselectSection ${FubiNoSensor}
  !insertmacro UnselectSection ${FubiOpenNI1}
  !insertmacro UnselectSection ${FubiOpenNI2}
  !insertmacro UnselectSection ${FubiKinectSDK}
  !insertmacro UnselectSection ${FubiKinectSDK2}
  !insertmacro UnselectSection ${FubiPreKinectSDK2}
  !insertmacro UnselectSection ${FubiKinectSDK2Leap}
  !insertmacro UnselectSection ${FubiAll}
  ;Now check which sensor dependencies 
  ReadEnvStr $envString "NITE2_REDIST"
  IfErrors 0 NiteFound
   ${disableSection} ${FubiOpenNI2}
   ${disableSection} ${FubiPreKinectSDK2}
   ${disableSection} ${FubiAll}	
  NiteFound:  
   ReadEnvStr $envString "OPEN_NI_BIN"
   IfErrors 0 OpenNIFound
  OpenNIError:
   ${disableSection} ${FubiOpenNI1}
   ${disableSection} ${FubiPreKinectSDK2}
   ${disableSection} ${FubiAll}
   Goto OpenNIFinished
  OpenNIFound:
    ReadEnvStr $envString "XN_NITE_INSTALL_PATH"
    IfErrors OpenNIError 0
   OpenNIFinished:
   ReadEnvStr $envString "KINECTSDK10_DIR"
   IfErrors KinectError KinectFound
   KinectError:
	${disableSection} ${FubiKinectSDK}
	${disableSection} ${FubiPreKinectSDK2}
	${disableSection} ${FubiAll}
	Goto KinectFinished
   KinectFound:
    ReadEnvStr $envString "FTSDK_DIR"
    IfErrors KinectError 0
   KinectFinished:
   ReadEnvStr $envString "KINECTSDK20_DIR"
   IfErrors 0 Kinect2Finished
	${disableSection} ${FubiKinectSDK2}
	${disableSection} ${FubiKinectSDK2Leap}
	${disableSection} ${FubiAll}
   Kinect2Finished:
  ; Select "best" sensor combinations  
  !insertmacro SectionFlagIsSet ${FubiAll} ${SF_RO} AllSensorsImpossible ""
    !insertmacro SelectSection ${FubiAll}
    StrCpy $selectedSensorSection ${FubiAll}
    Goto BestSelectionFinished
  AllSensorsImpossible:  
  !insertmacro SectionFlagIsSet ${FubiKinectSDK2Leap} ${SF_RO} FubiKinectSDK2LeapImpossible ""
    !insertmacro SelectSection ${FubiKinectSDK2Leap}
    StrCpy $selectedSensorSection ${FubiKinectSDK2Leap}
    Goto BestSelectionFinished
  FubiKinectSDK2LeapImpossible:  
  !insertmacro SectionFlagIsSet ${FubiKinectSDK2} ${SF_RO} FubiKinectSDK2Impossible ""
    !insertmacro SelectSection ${FubiKinectSDK2}
    StrCpy $selectedSensorSection ${FubiKinectSDK2}
    Goto BestSelectionFinished
  FubiKinectSDK2Impossible:  
  !insertmacro SectionFlagIsSet ${FubiPreKinectSDK2} ${SF_RO} FubiPreKinectSDK2Impossible ""
    !insertmacro SelectSection ${FubiPreKinectSDK2}
    StrCpy $selectedSensorSection ${FubiPreKinectSDK2}
    Goto BestSelectionFinished
  FubiPreKinectSDK2Impossible:  
  !insertmacro SectionFlagIsSet ${FubiKinectSDK} ${SF_RO} FubiKinectSDKImpossible ""
    !insertmacro SelectSection ${FubiKinectSDK}
    StrCpy $selectedSensorSection ${FubiKinectSDK}
    Goto BestSelectionFinished
  FubiKinectSDKImpossible:  
  !insertmacro SectionFlagIsSet ${FubiOpenNI2} ${SF_RO} FubiOpenNI2Impossible ""
    !insertmacro SelectSection ${FubiOpenNI2}
    StrCpy $selectedSensorSection ${FubiOpenNI2}
    Goto BestSelectionFinished
  FubiOpenNI2Impossible:  
  !insertmacro SectionFlagIsSet ${FubiOpenNI1} ${SF_RO} FubiOpenNI1Impossible ""
    !insertmacro SelectSection ${FubiOpenNI1}
    StrCpy $selectedSensorSection ${FubiOpenNI1}
    Goto BestSelectionFinished
  FubiOpenNI1Impossible:
  !insertmacro SelectSection ${FubiNoSensor}
  !insertmacro SetSectionFlag ${FubiNoSensor} ${SF_BOLD}
  !insertmacro SetSectionFlag ${FubiSensors} ${SF_EXPAND}  
  StrCpy $selectedSensorSection ${FubiNoSensor}
  Goto KeepNoSensor
  BestSelectionFinished:
  !insertmacro RemoveSection ${FubiNoSensor}
  KeepNoSensor:
FunctionEnd

Function .onSelChange
  !insertmacro StartRadioButtons $selectedSensorSection
    !insertmacro RadioButton ${FubiNoSensor}
    !insertmacro RadioButton ${FubiOpenNI1}
    !insertmacro RadioButton ${FubiOpenNI2}
    !insertmacro RadioButton ${FubiKinectSDK}
	!insertmacro RadioButton ${FubiKinectSDK2}
	!insertmacro RadioButton ${FubiPreKinectSDK2}
	!insertmacro RadioButton ${FubiKinectSDK2Leap}
	!insertmacro RadioButton ${FubiAll}
  !insertmacro EndRadioButtons	
FunctionEnd

;Description for the different sections
LangString DESC_FubiMain ${LANG_ENGLISH} "Precompiled FUBI library, GUI and sample application"
LangString DESC_FubiSensors ${LANG_ENGLISH} "FUBI supported sensor combinations"
LangString DESC_FubiNoSensor ${LANG_ENGLISH} "No Sensor (Fubi non-functional)"
LangString DESC_FubiOpenNI1 ${LANG_ENGLISH} "OpenNI v.1 support"
LangString DESC_FubiOpenNI2 ${LANG_ENGLISH} "OpenNI v.2 support"
LangString DESC_FubiKinectSDK1 ${LANG_ENGLISH} "Kinect SDK v.1 support"
LangString DESC_FubiKinectSDK2 ${LANG_ENGLISH} "Kinect SDK v.2 support"
LangString DESC_FubiPreKinectSDK2 ${LANG_ENGLISH} "OpenNI v.1+2 & Kinect SDK 1 support"
LangString DESC_FubiKinectSDK2Leap ${LANG_ENGLISH} "Kinect SDK 2 & Leap support"
LangString DESC_FubiAll ${LANG_ENGLISH} "All Sensors supported"
LangString DESC_FubiSources ${LANG_ENGLISH} "FUBI source files if you want to compile it yourself"
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${FubiMain} $(DESC_FubiMain)
  !insertmacro MUI_DESCRIPTION_TEXT ${FubiSensors} $(DESC_FubiSensors)
  !insertmacro MUI_DESCRIPTION_TEXT ${FubiNoSensor} $(DESC_FubiNoSensor)
  !insertmacro MUI_DESCRIPTION_TEXT ${FubiOpenNI1} $(DESC_FubiOpenNI1)
  !insertmacro MUI_DESCRIPTION_TEXT ${FubiOpenNI2} $(DESC_FubiOpenNI2)
  !insertmacro MUI_DESCRIPTION_TEXT ${FubiKinectSDK} $(DESC_FubiKinectSDK1)
  !insertmacro MUI_DESCRIPTION_TEXT ${FubiKinectSDK2} $(DESC_FubiKinectSDK2)
  !insertmacro MUI_DESCRIPTION_TEXT ${FubiPreKinectSDK2} $(DESC_FubiPreKinectSDK2)
  !insertmacro MUI_DESCRIPTION_TEXT ${FubiKinectSDK2Leap} $(DESC_FubiKinectSDK2Leap)
  !insertmacro MUI_DESCRIPTION_TEXT ${FubiAll} $(DESC_FubiAll)
  !insertmacro MUI_DESCRIPTION_TEXT ${FubiSources} $(DESC_FubiSources)
!insertmacro MUI_FUNCTION_DESCRIPTION_END