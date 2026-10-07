param(
    [string]$Game = "C:\Program Files (x86)\Steam\steamapps\common\PEAK",
    [string]$Dll = "$PSScriptRoot\..\bin\Release\net472\PEAK-MX.dll"
)
$ErrorActionPreference = "Stop"
[Reflection.Assembly]::Load([IO.File]::ReadAllBytes("$Game\BepInEx\core\Mono.Cecil.dll")) | Out-Null
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory("$Game\PEAK_Data\Managed")
$resolver.AddSearchDirectory("$Game\BepInEx\core")
$parameters = New-Object Mono.Cecil.ReaderParameters
$parameters.AssemblyResolver = $resolver
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path $Dll).Path, $parameters)
function Calls($method) {
    @($method.Body.Instructions | Where-Object { $_.OpCode.Code -in @("Call", "Callvirt") } | ForEach-Object { $_.Operand.FullName })
}
function Require($condition, $message) { if (!$condition) { throw $message } }
try {
    $checked = 0
    foreach ($member in $assembly.MainModule.GetMemberReferences()) {
        if ($member.DeclaringType.Scope.Name -notin @("Assembly-CSharp", "PhotonUnityNetworking", "UnityEngine.CoreModule", "UnityEngine.PhysicsModule")) { continue }
        Require ($null -ne $member.Resolve()) "Unresolved game/engine member: $member"
        $checked++
    }
    $spawner = $assembly.MainModule.Types | Where-Object FullName -EQ "PeakMX.EntitySpawner"
    Require ($null -ne $spawner) "Entity spawner is not compiled."
    $paths = @($spawner.Methods | Where-Object Name -EQ ".cctor" | ForEach-Object { $_.Body.Instructions } | Where-Object { $_.OpCode.Code -eq "Ldstr" } | ForEach-Object Operand)
    # ResourceManager paths verified against the installed PEAK 2.6.a resources.
    $expected = @("0_Items/Beetle", "0_Items/Scorpion", "0_Items/Frog", "Character_Scoutmaster", "MushroomZombie", "BeeSwarm", "Tornado")
    Require ($paths.Count -eq $expected.Count) "Unexpected entity catalog size."
    foreach ($path in $expected) { Require ($path -cin $paths) "Missing network resource: $path" }
    Require (($spawner.Fields | Where-Object Name -EQ "MaxBatch").Constant -eq 5) "Batch cap changed."
    Require (($spawner.Fields | Where-Object Name -EQ "MaxAlive").Constant -eq 30) "Live entity cap changed."
    $spawnCalls = Calls ($spawner.Methods | Where-Object Name -EQ "SpawnCore")
    Require ($spawnCalls -match "PhotonNetwork::InstantiateRoomObject") "Missing host network spawn."
    Require ($spawnCalls -match "PhotonNetwork::Instantiate\(") "Missing client network spawn."
    $guardCalls = Calls ($spawner.Methods | Where-Object Name -EQ "CommonBlockedStatus")
    Require ($guardCalls -match "AntiCheat::get_ClientToolsLocked") "Spawner bypasses anti-cheat policy."
    $positions = $assembly.MainModule.Types | Where-Object FullName -EQ "PeakMX.EspPositions"
    $positionCalls = Calls ($positions.Methods | Where-Object Name -EQ "TryCharacter")
    Require ($positionCalls -match "Character::get_Center") "ESP does not use the live body center."
    Require (!($positionCalls -match "get_transform|get_position")) "ESP can fall back to stale character root coordinates."
    $world = $assembly.MainModule.Types | Where-Object FullName -EQ "PeakMX.WorldEsp"
    Require ((Calls ($world.Methods | Where-Object Name -EQ "Draw")) -match "TryMarkerPosition") "World ESP draws frozen marker coordinates."
    $menu = $assembly.MainModule.Types | Where-Object FullName -EQ "PeakMX.Menu"
    Require ((Calls ($menu.Methods | Where-Object Name -EQ "DrawInventoryRedesign")) -match "DrawEntitySpawner") "Entity UI is not connected to Inventory."
    Require (!($assembly.MainModule.Types.Name -contains "Program")) "Engine-free test stubs leaked into the mod DLL."
    Write-Output "PASS: $checked game/engine members; 7 entity paths; spawn limits and anti-cheat guard; live ESP coordinates; inventory UI wiring."
}
finally { $assembly.Dispose(); $resolver.Dispose() }
