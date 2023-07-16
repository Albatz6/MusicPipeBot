using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MusicPipeBot.Services;

public class PipeService : IPipeService
{
    private readonly ILogger<PipeService> _logger;

    public PipeService(ILogger<PipeService> logger)
    {
        _logger = logger;
    }

    /// <returns>Path to saved file</returns>
    public string? DownloadTrack(string query)
    {
        var downloadId = Guid.NewGuid().ToString();
        Directory.CreateDirectory(downloadId);

        var result = ExecuteCommandLine($"spotdl download {query}", downloadId);
        if (result.Contains("SongError: No results found for"))
        {
            _logger.LogWarning("Couldn't find track download link for query '{query}'", query);
            RemoveTemporaryDirectories(new[] { downloadId });
            return null;
        }

        var trackPath = Directory.GetFiles(downloadId).FirstOrDefault();
        _logger.LogInformation("Loaded file {path}", result);
        return trackPath;
    }

    public bool RemoveTemporaryDirectories(IEnumerable<string> dirNames)
    {
        foreach (var dir in dirNames)
        {
            try
            {
                Directory.Delete(dir, true);
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
        // '/C' carries out the specified command and then terminates
        var command = $"/C {arguments}";
        var process = new Process();
        var startInfo = new ProcessStartInfo
        {
            // Todo: remember there's Bash
            WorkingDirectory = workingDirectory,
            FileName = "cmd.exe",
            Arguments = command,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        process.StartInfo = startInfo;
        process.Start();

        var standardOutput = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return standardOutput;
    }
}