Stub assemblies to reference types for building in CI but contain none of the game code.

## To generate

```ps1
dotnet tool install -g JetBrains.Refasmer.CliTool
refasmer -O ".\references\xbox\MelonLoader\net6" (Get-ChildItem -Path "C:\XboxGames\Neon White\Content\MelonLoader\net6\*.dll" | Select-Object -ExpandProperty FullName)
refasmer -O ".\references\xbox\Neon White_Data\Managed" (Get-ChildItem -Path "C:\XboxGames\Neon White\Content\Neon White_Data\Managed\*.dll" | Select-Object -ExpandProperty FullName)
refasmer -O ".\references\steam\MelonLoader\net6" (Get-ChildItem -Path "C:\Program Files (x86)\Steam\steamapps\common\Neon White\MelonLoader\net6\*.dll" | Select-Object -ExpandProperty FullName)
refasmer -O ".\references\steam\Neon White_Data\Managed" (Get-ChildItem -Path "C:\Program Files (x86)\Steam\steamapps\common\Neon White\Neon White_Data\Managed\*.dll" | Select-Object -ExpandProperty FullName)
```
