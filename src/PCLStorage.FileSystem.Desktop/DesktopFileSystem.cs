using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PCLStorage
{
    /// <summary>
    /// Implementation of <see cref="IFileSystem"/> over classic .NET file I/O APIs
    /// </summary>
    public class DesktopFileSystem : IFileSystem
    {
#if MAC
		// under the sandbox, need to read the HomeDirectory
		[System.Runtime.InteropServices.DllImport(ObjCRuntime.Constants.FoundationLibrary)]
		static extern IntPtr NSHomeDirectory();

		static string MacHomeDirectory 
		{
			get { return ((Foundation.NSString)ObjCRuntime.Runtime.GetNSObject(NSHomeDirectory())).ToString(); }
		}
#endif
        /// <summary>
        /// A folder representing storage which is local to the current device
        /// </summary>
        public IFolder LocalStorage
        {
            get
            {
                //  SpecialFolder.LocalApplicationData is not app-specific, so use the Windows Forms API to get the app data path
                //var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
#if ANDROID
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#elif IOS
                var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var localAppData = Path.Combine(documents, "..", "Library");
#elif MAC
				// (non-sandboxed) /Users/foo/Library/Application Support/ProcessName/
				// (sandboxed) /Users/foo/Library/Containers/<AppId>/Data/Library/Application Support/ProcessName/
				var name = Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().ProcessName);
				var localAppData = Path.Combine(MacHomeDirectory, "Library", "Application Support");
				if(!string.IsNullOrEmpty(name))
				{
					localAppData = Path.Combine(localAppData, name);
					// ensure it exists to stope FileSystemFolder from throwing exception
					if(!Directory.Exists(localAppData))
						Directory.CreateDirectory(localAppData);
				}
#else
                var localAppData = System.Windows.Forms.Application.LocalUserAppDataPath;
#endif
                return new FileSystemFolder(localAppData);
            }
        }

        /// <summary>
        /// A folder representing storage which may be synced with other devices for the same user
        /// </summary>
        public IFolder RoamingStorage
        {
            get
            {
#if ANDROID || IOS || MAC
                return null;
#else
                //  SpecialFolder.ApplicationData is not app-specific, so use the Windows Forms API to get the app data path
                //var roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var roamingAppData = System.Windows.Forms.Application.UserAppDataPath;
                return new FileSystemFolder(roamingAppData);
#endif
            }
        }

        /// <summary>
        /// Gets a file, given its path.  Returns null if the file does not exist.
        /// </summary>
        /// <param name="path">The path to a file, as returned from the <see cref="IFile.Path"/> property.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A file for the given path, or null if it does not exist.</returns>
        public async Task<IFile> GetFileFromPathAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            Requires.NotNullOrEmpty(path, "path");

            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);

            return File.Exists(path) ? new FileSystemFile(path) : null;

        }

        /// <summary>
        /// Gets a folder, given its path.  Returns null if the folder does not exist.
        /// </summary>
        /// <param name="path">The path to a folder, as returned from the <see cref="IFolder.Path"/> property.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A folder for the specified path, or null if it does not exist.</returns>
        public async Task<IFolder> GetFolderFromPathAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            Requires.NotNullOrEmpty(path, "path");

            await AwaitExtensions.SwitchOffMainThreadAsync(cancellationToken);
            return Directory.Exists(path) ? new FileSystemFolder(path, true) : null;

        }
    }
}
