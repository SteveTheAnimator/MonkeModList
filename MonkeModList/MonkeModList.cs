using BepInEx;
using ComputerPlusPlus;
using GorillaNetworking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace MonkeModList {
    [BepInDependency("com.kylethescientist.gorillatag.computerplusplus")]
    [BepInPlugin("com.mrbanana.gorillatag.monkemodlist", "MonkeModList", "1.0.2")]
    public class MonkeModList : BaseUnityPlugin {

        public int Version { get; private set; } = 102;
        public bool NewUpdate { get; private set; }
        public static MonkeModList instance;
        public bool UnableToGetData { get; private set; } = true;
        public string RawList { get; private set; } = null;
        public bool CanRefresh { get; private set; } = true;

        public List<string[]> listedMods = new List<string[]>();

        void Awake() { instance = this; CheckBatch(); }

        public void CheckBatch() {
            string path = Path.Combine(BepInEx.Paths.PluginPath, "MonkeModList-ModManagers");
            bool ModManagersExist = Directory.Exists(path);

            if(ModManagersExist) {
                string[] batchFiles = Directory.GetFiles(path, "*.bat");

                string UpdateModDLLtext = "echo\r\ntaskkill /IM \"Gorilla Tag.exe\"\r\ncd C:\\Program Files\\Oculus\\Software\\Software\\another-axiom-gorilla-tag\\BepInEx\\plugins\r\ndel /q LegMod.dll\r\nrename \"MonkeModList-LegMod.dll\" \"LegMod.dll\"\r\npause\r\n";
                string UpdateModZIPtext = "echo\r\ntaskkill /IM \"Gorilla Tag.exe\"\r\ncd PLUGIN_PATH\r\nrmdir /q MOD_NAME\r\ntar -xf MonkeModList-MOD_NAME.zip\r\nrename \"MonkeModList-MOD_NAME\" \"MOD_NAME\"\r\ndel MonkeModList-MOD_NAME.zip\r\npause";

                foreach (string batchFile in batchFiles) {
                    string batchFileName = Path.GetFileNameWithoutExtension(batchFile);
                    if(batchFileName == "Update Mod DLL") {
                        string fileContents = File.ReadAllText(batchFile);

                        if (fileContents != UpdateModDLLtext) {
                            File.WriteAllText(batchFile, UpdateModDLLtext);
                        }
                    }
                    else if(batchFileName == "Update Mod ZIP") {
                        string fileContents = File.ReadAllText(batchFile);

                        if (fileContents != UpdateModZIPtext) {
                            File.WriteAllText(batchFile, UpdateModZIPtext);
                        }
                    }
                }
            }
            else {
                Directory.CreateDirectory(path);
                StartCoroutine(DownloadModManagerFiles());
            }
        }

        IEnumerator DownloadModManagerFiles() {
            string path = Path.Combine(BepInEx.Paths.PluginPath, "MonkeModList-ModManagers", "Update Mod DLL.bat");
            string path1 = Path.Combine(BepInEx.Paths.PluginPath, "MonkeModList-ModManagers", "Update Mod ZIP.bat");
            UnityWebRequest www = UnityWebRequest.Get("https://github.com/MrBanana01/MonkeModList/releases/download/Managers/Update.Mod.DLL.bat");
            UnityWebRequest www1 = UnityWebRequest.Get("https://github.com/MrBanana01/MonkeModList/releases/download/Managers/Update.Mod.ZIP.bat");
            yield return www.SendWebRequest();
            yield return www1.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success) {
                UnableToGetData = false;
                File.WriteAllBytes(path, www.downloadHandler.data);
            }
            else {
                UnableToGetData = true;
            }

            if (www1.result == UnityWebRequest.Result.Success) {
                UnableToGetData = false;
                File.WriteAllBytes(path1, www.downloadHandler.data);
            }
            else {
                UnableToGetData = true;
            }

            www.Dispose();
            www1.Dispose();
        }

        public void GetModList() { StartCoroutine(GetData()); StartCoroutine(RefreshDelay()); }
        IEnumerator GetData() {
            if (CanRefresh) {
                UnityWebRequest www1 = UnityWebRequest.Get("https://raw.githubusercontent.com/MrBanana01/MonkeModList/master/CurrentVersion");
                yield return www1.SendWebRequest();

                if (www1.result == UnityWebRequest.Result.Success && !www1.isNetworkError && !www1.isHttpError) {
                    if(int.Parse(www1.downloadHandler.text) > Version)
                        NewUpdate = true;
                    else
                        NewUpdate = false;

                    UnableToGetData = false;
                }
                else {
                    UnableToGetData = true;
                }

                UnityWebRequest www = UnityWebRequest.Get("https://raw.githubusercontent.com/MrBanana01/MonkeModList/master/List");
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success && !www.isNetworkError && !www.isHttpError) {
                    RawList = www.downloadHandler.text;
                    UnableToGetData = false;
                }
                else {
                    UnableToGetData = true;
                    RawList = null;
                }

                ScreenData.instance.RefreshModList();

                www.Dispose();
            }
        }
        
        IEnumerator RefreshDelay() {
            CanRefresh = false;
            yield return new WaitForSeconds(20);
            CanRefresh = true;
        }

        public void DownloadSelectedMod() { if (listedMods[ScreenData.instance.SelectedMod] != null) { StartCoroutine(DownloadFile()); } }
        IEnumerator DownloadFile() {
            ScreenData.instance.FailedToDownload = false;
            ScreenData.instance.Successfully = false;
            ScreenData.instance.DownloadingMod = true;
            ScreenData.instance.UpdateWarning = false;

            string fileExtension = Path.GetExtension(listedMods[ScreenData.instance.SelectedMod][3].ToLower());
            string path = Path.Combine(BepInEx.Paths.PluginPath, $"{listedMods[ScreenData.instance.SelectedMod][0]}{fileExtension}");
            string dupeName = Path.Combine(BepInEx.Paths.PluginPath, $"MonkeModList-{listedMods[ScreenData.instance.SelectedMod][0]}{fileExtension}");

            UnityWebRequest www = UnityWebRequest.Get(listedMods[ScreenData.instance.SelectedMod][3]);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError || www.result != UnityWebRequest.Result.Success) {
                ScreenData.instance.FailedToDownload = true;
                ScreenData.instance.Successfully = false;
                ScreenData.instance.DownloadingMod = false;
                ScreenData.instance.UpdateWarning = false;
                UnityEngine.Debug.Log(www.error);
                yield break;
            }
            else {
                if (fileExtension == ".zip") {
                    bool folderContainsMod = Directory.GetDirectories(BepInEx.Paths.PluginPath, "*", SearchOption.AllDirectories)
                        .Any(dir => dir.Contains(listedMods[ScreenData.instance.SelectedMod][0]));

                    if (folderContainsMod) {
                        ScreenData.instance.FailedToDownload = false;
                        ScreenData.instance.Successfully = false;
                        ScreenData.instance.DownloadingMod = false;
                        ScreenData.instance.UpdateWarning = true;
                        File.WriteAllBytes(dupeName, www.downloadHandler.data);
                        yield return new WaitForSeconds(5);
                        UpdateSelectedMod();
                        yield break;
                    }
                    else {
                        ScreenData.instance.FailedToDownload = false;
                        ScreenData.instance.Successfully = true;
                        ScreenData.instance.DownloadingMod = false;
                        ScreenData.instance.UpdateWarning = false;
                        File.WriteAllBytes(path, www.downloadHandler.data);
                        ZipFile.ExtractToDirectory(dupeName, BepInEx.Paths.PluginPath);
                        File.Delete(dupeName);
                        yield break;
                    }
                }
                else if (fileExtension == ".dll") {
                    string fileContainingMod = Directory.GetFiles(BepInEx.Paths.PluginPath)
                        .FirstOrDefault(file => Path.GetFileName(file).Contains(listedMods[ScreenData.instance.SelectedMod][0]));

                    if (fileContainingMod != null) {
                        ScreenData.instance.FailedToDownload = false;
                        ScreenData.instance.Successfully = false;
                        ScreenData.instance.DownloadingMod = false;
                        ScreenData.instance.UpdateWarning = true;
                        File.WriteAllBytes(dupeName, www.downloadHandler.data);
                        yield return new WaitForSeconds(5);
                        UpdateSelectedMod();
                        yield break;
                    }
                    else {
                        File.WriteAllBytes(path, www.downloadHandler.data);

                        ScreenData.instance.FailedToDownload = false;
                        ScreenData.instance.Successfully = true;
                        ScreenData.instance.DownloadingMod = false;
                        ScreenData.instance.UpdateWarning = false;
                        yield break;
                    }
                }
            }

            www.Dispose();
        }

        public void UpdateSelectedMod() { StartCoroutine(UpdateMod(false)); }
        public void UpdateSelf() { StartCoroutine(UpdateMod(true)); }
        IEnumerator UpdateMod(bool updateSelf) {
            if (!updateSelf) {
                string fileExtension = Path.GetExtension(listedMods[ScreenData.instance.SelectedMod][3].ToLower());

                if (fileExtension == ".dll") {
                    string filePath = Path.Combine(BepInEx.Paths.PluginPath, "MonkeModList-ModManagers", "Update Mod DLL.bat");
                    string filePathDir = Path.Combine(BepInEx.Paths.PluginPath, "MonkeModList-ModManagers");
                    string[] lines = File.ReadAllLines(filePath);

                    for (int i = 0; i < lines.Length; i++) {
                        lines[i] = lines[i].Replace("MOD_NAME", listedMods[ScreenData.instance.SelectedMod][0]);
                        lines[i] = lines[i].Replace("PLUGIN_PATH", BepInEx.Paths.PluginPath);
                    }

                    File.WriteAllLines(filePath, lines);

                    Process proc = new Process();

                    proc.StartInfo.WorkingDirectory = filePathDir;
                    proc = new Process();
                    proc.StartInfo.WorkingDirectory = filePathDir;
                    proc.StartInfo.FileName = "Update Mod DLL.bat";
                    proc.StartInfo.CreateNoWindow = true;
                    proc.Start();
                    proc.WaitForExit();
                }
                else if (fileExtension == ".zip") {
                    string filePath = Path.Combine(BepInEx.Paths.PluginPath, "MonkeModList-ModManagers", "Update Mod ZIP.bat");
                    string filePathDir = Path.Combine(BepInEx.Paths.PluginPath, "MonkeModList-ModManagers");
                    string[] lines = File.ReadAllLines(filePath);

                    for (int i = 0; i < lines.Length; i++) {
                        lines[i] = lines[i].Replace("MOD_NAME", listedMods[ScreenData.instance.SelectedMod][0]);
                        lines[i] = lines[i].Replace("PLUGIN_PATH", BepInEx.Paths.PluginPath);
                    }

                    File.WriteAllLines(filePath, lines);

                    Process proc = new Process();

                    proc.StartInfo.WorkingDirectory = filePathDir;
                    proc = new Process();
                    proc.StartInfo.WorkingDirectory = filePathDir;
                    proc.StartInfo.FileName = "Update Mod ZIP.bat";
                    proc.StartInfo.CreateNoWindow = true;
                    proc.Start();
                    proc.WaitForExit();
                }
            }
            else {
                UnityWebRequest www = UnityWebRequest.Get("https://github.com/MrBanana01/MonkeModList/releases/download/1/MonkeModList.dll");
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError || www.result != UnityWebRequest.Result.Success) {
                    UnityEngine.Debug.Log(www.error);
                    yield break;
                }
                else {
                    string dupeName = Path.Combine(BepInEx.Paths.PluginPath, $"MonkeModList-MonkeModList.dll");
                    File.WriteAllBytes(dupeName, www.downloadHandler.data);

                    string filePath = Path.Combine(BepInEx.Paths.PluginPath, "MonkeModList-ModManagers", "Update Mod DLL.bat");
                    string filePathDir = Path.Combine(BepInEx.Paths.PluginPath, "MonkeModList-ModManagers");
                    string[] lines = File.ReadAllLines(filePath);

                    for (int i = 0; i < lines.Length; i++) {
                        lines[i] = lines[i].Replace("MOD_NAME", "MonkeModList");
                        lines[i] = lines[i].Replace("PLUGIN_PATH", BepInEx.Paths.PluginPath);
                    }

                    File.WriteAllLines(filePath, lines);

                    Process proc = new Process();

                    proc.StartInfo.WorkingDirectory = filePathDir;
                    proc = new Process();
                    proc.StartInfo.WorkingDirectory = filePathDir;
                    proc.StartInfo.FileName = "Update Mod DLL.bat";
                    proc.StartInfo.CreateNoWindow = true;
                    proc.Start();
                    proc.WaitForExit();
                }
            }
            yield break;
        }
    }

    internal class ScreenData : IScreen {
        public static ScreenData instance;

        public string Title => "MOD LIST";

        public string Description => "[OPTION1] = Refresh\n[W OR S] = Scroll\n[A or D] = SwitchTab\n<color=yellow>MADE BY MRBANANA</color>";

        public int CurrentPage { get; private set; } = 0;

        public int SelectedMod { get; private set; } = 0;

        public int ModsPerPage { get; private set; } = 6;

        public bool DownloadingMod = false;

        public bool FailedToDownload = false;

        public bool Successfully = false;

        public bool Started = true;

        public bool AllMods = false;

        public bool Version = false;

        public bool UpdateWarning = false;

        public string GetContent() {
            StringBuilder context = new StringBuilder();
            context.Clear();

            if (DownloadingMod) {
                context.AppendLine($"\n<color=yellow>DOWNLOADING MOD: {MonkeModList.instance.listedMods[SelectedMod][0]}!</color>");
            }

            if (FailedToDownload) {
                context.AppendLine($"\n<color=red>FAILED TO DOWNLOAD THE MOD: {MonkeModList.instance.listedMods[SelectedMod][0]}!</color>");
            }

            if (Successfully) {
                context.AppendLine($"\n<color=lime>SUCCESSFULY DOWNLOADED/UPDATED THE MOD: {MonkeModList.instance.listedMods[SelectedMod][0]}!</color>");
                context.AppendLine($"<color=lime>PLEASE RESTART YOUR GAME TO LOAD THE MOD.</color>");
                context.AppendLine($"<color=lime>PRESS ENTER TO GO BACK AND DOWNLOAD MORE MODS.</color>");
            }

            if (Started) {
                context.AppendLine($"\n<color=white>PRESS OPTION-1 TO GET STARTED</color>");
            }

            if (UpdateWarning) {
                context.AppendLine($"\n<color=yellow>WARNING! UPDATING A MOD WILL REQUIRE THE GAME TO SELF CLOSE IN ORDER TO FULLY UPDATE THE MOD.</color>");
                context.AppendLine($"<color=yellow>THIS FEATURE IS ALSO IN BETA, SO DON'T EXPECT IT TO WORK AS WANTED</color>");
                context.AppendLine($"\n<color=yellow>UPDATING...</color>");
            }

            if (DownloadingMod || FailedToDownload || Successfully || Started || UpdateWarning)
                return context.ToString();

            StringBuilder content = new StringBuilder();

            if (MonkeModList.instance.UnableToGetData) {
                content.AppendLine("\n<color=yellow>GETTING MOD LIST FROM GITHUB...</color>");
                content.AppendLine("<color=yellow>IF THIS TAKES A WHILE THAT MIGHT MEAN THE REPO IS PRIVATE OR YOU ARE UNABLE TO CONNECT.</color>");
            }
            else if(AllMods) {
                int startIndex = CurrentPage * ModsPerPage;
                int endIndex = Mathf.Min(startIndex + ModsPerPage, MonkeModList.instance.listedMods.Count);

                for (int i = startIndex; i < endIndex; i++) {
                    int displayedIndex = i - (CurrentPage * ModsPerPage);

                    string modInfo = $"{MonkeModList.instance.listedMods[i][0].ToUpper()}, {MonkeModList.instance.listedMods[i][1].ToUpper()}, {MonkeModList.instance.listedMods[i][2].ToUpper()}";

                    if (displayedIndex == SelectedMod) {
                        modInfo = "> " + modInfo;
                    }

                    content.AppendLine(modInfo);
                }
            }
            else if (Version) {
                content.AppendLine("---------------<VERSION>---------------");
                content.AppendLine($"VERSION: {MonkeModList.instance.Version}");

                if (MonkeModList.instance.NewUpdate) {
                    content.AppendLine("\n<color=yellow>NEW VERSION AVAILABLE, PRESS ENTER TO UPDATE MONKEMODLIST</color>");
                }
            }

            return content.ToString();
        }

        public void OnKeyPressed(GorillaKeyboardButton button) {
            if (button.characterString == "option1") {
                if(Started)
                    Started = false;

                AllMods = true;
                MonkeModList.instance.GetModList();
            }
            else if (button.characterString == "W") {
                SelectedMod--;
                if (SelectedMod < 0) {
                    SelectedMod = ModsPerPage - 1;
                    if (CurrentPage > 0) {
                        CurrentPage--;
                    }
                }
            }
            else if (button.characterString == "S") {
                SelectedMod++;
                if (SelectedMod >= ModsPerPage) {
                    SelectedMod = 0;
                    CurrentPage++;
                }
            }
            else if (button.characterString == "A" || button.characterString == "D") {
                if (AllMods) {
                    AllMods = false;
                    Version = true;
                }
                else if (Version) {
                    AllMods = true;
                    Version = false;
                }
                //I don't care if "I have a better way to do this", I just really don't care.
            }
            else if (button.characterString == "enter")
            {
                int displayedIndex = SelectedMod + (CurrentPage * ModsPerPage);
                if (FailedToDownload) {
                    FailedToDownload = false;
                    SelectedMod = 0;
                    CurrentPage = 0;
                }
                else if (Successfully) {
                    Successfully = false;
                    SelectedMod = 0;
                    CurrentPage = 0;
                }
                else if(MonkeModList.instance.NewUpdate && Version) {
                    MonkeModList.instance.UpdateSelf();
                }
                else if (displayedIndex >= 0 && displayedIndex < MonkeModList.instance.listedMods.Count)  {
                    SelectedMod = displayedIndex;
                    MonkeModList.instance.DownloadSelectedMod();
                }
            }
        }


        public void RefreshModList() {
            if(MonkeModList.instance.RawList != null) {
                MonkeModList.instance.listedMods.Clear();

                string[] lines = MonkeModList.instance.RawList.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines) {
                    string[] parts = line.Split(' ');

                    if (parts.Length >= 4) {
                        string name = parts[0];
                        string version = parts[1];
                        string credit = parts[2];
                        string url = parts[3];

                        string[] modInfo = { name, version, credit, url };
                        MonkeModList.instance.listedMods.Add(modInfo);
                    }
                }
            }

            if (MonkeModList.instance.NewUpdate) {
                Version = true;
                AllMods = false;
            }
        }

        public void Start() { instance = this; }
    }
}