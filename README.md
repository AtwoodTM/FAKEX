## FAKEX

[![Build status](https://ci.appveyor.com/api/projects/status/vgc0liam1cpre6xy?svg=true)](https://ci.appveyor.com/project/djanosik/fakex)

[FAKE](https://github.com/fsharp/FAKE) scripts for building [DNX](https://github.com/aspnet/home) projects. Everything you need is to put files from `template` folder to the root of your project. If you want to use a build number provided by CI server as project's version, use `1.0.0-ci` in `project.json` as a placeholder.

```json
{
    "version": "1.0.0-ci",
    "dependencies": {
        "OtherProject": "1.0.0-ci"
    }
}
```

For now, the build won't run on Linux and Mac OS (contributions are welcome).