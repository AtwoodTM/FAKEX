open Fake
open System
open System.IO

// functions 

let IsTeamCity = (environVar "TEAMCITY_PROJECT_NAME") <> null
let IsAppVeyor = (environVar "APPVEYOR_PROJECT_NAME") <> null
let inline FileName fullName = Path.GetFileName fullName 

let dnu args =
    ExecProcessWithLambdas (fun info ->
        info.FileName <- (environVar "DNX_FOLDER") + "\dnu.cmd"
        info.Arguments <- args + " --quiet"
    ) TimeSpan.MaxValue true failwith traceImportant
        |> ignore
        
let dnx args =
    ExecProcessWithLambdas (fun info ->
        info.FileName <- (environVar "DNX_FOLDER") + "\dnx.exe"
        info.Arguments <- args
    ) TimeSpan.MaxValue true failwith traceImportant
        |> ignore        

let GetBuildVersion =
    if IsAppVeyor then (environVar "APPVEYOR_BUILD_VERSION")
    else if IsTeamCity then (environVar "BUILD_NUMBER")
    else null
    
let UpdateVersion version project =
    log ("Updating version in " + project)
    ReplaceInFile (fun s -> s.Replace("1.0.0-ci", version)) project
    
let BuildProject project =
    dnu ("pack --configuration Release " + (DirectoryName project))
    
let CopyArtifact artifact =
    log ("Copying artifact " + (FileName artifact))
    ensureDirectory "artifacts"
    CopyFile "artifacts" artifact

let IsTestProject project = 
    let content = ReadFileAsString project
    content.Contains("\"test\"")
    
let IsXunitProject project = 
    let content = ReadFileAsString project
    content.Contains("\"xunit.runner.dnx\"")
        
let RunTests project =
    if IsTestProject project then
        if (IsAppVeyor && IsXunitProject project) 
        then
            ensureDirectory "temp"
            dnx ((DirectoryName project) + " test -xml temp/xunit-results.xml")
            UploadTestResultsXml "xunit" "temp"
            DeleteDir "temp"
        else     
            dnx ((DirectoryName project) + " test")

// targets
    
Target "Clean" (fun _ ->
    !! "artifacts" ++ "**/bin"
        |> DeleteDirs
)

Target "UpdateVersions" (fun _ ->
    let version = GetBuildVersion    
    if not (String.IsNullOrEmpty version) then 
        !! "**/project.json"
            |> Seq.iter(UpdateVersion version)
)

Target "Restore" (fun _ ->
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

Target "Build" (fun _ ->)

"Clean"
  ==> "UpdateVersions"
  ==> "Restore"
  ==> "BuildProjects"
  ==> "CopyArtifacts"
  ==> "RunTests"
  ==> "Build"