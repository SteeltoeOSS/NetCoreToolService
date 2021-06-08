using System;
using System.IO;
using System.Reflection;

namespace Steeltoe.NetCoreToolService.Utils
{
    public abstract class TempPath : IDisposable
    {
        public string FullName { get; }

        public string Name { get; }

        public TempPath()
        {
            Name = Guid.NewGuid().ToString();
            FullName = Path.Join(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name, Name);
        }

        ~TempPath()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}
