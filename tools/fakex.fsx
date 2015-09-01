[<AutoOpen>] 
module Fakex

open Fake
open System
open System.Collections.Generic
open System.IO

type WebDeployArgs = { 
    appPath:string; 
    project:string; 
    serviceUrl:string; 
    skipExtraFiles:bool; 
    userName:string; 
    password:string 
}

let mutable DnxHome = "[unknown]"

// helpers

let inline FileName fullName = Path.GetFileName fullName
    
let BuildFailed errors =
    raise (BuildException("The project build failed.", errors |> List.ofSeq))
    
let DeployFailed errors =
    raise (BuildException("The project deployment failed.", errors |> List.ofSeq))
    
let TestsFailed errors =
    raise (BuildException("The project tests failed.", errors |> List.ofSeq))
        
let Run workingDirectory fileName args =
    let errors = new List<string>()
    let messages = new List<string>()
    let timout = TimeSpan.MaxValue
        
    let error msg =
        traceError msg
        errors.Add msg
        
    let message msg =
        traceImportant msg
        messages.Add msg
        
    let code = 
        ExecProcessWithLambdas (fun info ->
            info.FileName <- fileName
            info.WorkingDirectory <- workingDirectory
            info.Arguments <- args
        ) timout true error message
    
    ProcessResult.New code messages errors
    
// processes

let dnvm args =
    let result = Run currentDirectory (__SOURCE_DIRECTORY__ + "\\dnvm.cmd") args
    
    if result.OK && (startsWith "use " args) then 
        let message = result.Messages.[0]
        let homeEnd = message.IndexOf("\\bin to process PATH") - 7
        DnxHome <- message.Substring(7, homeEnd)
        
let msdeploy args =
    let result = Run currentDirectory (ProgramFilesX86 + "\\IIS\\Microsoft Web Deploy V3\\msdeploy.exe") args
    if not result.OK then DeployFailed result.Errors

let dnu failedF args =
    let result = Run currentDirectory (DnxHome + "\\bin\\dnu.cmd") (args + " --quiet")
    if not result.OK then failedF result.Errors
        
let dnx failedF workingDirectory command =
    let result = Run workingDirectory (DnxHome + "\\bin\\dnx.exe") (". " + command)
    if not result.OK then failedF result.Errors
    
// functions
    
let BackupProject project =
    CopyFile (project + ".bak") project
        
let UpdateVersion version project =
    log ("Updating version in " + project)   
    ReplaceInFile (fun s -> replace "1.0.0-ci" version s) project
    
let BuildProject project =
    dnu BuildFailed ("pack \"" + (DirectoryName project) + "\" --configuration Release")
    
let CopyArtifact artifact =
    log ("Copying artifact " + (FileName artifact))
    ensureDirectory "artifacts"
    CopyFile "artifacts" artifact
              
let RestoreProject backup =
    if endsWith ".bak" backup then
        CopyFile (replace ".bak" "" backup) backup
        DeleteFile backup
        
let RunTests project =
    dnx TestsFailed (DirectoryName project) "test"    

let WebDeploy (args:WebDeployArgs) =
    let projDirectory = DirectoryName (FullName args.project)
    let outDirectory =  projDirectory+ "\\bin\\out"   
     
    DeleteDir outDirectory    
    dnu DeployFailed ("publish \"" + (DirectoryName (FullName args.project)) + "\" --out \"" + outDirectory + "\" --configuration Release --no-source --runtime \"" + DnxHome + "\" --wwwroot-out \"wwwroot\"")
    msdeploy ("-source:IisApp=\"" + outDirectory + "\\wwwroot\" -dest:IisApp=\"" + args.appPath + "\",ComputerName=\"https://" + args.serviceUrl + "/msdeploy.axd\",UserName=\"" + args.userName 
        + "\",Password=\"" + args.password + "\",IncludeAcls=\"False\",AuthType=\"Basic\" -verb:sync -enableLink:contentLibExtension -retryAttempts:2" 
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
    let version = if buildServer <> BuildServer.LocalBuild then buildVersion else "1.0.0"
    
    !! "src/*/project.json" ++ "test/*/project.json"
        |> Seq.iter(UpdateVersion version)
)

Target "RestoreDependencies" (fun _ ->
    dnu BuildFailed "restore"
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