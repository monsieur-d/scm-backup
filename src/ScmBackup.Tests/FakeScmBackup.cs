using System;

namespace ScmBackup.Tests
{
    public class FakeScmBackup : IScmBackup
    {
        public Exception ToThrow { get; set; }

        public bool Run()
        {
            if (this.ToThrow != null)
            {
                throw this.ToThrow;
            }

            return true;
        }
    }
}
