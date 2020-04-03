using ScmBackup.Hosters;
using System.Collections.Generic;

namespace ScmBackup
{
    internal interface IBackupMaker
    {
        bool Backup(ConfigSource source, IEnumerable<HosterRepository> repos);
    }
}
