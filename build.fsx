// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r "./packages/FAKE/tools/FakeLib.dll"
open Fake

#load "./packages/FSharp.Formatting/FSharp.Formatting.fsx"
open FSharp.Literate
open System.IO

let script = Path.Combine(__SOURCE_DIRECTORY__, "./docs/index.fsx")
let template = Path.Combine(__SOURCE_DIRECTORY__, "./docs/template.html")
Literate.ProcessScriptFile(script, template)

// --------------------------------------------------------------------------------------
// Build variables
// --------------------------------------------------------------------------------------

let buildDir  = "./build/"

// --------------------------------------------------------------------------------------
// Targets
// --------------------------------------------------------------------------------------

Target "Clean" (fun _ ->
  CleanDir buildDir
)

Target "Build" (fun _ ->
  let result = Shell.Exec("dotnet", "msbuild /p:Configuration=Release Farango.fsproj", "./Farango/")
  if result <> 0 then failwithf "Farango failed to compile with exit code %d" result
)

// --------------------------------------------------------------------------------------
// Build order
// --------------------------------------------------------------------------------------

"Clean"
  ==> "Build"

RunTargetOrDefault "Build"