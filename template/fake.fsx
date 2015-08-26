#r "build/FAKE/tools/FakeLib.dll"
#load "build/FAKEX/tools/fakex.fsx"
open Fake

Target "SetupRuntime" (fun _ ->
    if (environVar "SKIP_DNX_INSTALL") <> "1" then
        dnvm "install '1.0.0-beta6' -a default -runtime CLR -arch x86 -nonative"
        dnvm "install default -runtime CoreCLR -arch x86 -nonative"
        
    dnvm "use default -runtime CLR -arch x86"
)

"SetupRuntime" ==> "RestoreDependencies"

// Target "Deploy" (fun _ ->
//     WebDeploy { 
//         appPath = "[IisAppPath]";
//         project = "src/[ProjectName]/project.json";
//         serviceUrl = "[MsDeployServiceUrl]";
//         skipExtraFiles = false;
//         userName = "[UserName]";
//         password = (environVar "WEBDEPLOY_PWD")
//     }
// )

// "SetupRuntime" ==> "Deploy"

RunTargetOrDefault "Build"