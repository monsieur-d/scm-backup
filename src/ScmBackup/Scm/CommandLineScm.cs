using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ScmBackup.Scm
{
    internal abstract class CommandLineScm : IScm
    {
        private string executable;

        public abstract string ShortName { get; }

        public abstract string DisplayName { get; }

        protected IContext Context { get; set; }

        public IFileSystemHelper FileSystemHelper { get; set; }
        /// <summary>
        /// The command that needs to be called
        /// </summary>
        protected abstract string CommandName { get; }

        public CommandLineScm(IFileSystemHelper fileSystemHelper, IContext context)
        {
            FileSystemHelper = fileSystemHelper;
            Context = context;
        }

        /// <summary>
        /// Check whether the SCM exists on this computer
        /// Must be implemented in the child classes by calling ExecuteCommand and checking the result.
        /// </summary>
        public abstract bool IsOnThisComputer();

        /// <summary>
        /// Gets the SCM's version number.
        /// Must be implemented in the child classes by calling ExecuteCommand and checking the result.
        /// Should throw exceptions if the version number can't be determined.
        /// </summary>
        public abstract string GetVersionNumber();

        /// <summary>
        /// Executes the command line tool.
        /// </summary>
        protected CommandLineResult ExecuteCommand(string args)
        {
            if (string.IsNullOrWhiteSpace(this.executable))
            {
                this.GetExecutable();
            }

            var info = new ProcessStartInfo
            {
                FileName = this.executable,
                Arguments = args,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var proc = Process.Start(info);
            var result = new CommandLineResult
            {
                StandardError = proc.StandardError.ReadToEnd(), StandardOutput = proc.StandardOutput.ReadToEnd()
            };
            proc.WaitForExit();

            result.ExitCode = proc.ExitCode;
            return result;
        }

        /// <summary>
        /// Checks whether the given directory is a repository
        /// Must be implemented in the child classes by calling ExecuteCommand and checking the result.
        /// </summary>
        public abstract bool DirectoryIsRepository(string directory);

        /// <summary>
        /// Creates a repository in the given directory
        /// Must be implemented in the child classes by calling ExecuteCommand and checking the result.
        /// </summary>
        public abstract void CreateRepository(string directory);

        /// <summary>
        /// Checks whether a repository exists under the given URL
        /// </summary>
        public bool RemoteRepositoryExists(string remoteUrl)
        {
            return RemoteRepositoryExists(remoteUrl, null);
        }

        /// <summary>
        /// Checks whether a repository exists under the given URL
        /// </summary>
        public abstract bool RemoteRepositoryExists(string remoteUrl, ScmCredentials credentials);

        /// <summary>
        /// Pulls from a remote repository into a local folder.
        /// If the folder doesn't exist or is not a repository, it's created first.
        /// Must be implemented in the child classes by calling ExecuteCommand and checking the result.
        /// </summary>
        public void PullFromRemote(string remoteUrl, string directory)
        {
            PullFromRemote(remoteUrl, directory, null);
        }

        /// <summary>
        /// Pulls from a remote repository into a local folder.
        /// If the folder doesn't exist or is not a repository, it's created first.
        /// Must be implemented in the child classes by calling ExecuteCommand and checking the result.
        /// </summary>
        public abstract void PullFromRemote(string remoteUrl, string directory, ScmCredentials credentials);

        /// <summary>
        /// Checks whether the repo in this directory contains a commit with this ID
        /// Must be implemented in the child classes by calling ExecuteCommand and checking the result.
        /// </summary>
        public abstract bool RepositoryContainsCommit(string directory, string commitid);

        /// <summary>
        /// Gets the file to execute
        /// (either a complete path from the config, or this.CommandName)
        /// </summary>
        private void GetExecutable()
        {
            this.executable = this.CommandName;

            // check if there's an path in the "Scms" section in the config
            // (if it's there, the file MUST exist!)
            if (Context == null)
            {
                throw new InvalidOperationException(Resource.CommandLineScm_ContextIsNull);
            }

            var config = Context.Config;
            var configValue = config.Scms?
                .FirstOrDefault(s => string.Equals(s.Name, this.ShortName, StringComparison.CurrentCultureIgnoreCase));
                
            if (configValue == null || string.IsNullOrEmpty(configValue.Path)) 
                return;

                    if (!File.Exists(configValue.Path))
                    {
                        throw new FileNotFoundException(string.Format(Resource.ScmNotOnThisComputer + ": {1}", this.DisplayName, configValue.Path));
                    }

                    this.executable = configValue.Path;
                }
            }
        }
