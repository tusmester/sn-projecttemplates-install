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
        internal const string DefaultRepositoryLocalPath = "D:\\Dev\\github\\sn-vs-projecttemplates";
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
                if (!System.IO.Directory.Exists(repositoryLocalPath))
                {
                    Repository.Clone(TemplateRepoGitUrl, repositoryLocalPath, new CloneOptions
                    {
                        BranchName = branchName
                    });
                }

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

                    Commands.Checkout(repo, branch);

                    // User information to create a merge commit
                    var signature = new Signature(new Identity("MERGE_USER_NAME", "MERGE_USER_EMAIL"),
                        DateTimeOffset.Now);

                    Commands.Pull(repo, signature, new PullOptions
                    {
                        MergeOptions = new MergeOptions
                        {
                            CommitOnSuccess = false,
                            FailOnConflict = true 
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                throw new SnTemplateInstallerException(ex.Message);
            }
        }
    }
}
