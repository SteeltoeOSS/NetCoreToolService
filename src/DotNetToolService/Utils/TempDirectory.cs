using System;
using System.IO;
using System.Reflection;

namespace Steeltoe.DotNetToolService.Utils
{
    public sealed class TempDirectory : IDisposable
    {
        public string FullName { get; }

        public TempDirectory()
        {
            FullName = Path.Join(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name,
                Guid.NewGuid().ToString());
            Directory.CreateDirectory(FullName);
        }

        ~TempDirectory()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }


        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }

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
