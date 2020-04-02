using System;
using ScmBackup.Hosters;
using ScmBackup.Http;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ByteSizeLib;
using Ionic.Zip;

namespace ScmBackup
{
    /// <summary>
    /// Backs up all repositories from a single source
    /// </summary>
    internal class BackupMaker : IBackupMaker
    {
        private const string LongTermBackupSuffix = "_LT";
        private const string archiveFormatFileEnding = "zip";
        private readonly ILogger logger;
        private readonly IFileSystemHelper fileHelper;
        private readonly IHosterBackupMaker backupMaker;
        private readonly IContext context;

        public BackupMaker(ILogger logger, IFileSystemHelper fileHelper, IHosterBackupMaker backupMaker, IContext context)
        {
            this.logger = logger;
            this.fileHelper = fileHelper;
            this.backupMaker = backupMaker;
            this.context = context;
        }

        public void Backup(ConfigSource source, IEnumerable<HosterRepository> repos)
        {
            var isLongTermBackup = context.Config.IsLongTermBackup;

            var logMessage = Resource.BackupMaker_Source;
            if (isLongTermBackup) 
                logMessage += " (long term backup)";
            this.logger.Log(ErrorLevel.Info, logMessage, source.Title);
            
            string sourceFolder = this.fileHelper.CreateSubDirectory(context.Config.LocalFolder, $"{source.Title}{(isLongTermBackup ? LongTermBackupSuffix : "")}");

            var url = new UrlHelper();

            var reposToZip = new List<string>();

            foreach (var repo in repos)
            {
                string repoFolderPath = this.fileHelper.CreateSubDirectory(sourceFolder, repo.FullName);

                var lastUpdateFilePath = Path.Combine(repoFolderPath, "lastUpdated");

                if (!isLongTermBackup && File.Exists(lastUpdateFilePath) &&
                    DateTime.TryParse(File.ReadAllText(lastUpdateFilePath), out var lastUpdated) &&
                    repo.LastUpdated <= lastUpdated) 
                    continue;

                var logUrl = repo.CloneUrl;
                if (source.ScmAuthenticationType == ScmAuthenticationType.Https)
                    logUrl = url.RemoveCredentialsFromUrl(logUrl);

                this.logger.Log(ErrorLevel.Info, Resource.BackupMaker_Repo, repo.Scm.ToString(), logUrl);

                this.backupMaker.MakeBackup(source, repo, repoFolderPath);

                File.WriteAllText(lastUpdateFilePath, repo.LastUpdated.ToString("o"));

                reposToZip.Add(repoFolderPath);
            }

            if (!reposToZip.Any())
            {
                logger.Log(ErrorLevel.Info, Resource.NothingHasChanged, source.Title);
                return;
            }

            var destinationPath = Path.Combine(context.Config.BackupTargetFolder,
                $"{source.Name}_{DateTime.Now:yyyyMMdd-HHmm}{(isLongTermBackup ? LongTermBackupSuffix : "")}.{archiveFormatFileEnding}");

            logger.Log(ErrorLevel.Info, Resource.ZippingRepo, source.Title, destinationPath);
            
            using var zipFile = new ZipFile
            {
                CompressionMethod = CompressionMethod.Deflate,
                CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression
            };
            foreach (var repoPath in reposToZip)
            {
                zipFile.AddDirectory(repoPath, new DirectoryInfo(repoPath).Name);
            }

            zipFile.Save(destinationPath);

            CleanUpBackups();
        }

        private void CleanUpBackups()
        {
            logger.Log(ErrorLevel.Info, Resource.CleaningUpBackup);
            var backupDirInfo = new DirectoryInfo(context.Config.BackupTargetFolder);
            var backupFiles = backupDirInfo
                .EnumerateFiles()
                .OrderByDescending(file => file.CreationTime)
                .ToList();


            var longTermBackupFilesEnding = $"{LongTermBackupSuffix}.{archiveFormatFileEnding}";

            var standardBackupFiles =
                backupFiles.Where(file => !file.Name.EndsWith(longTermBackupFilesEnding));
            var standardBackupFilesToDelete = standardBackupFiles.Skip(context.Config.NumberOfBackupsToRetain);
            foreach (var fileInfo in standardBackupFilesToDelete)
            {
                logger.Log(ErrorLevel.Info, Resource.RemovingOldBackup, fileInfo.Name);
                fileInfo.Delete();
            }

            var longTermBackupFiles =
                backupFiles.Where(file => file.Name.EndsWith(longTermBackupFilesEnding));
            var longTermFilesToDelete = longTermBackupFiles.Skip(context.Config.NumberOfLongTermBackupsToRetain);
            foreach (var fileInfo in longTermFilesToDelete)
            {
                logger.Log(ErrorLevel.Info, Resource.RemovingOldBackup, fileInfo.Name);
                fileInfo.Delete();
            }

            if (context.Config.MaxTotalBackupsSize == null)
                return;

            if (!ByteSize.TryParse(context.Config.MaxTotalBackupsSize, out var maxTotalBackupsSize))
            {
                logger.Log(ErrorLevel.Error, Resource.MaxTotalBackupSizeInWrongFormat);
                return;
            }

            //delete oldest files to get under the max total size limitation
            //refresh and order by descending now
            backupFiles = backupDirInfo
                .EnumerateFiles()
                .OrderByDescending(file => file.CreationTime)
                .ToList();

            var currentAggregatedSize = 0L;
            var backupFilesLargerThanMaxSize = 
                backupFiles.Select(file =>
                {
                    var accumulatedSize = currentAggregatedSize += file.Length;
                    return (file: file, totalSize: accumulatedSize);
                })
                .SkipWhile(fileTuple => fileTuple.totalSize <= maxTotalBackupsSize.Bytes)
                .Select(fileTuple => fileTuple.file)
                .ToList();

            foreach (var fileInfo in backupFilesLargerThanMaxSize)
            {
                logger.Log(ErrorLevel.Info, Resource.RemovingOldBackup, fileInfo.Name);
                fileInfo.Delete();
            }

            logger.Log(ErrorLevel.Info, Resource.CleaningUpFinished);

        }
    }
}
