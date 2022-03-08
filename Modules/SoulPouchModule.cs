using ThunderRoad;

namespace SoulGemSystem
{
    public class SoulPouchModule : ItemModule
    {
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<SoulPouch>();
        }
    }
}
