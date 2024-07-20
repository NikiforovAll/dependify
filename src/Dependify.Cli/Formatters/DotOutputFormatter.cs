namespace Dependify.Cli.Formatters;

using Dependify.Core.Graph;
using Dependify.Core.Serializers;

internal class DotOutputFormatter(TextWriter textWriter) : IOutputFormatter
{
    private bool disposed;

    public void Write<T>(T data)
    {
        ObjectDisposedException.ThrowIf(this.disposed, textWriter);

        textWriter.WriteLine(GraphvizSerializer.ToString(data as DependencyGraph));
        textWriter.Flush();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects).
                textWriter?.Dispose();
            }

            // Free unmanaged resources (unmanaged objects) and override a finalizer below.
            // Set large fields to null.

            this.disposed = true;
        }
    }

    // Override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~DotOutputFormatter()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        this.Dispose(false);
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        this.Dispose(true);
        // Uncomment the following line if the finalizer is overridden above.
        GC.SuppressFinalize(this);
    }
}
