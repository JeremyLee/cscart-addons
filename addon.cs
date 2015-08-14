//css_ref System.Windows.Forms
//css_include Symlink.cs
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security.Principal;
using System.Reflection;
using System.IO;
using System.Text;

class Program {
  static bool restarted = false;
  static string[][] folders = new string[][] {
    new string[] {
      "/app/addons",
      "/design/themes/basic/templates/addons"
    },
    new string[] {
      "/design/backend/css/addons",
      "/design/backend/mail/templates/addons",
      "/design/backend/media/images/addons",
      "/design/backend/templates/addons",
      "/design/backend/templates/views/addons"
    },
    new string[] {
      "/design/themes/basic/css/addons",
      "/design/themes/basic/mail/templates/addons",
      "/design/themes/basic/media/images/addons",
    },
    new string[] {
      "/js/addons",
    },
    new string[] {
      "/var/themes_repository/basic/css/addons",
      "/var/themes_repository/basic/mail/templates/addon",
      "/var/themes_repository/basic/media/images/addons",
      "/var/themes_repository/basic/templates/addons",
    }
  };
  static string[] foldersFlattened;

  

  public static void Main(string[] args) {
    if(args.Length == 0) {
      ShowUsage();
      return;
    }
    
    if(args[args.Length - 1] == "restarted") {
      restarted = true;
      string[] newArgs = new string[args.Length - 1];
      Array.Copy(args, 0, newArgs, 0, args.Length - 1);
      args = newArgs;
    }

    switch(args[0]) {
      case "createlinks":
        CreateLinksCommand(args);
        break;
      case "removelinks":
      
        break;
      case "extract":
      
        break;
      case "createfolder":
        CreateFolderCommand(args);
        break;
      case "showfolders":
        ShowFolders();
        break;
      default:
        ShowUsage(true);
        break;
    }
  }
  
  static void CreateFolderCommand(string[] args) {
    if (!IsElevated()) {
      Console.WriteLine("Administrator privileges are required to create symlinks.\r\n\r\nElevating...");
      System.Threading.Thread.Sleep(1500);
      RestartElevated();
      return;
    }
    
    string addonName;
    string cscartPath;
    string addonDirectory;

    if(args.Length < 2 || args.Length > 3) {
      ShowUsage();
      return;
    } else if (args.Length == 2) {
      addonName = args[1];
      if(!GetCSCartPath(addonName, out addonDirectory, out cscartPath)) {
        ShowUsage();
        return;
      }
    } else {
      addonName = args[1];
      cscartPath = args[2];
      addonDirectory = Path.Combine(Environment.CurrentDirectory, addonName);
    }
    
    ShowFolders();
    int folderID;
    
    while(true) {
      Console.Write("Press the number of the folder you wish to create and press enter.\r\n To cancel, just press enter: > ");
      string input = Console.ReadLine();
      if(input.Length == 0) {
        return;
      }
      if(int.TryParse(input, out folderID) && folderID >= 0 && folderID < foldersFlattened.Length)
        break;
    }
    string cscartFolderPath = Path.Combine(cscartPath, foldersFlattened[folderID], addonName);
    
    bool copyIfExist = false;
    if(Directory.Exists(cscartFolderPath)) {
      while(true) {
        Console.Write("The directory you specified already exists in the CS-Cart Installation.\r\nDo you want to move the contents to the local addon folder and create a symlink? (y or n) : ");
        string result = Console.ReadLine().ToLower();
        if(result == "y") {
          copyIfExist = true;
          break;
        } else if(result == "n") {
          copyIfExist = false;
          break;
        } else if(result == "c")
          return;
      }
    }
    
    CreateFolder(cscartPath, addonDirectory, addonName, foldersFlattened[folderID], copyIfExist);
    Console.ReadKey();
  }
  
