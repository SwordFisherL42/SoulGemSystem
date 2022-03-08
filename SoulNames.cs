using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SoulGemSystem
{
    public static class SoulNames
    {
        const char apostrophe = '\'';
        const char postfix = 's';
        const string item_designate = "Soul";
        public const string modDataPath = "StreamingAssets/Mods/SoulGemSystem/CustomData";
        public const string namesFile = "soul-names.txt";
        public const string persistenceFile = "stored-souls.txt";
        static System.Random random = new System.Random();
        public static HashSet<string> names = new HashSet<string>(File.ReadLines(Path.Combine(Application.dataPath, modDataPath, namesFile)));
        public static string GetRandomName() { return names.ElementAt(random.Next(names.Count)); }
        public static string GetName(int i) { return names.ElementAt(i); }
        public static string ParseItemName(string name)
        {
            name += apostrophe;
            if (name[name.Length - 2] != postfix) name += postfix;
            return $"{name} {item_designate}";  // i.e. "Harris' Soul" vs "Jane's Soul"
        }
    }
}
