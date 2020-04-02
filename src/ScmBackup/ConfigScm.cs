namespace ScmBackup
{
    public class ConfigScm
    {
        /// <summary>
        /// Name of the SCM
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Path to executable
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// indicates if all files should be pulled from lfs
        /// </summary>
        public bool LfsFetch { get; set; }
    }
}