  static bool CreateFolder(string cscart, string addonDirectory, string addonName, string folder, bool copyIfExist) {
    string cscartFolderPath = Path.Combine(cscart, folder, addonName);
    string addonFolderPath = Path.Combine(addonDirectory, folder, addonName);
    if(Directory.Exists(addonFolderPath)) {
      Console.WriteLine("The directory '{0}' already exists and can not be created.", addonFolderPath.Replace('\\', '/'));
      return false;
    }
    
    
    if(!Directory.Exists(cscartFolderPath)) {
      Directory.CreateDirectory(addonFolderPath);
    } else if(copyIfExist) {
      Console.Write("Found matching folder in CS-Cart directory. Moving files to the local directory and creating symlink...");
      MoveDirectory(cscartFolderPath, addonFolderPath);
      Symlink.CreateLink(cscartFolderPath, addonFolderPath);
    }
    
    return true;
  }
  
  static void CreateLinksCommand(string[] args) {
    string addonName;
    string cscartPath;
    string addonDirectory;
    
    if(args.Length < 2 || args.Length > 3) {
      ShowUsage();
      return;
    } else if (args.Length == 2) {
      addonName = args[1];
      if(!GetCSCartPath(addonName, out addonDirectory, out cscartPath)) {
        ShowUsage();
        return;
      }
    } else {
      addonName = args[1];
      cscartPath = args[2];
      addonDirectory = Path.Combine(Environment.CurrentDirectory, addonName);
    }


    var addonFolders = new Dictionary<string, string>();
    var foldersExistingInCSCart = new List<string>();

    foreach(var folder in foldersFlattened) {
      var addonFolder = Path.Combine(addonDirectory, folder, addonName).Replace('\\', '/');
      var cscartAddonFolder = Path.Combine(cscartPath, folder, addonName).Replace('\\', '/');
      
      if(Directory.Exists(addonFolder)) {
        if(Directory.Exists(cscartAddonFolder)) {
          Console.WriteLine("Symlink exists {0} --> {1}", cscartAddonFolder, Symlink.GetTarget(cscartAddonFolder).Replace('\\', '/'));
          if(Path.GetFullPath(Symlink.GetTarget(cscartAddonFolder)).TrimEnd('\\')
             == Path.GetFullPath(addonFolder).TrimEnd('\\'))
            continue;
          foldersExistingInCSCart.Add(cscartAddonFolder);
        } else if (File.Exists(cscartAddonFolder)) {
          foldersExistingInCSCart.Add(cscartAddonFolder);
        } else {
          addonFolders.Add(cscartAddonFolder, addonFolder);
        }
      }
    }

    if(foldersExistingInCSCart.Count != 0) {
      Console.WriteLine("The following folders already exist in the CS-Cart installation. Please remove them so that symlinks can be created to your addon directory.\r\n");
      foreach(var existing in foldersExistingInCSCart) {
        Console.WriteLine(existing);
      }

      return;
    }
    
    if(addonFolders.Count == 0) {
      Console.WriteLine("No symlinks to create...");
      return;
    }
    
    if(!restarted) {
      Console.Write("\r\nDo you want to create the following symlinks? (y/n) ");
      foreach (var link in addonFolders) {
        Console.WriteLine("{0} --> {1}", link.Key, link.Value);
      }

      while(true) {
        var c = Console.ReadKey().KeyChar;
        if(c == 'y') {
          break;
        } else if(c == 'n') {
          Console.WriteLine("Exiting");
          return;
        }
        Console.Write("\r\nDo you want to create the preceding symlinks? (y/n) ");
      }
    }
    
    if (!IsElevated()) {
      Console.WriteLine("Administrator privileges are required to create symlinks.\r\n\r\nElevating...");
      System.Threading.Thread.Sleep(1500);
      RestartElevated();
      return;
    }
    
    bool errored = false;
    Console.WriteLine("Creating the following symlinks:");
    foreach(var link in addonFolders) {
      Console.WriteLine("{0} --> {1}", link.Key, link.Value);
      try {
        Symlink.CreateLink(link.Key, link.Value);
      } catch(Exception ex) {
        errored = true;
        Console.WriteLine(ex.ToString());
      }
    }
    if(errored) {
      Console.Write("Press any key to continue...");
      Console.ReadKey();
    }
  }
  
