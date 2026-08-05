using System;
using System.IO;
using CarFlipTycoon.SaveSystem;
using UnityEngine;

namespace CarFlipTycoon.Core
{
    /// <summary>Lokales Speichern/Laden des Spielstands als JSON-Datei im persistentDataPath.</summary>
    public class SaveManager : MonoBehaviour
    {
        private const string SaveFileName = "carfliptycoon_save.json";

        public static SaveManager Instance { get; private set; }

        public SaveData CurrentSave { get; private set; }

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    CurrentSave = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
                }
                else
                {
                    CurrentSave = new SaveData();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Speicherstand konnte nicht geladen werden, starte mit neuem Save: {e}");
                CurrentSave = new SaveData();
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentSave, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Speicherstand konnte nicht geschrieben werden: {e}");
            }
        }

        public void ResetSave()
        {
            CurrentSave = new SaveData();
            Save();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            Save();
        }
    }
}
