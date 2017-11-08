var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");

var nugetApiKey = EnvironmentVariable("NUGET_API_KEY");
var version = EnvironmentVariable("version") ?? "0.0.1";
var build = EnvironmentVariable("build") ?? "1";

var fullVersion = $"{version}.{build}";
var semVersion = EnvironmentVariable("tag") ?? $"{version}-beta{build}";

Information($"");
Information($"============================");
Information($"Version {version}");
Information($"Build {build}");
Information($"SemVersion {semVersion}");
Information($"============================");

Task("Clean")
    .Does(() => 
    {
        DotNetCoreClean("../");
        CleanDirectory("../artifacts");
    });

Task("Restore")
    .Does(() => 
    {
        DotNetCoreRestore("../");
    });

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        var file = "../src/Akka.Cluster.SplitBrainResolver/AssemblyInfo.g.cs";

        CreateAssemblyInfo(file, new AssemblyInfoSettings {
            Version = fullVersion,
            FileVersion = fullVersion,
            InformationalVersion = semVersion
        });

        var settings = new DotNetCoreBuildSettings
        {
            Configuration = configuration
        };

        DotNetCoreBuild("../", settings);
    });

Task("Test")
    .Does(() =>
    {
        var settings = new DotNetCoreTestSettings
        {
            Configuration = configuration,
            NoBuild = true
        };

        var projectFiles = GetFiles("../tests/**/*.csproj");
        foreach(var file in projectFiles)
        {
            Information($"Testing {file}");
            DotNetCoreTest(file.FullPath, settings);
        }
    });

Task("Pack")
    .Does(() =>
    {
        var msBuildSettings = new DotNetCoreMSBuildSettings();
        msBuildSettings.SetVersion(semVersion);

        var settings = new DotNetCorePackSettings
        {
            Configuration = configuration,
            OutputDirectory = "../artifacts",
            MSBuildSettings = msBuildSettings,
            NoBuild = true
        };

        DotNetCorePack("../src/Akka.Cluster.SplitBrainResolver", settings);
    });    

Task("Push")
    .IsDependentOn("Pack")
    .Does(() =>
    {
        // Get the path to the package.
        var package = $"../artifacts/Akka.Cluster.SplitBrainResolver.{semVersion}.nupkg";

        // Push the package.
        NuGetPush(package, new NuGetPushSettings {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = nugetApiKey
        });
    });    

RunTarget(target);