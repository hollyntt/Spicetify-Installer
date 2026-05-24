using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Spicetify_Installer;

internal static class Program
{
    private static bool _testMode = false;
    private static int _waitSeconds = 2;
    private static readonly int CommandTimeoutSeconds = 45; // Increased timeout

    static void Main()
    {
        Console.WriteLine("=== spicetify updater ===\n");

        try
        {
            Console.WriteLine("Trying upgrade first...");
            if (RunCommand("spicetify", "upgrade"))
            {
                if (RunCommand("spicetify", "apply"))
                {
                    Console.WriteLine("\n✅ All good!");
                    return;
                }
            }

            Console.WriteLine("\nUpgrade failed or not installed, doing full install...");
            FullInstall();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
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
                RedirectStandardInput = true,     // Important: Allow sending input
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            // Send empty input in case spicetify asks for confirmation
            try
            {
                process.StandardInput.WriteLine();
                process.StandardInput.Close();
            }
            catch { /* Ignore if input redirection fails */ }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            // Wait with timeout
            if (!process.WaitForExit(CommandTimeoutSeconds * 1000))
            {
                Console.WriteLine($"⚠️ Command timed out after {CommandTimeoutSeconds} seconds - killing process");
                try { process.Kill(true); } catch { }
                return false;
            }

            if (!string.IsNullOrWhiteSpace(output))
                Console.WriteLine(output.Trim());

            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine(error.Trim());

            if (process.ExitCode != 0)
            {
                Console.WriteLine("❌ failed :(");
                return false;
            }

            Console.WriteLine("✅ Success");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Something broke: {ex.Message}");
            return false;
        }
    }

    static void FullInstall()
    {
        Console.WriteLine("\n=== Doing full install ===");

        if (_waitSeconds > 0)
            Thread.Sleep(_waitSeconds * 1000);

        if (IsWindows())
        {
            Console.WriteLine("Windows detected");
            string psCommand = "iwr -useb https://raw.githubusercontent.com/spicetify/cli/main/install.ps1 | iex";
            RunPowerShell(psCommand);
        }
        else
        {
            Console.WriteLine("Linux/Mac detected");
            string cmd = "curl -fsSL https://raw.githubusercontent.com/spicetify/cli/main/install.sh | sh";
            RunCommand("sh", $"-c \"{cmd}\"", "Spicetify Installer");
        }

        Console.WriteLine("Applying themes and extensions...");
        RunCommand("spicetify", "apply");

        Console.WriteLine("\n🎉 Done! Open Spotify and install the Marketplace.");
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

            if (!string.IsNullOrWhiteSpace(output))
                Console.WriteLine(output.Trim());

            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine(error.Trim());

            if (process.ExitCode == 0)
                Console.WriteLine("✅ PowerShell installer completed");
            else
                Console.WriteLine("❌ PowerShell installer failed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ PowerShell installer failed: {ex.Message}");
        }
    }
}