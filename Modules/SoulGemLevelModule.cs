using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ThunderRoad;
using UnityEngine;

namespace SoulGemSystem
{
    public class SoulGemLevelModule : LevelModule
    {
        public static SoulGemLevelModule local;
        public static SoulStorage soulTracker;
        public static Settings settings;
        public static ItemLibrary itemSpawnLibrary;
        public string soulGemItemID;
        public string[] clipAddresses;
        public string materialAddress;
        public System.Random random = new System.Random();
        public List<AudioClip> audioClips = new List<AudioClip>();
        public Material gemMat;
        bool spawnFlag;

        public override IEnumerator OnLoadCoroutine()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            if (settings == null) settings = Settings.ReadFromDisk();
            if ((settings.version != version) || (settings.gemSettings.Count < SoulGemLibrary.GemActionTypeValues.Length))
            {
                Debug.LogWarning($"[SoulGemSystem][Warning][{Time.time}] Mismatch in user settings. Reverting to default settings");
                settings = Settings.ReadFromDisk(Settings.defaultSettingsFile);
            }
            if (local == null) local = this;
            if (soulTracker == null) soulTracker = new SoulStorage(SoulNames.modDataPath, SoulNames.persistenceFile);
            if (itemSpawnLibrary == null) itemSpawnLibrary = new ItemLibrary();
            itemSpawnLibrary.BuildItemsList();
            EventManager.onCreatureKill += DropCreatureSoul;
            Debug.Log($"[SoulGemSystem][LevelModule][{Time.time}] Global references created for SoulGemSystem v{version}");
            Debug.Log($"[SoulGemSystem][LevelModule] Mod Settings Loaded:\n{settings.ToString()}");
            foreach (string clipAddress in clipAddresses)
            {
                Catalog.LoadAssetAsync<AudioClip>(clipAddress, value =>
                {
                    audioClips.Add(value);
                    Debug.Log($"[SoulGemSystem][LevelModule][{Time.time}] Loaded Gem Audio: {clipAddress}");
                }, clipAddress);
            }
            if (!string.IsNullOrEmpty(materialAddress))
                Catalog.LoadAssetAsync<Material>(materialAddress, mat => 
                {
                    gemMat = new Material(mat);
                    Debug.Log($"[SoulGemSystem][LevelModule][{Time.time}] Loaded Gem Material: {materialAddress}");
                }, materialAddress);
            return base.OnLoadCoroutine();
        }

        public override void OnUnload()
        {
            base.OnUnload();
            local = null;
            soulTracker = null;
            EventManager.onCreatureKill -= DropCreatureSoul;
        }

        void DropCreatureSoul(Creature creature, Player player, CollisionInstance collisionInstance, EventTime eventTime)
        {
            if (!spawnFlag) { spawnFlag = true; return; }
            spawnFlag = false; // TODO: 'spawnFlag' is a workaround for onCreatureKill being called twice by EventManager
            if ((random.Next(1, 100) / 100f) > settings.dropRate) return;
            ItemData soulGemItemData = Catalog.GetData<ItemData>(soulGemItemID, true);
            if (soulGemItemData == null) return;
            Vector3 soulSpawnPoint = new Vector3(
                creature.ragdoll.transform.position.x,
                creature.ragdoll.transform.position.y + settings.spawnHeight,
                creature.ragdoll.transform.position.z);
            soulGemItemData.SpawnAsync(spawnedGem =>
            {
                try
                {
                    spawnedGem.transform.position = soulSpawnPoint;
                    spawnedGem.transform.rotation = Quaternion.identity;
                    if (!spawnedGem.TryGetComponent(out SoulGem sg)) return; 
                    sg.DroppedFromCreature();
                }
                catch (Exception e) { Debug.Log($"[SoulGemSystem][ERROR] {e.ToString()}"); }
            },
            soulSpawnPoint,
            Quaternion.identity);
        }
    }
}
