using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class PostBuildFFmpegCopier : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {

        string buildPath = report.summary.outputPath;
        
        // If build path is a file
        if (!Directory.Exists(buildPath))
        {
            buildPath = Path.GetDirectoryName(report.summary.outputPath);
        }
        
        string platform = report.summary.platform.ToString();

        Debug.Log($"[PostBuild] Platform: {platform}, Output: {buildPath}");

        // Define source and target per platform
        switch (report.summary.platform)
        {
            case BuildTarget.StandaloneWindows64:
                CopyFFmpegWindows(buildPath);
                break;
            case BuildTarget.StandaloneOSX:
                CopyFFmpegMacOS(buildPath);
                break;
            case BuildTarget.StandaloneLinux64:
                CopyFFmpegLinux(buildPath);
                break;
            default:
                Debug.LogWarning($"[PostBuild] No FFmpeg copy logic for {platform}");
                break;
        }
    }

    private void CopyFFmpegWindows(string buildPath)
    {
        string dataDir = Path.Combine(buildPath, "LaspView_Data");
        string source = "Assets/Editor/FFmpeg/Windows/ffmpeg.exe";
        string dest = Path.Combine(dataDir, "ffmpeg.exe");

        CopyFile(source, dest, "Windows");
    }

    private void CopyFFmpegMacOS(string buildPath)
    {
        // macOS: .app bundle
        string contentsDir = Path.Combine(buildPath, "Contents");
        string source = "Assets/Editor/FFmpeg/macOS/ffmpeg";
        string dest = Path.Combine(contentsDir, "ffmpeg");

        CopyFile(source, dest, "macOS");
        // SetExecutable(dest);
    }

    private void CopyFFmpegLinux(string buildPath)
    {
        string dataDir = Path.Combine(buildPath, "LaspView_Data");
        string source = "Assets/Editor/FFmpeg/Linux/ffmpeg";
        string dest = Path.Combine(dataDir, "ffmpeg");

        CopyFile(source, dest, "Linux");
        // SetExecutable(dest);
    }

    private void CopyFile(string source, string dest, string platform)
    {
        if (!File.Exists(source))
        {
            Debug.LogError($"[PostBuild] {platform} FFmpeg not found: {source}");
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            File.Copy(source, dest, true);
            Debug.Log($"[PostBuild] Copied {platform} FFmpeg → {dest}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PostBuild] Failed to copy {platform} FFmpeg: {e.Message}");
        }
    }

    private void SetExecutable(string path)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{path}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
        }
        catch { /* ignore */ }
    }
}