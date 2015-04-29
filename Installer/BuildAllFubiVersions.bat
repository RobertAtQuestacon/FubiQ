@echo off

REM Temporarily rename the FubiConfig.h
ren ..\Fubi\FubiConfig.h _lock_FubiConfig.h
REM and use the no sensor config as default
copy /y "..\Fubi\FubiConfig_NoSensor.h" "..\Fubi\FubiConfig.h"


REM Build the Fubi project with all different sensor combinations
%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Fubi\Fubi_2013.vcxproj" /t:rebuild /p:Configuration=Release,SolutionDir="../",CommandLinePreProcessors=FUBI_USE_OPENNI1,Platform=Win32,VisualStudioVersion=12.0
ren ..\bin\Fubi.dll Fubi_OpenNI1.dll
%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Fubi\Fubi_2013.vcxproj" /t:rebuild /p:Configuration=Release,Platform=Win32,VisualStudioVersion=12.0,SolutionDir="../",CommandLinePreProcessors=FUBI_USE_OPENNI2
ren ..\bin\Fubi.dll Fubi_OpenNI2.dll
%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Fubi\Fubi_2013.vcxproj" /t:rebuild /p:Configuration=Release,SolutionDir="../",CommandLinePreProcessors=FUBI_USE_KINECT_SDK,Platform=Win32,VisualStudioVersion=12.0
ren ..\bin\Fubi.dll Fubi_KinectSDK1.dll
%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Fubi\Fubi_2013.vcxproj" /t:rebuild /p:Configuration=Release,SolutionDir="../",CommandLinePreProcessors=FUBI_USE_KINECT_SDK_2,Platform=Win32,VisualStudioVersion=12.0
ren ..\bin\Fubi.dll Fubi_KinectSDK2.dll
%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Fubi\Fubi_2013.vcxproj" /t:rebuild /p:Configuration=Release,SolutionDir="../",CommandLinePreProcessors="FUBI_USE_OPENNI1;FUBI_USE_OPENNI2;FUBI_USE_KINECT_SDK",Platform=Win32,VisualStudioVersion=12.0
ren ..\bin\Fubi.dll Fubi_PreKinectSDK2.dll
%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Fubi\Fubi_2013.vcxproj" /t:rebuild /p:Configuration=Release,SolutionDir="../",CommandLinePreProcessors="FUBI_USE_KINECT_SDK_2;FUBI_USE_LEAP",Platform=Win32,VisualStudioVersion=12.0
ren ..\bin\Fubi.dll Fubi_KinectSDK2_Leap.dll
%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Fubi\Fubi_2013.vcxproj" /t:rebuild /p:Configuration=Release,SolutionDir="../",CommandLinePreProcessors="FUBI_USE_OPENNI1;FUBI_USE_OPENNI2;FUBI_USE_KINECT_SDK;FUBI_USE_KINECT_SDK_2;FUBI_USE_LEAP",Platform=Win32,VisualStudioVersion=12.0
ren ..\bin\Fubi.dll Fubi_All.dll

REM Now Build the whole Fubi solution without any sensor 
%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "..\Fubi_2013.sln" /t:rebuild /p:Configuration=Release,Platform=Win32,VisualStudioVersion=12.0

REM Restore FubiConfig.h
del ..\Fubi\FubiConfig.h
ren ..\Fubi\_lock_FubiConfig.h FubiConfig.h 

PAUSE