using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace SoulGemSystem
{
    public class SoulPouch : MonoBehaviour
    {
        readonly Action writeToDisk = () => { SoulGemLevelModule.soulTracker?.WriteToDisk(); };
        Item item;
        SoulPouchModule module;
        Holder holder;
        Item.HolderDelegate writeToDiskOnSnap;
        Item.HolderDelegate writeToDiskOnUnSnap;
        Item.GrabDelegate Item_OnGrabEvent;
        Item.ReleaseDelegate Item_OnUngrabEvent;
        Item.SpawnEvent writeToDiskOnSpawn;
        Item.CullEvent writeToDiskOnCull;
        List<Item> spawnBuffer;
        bool ignoreTracker;
        bool spawnLock;
        bool snapped;
        bool held;

        void Awake()
        {
            if (SoulGemLevelModule.soulTracker == null || SoulGemLevelModule.local == null) return;
            SoulGemLevelModule.soulTracker.AddPouch(this);
            spawnBuffer = new List<Item>();

            writeToDiskOnSnap = new Item.HolderDelegate((h) => { writeToDisk(); snapped = true; });
            writeToDiskOnUnSnap = new Item.HolderDelegate((h) => { writeToDisk(); snapped = false; });

            Item_OnGrabEvent = new Item.GrabDelegate((h,r) => { held = true; });
            Item_OnUngrabEvent = new Item.ReleaseDelegate((h, r, t) => { held = false; });

            writeToDiskOnSpawn = new Item.SpawnEvent((e) => { writeToDisk(); SoulGemLevelModule.soulTracker.RemovePouch(this); });
            writeToDiskOnCull = new Item.CullEvent((c) => { writeToDisk(); SoulGemLevelModule.soulTracker.RemovePouch(this); });

            item = GetComponent<Item>();
            module = item.data.GetModule<SoulPouchModule>();
            holder = GetComponentInChildren<Holder>();
            item.OnGrabEvent += Item_OnGrabEvent;
            item.OnUngrabEvent += Item_OnUngrabEvent;
            item.OnSnapEvent += writeToDiskOnSnap;
            item.OnUnSnapEvent += writeToDiskOnUnSnap;
            item.OnDespawnEvent += writeToDiskOnSpawn;
            item.OnCullEvent += writeToDiskOnCull;
            holder.Snapped += Holder_Snapped;
            holder.UnSnapped += Holder_UnSnapped;
        }

        void Start()
        {
            PopulatePouch();
        }

        void OnDestory()
        {
            SoulGemLevelModule.soulTracker.RemovePouch(this);
            item.OnGrabEvent -= Item_OnGrabEvent;
            item.OnUngrabEvent -= Item_OnUngrabEvent;
            item.OnSnapEvent -= writeToDiskOnSnap;
            item.OnUnSnapEvent -= writeToDiskOnUnSnap;
            item.OnDespawnEvent -= writeToDiskOnSpawn;
            holder.Snapped -= Holder_Snapped;
            holder.UnSnapped -= Holder_UnSnapped;
        }

        void OnApplicationQuit()
        {
            writeToDisk();
        }

        void Holder_UnSnapped(Item i)
        {
            if (Player.local.creature == null || Player.local.creature.ragdoll == null) return;
            if (i.TryGetComponent(out SoulGem sg))
            {
                TrackGemSnap(sg, snap:false);
            }
        }

        void Holder_Snapped(Item i)
        {
            if (Player.local.creature == null || Player.local.creature.ragdoll == null) return;
            if (i.TryGetComponent(out SoulGem sg))
            {
                TrackGemSnap(sg, snap: true);
                return;
            }
            holder.UnSnap(i);  // Reject non-soul item
        }

        void TrackGemSnap(SoulGem sg, bool snap = true)
        {
            if (ignoreTracker) return;
            if (snap) SoulGemLevelModule.soulTracker.AddSoul(sg.GetSoulName(), sg.GetCharge() ? 1 : 0, (int) sg.GetGemType());
            else SoulGemLevelModule.soulTracker.RemoveSoul(sg.GetSoulName());
            SoulGemLevelModule.soulTracker.UpdateAllStorages(this);
        }

        void SpawnAndBuffer(string name, bool charge, int type)
        {
            ItemData soulGemItemData = Catalog.GetData<ItemData>(SoulGemLevelModule.local.soulGemItemID, true);
            if (soulGemItemData == null) return;
            soulGemItemData.SpawnAsync(spawnedGem =>
            {
                try
                {
                    spawnedGem.transform.position = transform.position;
                    spawnedGem.transform.rotation = Quaternion.identity;
                    if (!spawnedGem.TryGetComponent(out SoulGem sg)) return;
                    sg.SetRenderMaterial(SoulGemLevelModule.local.gemMat);
                    sg.SetAudioClips(SoulGemLevelModule.local.audioClips);
                    sg.SetName(name);
                    sg.SetGemType((GemActionType)type);
                    sg.SetCharge(state: charge, init: true);
                    sg.collider.enabled = false;
                    sg.renderer.enabled = false;
                    spawnBuffer.Add(spawnedGem);
                    spawnLock = false;
                }
                catch (Exception e) { Debug.Log($"[SoulGemSystem][ERROR] {e.ToString()}"); }
            },
            transform.position,
            Quaternion.identity);
        }

        bool FreezeCondition() { return !(snapped || held); }

        IEnumerator WaitOnPopulate()
        {
            ignoreTracker = true;
            spawnBuffer = new List<Item>();
            if (FreezeCondition()) item.rb.isKinematic = true;
            Item[] heldObjects = holder.items.ToArray();
            foreach (Item i in heldObjects)
            {   // Clear Contents
                holder.UnSnap(i);
                i.Despawn();
            }
            yield return new WaitForEndOfFrame();
            foreach (KeyValuePair<string, int[]> trackedSoul in SoulGemLevelModule.soulTracker.volatileStorage)
            {
                if (trackedSoul.Value.Length < 2) continue;
                spawnLock = true;
                SpawnAndBuffer(name: trackedSoul.Key, charge: trackedSoul.Value[0] == 1, type: trackedSoul.Value[1]);
                do yield return null;
                while (spawnLock);
            }

            foreach (Item spawnedGem in spawnBuffer)
            {
                holder.Snap(spawnedGem, silent: true);
                if (spawnedGem.TryGetComponent(out SoulGem sg)) sg.renderer.enabled = true;
            }
            if (FreezeCondition()) item.rb.isKinematic = false;
            ignoreTracker = false;
        }

        public void PopulatePouch() { StartCoroutine(WaitOnPopulate()); }
    }
}

