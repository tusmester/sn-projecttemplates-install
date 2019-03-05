using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace SnTemplateInstaller.Console
{
    public class Options
    {
        [Option('f', "feature", Required = true, HelpText = "Feature name.")]
        public string Feature { get; set; }

        [Option('t', "template", Required = false, HelpText = "Template name.", Default = "SnWebApplication")]
        public string Template { get; set; }

        [Option('r', "repository", Required = false, HelpText = "Template repository path on the local machine.")]
        public string RepositoryPath { get; set; }

        [Option('t', "target", Required = false, HelpText = "Target folder path.")]
        public string TargetPath { get; set; }

        [Option('d', "databaseserver", Required = false, HelpText = "Database server name.")]
        public string DatabaseServer { get; set; }
    }
}
