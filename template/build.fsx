#r "build/FAKE/tools/FakeLib.dll"
#load "build/FAKEX/tools/fakex.fsx"
open Fake

// custom tasks

RunTargetOrDefault "Build"