using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace SoulGemSystem
{
    public class SoulWeapon : MonoBehaviour
    {
        Item item;
        SoulWeaponModule module;

        SpellCastCharge weaponSpell;

        Imbue itemMainImbue;

        void Awake()
        {
            item = GetComponent<Item>();
            module = item.data.GetModule<SoulWeaponModule>();

            try { weaponSpell = Catalog.GetData<SpellCastCharge>(module.spellIDs[0], true); }
            catch { Debug.LogError(string.Format("[SoulGemSystem] Exception! Unable to Find Spell {0}", module.spellIDs[0])); }

            try
            {
                if (item.imbues.Count > 0) itemMainImbue = item.imbues[0];
                else itemMainImbue = null;
            }
            catch { Debug.LogError(string.Format("[SoulGemSystem] Exception! Unable to Find/Set main Imbue for item {0}", item.name)); }

        }

        public void ChargeWeapon()
        {
            Debug.Log($"[SoulGemSystem] Charging Weapon");
            StartCoroutine(TransferDeltaEnergy(itemMainImbue, weaponSpell));
        }

        private IEnumerator TransferDeltaEnergy(Imbue itemImbue, SpellCastCharge activeSpell, float energyDelta = 1.0f)
        {
            int counts = (int)Mathf.Round(200.0f / energyDelta);
            for (int i = 0; i < counts; i++)
            {
                try
                {
                    itemImbue.Transfer(activeSpell, energyDelta);
                    if (itemImbue.energy >= itemImbue.maxEnergy) { break; }
                }
                catch { }
                yield return null;
            }
            yield return null;
        }
    }
}
