using System;
using ThunderRoad;
using UnityEngine;

namespace SoulGemSystem
{
    public class SoulGemModule : ItemModule
    {
        // Prefab References
        public string gemAnimator;
        public string gemAudio;
        public string gemCollider;
        public string gemParticle;
        public string gemRenderer;
        // Control Fields
        public bool usesCustomShader = true;
        public bool forceType = false;
        public GemActionType forcedType = GemActionType.unstable;
        public float hapticForce = 10.0f;
        public GemActionType parsedGemType;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            parsedGemType = RandomWeightedType();
            item.gameObject.AddComponent<SoulGem>();
        }

        GemActionType RandomWeightedType()
        {
            int rng = SoulGemLevelModule.local.random.Next(SoulGemLevelModule.settings.GetWeightSum());
            int index = 0;
            foreach (int w in SoulGemLevelModule.settings.GetWeights())
            {
                if (rng < w)
                    return (GemActionType) index;
                rng -= w;
                index++;
            }
            return (GemActionType)index;
        }
    }
}
