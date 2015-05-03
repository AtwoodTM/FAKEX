#r "build/FAKE/tools/FakeLib.dll"
#load "build/FAKEX/tools/targets.fsx"
open Fake

// custom tasks

RunTargetOrDefault "Build"