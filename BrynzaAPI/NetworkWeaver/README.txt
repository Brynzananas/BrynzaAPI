This app will patch a Script Assembly by generating all the necessary stuff so that your networking stuff :
Commands Attribute, RPC stuff etc... will work.

Args when launching this weaver must be, in this order:
string unityEngine => Path to UnityEngine.CoreModule.dll
string unetDLL => Path to UnityEngine.Networking.dll
string outputDirectory => Folder Path where your assembly is
string assembly => Path to the assembly.dll that need to be patched
string extraAssemblyPath => Path to a folder where all the assemblies needed for compiling your assembly are

Typical usage :

Unity.UNetWeaver.exe "D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\UnityEngine.CoreModule.dll" "D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\Managed\com.unity.multiplayer-hlapi.Runtime.dll" "D:\Ror2Mods\BrynzaAPI\BrynzaAPI\BrynzaAPI\NetworkWeaver" "D:\r2modman\RiskOfRain2\profiles\my_mod\BepInEx\plugins\BrynzaAPI\Debug\netstandard2.1\BrynzaAPI.dll" "D:\Ror2Mods\BrynzaAPI\BrynzaAPI\BrynzaAPI\NetworkWeaver\libs"
