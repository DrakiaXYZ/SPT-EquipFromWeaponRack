using BepInEx;
using DrakiaXYZ.EquipFromWeaponRack.Patches;

namespace DrakiaXYZ.EquipFromWeaponRack
{
    [BepInPlugin("xyz.drakia.equipfromweaponrack", "DrakiaXYZ-EquipFromWeaponRack", "1.2.0")]
    [BepInDependency("com.SPT.core", "3.9.0")]
    public class EquipFromWeaponRackPlugin : BaseUnityPlugin
    {
        public EquipFromWeaponRackPlugin()
        {
            new EquipItemWindowListPatch().Enable();
            new TagWeaponRackPatch().Enable();
        }
    }
}
