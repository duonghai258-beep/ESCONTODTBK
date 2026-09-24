param(
  [string]$SacRoot = 'C:\Program Files\Autodesk\Subassembly Composer 2022',
  [string]$OutFile = (Join-Path $env:USERPROFILE 'Documents\SAC2022_BTXM5\15_SAC2022_API_SCHEMA_PROBE.txt')
)

$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force -Path (Split-Path $OutFile) | Out-Null

$files = Get-ChildItem -LiteralPath $SacRoot -Filter '*.dll' -File
$loaded = @{}
foreach($f in $files){
  try {
    $a=[Reflection.Assembly]::LoadFrom($f.FullName)
    $loaded[$a.FullName]=$a
  } catch {}
}

$names=@(
 'Autodesk.SubassemblyComposer.FileAccess.PktFileAccess',
 'Autodesk.SubassemblyComposer.FileAccess.PktStructure',
 'Autodesk.SubassemblyComposer.FileAccess.ParamCollection',
 'Autodesk.SubassemblyComposer.FileAccess.ParamItem',
 'Autodesk.SubassemblyComposer.FileAccess.SubassemblyItem',
 'Autodesk.SubassemblyComposer.FileAccess.SerializerManager',
 'Autodesk.SubassemblyComposer.WorkflowEngine.ParameterTypeRegister',
 'Autodesk.SubassemblyComposer.WorkflowEngine.ParameterFactory',
 'Autodesk.SubassemblyComposer.WorkflowEngine.IParameter',
 'Autodesk.SubassemblyComposer.WorkflowEngine.DoubleParameter',
 'Autodesk.SubassemblyComposer.WorkflowEngine.GradeParameter',
 'Autodesk.SubassemblyComposer.WorkflowEngine.SlopeParameter',
 'Autodesk.SubassemblyComposer.WorkflowEngine.ElevationTargetParameter',
 'Autodesk.SubassemblyComposer.WorkflowEngine.SurfaceTargetParameter',
 'Autodesk.SubassemblyComposer.WorkflowEngine.OffsetTargetParameter',
 'Autodesk.SubassemblyComposer.WorkflowEngine.WorkflowHost',
 'Autodesk.SubassemblyComposer.ActivityLibrary.CreatePoint',
 'Autodesk.SubassemblyComposer.ActivityLibrary.CreateLink',
 'Autodesk.SubassemblyComposer.ActivityLibrary.CreateShape',
 'Autodesk.SubassemblyComposer.ActivityLibrary.ActivityDesigner.CreatePointDesigner',
 'Autodesk.SubassemblyComposer.ActivityLibrary.ActivityDesigner.CreateLinkDesigner',
 'Autodesk.SubassemblyComposer.ActivityLibrary.ActivityDesigner.CreateShapeDesigner',
 'Autodesk.SubassemblyComposer.ActivityLibrary.ActivityDesigner.AuxSurfaceLinkDesigner'
)

function Find-Type([string]$n){
  foreach($a in $loaded.Values){
    $t=$a.GetType($n,$false)
    if($null -ne $t){ return $t }
  }
  return $null
}

$sb=[Text.StringBuilder]::new()
[void]$sb.AppendLine('SAC 2022 ACTUAL API SCHEMA PROBE')
[void]$sb.AppendLine("Date=$(Get-Date -Format s)")
[void]$sb.AppendLine("Root=$SacRoot")
[void]$sb.AppendLine()

foreach($n in $names){
  $t=Find-Type $n
  [void]$sb.AppendLine(('='*90))
  [void]$sb.AppendLine("TYPE $n")
  if($null -eq $t){ [void]$sb.AppendLine('NOT_FOUND'); continue }

  [void]$sb.AppendLine("Assembly=$($t.Assembly.FullName)")
  [void]$sb.AppendLine('CONSTRUCTORS')
  foreach($c in $t.GetConstructors([Reflection.BindingFlags]'Public,NonPublic,Instance,Static')){
    [void]$sb.AppendLine("  $c")
  }

  [void]$sb.AppendLine('PROPERTIES')
  foreach($p in $t.GetProperties([Reflection.BindingFlags]'Public,NonPublic,Instance,Static')){
    [void]$sb.AppendLine("  $($p.PropertyType.FullName) $($p.Name) CanRead=$($p.CanRead) CanWrite=$($p.CanWrite)")
  }

  [void]$sb.AppendLine('FIELDS')
  foreach($f in $t.GetFields([Reflection.BindingFlags]'Public,NonPublic,Instance,Static')){
    [void]$sb.AppendLine("  $($f.FieldType.FullName) $($f.Name) Static=$($f.IsStatic)")
  }

  [void]$sb.AppendLine('METHODS')
  foreach($m in $t.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly')){
    if($m.IsSpecialName){ continue }
    $args=($m.GetParameters() | ForEach-Object { "$($_.ParameterType.FullName) $($_.Name)" }) -join ', '
    [void]$sb.AppendLine("  $($m.ReturnType.FullName) $($m.Name)($args)")
  }

  if($t.IsEnum){
    [void]$sb.AppendLine('ENUM VALUES')
    [Enum]::GetNames($t) | ForEach-Object { [void]$sb.AppendLine("  $_") }
  }
}

[IO.File]::WriteAllText($OutFile,$sb.ToString(),[Text.UTF8Encoding]::new($false))
Write-Host "DONE: $OutFile"
