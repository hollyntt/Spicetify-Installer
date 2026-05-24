using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Spicetify_Installer;

internal static class Program
{
    private static bool _testMode = false;
    private static int _waitSeconds = 2;
    private static readonly int CommandTimeoutSeconds = 60;

    static void Main()
    {
        Console.WriteLine("=== Spicetify Updater ===\n");
        Console.WriteLine("Make sure Spotify is closed before running this.\n");

        try
        {
            // Kill any existing spicetify processes first
            KillSpicetifyProcesses();

            Console.WriteLine("Trying upgrade first...");
            bool upgradeSuccess = RunCommand("spicetify", "upgrade");

            if (upgradeSuccess)
            {
                if (RunCommand("spicetify", "apply"))
                {
                    Console.WriteLine("\n✅ All good!");
                    return;
                }
            }

            Console.WriteLine("\nUpgrade failed or not installed → doing full install...");
            FullInstall();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Critical error: {ex.Message}");
        }
    }

    static void KillSpicetifyProcesses()
    {
        try
        {
            foreach (var proc in Process.GetProcessesByName("spicetify"))
            {
                if (!proc.HasExited)
                {
                    proc.Kill(true);
                    proc.WaitForExit(2000);
                }
            }
            foreach (var proc in Process.GetProcessesByName("spotify"))
            {
                // Optional: don't kill Spotify unless you want to
            }
        }
        catch { }
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

        Process? process = null;
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process = new Process { StartInfo = startInfo };
            process.Start();

            // Feed input in case it prompts
            try
            {
                process.StandardInput.WriteLine("y");
                process.StandardInput.WriteLine();
                process.StandardInput.Close();
            }
            catch { }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (!process.WaitForExit(CommandTimeoutSeconds * 1000))
            {
                Console.WriteLine($"⚠️ Timed out after {CommandTimeoutSeconds}s - killing...");
                try { process.Kill(true); } catch { }
                return false;
            }

            if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine(output.Trim());
            if (!string.IsNullOrWhiteSpace(error)) Console.WriteLine("ERROR: " + error.Trim());

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to run command: {ex.Message}");
            return false;
        }
        finally
        {
            // Aggressive cleanup
            if (process != null)
            {
                try
                {
                    if (!process.HasExited) process.Kill(true);
                    process.Dispose();
                }
                catch { }
            }
        }
    }

    static void FullInstall()
    {
        Console.WriteLine("\n=== Full Install ===");

        if (_waitSeconds > 0)
            Thread.Sleep(_waitSeconds * 1000);

        KillSpicetifyProcesses();

        if (IsWindows())
        {
            Console.WriteLine("Windows detected - running PowerShell installer...");
            RunPowerShellInstall();
        }
        else
        {
            Console.WriteLine("Linux/Mac detected");
            string cmd = "curl -fsSL https://raw.githubusercontent.com/spicetify/cli/main/install.sh | sh";
            RunCommand("sh", $"-c \"{cmd}\"", "Installing Spicetify");
        }

        Console.WriteLine("Applying changes...");
        RunCommand("spicetify", "backup apply");

        Console.WriteLine("\n🎉 Done! Launch Spotify now.");
    }

    static void RunPowerShellInstall()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"iwr -useb https://raw.githubusercontent.com/spicetify/cli/main/install.ps1 | iex\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit(90000); // 90 second timeout for installer

            if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine(output.Trim());
            if (!string.IsNullOrWhiteSpace(error)) Console.WriteLine("PS Error: " + error.Trim());

            Console.WriteLine(process.ExitCode == 0 ? "✅ Installer finished" : "❌ Installer failed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ PowerShell install failed: {ex.Message}");
        }
    }
}