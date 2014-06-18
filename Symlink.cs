using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

static class Symlink {
  // Thanks to http://chrisbensen.blogspot.com/2010/06/getfinalpathnamebyhandle.html?showComment=1285905429459#c3690594526243963646

  private const int FILE_SHARE_READ = 1;
  private const int FILE_SHARE_WRITE = 2;

  private const int CREATION_DISPOSITION_OPEN_EXISTING = 3;

  private const int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

  // http://msdn.microsoft.com/en-us/library/aa364962%28VS.85%29.aspx
  [DllImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", CharSet = CharSet.Unicode, SetLastError = true)]
  static extern int GetFinalPathNameByHandle(IntPtr handle, [In, Out] StringBuilder path, int bufLen, int flags);

  // http://msdn.microsoft.com/en-us/library/aa363858(VS.85).aspx
  [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
  static extern SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, int dwShareMode,
                                          IntPtr SecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

  [DllImport("kernel32.dll", SetLastError = true)]
  [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
  static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

  
  public static string GetTarget(DirectoryInfo symlink) {
    SafeFileHandle directoryHandle = CreateFile(symlink.FullName, 0, 2, System.IntPtr.Zero, CREATION_DISPOSITION_OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, System.IntPtr.Zero);
    if (directoryHandle.IsInvalid)
      throw new Win32Exception(Marshal.GetLastWin32Error());

    StringBuilder path = new StringBuilder(512);
    int size = GetFinalPathNameByHandle(directoryHandle.DangerousGetHandle(), path, path.Capacity, 0);
    if (size < 0)
      throw new Win32Exception(Marshal.GetLastWin32Error());
    // The remarks section of GetFinalPathNameByHandle mentions the return being prefixed with "\\?\"
    // More information about "\\?\" here -> http://msdn.microsoft.com/en-us/library/aa365247(v=VS.85).aspx
    if (path[0] == '\\' && path[1] == '\\' && path[2] == '?' && path[3] == '\\')
      return path.ToString().Substring(4);
    else
      return path.ToString();
  }

  public static string GetTarget(string symlink) {
    return GetTarget(new DirectoryInfo(symlink));
  }
  
  public static void CreateLink(string link, string target) {
    if(!File.Exists(target) && !Directory.Exists(target))
      throw new FileNotFoundException("Can't create a Symlink to a non-existent file or directory.", target);
    //target = Path.GetFullPath(target);
    //link = Path.GetFullPath(target);
    bool succeeded;
    if(File.GetAttributes(target).HasFlag(FileAttributes.Directory))
      succeeded = CreateSymbolicLink(link, target, 1);
    else
      succeeded = CreateSymbolicLink(link, target, 0);
    
    if(!succeeded)
      throw new Win32Exception(Marshal.GetLastWin32Error());
  }
}
