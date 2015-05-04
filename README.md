# FAKEX

[![Build status](https://ci.appveyor.com/api/projects/status/fg6nhtpuovh52s4f?svg=true)](https://ci.appveyor.com/project/djanosik/fakex)

[FAKE](https://github.com/fsharp/FAKE) scripts for building [DNX](https://github.com/aspnet/home) projects. Everything you need is to put files from `template` folder to the root of your project. 
CI servers TeamCity and AppVeyor are supported. If you want to use a build number as project's version, use `1.0.0-ci` in `project.json` as a placeholder.

    {
        "version": "1.0.0-ci",
        "dependencies": {
            "OtherProject": "1.0.0-ci"
        }
    }

For now, the build won't run on Linux and Mac OS (contributions are welcome).