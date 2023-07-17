using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace MusicPipeBot.Services;

public class PipeService : IPipeService
{
    private const string UserfilesDirName = "userfiles";
    private readonly ILogger<PipeService> _logger;

    public PipeService(ILogger<PipeService> logger)
    {
        _logger = logger;
    }

    /// <returns>Path to saved file</returns>
    public string? DownloadTrack(string query)
    {
        var downloadId = Guid.NewGuid();
        var downloadPath = $"{UserfilesDirName}/{downloadId}";
        Directory.CreateDirectory(downloadPath);

        try
        {
            var result = ExecuteCommandLine($"spotdl download {query}", downloadPath);
            if (result.Contains("SongError: No results found for"))
            {
                _logger.LogWarning("Couldn't find track download link for query '{query}'", query);
                RemoveTemporaryDirectories(new[] { downloadId.ToString() });
                return null;
            }

            var trackPath = Directory.GetFiles(downloadPath).FirstOrDefault();
            _logger.LogInformation("Loaded file {path}", trackPath);
            _logger.LogInformation("Full SpotDL response: {response}", result);
            return trackPath;
        }
        catch (Exception e)
        {
            _logger.LogError("SpotDL execution failed. Error: {message}", e.Message);
            return null;
        }
    }

    public bool RemoveTemporaryDirectories(IEnumerable<string> directories)
    {
        foreach (var dir in directories)
        {
            try
            {
                Directory.Delete($"{UserfilesDirName}/{dir}", true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Couldn't delete dir {name}. Error: {error}", dir, ex.Message);
                return false;
            }
        }

        _logger.LogInformation("Successfully removed all temporary directories");
        return true;
    }

    private static string ExecuteCommandLine(string arguments, string? workingDirectory = null)
    {
        var (command, cmdName) = GetPlatformSpecificParams(arguments);
        var process = new Process();
        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = workingDirectory,
            FileName = cmdName,
            Arguments = command,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process.StartInfo = startInfo;
        process.Start();

        var standardOutput = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return standardOutput;
    }

    private static (string Command, string CmdName) GetPlatformSpecificParams(string arguments)
    {
        string command;
        string cmdName;

        var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        if (isLinux)
        {
            command = $"-c \"{arguments}\"";
            cmdName = "/bin/bash";
        }
        else
        {
            // '/C' carries out the specified command and then terminates
            command = $"/C {arguments}";
            cmdName = "cmd.exe";
        }

        return (command, cmdName);
    }
}