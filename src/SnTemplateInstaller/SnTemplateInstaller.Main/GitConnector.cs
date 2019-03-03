using LibGit2Sharp;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace SnTemplateInstaller.Main
{
    public class GitConnector
    {
        private const string TemplateRepoGitUrl = "https://github.com/SenseNet/sn-vs-projecttemplates.git";
        private const string DefaultRepositoryLocalPath = "D:\\Dev\\github\\sn-vs-projecttemplates";
        private const string DefaultBranchName = "develop";

        public static void GetTemplateRepository(
            string repositoryLocalPath = DefaultRepositoryLocalPath, 
            string branchName = DefaultBranchName)
        {
            if (string.IsNullOrEmpty(repositoryLocalPath))
                throw new SnTemplateInstallerException("Repository local path cannot be empty.");
            if (string.IsNullOrEmpty(branchName))
                throw new SnTemplateInstallerException("Beanch name cannot be empty.");

            try
            {
                if (System.IO.Directory.Exists(repositoryLocalPath))
                {
                    using (var repo = new Repository(repositoryLocalPath))
                    {
                        var branch = repo.Branches[branchName];
                        if (branch == null)
                        {
                            var trackedBranch = repo.Branches[$"origin/{branchName}"];
                            if (trackedBranch == null)
                                throw new SnTemplateInstallerException($"Branch {branchName} not found.");

                            branch = repo.CreateBranch(branchName, $"origin/{branchName}");
                            repo.Branches.Update(branch, b => b.TrackedBranch = trackedBranch.CanonicalName);
                        }

                        if (branch == null)
                            throw new SnTemplateInstallerException($"Branch {branchName} does not exist.");
                        
                        var currentBranch = Commands.Checkout(repo, branch);

                        foreach (var br in repo.Branches)
                        {
                            Console.WriteLine($"{br.FriendlyName} {br.RemoteName} IsRemote:{br.IsRemote} IsTracking:{br.IsTracking}");
                        }

                        // User information to create a merge commit
                        var signature = new Signature(new Identity("MERGE_USER_NAME", "MERGE_USER_EMAIL"), DateTimeOffset.Now);

                        Commands.Pull(repo, signature, null);
                    }
                }
                else
                {
                    CloneTemplateRepository(repositoryLocalPath, branchName);
                }
            }
            catch (Exception ex)
            {
                throw new SnTemplateInstallerException(ex.Message);
            }
        }
        private static void CloneTemplateRepository(string repositoryLocalPath, string branchName)
        {
            try
            {
                Repository.Clone(TemplateRepoGitUrl, repositoryLocalPath, new CloneOptions
                {
                    BranchName = branchName
                });
            }
            catch (LibGit2SharpException ex)
            {
                throw new SnTemplateInstallerException(ex.Message);
            }
        }
    }
}
