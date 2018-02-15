// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r "./packages/FAKE/tools/FakeLib.dll"
open Fake

#load "./packages/FSharp.Formatting/FSharp.Formatting.fsx"
open FSharp.Literate
open System.IO

let script = Path.Combine(__SOURCE_DIRECTORY__, "./Farango/docs/Documentation.fsx")
let template = Path.Combine(__SOURCE_DIRECTORY__, "./packages/FSharp.Formatting/literate/templates/template-file.html")
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
  !! "**/Farango.fsproj"
  |> MSBuildReleaseExt buildDir [("Verbosity", "Quiet")] "Build"
  |> ignore
)

// --------------------------------------------------------------------------------------
// Build order
// --------------------------------------------------------------------------------------

"Clean"
  ==> "Build"

RunTargetOrDefault "Build"