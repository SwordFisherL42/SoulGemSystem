using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace SoulGemSystem
{
    public class SoulStorage
    {
        readonly string storageDir;
        readonly string storageFile;
        readonly char[] trimChars = { ' ' };
        const char lineDelimiter = ',';
        public Dictionary<string, int[]> volatileStorage;
        public List<SoulPouch> activePouches;

        public SoulStorage(string storageDir, string storageFile, bool loadFromDisk = true)
        {
            this.storageDir = storageDir;
            this.storageFile = storageFile;
            volatileStorage = new Dictionary<string, int[]>();
            activePouches = new List<SoulPouch>();
            if (loadFromDisk) LoadFromDisk();
        }

        public void AddSoul(string name, int charged, int type)
        {
            if (volatileStorage.ContainsKey(name)) return;
            int[] gemData = new int[] { charged, type };
            volatileStorage.Add(name, gemData);
        }

        public void RemoveSoul(string name)
        {
            if (!volatileStorage.ContainsKey(name)) return;
            volatileStorage.Remove(name);
        }

        public void LoadFromDisk()
        {
            Debug.Log($"[SoulGemSystem][{Time.time}] Reading Soul Data from Disk ...");
            foreach (string line in File.ReadAllLines(Path.Combine(Application.dataPath, storageDir, storageFile)))
            {
                string[] lineData = line.Split(lineDelimiter);
                if (lineData.Length < 3) continue;
                AddSoul(lineData[0].Trim(trimChars), int.Parse(lineData[1].Trim(trimChars)), int.Parse(lineData[2].Trim(trimChars)));
            }
        }

        public void WriteToDisk()
        {
            Debug.Log($"[SoulGemSystem][{Time.time}] Saving Soul Data ...");
            List<string> lineData = new List<string>();
            foreach(KeyValuePair<string, int[]> volatileSoul in volatileStorage)
                lineData.Add($"{volatileSoul.Key},{volatileSoul.Value[0]},{volatileSoul.Value[1]}");
            File.WriteAllLines(Path.Combine(Application.dataPath, storageDir, storageFile), lineData.ToArray());
            Debug.Log($"[SoulGemSystem][{Time.time}] Save Successful.");
        }
        
        public void AddPouch(SoulPouch p)
        {
            if (activePouches.Contains(p)) return;
            activePouches.Add(p);
            Debug.Log($"[SoulGemSystem][{Time.time}] Added instance to 'activePouches'. Total: {activePouches.Count}");
        }

        public void RemovePouch(SoulPouch p)
        {
            if (!activePouches.Contains(p)) return;
            activePouches.Remove(p);
            Debug.Log($"[SoulGemSystem][{Time.time}] Removed instance from 'activePouches'. Total: {activePouches.Count}");
        }

        public void UpdateAllStorages(SoulPouch caller)
        {
            foreach (SoulPouch pouch in activePouches)
            {
                if (pouch == caller) continue;
                Debug.Log($"[SoulGemSystem][{Time.time}] Populating pouch instance ... ");
                pouch.PopulatePouch();
            }
        }

    }
}
