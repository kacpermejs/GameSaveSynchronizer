using GameSaveSynchronizerCore;
using GameSaveSynchronizerCore.Models;
using Google.Apis.Drive.v3;

namespace GameSaveSynchronizerConsole {
    internal class Program {

        static void Main(string[] args) {
            Synchronizer.Initialize();

            if (!Synchronizer.LoadWorkingFolderFromJSON()) {
                ListAndSelectFolder();
            }
        }

        private static void ListAndSelectFolder() {
            var folders = Synchronizer.ListDriveFiles();

            Console.WriteLine("Folders:");
            if (folders != null && folders.Count > 0) {
                int i = 0;
                foreach (var folder in folders) {
                    Console.WriteLine("{0} ({1})\n", folder.Name, i++);
                }

                FolderData? selectedFolder = null;

                while (selectedFolder == null) {

                    // Prompt user to select a folder by entering the folder ID
                    Console.WriteLine("Enter the ID of the folder to use: ");
                    try {
                        string? userInput = Console.ReadLine() ?? "-1";
                        int folderIndex = int.Parse(userInput);
                        var folder = folders.ElementAt(folderIndex);

                        selectedFolder = new FolderData {
                            FolderId   = folder.Id,
                            FolderName = folder.Name
                        };

                        Synchronizer.SaveSelectedFolder(selectedFolder);
                    }
                    catch {
                        Console.WriteLine("Wrong index selected! Try again");
                        continue;
                    }

                }
            } else {
                Console.WriteLine("No folders found.");
            }
        }

        
    }
}
