using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Repeatable WebGL builds for the itch.io release, from the menu or from the command
/// line so the settings that matter for the web target cannot drift between builds.
/// </summary>
public static class WebGLBuilder
{
    private const string DefaultOutputDirectory = "Builds/WebGL";

    [MenuItem("Build/WebGL (itch.io)")]
    public static void BuildFromMenu()
    {
        BuildReport report = RunBuild(ResolveOutputPath());
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"WebGL build failed: {report.summary.result}");
        }
    }

    /// <summary>
    /// Command line entry point. Exits non-zero on failure so a script can detect it:
    /// Unity.exe -batchmode -quit -projectPath . -buildTarget WebGL
    ///           -executeMethod WebGLBuilder.BuildFromCommandLine -outputPath Builds/WebGL
    /// </summary>
    public static void BuildFromCommandLine()
    {
        try
        {
            BuildReport report = RunBuild(ResolveOutputPath());
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
        catch (Exception exception)
        {
            Debug.LogError($"WebGL build threw: {exception}");
            EditorApplication.Exit(1);
        }
    }

    private static BuildReport RunBuild(string outputPath)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes in the build settings.");
        }

        Directory.CreateDirectory(outputPath);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Debug.Log(
            $"WebGL build {summary.result} -> {outputPath} " +
            $"({summary.totalSize / (1024f * 1024f):F1} MB in {summary.totalTime})");

        return report;
    }

    private static string ResolveOutputPath()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i] == "-outputPath")
            {
                return arguments[i + 1];
            }
        }

        return DefaultOutputDirectory;
    }
}
