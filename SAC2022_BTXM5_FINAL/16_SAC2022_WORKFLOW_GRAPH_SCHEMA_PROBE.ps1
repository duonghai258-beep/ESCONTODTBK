#requires -Version 5.1
[CmdletBinding()]
param(
  [string]$SacRoot = 'C:\Program Files\Autodesk\Subassembly Composer 2022',
  [string]$OutFile = 'C:\Users\duong\Documents\SAC2022_BTXM5\16_SAC2022_WORKFLOW_GRAPH_SCHEMA_PROBE.txt'
)

$ErrorActionPreference = 'Stop'
$dir = Split-Path -Parent $OutFile
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$lines = New-Object System.Collections.Generic.List[string]
function W([string]$s=''){ $lines.Add($s) }

W 'SAC 2022 WORKFLOW GRAPH SCHEMA PROBE V2'
W ('Date=' + (Get-Date).ToString('s'))
W ('Root=' + $SacRoot)
W ''

$loaded = @{}
Get-ChildItem -LiteralPath $SacRoot -Filter '*.dll' -Recurse -File -ErrorAction SilentlyContinue |
  Sort-Object FullName | ForEach-Object {
    try {
      $a = [Reflection.Assembly]::LoadFrom($_.FullName)
      $loaded[$a.FullName] = $a
    } catch {}
  }

$allTypes = @()
foreach($a in $loaded.Values){
  try { $allTypes += $a.GetTypes() } catch {}
}

function Dump-Type([Type]$t){
  W ('TYPE ' + $t.FullName)
  W ('ASSEMBLY ' + $t.Assembly.FullName)
  W 'CONSTRUCTORS'
  foreach($c in $t.GetConstructors([Reflection.BindingFlags]'Public,NonPublic,Instance,Static')){
    try { W ('  ' + $c.ToString()) } catch {}
  }
  W 'PROPERTIES'
  foreach($p in $t.GetProperties([Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly')){
    try { W ('  ' + $p.PropertyType.FullName + ' ' + $p.Name + ' CanRead=' + $p.CanRead + ' CanWrite=' + $p.CanWrite) } catch {}
  }
  W 'FIELDS'
  foreach($f in $t.GetFields([Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly')){
    try { W ('  ' + $f.FieldType.FullName + ' ' + $f.Name + ' Static=' + $f.IsStatic) } catch {}
  }
  W 'METHODS'
  foreach($m in $t.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly')){
    try { W ('  ' + $m.ToString()) } catch {}
  }
  W ('ENDTYPE ' + $t.FullName)
  W ''
}

W '=== CANDIDATE WORKFLOW/CONTROL TYPES ==='
$candidates = $allTypes | Where-Object {
  $_.FullName -match '(?i)(Sequence|Decision|Flowchart|Conditional|Condition|IfActivity|Switch|While|DoWhile|ForEach|Parallel|ActivityBuilder|DynamicActivity|Workflow)' -and
  $_.FullName -notmatch '(?i)(Designer|Presentation|View|Converter|Template|Test)'
} | Sort-Object FullName -Unique
foreach($t in $candidates){ Dump-Type $t }

W '=== ALL SAC ACTIVITY LIBRARY TYPES ==='
$activityTypes = $allTypes | Where-Object {
  $_.FullName -like 'Autodesk.SubassemblyComposer.ActivityLibrary.*' -and
  $_.IsClass -and
  $_.FullName -notmatch '(?i)Designer'
} | Sort-Object FullName -Unique
foreach($t in $activityTypes){
  W ($t.FullName + ' | Base=' + $(if($t.BaseType){$t.BaseType.FullName}else{''}) + ' | Abstract=' + $t.IsAbstract)
}
W ''

W '=== CORE ACTIVITY SCHEMA: CreatePoint/CreateLink/CreateShape ==='
foreach($name in @(
 'Autodesk.SubassemblyComposer.ActivityLibrary.CreatePoint',
 'Autodesk.SubassemblyComposer.ActivityLibrary.CreateLink',
 'Autodesk.SubassemblyComposer.ActivityLibrary.CreateShape'
)){
  $t=$allTypes | Where-Object FullName -eq $name | Select-Object -First 1
  if($t){ Dump-Type $t } else { W ('MISSING ' + $name) }
}

W '=== WF ACTIVITY BASE/CONTAINER SCHEMA ==='
foreach($name in @(
 'System.Activities.Activity',
 'System.Activities.DynamicActivity',
 'System.Activities.Statements.Sequence',
 'System.Activities.Statements.Flowchart',
 'System.Activities.Statements.If',
 'System.Activities.Statements.ConditionalActivity'
)){
  $t=$allTypes | Where-Object FullName -eq $name | Select-Object -First 1
  if($t){ Dump-Type $t } else { W ('NOT_LOADED ' + $name) }
}

W '=== PARAMETER/ARGUMENT SCHEMA ==='
foreach($name in @(
 'Autodesk.SubassemblyComposer.WorkflowEngine.ParameterTypeRegister',
 'Autodesk.SubassemblyComposer.WorkflowEngine.ParameterFactory',
 'Autodesk.SubassemblyComposer.WorkflowEngine.IParameter',
 'Autodesk.SubassemblyComposer.WorkflowEngine.TargetParameter',
 'Autodesk.SubassemblyComposer.WorkflowEngine.WorkflowHost'
)){
  $t=$allTypes | Where-Object FullName -eq $name | Select-Object -First 1
  if($t){ Dump-Type $t } else { W ('MISSING ' + $name) }
}

W '=== INSTALLED PKT/XAML INVENTORY ==='
Get-ChildItem -LiteralPath $SacRoot -Recurse -File -ErrorAction SilentlyContinue |
  Where-Object { $_.Extension -in '.pkt','.xaml','.xml' } |
  Sort-Object FullName |
  Select-Object -First 500 |
  ForEach-Object { W ($_.FullName + ' | ' + $_.Length + ' bytes') }
W ''

W '=== REFLECTION: SERIALIZATION-RELEVANT METHODS ==='
foreach($a in $loaded.Values){
  foreach($t in @($a.GetTypes())){
    foreach($m in $t.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Static,Instance')){
      if($m.Name -match '(?i)(Serialize|Deserialize|Save|Load|OpenPkt|CreatePkt|UpdateWorkflow|Execute|Persist|Xaml)'){
        try { W ($t.FullName + ' :: ' + $m.ToString()) } catch {}
      }
    }
  }
}

W '=== DONE ==='
Set-Content -LiteralPath $OutFile -Value $lines -Encoding UTF8
Write-Host ('DONE: ' + $OutFile)
