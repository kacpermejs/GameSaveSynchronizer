using GameSaveSynchronizerCore.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Linq;
using System.Text.Json;

namespace GameSaveSynchronizerCore {
    public static class Synchronizer {

        private static string[] Scopes = {
            DriveService.Scope.DriveFile,
            DriveService.Scope.DriveMetadataReadonly 
        };

        private static string ApplicationName = "Game Save Synchronizer";

        private static DriveService? _service;
        private static FolderData? _workingFolder = null;
        private static readonly string _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        private static readonly string _selectedFolderFileName = "selected_folder.json";

        public static void Initialize() {
            AuthorizeService();
        }

        public static IList<Google.Apis.Drive.v3.Data.File> ListDriveFiles() {
            if (_service == null) {
                throw new InvalidOperationException("Synchronizer is not initialized properly!");
            }
            FilesResource.ListRequest listRequest = _service.Files.List();
            listRequest.PageSize = 10;
            listRequest.Fields = "nextPageToken, files(id, name)";
            listRequest.Q = "mimeType = 'application/vnd.google-apps.folder' and trashed = false";

            // List folders
            var folders = listRequest.Execute().Files;
            return folders;
        }

        public static void SaveSelectedFolder(FolderData folder) {
            _workingFolder = folder;
            string savePath = Path.Combine(_dataPath, _selectedFolderFileName);
            string json = JsonSerializer.Serialize(folder);
            File.WriteAllText(savePath, json);
            Console.WriteLine("Selected folder ID saved to " + savePath);
        }

        public static bool LoadWorkingFolderFromJSON() {
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
            }
            catch (Exception ex) {
                Console.WriteLine($"Error loading folder data: {ex.Message}");
                return false;
            }

            if (_workingFolder == null) {
                return false;
            }

            Console.WriteLine("Loaded working folder data: {0} ({1})", _workingFolder.FolderName, _workingFolder.FolderId);
            return true;
        }

        private static void AuthorizeService() {
            string secretsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "secrets");
            string credPath = Path.Combine(secretsPath, "credentials.json");
            string tokenPath = Path.Combine(secretsPath, "token");
            var tokenStore = new FileDataStore(tokenPath, true);

            createDirectories(secretsPath);

            UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromFile(credPath).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                tokenStore).Result;

            Console.WriteLine("Credential file saved to: " + tokenPath);

            // Create Drive API service.
            _service = new DriveService(new BaseClientService.Initializer() {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        private static void createDirectories(string secretsPath) {
            if (!Directory.Exists(secretsPath)) {
                Directory.CreateDirectory(secretsPath);
            }
            if (!Directory.Exists(_dataPath)) {
                Directory.CreateDirectory(_dataPath);
            }
        }
    }
}
