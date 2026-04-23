namespace Appeon.PyPb.Tests.Common;

public class TempTextFile : IDisposable
{
    private bool disposedValue;


    private string path;

    public string Path { get => path; }

    public TempTextFile(string path, string contents)
    {
        var stream = File.CreateText(path);
        this.path = path;

        stream.Write(contents);
        stream.Close();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (File.Exists(Path)) File.Delete(Path);
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~TempFile()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
