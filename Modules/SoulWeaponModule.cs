using ThunderRoad;

namespace SoulGemSystem
{
    public class SoulWeaponModule : ItemModule
    {
        public string[] spellIDs = { "Fire", "Lightning", "Gravity" };
        public int selectedSpell = 0;
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<SoulWeapon>();
        }
    }
}
