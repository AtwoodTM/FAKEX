open Fake
open System

let dnu args =
    ExecProcessWithLambdas (fun info ->
        info.FileName <- (environVar "DNX_FOLDER") + "\dnu.cmd"
        info.Arguments <- args
    ) TimeSpan.MaxValue true failwith traceImportant
        |> ignore
        
let dnx args =
    ExecProcessWithLambdas (fun info ->
        info.FileName <- (environVar "DNX_FOLDER") + "\dnx.exe"
        info.Arguments <- args
    ) TimeSpan.MaxValue true failwith traceImportant
        |> ignore

Target "Clean" (fun _ ->
    !! "artifacts" ++ "**/bin"
        |> DeleteDirs
)

Target "UpdateVersions" (fun _ ->
    !! "**/project.json"
        |> fun projects -> for project in projects do
            ReplaceInFile (fun s -> 
                let version = environVar "CI_BUILD_VERSION"
                if not (String.IsNullOrEmpty version) then s.Replace("1.0.0-ci", version)
                else s
            ) project
)

Target "Restore" (fun _ ->
    dnu "restore"
)

Target "BuildSources" (fun _ ->
    !! "src/**/project.json"
        |> fun projects -> for project in projects do 
            dnu ("pack --configuration Release " + (DirectoryName project))
)

Target "CopyArtifacts" (fun _ ->    
    !! "src/**/*.nupkg"
        |> Copy "artifacts"
)

Target "BuildTests" (fun _ ->
    !! "test/**/project.json"
        |> fun projects -> for project in projects do 
            dnu ("build --configuration Release " + (DirectoryName project))
)

Target "Build" (fun _ ->)

Target "RunTests" (fun _ ->
    !! "test/**/project.json"
        |> fun projects -> for project in projects do 
            dnx ((DirectoryName project) + " test")
)

"Clean"
  ==> "UpdateVersions"
  ==> "Restore"
  ==> "BuildSources"
  ==> "CopyArtifacts"
  ==> "BuildTests"
  ==> "Build"