using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace SoulGemSystem
{
    public class GemSetting
    {
        public string description;
        public float charge;
        public int probability;
        public float[] color;
        public float[] emptyColor;
        const float normalizationFactor = 255f;
        const float alphaFactor = 1f;
        Color gemColor;
        Color emptyGemColor;

        public Color GetColor() { return gemColor; }

        public Color GetEmptyColor() { return emptyGemColor; }

        public override string ToString()
        {
            string repr = base.ToString();
            foreach (FieldInfo field in GetType().GetFields())
                repr += $"\n{field.Name}: {field.GetValue(this)}";
            return repr;
        }

        [OnDeserialized]
        void SetColor(StreamingContext context)
        {
            gemColor = new Color(color[0] / normalizationFactor, color[1] / normalizationFactor, color[2] / normalizationFactor, alphaFactor);
            emptyGemColor = new Color(emptyColor[0] / normalizationFactor, emptyColor[1] / normalizationFactor, emptyColor[2] / normalizationFactor, 1f);
        }
    }

    public class Settings
    {
        public float dropRate;
        public float spawnHeight;
        public float minThrowVelocity;
        public float blastForce;
        public float blastRadius;
        public float liftMult;
        public bool useCustomRGB;
        public float glowIntensity;
        public Dictionary<GemActionType, GemSetting> gemSettings;
        int[] spawnWeights;
        int cumulativeSum;
        const string settingsFile = "StreamingAssets/Mods/SoulGemSystem/user_settings.json";
       
        public static Settings ReadFromDisk()
        {
            string json = File.ReadAllText(Path.Combine(Application.dataPath, settingsFile));
            return JsonConvert.DeserializeObject<Settings>(json);
        }

        public int[] GetWeights() { return spawnWeights; }

        public int GetWeightSum() { return cumulativeSum; }

        public override string ToString()
        {
            string repr = base.ToString();
            foreach (FieldInfo field in GetType().GetFields())
                repr += $"\n{field.Name}: {field.GetValue(this)}";
            foreach (KeyValuePair<GemActionType, GemSetting> g in gemSettings)
                repr += $"\n{g.Value.ToString()}";
            return repr;
        }

        [OnDeserialized]
        void CalculateFields(StreamingContext context)
        {
            if (SoulGemLibrary.GemActionTypeValues.Length > gemSettings.Count) return;
            spawnWeights = new int[SoulGemLibrary.GemActionTypeValues.Length];
            int i = 0;
            cumulativeSum = 0;
            foreach (GemActionType gemAction in SoulGemLibrary.GemActionTypeValues)
            {
                cumulativeSum += gemSettings[gemAction].probability;
                spawnWeights[i] = gemSettings[gemAction].probability;
                i++;
            }
        }
    }
}