using SnTemplateInstaller.Main;
using System;
using System.Threading.Tasks;
using CommandLine;

namespace SnTemplateInstaller.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Options options = null;

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o => { options = o; })
                .WithNotParsed(errors => { System.Console.WriteLine("Finished with error."); });

            if (options != null)
                await InstallSensenet(options);
        }

        private static async Task InstallSensenet(Options o)
        {
            var installer = GetInstaller();

            try
            {
                await installer.InstallSensenet(
                    o.Template,
                    o.Feature,
                    o.RepositoryPath,
                    o.TargetPath,
                    o.DatabaseServer).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
            }
        }

        private static Installer GetInstaller()
        {
            var installer = new Installer();

            installer.OnTaskStarted += (sender, e) =>
            {
                System.Console.WriteLine($"{ e.Task }...");
            };
            installer.OnTaskFinished += (sender, e) =>
            {
                System.Console.WriteLine($"{ e.Task } finished.".PadRight(50, '.') + $"{e.Elapsed}");

                if (e.ElapsedFull > TimeSpan.Zero)
                {
                    System.Console.WriteLine("------------------------------------------------------------------");
                    System.Console.WriteLine($"FINISHED: {e.ElapsedFull}");
                    System.Console.WriteLine("------------------------------------------------------------------");
                }
            };

            return installer;
        }
    }
}
