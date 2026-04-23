namespace Appeon.PyPb.PyPbContextWrapper
{
    public class PyPbContextWrapper
    {
        private const string FileLockName = ".pypbcontextlock";
        private static FileStream? FileLock = null;

        /// <summary>
        /// Initialize a PyPbContext provided a Python Runtime DLL
        /// </summary>
        /// <param name="dllpath"></param>
        /// <param name="result"></param>
        /// <param name="error"></param>
        /// <returns>0 on success</returns>
        public static int Init(string dllpath, out PyPbContext? result, out string? error)
        {
            result = null;
            try
            {
                var context = PyPbContext.Init(dllpath, out error);

                if (error is not null)
                    throw new Exception(error);

                result = context;
                return 0;
            }
            catch (Exception e)
            {
                error = e.Message;
                return -1;
            }
        }

        /// <summary>
        /// Attempt to initialize a PyPbContext acquiring the runtime using Python.Included
        /// and download a list of packages through pip
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="context"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static int InitWithLocalRuntime(string[]? dependencies, out PyPbContext? context, out string? error)
        {
            context = null;
            try
            {
                context = PyPbContext.Init(dependencies, out error);
                return 0;
            }
            catch (Exception e)
            {
                error = e.Message;

                return -1;
            }
        }

        /// <summary>
        /// Returns whether or not an instance of PyPbContext is already in memory
        /// </summary>
        /// <returns></returns>
        public static bool IsInitialized() => PyPbContext.IsInit();

        /// <summary>
        /// Returns the existing instance of <see cref="PyPbContext"/>
        /// </summary>
        /// <returns></returns>
        public static PyPbContext? CurrentInstance() => PyPbContext.Instance;

        /// <summary>
        /// Creates a lock file to ensure that only one instance is running
        /// </summary>
        /// <returns></returns>
        public static bool GetFileLock()
        {
            try
            {
                if (FileLock is not null)
                    return true;
                FileLock = File.Open(FileLockName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Releases the file lock
        /// </summary>
        public static bool ReleaseFileLock()
        {
            if (FileLock is not null)
            {
                FileLock.Dispose();
                FileLock = null;
                try
                {
                    File.Delete(FileLockName);
                    return true;
                }
                catch
                {
                }
            }

            return false;
        }
    }
}
