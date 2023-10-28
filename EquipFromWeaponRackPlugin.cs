using BepInEx;
using DrakiaXYZ.EquipFromWeaponRack.Patches;

namespace DrakiaXYZ.EquipFromWeaponRack
{
    [BepInPlugin("xyz.drakia.equipfromweaponrack", "DrakiaXYZ-EquipFromWeaponRack", "1.0.0")]
    public class EquipFromWeaponRackPlugin : BaseUnityPlugin
    {
        public EquipFromWeaponRackPlugin()
        {
            new EquipItemWindowListPatch().Enable();
        }
    }
}
