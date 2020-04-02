using System;
using System.Linq;
using System.Reflection;

namespace ScmBackup
{

    /// <summary>
    /// "application context" for global information
    /// </summary>
    internal class Context : IContext
    {
        private class ConfigOverride
        {
            public bool? LfsFetch { get; set; }
            public bool? IsLongTermBackup { get; set; }
        }

        private readonly IConfigReader reader;
        private Config config;
        private readonly ConfigOverride configOverride = new ConfigOverride();

        public Context(IConfigReader reader)
        {
            this.reader = reader;

            var assembly = typeof(ScmBackup).GetTypeInfo().Assembly;
            VersionNumber = assembly.GetName().Version;
            VersionNumberString= assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            AppTitle = Resource.AppTitle + " " + VersionNumberString;
            UserAgent = Resource.AppTitle.Replace(" ", "-");
            var arguments = Environment.GetCommandLineArgs();

            foreach (var argument in arguments)
            {
                switch (argument.TrimStart('-').ToLower())
                {
                    case "lfsfetch":
                        configOverride.LfsFetch = true;
                        break;
                    case "islongtermbackup":
                        configOverride.IsLongTermBackup = true;
                        break;
                }
            }
        }

        public Version VersionNumber { get; private set; }

        public string VersionNumberString { get; private set; }

        public string AppTitle { get; private set; }

        public string UserAgent { get; private set; }

        public Config Config
        {
            get
            {
                if (config == null)
                {
                    config = reader.ReadConfig();

                    if (configOverride.IsLongTermBackup ?? false)
                    {
                        config.IsLongTermBackup = true;
                    }

                    if ((configOverride.LfsFetch ?? false) || config.IsLongTermBackup)
                    {
                        var gitConfig = config.Scms.FirstOrDefault(scm =>
                            scm.Name.Equals("git", StringComparison.InvariantCultureIgnoreCase));
                        if (gitConfig == null)
                        {
                            gitConfig = new ConfigScm{Name = "git"};
                            config.Scms.Add(gitConfig);
                        }

                        gitConfig.LfsFetch = true;
                    }

                }

                return config;
            }
        }
    }
}