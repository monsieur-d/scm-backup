using System.Collections.Generic;

namespace ScmBackup
{
    /// <summary>
    /// Holds all configuration values
    /// </summary>
    internal class Config
    {
        public string LocalFolder { get; set; }

        public string BackupTargetFolder { get; set; }

        public int NumberOfBackupsToRetain { get; set; }

        public int NumberOfLongTermBackupsToRetain { get; set; }

        public string MaxTotalBackupsSize { get; set; }

        /// <summary>
        /// is this a long term backup => do not check if repo has changed, backup anyway + mark backup as lt backup
        /// </summary>
        public bool IsLongTermBackup { get; set; }

        public int WaitSecondsOnError { get; set; }

        public List<ConfigScm> Scms { get; set; }

        public List<ConfigSource> Sources { get; set; }

        public ConfigEmail Email { get; set; }

        public Config()
        {
            this.Sources = new List<ConfigSource>();
            this.Scms = new List<ConfigScm>();
        }
    }
}
