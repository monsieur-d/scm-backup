using System;
using System.IO;

namespace ScmBackup.Scm
{
    [Scm(Type = ScmType.Git)]
    internal class GitScm : CommandLineScm, IScm
    {
        public GitScm(IFileSystemHelper fileSystemHelper, IContext context) : base(fileSystemHelper, context)
        { }

        public override string ShortName
        {
            get { return "git"; }
        }

        public override string DisplayName
        {
            get { return "Git"; }
        }

        protected override string CommandName
        {
            get { return "git"; }
        }

        private bool LfsFetch => ScmConfig?.LfsFetch ?? false;

        public override bool IsOnThisComputer()
        {
            var result = this.ExecuteCommand("--version");

            if (result.Successful && result.StandardOutput.ToLower().Contains("git version"))
            {
                return true;
            }

            return false;
        }

        public override string GetVersionNumber()
        {
            var result = this.ExecuteCommand("--version");

            if (result.Successful)
            {
                const string search = "git version ";
                return result.StandardOutput.Substring(result.StandardOutput.IndexOf(search) + search.Length).Replace("\n", "");
            }

            throw new InvalidOperationException(result.Output);
        }

        public override bool DirectoryIsRepository(string directory)
        {
            // SCM Backup uses bare repos only, so we don't need to check for non-bare repos at all
            string cmd = $"-C \"{directory}\" rev-parse --is-bare-repository";
            var result = this.ExecuteCommand(cmd);

            if (result.Successful && result.StandardOutput.ToLower().StartsWith("true"))
            {
                return true;
            }

            return false;
        }

        public override void CreateRepository(string directory)
        {
            if (!this.DirectoryIsRepository(directory))
            {
                string cmd = $"init --bare \"{directory}\"";
                var result = this.ExecuteCommand(cmd);

                if (!result.Successful)
                {
                    throw new InvalidOperationException(result.Output);
                }
            }
        }

        public override bool RemoteRepositoryExists(string remoteUrl, ScmCredentials credentials)
        {
            if (credentials != null)
            {
                remoteUrl = this.CreateRepoUrlWithCredentials(remoteUrl, credentials);
            }

            string cmd = "ls-remote " + remoteUrl;
            var result = this.ExecuteCommand(cmd);

            return result.Successful;
        }

        public override void PullFromRemote(string remoteUrl, string directory, ScmCredentials credentials)
        {
            if (!this.DirectoryIsRepository(directory))
            {
                if (Directory.Exists(directory) && !this.FileSystemHelper.DirectoryIsEmpty(directory))
                {
                    throw new InvalidOperationException(string.Format(Resource.ScmTargetDirectoryNotEmpty, directory));
                }
                
                this.CreateRepository(directory);
            }

            if (credentials != null)
            {
                remoteUrl = this.CreateRepoUrlWithCredentials(remoteUrl, credentials);
            }
            
            string cmd = $"-C \"{directory}\" fetch --force --prune {remoteUrl} refs/heads/*:refs/heads/* refs/tags/*:refs/tags/*";
            var result = this.ExecuteCommand(cmd);

            if (!result.Successful)
            {
                throw new InvalidOperationException(result.Output);
            }

            if (LfsFetch)
            {
                string pullLfsCmd = $"-C \"{directory}\" lfs fetch --all {remoteUrl}";
                var resultPullLfs = this.ExecuteCommand(pullLfsCmd);

                if (!resultPullLfs.Successful)
                {
                    throw new InvalidOperationException(resultPullLfs.Output);
                }
            }


        }

        public override bool RepositoryContainsCommit(string directory, string commitid)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(string.Format(Resource.DirectoryDoesntExist, directory));
            }

            if (!this.DirectoryIsRepository(directory))
            {
                throw new InvalidOperationException(string.Format(Resource.DirectoryNoRepo, directory));
            }

            // https://stackoverflow.com/a/21878920/6884
            string cmd = $"-C \"{directory}\" rev-parse --quiet --verify {commitid}^{{commit}}";
            var result = this.ExecuteCommand(cmd);

            if (result.Successful && result.Output.StartsWith(commitid))
            {
                return true;
            }

            return false;
        }

        public string CreateRepoUrlWithCredentials(string url, ScmCredentials credentials)
        {
            // https://stackoverflow.com/a/10054470/6884
            var uri = new UriBuilder(url);
            uri.UserName = credentials.User;
            uri.Password = credentials.Password;
            return uri.ToString();
        }
    }
}