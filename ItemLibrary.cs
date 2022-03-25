using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;
namespace SoulGemSystem
{
    public class ItemLibrary
    {
        public static List<string> availableDrops = new List<string>();
        const string forbiddenText = "SoulGem";

        public void BuildItemsList()
        {
            availableDrops = Catalog.GetAllID<ItemData>().FindAll(ValidItem);
#if DEBUG
            if (availableDrops.Count == 0) UnityEngine.Debug.LogWarning("Empty Collection Found for ItemLibrary");
#endif
        }

        public string RandomItem()
        {
            if (availableDrops == null)
                BuildItemsList();
            return availableDrops[UnityEngine.Random.Range(0, availableDrops.Count)];
        }

        bool ValidItem(string i)
        {
            ItemData iItem = Catalog.GetData<ItemData>(i, true);
            return iItem.purchasable 
                && (iItem.type.Equals(ItemData.Type.Weapon) || iItem.type.Equals(ItemData.Type.Potion))
                && !iItem.id.ToString().Contains(forbiddenText);
        }
    }
}
