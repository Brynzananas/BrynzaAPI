REM original version https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Networking/UNet/
REM open this in vs it'll be so much nicer

REM call postbuild.bat $(TargetDir) $(AssemblyName)

REM bin/Release/netstandard2.1
set Output=%1

REM AssemblyName.dll
set DLL=%2.dll
REM AssemblyName.pdb
set PDB=%2.pdb

set Libs=NetworkWeaver\libs
set Store=..\Thunderstore
set Zip=%Store%\Release.zip

set Log=%Output%OUTPUT.log

if exist %Log% Del %Log%

REM le epic networking patch
.\NetworkWeaver\Unity.UNetWeaver.exe "D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\UnityEngine.CoreModule.dll" "D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\com.unity.multiplayer-hlapi.Runtime.dll"  %Output% "D:\r2modman\RiskOfRain2\profiles\my_mod\BepInEx\plugins\BrynzaAPI\Debug\netstandard2.1\BrynzaAPI.dll" "D:\Ror2Mods\BrynzaAPI\BrynzaAPI\BrynzaAPI\NetworkWeaver\libs"