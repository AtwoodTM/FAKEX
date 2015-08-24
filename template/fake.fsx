#r "build/FAKE/tools/FakeLib.dll"
#load "build/FAKEX/tools/fakex.fsx"
open Fake

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

RunTargetOrDefault "Build"