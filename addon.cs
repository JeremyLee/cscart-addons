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

    switch(args[0]) {
      case "createlinks":
        CreateLinksCommand(args);
        break;
      case "removelinks":
      
        break;
      case "extract":
      
        break;
      case "createfolder":
      
        break;
      case "showfolders":
        ShowFolders();
        break;
      default:
        ShowUsage(true);
        break;
    }
  }
  
  static void CreateLinksCommand(string[] args) {
    if(args.Length != 3) {
      ShowUsage();
      return;
    }

    string addonName = args[1];
    string cscartPath = args[2];

    string addonDirectory = Path.Combine(Environment.CurrentDirectory, addonName);

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

    if (RestartElevated()) {
      Console.WriteLine("Creating the following symlinks:");
      foreach (var link in addonFolders) {
        Console.WriteLine("{0} --> {1}", link.Key, link.Value);
      }
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
    
    string args = "";
    string[] arguments = Environment.GetCommandLineArgs();
    for (int i = 1; i < arguments.Length; i++)
      args += "\"" + arguments[i] + "\" ";

    ProcessStartInfo startInfo = new ProcessStartInfo();
    startInfo.UseShellExecute = true;
    startInfo.WorkingDirectory = Environment.CurrentDirectory;
    startInfo.FileName = System.Windows.Forms.Application.ExecutablePath;
    startInfo.Arguments = args;
    startInfo.Verb = "runas";

    Process.Start(startInfo);
    return true;
  }
}
