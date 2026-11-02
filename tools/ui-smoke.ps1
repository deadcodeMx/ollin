param(
    [string]$Player = "src\Ollin.Player\bin\Debug\net8.0\Ollin.Player.exe",
    [string]$Editor = "src\Ollin.Editor\bin\Debug\net8.0\Ollin.Editor.exe",
    [string]$Project = "dist\DemoTecnico"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

function Resolve-FromRoot([string]$path) {
    if (Test-Path $path) { return (Resolve-Path $path).Path }
    return (Resolve-Path (Join-Path $root $path)).Path
}

$playerPath = Resolve-FromRoot $Player
$editorPath = Resolve-FromRoot $Editor
$projectPath = Resolve-FromRoot $Project
$playerLog = Join-Path (Split-Path $playerPath) "player.log"

Write-Host "UI smoke: compilando pruebas..."
dotnet run --project (Join-Path $root "tests\Ollin.Tests\Ollin.Tests.csproj") -v q | Out-Host

Write-Host "UI smoke: iniciando reproductor..."
$playerProcess = Start-Process -FilePath $playerPath -ArgumentList @("--project", $projectPath) -PassThru
Start-Sleep -Seconds 7
$ws = New-Object -ComObject WScript.Shell
[void]$ws.AppActivate($playerProcess.Id)

$ws.SendKeys("{ESC}")
Start-Sleep -Milliseconds 800
$ws.SendKeys("{ESC}")
Start-Sleep -Milliseconds 800

$log = Get-Content $playerLog -Raw -ErrorAction SilentlyContinue
if ($log -notmatch "Reproducción en pausa") { throw "No se registró la pausa del reproductor." }
if ($log -notmatch "Reproducción reanudada") { throw "No se registró la reanudación del reproductor." }

Stop-Process -Id $playerProcess.Id -Force
Write-Host "UI smoke: pausa/reanudación OK."

Write-Host "UI smoke: abriendo grafo..."
$editorProcess = Start-Process -FilePath $editorPath -ArgumentList @("--project", $projectPath, "--graph") -PassThru
Start-Sleep -Seconds 8

Add-Type @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public class OllinSmokeWindows
{
    public delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);

    public static bool HasGraph(uint pid)
    {
        bool found = false;
        EnumWindows((h, l) =>
        {
            uint p;
            GetWindowThreadProcessId(h, out p);
            if (p == pid)
            {
                var text = new StringBuilder(512);
                GetWindowText(h, text, 512);
                if (text.ToString().StartsWith("Grafo de nodos")) found = true;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }
}
"@

if (-not [OllinSmokeWindows]::HasGraph([uint32]$editorProcess.Id)) {
    throw "No se abrió la ventana del grafo."
}

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$automationRoot = [System.Windows.Automation.AutomationElement]::RootElement
$processCondition = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
    $editorProcess.Id)
$editorWindow = $automationRoot.FindFirst([System.Windows.Automation.TreeScope]::Children, $processCondition)

function Find-ByAutomationName([string]$name) {
    if ($null -eq $editorWindow) { return $null }
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $editorWindow.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

if ($null -eq (Find-ByAutomationName "Buscar nodo")) {
    throw "El buscador no expone nombre de accesibilidad."
}

if ($null -eq (Find-ByAutomationName "More graph actions")) {
    throw "El botón Más no expone nombre de accesibilidad."
}

Write-Host "UI smoke: nombres de accesibilidad OK."

Stop-Process -Id $editorProcess.Id -Force
Write-Host "UI smoke: grafo OK."
Write-Host "UI smoke COMPLETADO."
