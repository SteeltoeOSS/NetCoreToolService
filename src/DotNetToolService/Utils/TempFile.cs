using System.IO;

namespace Steeltoe.DotNetToolService.Utils
{
    public sealed class TempFile : TempPath
    {
        public TempFile() : this(true)
        {
        }

        public TempFile(bool create)
        {
            if (create)
            {
                File.Create(FullName).Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!(File.Exists(FullName)))
            {
                return;
            }

            try
            {
                File.Delete(FullName);
            }
            catch
            {
                // ignore
            }
        }
    }
}
