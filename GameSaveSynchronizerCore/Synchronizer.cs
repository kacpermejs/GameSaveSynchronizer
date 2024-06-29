using GameSaveSynchronizerCore.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Linq;
using System.Text.Json;

namespace GameSaveSynchronizerCore {
    public class Synchronizer {

        private static string[] Scopes = {
            DriveService.Scope.DriveFile,
            DriveService.Scope.DriveMetadataReadonly 
        };

        private static string ApplicationName = "Game Save Synchronizer";

        private static DriveService _service;
        private static FolderData? _workingFolder = null;
        private static string _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        private static string _selectedFolderFileName = "selected_folder.json";

        public async void Initialize() {
            UserCredential credential;

            string secretsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "secrets");
            if (!Directory.Exists(secretsPath)) {
                Directory.CreateDirectory(secretsPath);
            }
            if (!Directory.Exists(_dataPath)) {
                Directory.CreateDirectory(_dataPath);
            }

            string credPath = Path.Combine(secretsPath, "credentials.json");
            string tokenPath = Path.Combine(secretsPath, "token");
            var tokenStore = new FileDataStore(tokenPath, true);

            using (var stream = new FileStream(credPath, FileMode.Open, FileAccess.Read)) {

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    tokenStore).Result;

                Console.WriteLine("Credential file saved to: " + tokenPath);
            }

            // Create Drive API service.
            _service = new DriveService(new BaseClientService.Initializer() {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });


            if (!LoadWorkingFolderFromJSON()) {
                ListAndSelectFolder();
            }
        }

        static void ListAndSelectFolder() {
            // Define parameters of request
            FilesResource.ListRequest listRequest = _service.Files.List();
            listRequest.PageSize = 10;
            listRequest.Fields = "nextPageToken, files(id, name)";
            listRequest.Q = "mimeType = 'application/vnd.google-apps.folder' and trashed = false";

            // List folders
            var folders = listRequest.Execute().Files;
            Console.WriteLine("Folders:");
            if (folders != null && folders.Count > 0) {
                int i = 0;
                foreach (var folder in folders) {
                    Console.WriteLine("{0} ({1})\n", folder.Name, i++);
                }
                while (_workingFolder == null) {

                    // Prompt user to select a folder by entering the folder ID
                    Console.WriteLine("Enter the ID of the folder to use: ");
                    try {
                        string? userInput = Console.ReadLine() ?? "-1";
                        int folderIndex = int.Parse(userInput);
                        var folder = folders.ElementAt(folderIndex);

                        var folderData = new FolderData {
                            FolderId = folder.Id,
                            FolderName = folder.Name
                        };

                        SaveSelectedFolder(folderData);
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

        private static void SaveSelectedFolder(FolderData folder) {
            _workingFolder = folder;
            string savePath = Path.Combine(_dataPath, _selectedFolderFileName);
            string json = JsonSerializer.Serialize(folder);
            File.WriteAllText(savePath, json);
            Console.WriteLine("Selected folder ID saved to " + savePath);
        }

        private static bool LoadWorkingFolderFromJSON() {
            string selectedFolderPath = Path.Combine(_dataPath, _selectedFolderFileName);

            if (!File.Exists(selectedFolderPath)) {
                Console.WriteLine("Selected folder data file not found.");
                return false;
            }

            try {
                // Read the JSON from the file
                string json = File.ReadAllText(selectedFolderPath);

                // Deserialize the JSON to a FolderData object
                _workingFolder = JsonSerializer.Deserialize<FolderData>(json);
                Console.WriteLine("Loaded working folder data: {0} ({1})", _workingFolder.FolderName, _workingFolder.FolderId);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error loading folder data: {ex.Message}");
                return false;
            }

            return true;
        }


        public void Synchronize() {

        }
    }
}