  static bool GetCSCartPath(string addonName, out string addonDirectory, out string cscartPath) {
    addonDirectory = Path.Combine(Environment.CurrentDirectory, addonName);
    if(File.Exists(Path.Combine(addonDirectory, ".cscart"))) {
      cscartPath = File.ReadAllText(Path.Combine(addonDirectory, ".cscart"));
      return true;
    }
    cscartPath = null;
    return false;
  }

  static void ShowFolders() {
    int i = 0;
    foreach(var folderList in folders){
      foreach(var folder in folderList) {
        Console.WriteLine("{0} - {1}", i, folder);
        i++;
      }
      Console.WriteLine();
    }
  }
  
  static void ShowUsage(bool pause = false) {
    Console.WriteLine(
@"cscs.exe addon (createlinks|removelinks|extract) addon_name cscart_path
cscs.exe addon createfolder addon_name [cscart_path] [folder_id]

  createlinks - Creates symlinks in the CS-Cart installation to the appropriate local addon directories.
  
  removelinks - Removes any symlinks to the specified addon in the CS-Cart installation. Does not remove any directories, only symlinks.
  
  extract - Copies the contents of each addon directory in the CS-Cart directory to a local directory.
  
  createfolder - Shows a list of valid addon folders and allows you to specify one to create.
  If neither of the folder_id or cscart_path arguments are specified, a list of valid addon folders will be shown giving you the option to create one of them.
  If the folder_id argument is specified, the specified folder is selected automatically.
  If the cscart_path argument is specified, a symlink in the CS-Cart installation is created to the folder that is created."
    );
    if(pause) {
      Console.Write("Press any key to continue...");
      Console.ReadKey(true);
    }
  }
  

  static Program() {
    List<string> temp = new List<string>();
    foreach(var group in folders) {
      foreach(var folder in group) {
        if(folder.StartsWith("/") || folder.StartsWith("\\"))
          temp.Add(folder.Substring(1).Replace('\\', '/'));
        else
          temp.Add(folder.Replace('\\', '/'));
      }
    }

    foldersFlattened = temp.ToArray();
  }
  
  internal static bool RestartElevated() {
    if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
      return false;
    
    StringBuilder args = new StringBuilder();
    
    string[] arguments = Environment.GetCommandLineArgs();
    for (int i = 1; i < arguments.Length; i++)
      args.AppendFormat("\"{0}\" ", arguments[i]);

    args.Append(" \"restarted\"");
    ProcessStartInfo startInfo = new ProcessStartInfo();
    startInfo.UseShellExecute = true;
    startInfo.WorkingDirectory = Environment.CurrentDirectory;
    startInfo.FileName = System.Windows.Forms.Application.ExecutablePath;
    startInfo.Arguments = args.ToString();
    startInfo.Verb = "runas";

    Process.Start(startInfo);
    return true;
  }
  
  internal static bool IsElevated() {
    return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
  }
  
  public static void MoveDirectory(string source, string target)
  {
    var stack = new Stack<Folders>();
    stack.Push(new Folders(source, target));

    while (stack.Count > 0)
    {
      var folders = stack.Pop();
      Directory.CreateDirectory(folders.Target);
      foreach (var file in Directory.GetFiles(folders.Source, "*.*"))
      {
        string targetFile = Path.Combine(folders.Target, Path.GetFileName(file));
        if (File.Exists(targetFile)) File.Delete(targetFile);
        File.Move(file, targetFile);
      }

      foreach (var folder in Directory.GetDirectories(folders.Source))
      {
        stack.Push(new Folders(folder, Path.Combine(folders.Target, Path.GetFileName(folder))));
      }
    }
    Directory.Delete(source, true);
  }
  
  public class Folders
  {
    public string Source { get; private set; }
    public string Target { get; private set; }

    public Folders(string source, string target)
    {
      Source = source;
      Target = target;
    }
  }
}
