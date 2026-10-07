param(
    [string]$Game = "C:\Program Files (x86)\Steam\steamapps\common\PEAK",
    [string]$Dll = "$PSScriptRoot\..\bin\Release\net472\PEAK-MX.dll"
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot
$source = (Get-ChildItem "$root\src" -Filter *.cs | ForEach-Object {
    [IO.File]::ReadAllText($_.FullName)
}) -join [Environment]::NewLine
if ($source -match 'peak-mx\.rkngov\.com|TelemetryToken|ActionTracker|FeedbackClient|ClientIdentity|InstallAsync|DownloadData|DownloadFile') {
    throw "Removed backend or installer code found in source."
}
$requests = [regex]::Matches($source, 'WebRequest\.Create\("([^"]+)"\)')
if ($requests.Count -ne 1 -or $requests[0].Groups[1].Value -ne "https://api.github.com/repos/maxkir041/PEAK-MX/releases/latest") {
    throw "Unexpected HTTP endpoint."
}
[Reflection.Assembly]::Load([IO.File]::ReadAllBytes("$Game\BepInEx\core\Mono.Cecil.dll")) | Out-Null
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory("$Game\PEAK_Data\Managed")
$resolver.AddSearchDirectory("$Game\BepInEx\core")
$parameters = New-Object Mono.Cecil.ReaderParameters
$parameters.AssemblyResolver = $resolver
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path $Dll).Path, $parameters)
$checked = 0
try {
    foreach ($member in $assembly.MainModule.GetMemberReferences()) {
        if ($member.DeclaringType.Scope.Name -ne "Assembly-CSharp") { continue }
        if ($null -eq $member.Resolve()) { throw "Unresolved game member: $member" }
        $checked++
    }
    $removed = @("Stats", "Diagnostics", "ClientIdentity", "FeedbackClient", "DonationSupport", "TelemetryToken", "ActionTracker")
    foreach ($type in $assembly.MainModule.Types) {
        if ($type.Name -in $removed) { throw "Removed type still compiled: $type" }
    }
    Write-Output "PASS: $checked game members resolved; no backend types or installer; GitHub metadata only."
}
finally { $assembly.Dispose(); $resolver.Dispose() }
