open Fake
open System
open System.IO

// functions 

let inline FileName fullName = Path.GetFileName fullName
let version = if buildServer <> BuildServer.LocalBuild then buildVersion else "1.0.0"

let dnvm args =
    ExecProcessWithLambdas (fun info ->
        info.FileName <- __SOURCE_DIRECTORY__ + "\dnvm.cmd"
        info.Arguments <- args
    ) TimeSpan.MaxValue true failwith traceImportant
        |> ignore

let dnu args =
    dnvm ("exec default dnu " + args + " --quiet")
        
let dnx args =
    dnvm ("exec default dnx " + args)
        
let BackupProject project =
    CopyFile (project + ".bak") project
        
let UpdateVersion version project =
    log ("Updating version in " + project)   
    ReplaceInFile (fun s -> s.Replace("1.0.0-ci", version)) project
    
let BuildProject project =
    dnu ("pack --configuration Release " + (DirectoryName project))
    
let CopyArtifact artifact =
    log ("Copying artifact " + (FileName artifact))
    ensureDirectory "artifacts"
    CopyFile "artifacts" artifact
              
let RestoreProject backup =
    if endsWith ".bak" backup then
        CopyFile (replace ".bak" "" backup) backup
        DeleteFile backup
        
let RunTests project =
    dnx ("\"" + (DirectoryName project) + "\" test")    
 

// targets
    
Target "Clean" (fun _ ->
    !! "artifacts" ++ "**/bin"
        |> DeleteDirs
)

Target "BackupProjects" (fun _ ->
    !! "**/project.json" ++ "**/project.lock.json"
        |> Seq.iter(BackupProject)
        
    ActivateFinalTarget "RestoreProjects"
)

Target "UpdateVersions" (fun _ ->    
    !! "**/project.json"
        |> Seq.iter(UpdateVersion version)
)

Target "RestoreDependencies" (fun _ ->
    dnu "restore"
)

Target "BuildProjects" (fun _ ->
    !! "src/**/project.json" 
        |> Seq.iter(BuildProject)
)

Target "CopyArtifacts" (fun _ ->    
    !! "src/**/*.nupkg" 
        |> Seq.iter(CopyArtifact)
)

Target "RunTests" (fun _ ->
    !! "test/**/project.json" 
        |> Seq.iter(RunTests)
)

FinalTarget "RestoreProjects" (fun _ ->
    !! "**/project.json.bak" ++ "**/project.lock.json.bak"
        |> Seq.iter(RestoreProject)
)

Target "Build" (fun _ ->)

"Clean"
  ==> "BackupProjects"
  ==> "UpdateVersions"
  ==> "RestoreDependencies"
  ==> "BuildProjects"
  ==> "CopyArtifacts"
  ==> "RunTests"
  ==> "Build"