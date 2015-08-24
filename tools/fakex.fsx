[<AutoOpen>] 
module Fakex

open Fake
open System
open System.IO

// types

type WebDeployArgs = { appPath:string; project:string; serviceUrl:string; skipExtraFiles:bool; userName:string; password:string }

// functions 

let inline FileName fullName = Path.GetFileName fullName

let dnvm args =
    ExecProcessWithLambdas (fun info ->
        info.FileName <- __SOURCE_DIRECTORY__ + "\dnvm.cmd"
        info.Arguments <- args
    ) TimeSpan.MaxValue true failwith traceImportant
        |> ignore
        
let msdeploy args =
    ExecProcessWithLambdas (fun info ->
        info.FileName <- ProgramFilesX86 + "\IIS\Microsoft Web Deploy V3\msdeploy.exe"
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
    ReplaceInFile (fun s -> replace "1.0.0-ci" version s) project
    
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

let WebDeploy (args:WebDeployArgs) =
    let outDirectory = FullName "artifacts/publish"
    DeleteDir outDirectory
    dnu ("publish '" + (DirectoryName (FullName args.project)) + "' --out '" + outDirectory + "' --configuration Release --no-source --runtime active --wwwroot-out 'wwwroot'")
    msdeploy ("-source:IisApp='" + outDirectory + "\wwwroot' -dest:IisApp='" + args.appPath + "',ComputerName='https://" + args.serviceUrl + "/msdeploy.axd',UserName='" + args.userName 
        + "',Password='" + args.password + "',IncludeAcls='False',AuthType='Basic' -verb:sync -enableLink:contentLibExtension -retryAttempts:2" 
        + if args.skipExtraFiles then " -enableRule:DoNotDeleteRule" else "" + " -disablerule:BackupRule")

// targets
    
Target "Clean" (fun _ ->
    !! "artifacts" ++ "src/*/bin" ++ "test/*/bin"
        |> DeleteDirs
)

Target "BackupProjects" (fun _ ->
    !! "src/*/project.json" ++ "src/*/project.lock.json" ++ "test/*/project.json" ++ "test/*/project.lock.json"
        |> Seq.iter(BackupProject)
        
    ActivateFinalTarget "RestoreProjects"
)

Target "UpdateVersions" (fun _ ->    
    let version = if buildServer <> BuildServer.LocalBuild 
        then buildVersion else "1.0.0"
    
    !! "src/*/project.json" ++ "test/*/project.json"
        |> Seq.iter(UpdateVersion version)
)

Target "RestoreDependencies" (fun _ ->
    dnu "restore"
)

Target "BuildProjects" (fun _ ->
    !! "src/*/project.json" 
        |> Seq.iter(BuildProject)
)

Target "CopyArtifacts" (fun _ ->    
    !! "src/*/bin/**/*.nupkg" 
        |> Seq.iter(CopyArtifact)
)

Target "RunTests" (fun _ ->
    !! "test/*/project.json" 
        |> Seq.iter(RunTests)
)

FinalTarget "RestoreProjects" (fun _ ->
    !! "src/*/project.json.bak" ++ "src/*/project.lock.json.bak" ++ "test/*/project.json.bak" ++ "test/*/project.lock.json.bak"
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