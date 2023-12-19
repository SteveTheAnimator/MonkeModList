using BepInEx;
using ComputerPlusPlus;
using GorillaNetworking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Utilla;
using System.IO;
using System.IO.Compression;

namespace MonkeModList {
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla")]
    [BepInDependency("com.kylethescientist.gorillatag.computerplusplus")]
    [BepInPlugin(ModInfo.GUID, ModInfo.Name, ModInfo.Version)]
    public class MonkeModList : BaseUnityPlugin {

        public static MonkeModList instance;
        public bool UnableToGetData { get; private set; } = true;
        public string RawList { get; private set; } = null;
        public bool CanRefresh { get; private set; } = true;

        public List<string[]> listedMods = new List<string[]>();

        void Start() { Events.GameInitialized += OnGameInitialized; }

        void OnGameInitialized(object sender, EventArgs e) {
            instance = this;
            GetModList();
        }

        public void GetModList() { StartCoroutine(GetData()); StartCoroutine(RefreshDelay()); }
        IEnumerator GetData() {
            UnityWebRequest www = UnityWebRequest.Get("https://raw.githubusercontent.com/MrBanana01/MonkeModList/master/List");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success) {
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

            string fileExtension = Path.GetExtension(listedMods[ScreenData.instance.SelectedMod][3].ToLower());
            string path = Path.Combine(BepInEx.Paths.PluginPath, $"{listedMods[ScreenData.instance.SelectedMod][0]}{fileExtension}");
            string dupeName = Path.Combine(BepInEx.Paths.PluginPath, $"{listedMods[ScreenData.instance.SelectedMod][0]}-MonkeModList{fileExtension}");

            UnityWebRequest www = UnityWebRequest.Get(listedMods[ScreenData.instance.SelectedMod][3]);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError || www.result != UnityWebRequest.Result.Success) {
                ScreenData.instance.FailedToDownload = true;
                ScreenData.instance.Successfully = false;
                ScreenData.instance.DownloadingMod = false;
                Debug.Log(www.error);
            }
            else {
                try {
                    if (fileExtension == ".zip") {
                        if (File.Exists(path.Replace(".zip", ""))) {
                            File.Delete(path);
                        }
                        string savePath = Path.Combine(BepInEx.Paths.PluginPath, dupeName);
                        File.WriteAllBytes(savePath, www.downloadHandler.data);
                        ZipFile.ExtractToDirectory(dupeName, BepInEx.Paths.PluginPath);
                        File.Delete(dupeName);
                        ScreenData.instance.FailedToDownload = false;
                        ScreenData.instance.Successfully = true;
                        ScreenData.instance.DownloadingMod = false;
                    }
                    else if (fileExtension == ".dll") {
                        if (File.Exists(path)) {
                            File.Delete(path);
                        }
                        string savePath = Path.Combine(BepInEx.Paths.PluginPath, path);
                        File.WriteAllBytes(savePath, www.downloadHandler.data);
                        ScreenData.instance.FailedToDownload = false;
                        ScreenData.instance.Successfully = true;
                        ScreenData.instance.DownloadingMod = false;
                    }
                    else {
                        ScreenData.instance.FailedToDownload = true;
                        ScreenData.instance.Successfully = false;
                        ScreenData.instance.DownloadingMod = false;
                    }
                }
                catch (Exception ex) {
                    Debug.Log(www.error);
                    ScreenData.instance.FailedToDownload = true;
                    ScreenData.instance.Successfully = false;
                    ScreenData.instance.DownloadingMod = false;
                }
            }

            www.Dispose();
        }
    }

    internal class ScreenData : IScreen {
        public static ScreenData instance;

        public string Title => "MOD LIST";

        public string Description => "WELCOME TO THE MONKE MOD LIST! \nIf you don't want your mod on this list, please ask me/MrBanana to remove it. \n<color=yellow>MADE BY MRBANANA</color>";

        public int CurrentPage { get; private set; } = 0;

        public int SelectedMod { get; private set; } = 0;

        public int ModsPerPage { get; private set; } = 5;

        public bool DownloadingMod = false;

        public bool FailedToDownload = false;

        public bool Successfully = false;

        public string GetContent() {
            StringBuilder downloadingMod = new StringBuilder();

            if (DownloadingMod) {
                downloadingMod.AppendLine($"\n<color=yellow>DOWNLOADING MOD: {MonkeModList.instance.listedMods[SelectedMod][0]}!</color>");
            }

            StringBuilder Failed = new StringBuilder();

            if (FailedToDownload) {
                Failed.AppendLine($"\n<color=red>FAILED TO DOWNLOAD THE MOD: {MonkeModList.instance.listedMods[SelectedMod][0]}!</color>");
            }

            StringBuilder Success = new StringBuilder();

            if (Successfully) {
                Success.AppendLine($"\n<color=lime>SUCCESSFULY DOWNLOADED THE MOD: {MonkeModList.instance.listedMods[SelectedMod][0]}!</color>");
                Success.AppendLine($"<color=lime>PLEASE RESTART YOUR GAME TO LOAD THE MOD.</color>");
                Success.AppendLine($"<color=lime>PRESS ENTER TO GO BACK AND DOWNLOAD MORE MODS.</color>");
            }

            if (DownloadingMod)
                return downloadingMod.ToString();

            if (FailedToDownload)
                return Failed.ToString();

            if(Successfully)
                return Success.ToString();

            StringBuilder content = new StringBuilder();

            if (MonkeModList.instance.UnableToGetData) {
                content.AppendLine("\n<color=red>COULD NOT GET THE MOD LIST FROM GITHUB!</color> ");
            }
            else {
                int startIndex = CurrentPage * ModsPerPage;
                int endIndex = Mathf.Min(startIndex + ModsPerPage, MonkeModList.instance.listedMods.Count);

                for (int i = startIndex; i < endIndex; i++) {
                    int displayedIndex = i - (CurrentPage * ModsPerPage);

                    string modInfo = $"{MonkeModList.instance.listedMods[i][0].ToUpper()}, {MonkeModList.instance.listedMods[i][1].ToUpper()}";

                    if (displayedIndex == SelectedMod) {
                        modInfo = "> " + modInfo;
                    }

                    content.AppendLine(modInfo);
                }
            }

            return content.ToString();
        }

        public void OnKeyPressed(GorillaKeyboardButton button) {
            if (button.characterString == "option1") {
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
            else if (button.characterString == "enter") {
                if (FailedToDownload)
                    FailedToDownload = false;
                else if (Successfully)
                    Successfully = false;
                else {
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
                        string url = string.Join(" ", parts.Skip(3));

                        string[] modInfo = { name, version, credit, url };
                        MonkeModList.instance.listedMods.Add(modInfo);
                    }
                }
            }
        }

        public void Start() { instance = this; MonkeModList.instance.GetModList(); }
    }
}