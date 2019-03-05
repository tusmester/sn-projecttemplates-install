using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SnTemplateInstaller.Main
{
    public class Installer
    {
        private const string DefaultDbServer = ".\\sql2016";
        private const string DefaultTargetFolder = "d:\\Dev\\github\\temp\\TEST";

        public event EventHandler<InstallerEventArgs> OnTaskStarted;
        public event EventHandler<InstallerEventArgs> OnTaskFinished;

        public async Task InstallSensenet(string solutionName, string featureName, 
            string repositoryLocalPath = GitConnector.DefaultRepositoryLocalPath, 
            string targetParent = DefaultTargetFolder,
            string dbServer = DefaultDbServer)
        {
            if (string.IsNullOrEmpty(solutionName))
                throw new SnTemplateInstallerException("Solution name cannot be empty.");
            if (string.IsNullOrEmpty(featureName))
                throw new SnTemplateInstallerException("Feature name cannot be empty.");

            if (string.IsNullOrEmpty(targetParent))
                targetParent = DefaultTargetFolder;
            if (string.IsNullOrEmpty(dbServer))
                dbServer = DefaultDbServer;
            if (string.IsNullOrEmpty(repositoryLocalPath))
                repositoryLocalPath = GitConnector.DefaultRepositoryLocalPath;

            var targetPath = Path.Combine(targetParent, featureName);

            var taskTimer = Stopwatch.StartNew();
            var fulltime = TimeSpan.Zero;

            LogTime(null, "Preparing git repository");

            await Task.Run(() => GitConnector.GetTemplateRepository()).ConfigureAwait(false);

            LogTime("Preparing git repository", "Copying solution");

            await CopySolution(repositoryLocalPath, solutionName, targetPath).ConfigureAwait(false);
            
            LogTime("Copying solution", "Building solution");

            await Task.Run(() => BuildSolution(targetPath)).ConfigureAwait(false);

            LogTime("Building solution", "Installing packages");

            await Task.Run(() => InstallPackages(targetPath, dbServer, featureName)).ConfigureAwait(false);

            LogTime("Installing packages", null);

            void LogTime(string finishedTaskTitle, string nextTaskTitle)
            {
                taskTimer.Stop();
                fulltime = fulltime.Add(taskTimer.Elapsed);

                if (!string.IsNullOrEmpty(finishedTaskTitle))
                {
                    OnTaskFinished?.Invoke(this, new InstallerEventArgs
                    {
                        Task = finishedTaskTitle,
                        Elapsed = taskTimer.Elapsed,
                        ElapsedFull = string.IsNullOrEmpty(nextTaskTitle) ? fulltime : TimeSpan.Zero
                    });
                }

                if (!string.IsNullOrEmpty(nextTaskTitle))
                {
                    OnTaskStarted?.Invoke(this, new InstallerEventArgs
                    {
                        Task = nextTaskTitle
                    });
                }
                
                taskTimer.Restart();
            }
        }

        public static void InstallPackages(string targetDirectory, string dbServer, string featureName)
        {
            var psPath = Path.Combine(targetDirectory, "install-all.ps1");
            if (!File.Exists(psPath))
                return;

            int result;

            try
            {
                result = RunInstallScript(psPath, dbServer, featureName);
            }
            catch (Exception ex)
            {
                throw new SnTemplateInstallerException(ex.Message);
            }

            if (result != 0)
                throw new SnTemplateInstallerException("Package install failed, error code: " + result);
        }

        private static int RunInstallScript(string scriptPath, string dbServer, string featureName)
        {
            int errorLevel;
            var processInfo = new ProcessStartInfo("powershell.exe", 
                $"-File \"{scriptPath}\" \"{dbServer}\" sn{featureName.ToLower()}")
            {
                CreateNoWindow = true,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? string.Empty
            };

            using (var process = Process.Start(processInfo))
            {
                process?.WaitForExit();

                errorLevel = process?.ExitCode ?? 0;
            }

            return errorLevel;
        }

        private static void BuildSolution(string targetDirectory)
        {
            var solutionPath = Path.Combine(targetDirectory, "SnWebApplication.sln");

            try
            {
                using (var pr = new Process())
                {
                    pr.StartInfo = new ProcessStartInfo("nuget", $"restore \"{solutionPath}\"")
                    {
                        CreateNoWindow = true,
                        RedirectStandardOutput = false,
                        UseShellExecute = false
                    };
                    pr.OutputDataReceived += (sender, args) => { };

                    pr.Start();

                    pr.WaitForExit(30000);
                }

                using (var pr = new Process())
                {
                    //UNDONE: execute msbuild without a hardcoded path
                    pr.StartInfo = new ProcessStartInfo(
                        "C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Enterprise\\MSBuild\\15.0\\Bin\\msbuild.exe", $"\"{solutionPath}\"")
                    {
                        CreateNoWindow = true,
                        RedirectStandardOutput = false,
                        UseShellExecute = false
                    };
                    pr.OutputDataReceived += (sender, args) => { };

                    pr.Start();

                    pr.WaitForExit(30000);
                }
            }
            catch (Exception ex)
            {
                throw new SnTemplateInstallerException(ex.Message);
            }
        }

        private static async Task CopySolution(string repositoryLocalPath, string solutionName, string targetPath)
        {
            if (string.IsNullOrEmpty(targetPath))
                throw new SnTemplateInstallerException("Target path cannot be empty.");
            if (string.IsNullOrEmpty(repositoryLocalPath))
                throw new SnTemplateInstallerException("Repository path cannot be empty.");
            if (string.IsNullOrEmpty(solutionName))
                throw new SnTemplateInstallerException("Solution name cannot be empty.");

            if (Directory.Exists(targetPath) && Directory.EnumerateFileSystemEntries(targetPath).Any())
                throw new SnTemplateInstallerException($"Directory {targetPath} exists and is not empty.");

            var solutionFolderPath = $"{repositoryLocalPath}\\src\\{solutionName}";
            if (!Directory.Exists(solutionFolderPath))
                throw new SnTemplateInstallerException($"Directory {solutionFolderPath} does not exist.");
            if (!Directory.EnumerateFiles(solutionFolderPath, "*.sln").Any())
                throw new SnTemplateInstallerException($"Directory {solutionFolderPath} does not contain a solution.");

            await CopyContents(solutionFolderPath, targetPath);
        }
        private static async Task CopyContents(string sourcePath, string targetPath)
        {
            foreach (var sourceDirectory in Directory.EnumerateDirectories(sourcePath))
            {
                var dirInfo = new DirectoryInfo(sourceDirectory);
                var targetDirectory = Path.Combine(targetPath, dirInfo.Name);

                Directory.CreateDirectory(targetDirectory);

                await CopyContents(sourceDirectory, targetDirectory);
            }

            await CopyFiles(sourcePath, targetPath);
        }
        private static async Task CopyFiles(string sourcePath, string targetPath)
        {
            foreach (var filename in Directory.EnumerateFiles(sourcePath))
            {
                using (var sourceStream = File.Open(filename, FileMode.Open))
                {
                    using (var destinationStream = File.Create(targetPath + filename.Substring(filename.LastIndexOf('\\'))))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
                    }
                }
            }
        }
    }
}
