using System;
using System.Linq;
using UnityEngine;
using ThunderRoad;

namespace SoulGemSystem
{
    public enum GemActionType
    {
        unstable,
        health,
        mana,
        focus
    }

    public delegate void GemAction(SoulGem crystal, SoulGemModule module, Transform transform, object inputAction = null);

    public static class SoulGemLibrary
    {
        public static Color gemPurple = new Color(0.53f, 0.33f, 0.82f, 1f);
        public static Color gemGreen = new Color(0.15f, 1f, 0.57f, 1f);
        public static Color gemBlue = new Color(0.035f, 0.52f, 0.89f, 1f);
        public static Color gemRed = new Color(0.84f, 0.19f, 0.19f, 1f);
        public static Color gemEmptyDefault = Color.black;

        public const string shaderBaseColor = "Color_9843a7b284934d45b9a6ae5a721b4b9e";
        public const string shaderEmissionColor = "Color_59207806fe1a40d3b411330211418451";
        public const string shaderEmissionBool = "Boolean_b95e7a15de6548339088526e6dfd73bb";
        public const string shaderGlowRate = "Vector1_b88b50ff010c4b04a0882f53146b3a44";
        public const string shaderRoughness = "Vector1_296157959f5d465fa31c4ba715d58bcb";

        public static Array GemActionTypeValues = Enum.GetValues(typeof(GemActionType));

        public static GemAction GenerateAction(GemActionType t)
        {
            switch (t)
            {
                case GemActionType.unstable:
                    return new GemAction(CrystalExplode);
                case GemActionType.health:
                    return new GemAction(CrystalHeal);
                case GemActionType.mana:
                    return new GemAction(CrystalMana);
                case GemActionType.focus:
                    return new GemAction(CrystalFocus);
                default:
                    return new GemAction(CrystalEffect);
            }
        }

        public static Color GenerateColor(GemActionType t)
        {
            switch(t)
            {
                case GemActionType.unstable:
                    return gemPurple;
                case GemActionType.health:
                    return gemGreen;
                case GemActionType.mana:
                    return gemBlue;
                case GemActionType.focus:
                    return gemRed;
                default:
                    return gemPurple;
            }
        }

        static void CrystalEffect(SoulGem crystal, bool meshVisible = true, bool originalClip = true)
        {
            crystal.renderer.enabled = meshVisible;
            crystal.collider.enabled = meshVisible;
            crystal.particle.Play();
            if (originalClip)
            {
                crystal.audio.clip = crystal.originalClip;
                crystal.audio.Play();
            }
            else crystal.PlayRandomClip();
        }

        public static void CrystalEffect(SoulGem crystal, SoulGemModule _m, Transform _t, object _o)
        {
            //bool o_set = false;
            //if (_o != null) o_set = true;
            bool o_set = _o != null ? true : false;
            CrystalEffect(crystal, originalClip: !o_set);
        }

        static void CrystalHeal(SoulGem crystal, SoulGemModule module, Transform _t, object _o)
        {
            crystal.SetCharge(false);
            CrystalEffect(crystal, module, _t, true);
            Player.currentCreature.Heal(crystal.energy, Player.currentCreature);
        }

        static void CrystalMana(SoulGem crystal, SoulGemModule module, Transform _t, object _o)
        {
            crystal.SetCharge(false);
            CrystalEffect(crystal, module, _t, true);
            Player.currentCreature.mana.currentMana = Mathf.Clamp(Player.currentCreature.mana.currentMana + crystal.energy, 0f, Player.currentCreature.mana.maxMana);
        }

        static void CrystalFocus(SoulGem crystal, SoulGemModule module, Transform _t, object _o)
        {
            crystal.SetCharge(false);
            CrystalEffect(crystal, module, _t, true);
            Player.currentCreature.mana.currentFocus = Mathf.Clamp(Player.currentCreature.mana.currentFocus + crystal.energy, 0f, Player.currentCreature.mana.maxFocus);
        }

        static void CrystalExplode(SoulGem crystal, SoulGemModule module, Transform transform, object button)
        {
            crystal.SetCharge(false);
            CrystalEffect(crystal, module, transform, true);
            ExplosiveForce(transform.position, crystal.energy, SoulGemLevelModule.settings.blastForce, SoulGemLevelModule.settings.blastRadius, SoulGemLevelModule.settings.liftMult);
        }

        static void ExplosiveForce(Vector3 origin, float damage, float force, float blastRadius, float liftMult, ForceMode forceMode = ForceMode.Impulse, bool ignorePlayer = true)
        {
            try
            {
                foreach (Item item in Item.allActive)
                {
                    if (item.gameObject.TryGetComponent(out SoulGem s)) continue;
                    if ((item.transform.position - origin).sqrMagnitude <= blastRadius*blastRadius)
                    {
                        item.rb.AddExplosionForce(force * item.rb.mass, origin, blastRadius, liftMult, forceMode);
                        item.rb.AddForce(Vector3.up * liftMult * item.rb.mass, forceMode);
                    }
                }
                Creature closestCreature = Creature.allActive.Where(c => 
                    (c.transform.position - origin).sqrMagnitude <= blastRadius * blastRadius && 
                    c != Player.currentCreature &&
                    (c.state == Creature.State.Alive || c.state == Creature.State.Destabilized) && 
                    c.isActiveAndEnabled && 
                    c.ragdoll.isActiveAndEnabled).OrderBy(c => (origin - c.ragdoll.transform.position).sqrMagnitude).FirstOrDefault();

                // if no living creatures in range, find first dead creature or exit
                if (closestCreature == null)
                {
                    closestCreature = Creature.allActive.Where(c => 
                        (c.transform.position - origin).sqrMagnitude <= blastRadius * blastRadius && 
                        c.state == Creature.State.Dead && 
                        c.isActiveAndEnabled && 
                        c.ragdoll.isActiveAndEnabled).OrderBy(c => (origin - c.ragdoll.transform.position).sqrMagnitude).FirstOrDefault();
                    if (closestCreature == null) return;
                }
                // Kill Creature, apply force to main body, slice RagdollParts and apply force to each part
                if (closestCreature.state == Creature.State.Alive)
                {
                    if (closestCreature.currentHealth <= damage)
                    {
                        closestCreature.TestKill();
                    }
                    else
                    {
                        closestCreature.Damage(new CollisionInstance(new DamageStruct(DamageType.Energy, damage)));
                        return; // Dont slice, leave alive.
                    }
                }
                closestCreature.locomotion.rb.AddExplosionForce(force * closestCreature.locomotion.rb.mass, origin, blastRadius, liftMult, forceMode);
                closestCreature.locomotion.rb.AddForce(Vector3.up * liftMult * closestCreature.locomotion.rb.mass, forceMode);
                FullSlice(closestCreature);
                foreach (RagdollPart part in closestCreature.ragdoll.parts)
                {
                    part.rb.AddExplosionForce(force * part.rb.mass, origin, blastRadius, liftMult, forceMode);
                    part.rb.AddForce(Vector3.up * liftMult * part.rb.mass, forceMode);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SoulGemSystem] { e.Message} \n {e.StackTrace}");
            }
        }

        static void FullSlice(Creature creature)
        {
            creature.ragdoll.headPart.Slice();
            creature.ragdoll.GetPart(RagdollPart.Type.LeftArm).Slice();
            creature.ragdoll.GetPart(RagdollPart.Type.RightArm).Slice();
            creature.ragdoll.GetPart(RagdollPart.Type.LeftLeg).Slice();
            creature.ragdoll.GetPart(RagdollPart.Type.RightLeg).Slice();
            return;
        }
    }
}
