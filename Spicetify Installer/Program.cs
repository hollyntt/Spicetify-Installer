using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Spicetify_Installer;

internal static class Program
{
    private static bool _testMode = false;
    private static int _waitSeconds = 2;

    static void Main()
    {
        Console.WriteLine("=== spicetify updater ===\n");

        try
        {
            Console.WriteLine("trying upgrade first...");
            if (RunCommand("spicetify", "upgrade"))
            {
                if (RunCommand("spicetify", "apply"))
                {
                    Console.WriteLine("\nall good!");
                    return;
                }
            }

            Console.WriteLine("\nupgrade failed or not installed, doing full thing...");
            FullInstall();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error: {ex.Message}");
        }
    }

    static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    static bool RunCommand(string filename, string arguments, string description = "")
    {
        if (_testMode)
        {
            Console.WriteLine($"[TEST] pretending fail: {description}");
            return false;
        }

        if (!string.IsNullOrEmpty(description))
            Console.WriteLine($"→ {description}");
        else
            Console.WriteLine($"→ {filename} {arguments}");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
                Console.WriteLine(output.Trim());

            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine(error.Trim());

            if (process.ExitCode != 0)
            {
                Console.WriteLine("failed :(");
                return false;
            }

            return true;
        }
        catch
        {
            Console.WriteLine("something broke");
            return false;
        }
    }

    static void FullInstall()
    {
        Console.WriteLine("\n=== doing full install ===");

        if (_waitSeconds > 0)
            Thread.Sleep(_waitSeconds * 1000);

        if (IsWindows())
        {
            Console.WriteLine("windows detected");
            string psCommand = "iwr -useb https://raw.githubusercontent.com/spicetify/cli/main/install.ps1 | iex";
            RunPowerShell(psCommand);
        }
        else
        {
            Console.WriteLine("linux/mac detected");
            string cmd = "curl -fsSL https://raw.githubusercontent.com/spicetify/cli/main/install.sh | sh";
            RunCommand("sh", $"-c \"{cmd}\"", "Spicetify Installer");
        }

        Console.WriteLine("applying...");
        RunCommand("spicetify", "apply");

        Console.WriteLine("\ndone. go open spotify and grab marketplace");
    }

    static void RunPowerShell(string command)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine(output.Trim());
            if (!string.IsNullOrWhiteSpace(error)) Console.WriteLine(error.Trim());
        }
        catch
        {
            Console.WriteLine("powershell installer failed");
        }
    }
}