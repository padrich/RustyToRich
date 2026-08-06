using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CarFlipTycoon.SaveSystem;
using UnityEngine;

namespace CarFlipTycoon.Core
{
    /// <summary>Lokales Speichern/Laden des Spielstands als JSON-Datei im persistentDataPath.</summary>
    public class SaveManager : MonoBehaviour
    {
        private const string SaveFileName = "carfliptycoon_save.json";

        // Nur zur Erkennung von außerhalb des Spiels bearbeiteten Speicherständen (z. B. per Texteditor
        // hochgesetzte Coins, gelöschte Timer, zurückgesetztes lastDailyRewardDateUtc) gedacht - kein
        // Schutz vor einem Angreifer, der die App selbst disassembliert. Für echten Schutz vor
        // entschlossenen Cheatern wäre eine serverseitige Validierung nötig.
        private static readonly byte[] IntegrityKey =
            Encoding.UTF8.GetBytes("CarFlipTycoon-LocalSaveIntegrity-v1-7hF2kQxN9mZ4pR6t");

        public static SaveManager Instance { get; private set; }

        public SaveData CurrentSave { get; private set; }

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        [Serializable]
        private class SaveEnvelope
        {
            public string data;
            public string signature;
        }

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
                    string raw = File.ReadAllText(SavePath);
                    CurrentSave = TryLoadSigned(raw) ?? new SaveData();
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

        /// <summary>
        /// Lädt einen signierten Spielstand, sofern Signatur und Inhalt zusammenpassen. Liefert in
        /// jedem anderen Fall (kein Envelope, fehlende/fehlerhafte Signatur, defektes JSON) null –
        /// es gibt bewusst KEINEN Fallback auf ein unsigniertes Format, da sich ein Angreifer sonst
        /// durch einfaches Weglassen des Envelopes an der Signaturprüfung vorbeischummeln könnte.
        /// </summary>
        private static SaveData TryLoadSigned(string raw)
        {
            SaveEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<SaveEnvelope>(raw);
            }
            catch
            {
                return null;
            }

            if (envelope == null || string.IsNullOrEmpty(envelope.data) || string.IsNullOrEmpty(envelope.signature))
            {
                return null;
            }

            if (!SignaturesMatch(ComputeSignature(envelope.data), envelope.signature))
            {
                Debug.LogWarning("[SaveManager] Speicherstand-Signatur ungültig (Datei wurde außerhalb des " +
                                  "Spiels verändert) - manipulierter Stand wird verworfen, starte mit neuem Save.");
                return null;
            }

            return JsonUtility.FromJson<SaveData>(envelope.data);
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentSave, true);
                var envelope = new SaveEnvelope { data = json, signature = ComputeSignature(json) };
                File.WriteAllText(SavePath, JsonUtility.ToJson(envelope));
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

        private static string ComputeSignature(string json)
        {
            using var hmac = new HMACSHA256(IntegrityKey);
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(hash);
        }

        /// <summary>Konstantzeit-Vergleich, um Timing-Seitenkanäle beim Signaturvergleich zu vermeiden.</summary>
        private static bool SignaturesMatch(string a, string b)
        {
            byte[] bytesA = Encoding.UTF8.GetBytes(a);
            byte[] bytesB = Encoding.UTF8.GetBytes(b);
            if (bytesA.Length != bytesB.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < bytesA.Length; i++)
            {
                diff |= bytesA[i] ^ bytesB[i];
            }

            return diff == 0;
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
