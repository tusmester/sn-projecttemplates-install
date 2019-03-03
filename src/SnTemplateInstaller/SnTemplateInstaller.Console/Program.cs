using SnTemplateInstaller.Main;
using System;

namespace SnTemplateInstaller.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            GitConnector.GetTemplateRepository("D:\\Dev\\github\\test\\sn-vs-tmp");
        }
    }
}
