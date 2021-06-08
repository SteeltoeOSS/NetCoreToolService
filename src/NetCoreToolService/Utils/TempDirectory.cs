using System.IO;

namespace Steeltoe.NetCoreToolService.Utils
{
    public sealed class TempDirectory : TempPath
    {
        public TempDirectory() : this(true)
        {
        }

        public TempDirectory(bool create)
        {
            if (create)
            {
                Directory.CreateDirectory(FullName);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!(Directory.Exists(FullName)))
            {
                return;
            }

            try
            {
                Directory.Delete(FullName, true);
            }
            catch
            {
                // ignore
            }
        }
    }
}
