using SnTemplateInstaller.Main;
using System;
using System.Threading.Tasks;

namespace SnTemplateInstaller.Console
{
    class Program
    {
        static async Task Main(string[] args)
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

            try
            {
                await installer.InstallSensenet("SnWebApplication", "feature1").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
            }
        }
    }
}
