using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace SoulGemSystem
{
    public class SoulGemLevelModule : LevelModule
    {
        public static SoulGemLevelModule local;
        public static SoulStorage soulTracker;
        public static Settings settings;
        public string soulGemItemID;
        public string[] clipAddresses;
        public string materialAddress;
        public System.Random random = new System.Random();
        public List<AudioClip> audioClips = new List<AudioClip>();
        public Material gemMat;
        bool spawnFlag;

        public override IEnumerator OnLoadCoroutine()
        {
            if (settings == null) settings = Settings.ReadFromDisk();
            Debug.Log($"[SoulGemSystem][LevelModule][{Time.time}] Mod Settings Loaded:\n{settings.ToString()}");
            if (local == null) local = this;
            if (soulTracker == null) soulTracker = new SoulStorage(SoulNames.modDataPath, SoulNames.persistenceFile);
            Debug.Log($"[SoulGemSystem][LevelModule][{Time.time}] Global instances created for SoulGemLevelModule & SoulStorage");
            EventManager.onCreatureKill += DropCreatureSoul;
            Debug.Log($"[SoulGemSystem][LevelModule][{Time.time}] Soul Gems Added to Creatures");
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
                    sg.SetRenderMaterial(gemMat);
                    sg.SetAudioClips(audioClips);
                    sg.SetName(SoulNames.GetRandomName());
                    sg.DroppedFromCreature();
                }
                catch (Exception e) { Debug.Log($"[SoulGemSystem][ERROR] {e.ToString()}"); }
            },
            soulSpawnPoint,
            Quaternion.identity);
        }
    }
}
