using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DrakiaXYZ.EquipFromWeaponRack.Patches
{
    internal class EquipItemWindowListPatch : ModulePatch
    {
        private static Type _inventoryControllerType;

        private static FieldInfo _inventoryControllerClassField;
        private static FieldInfo _itemAddressField;

        private static PropertyInfo _inventoryProperty;

        private static MethodInfo _getNotMergedItemsMethod;
        private static MethodInfo _canAcceptMethod;

        protected override MethodBase GetTargetMethod()
        {
            var targetClass = PatchConstants.EftTypes.Single(IsTargetClass);

            _inventoryControllerClassField = AccessTools.GetDeclaredFields(targetClass).Single(x => x.FieldType.Name == "InventoryControllerClass");
            _inventoryControllerType = _inventoryControllerClassField.FieldType;
            _inventoryProperty = AccessTools.Property(_inventoryControllerType, "Inventory");
            _itemAddressField = AccessTools.GetDeclaredFields(targetClass).Single(x => typeof(ItemAddress).IsAssignableFrom(x.FieldType));

            _getNotMergedItemsMethod = AccessTools.Method(PatchConstants.EftTypes.Single(x => AccessTools.GetMethodNames(x).Contains("GetNotMergedItems")), "GetNotMergedItems");

            Type canAcceptClass = PatchConstants.EftTypes.Single(IsCanAcceptClass);
            _canAcceptMethod = AccessTools.Method(canAcceptClass, "CanAccept");

            return AccessTools.GetDeclaredMethods(targetClass).Single(x => x.ReturnType.Equals(typeof(IEnumerable<Item>)));
        }

        private bool IsTargetClass(Type type)
        {
            return AccessTools.GetFieldNames(type).Contains("_placeHolder")
                && AccessTools.GetMethodNames(type).Contains("Show");
        }

        private bool IsCanAcceptClass(Type type)
        {
            return AccessTools.GetDeclaredMethods(type).Where(x => x.Name == "CanAccept" && x.IsStatic).Count() == 1 &&
                    AccessTools.GetMethodNames(type).Contains("CheckItemExcludedFilter");
        }

        [PatchPostfix]
        public static void PatchPostfix(GameObject ____placeHolder, object __instance, ref IEnumerable<Item> __result)
        {
            var itemAddress = _itemAddressField.GetValue(__instance) as ItemAddress;

            object inventoryControllerClass = _inventoryControllerClassField.GetValue(__instance);
            var inventory = _inventoryProperty.GetValue(inventoryControllerClass) as Inventory;
            foreach (var area in new EAreaType[] { EAreaType.WeaponStand, EAreaType.WeaponStandSecondary })
            {
                var areaStash = inventory.HideoutAreaStashes[area];
                var areaItems = _getNotMergedItemsMethod.Invoke(null, new object[] { areaStash }) as IEnumerable<Item>;
                __result = __result.Concat(areaItems);
            }

            __result = __result
                .Where(item => itemAddress.Item != item)
                .Where(item => {
                    return (bool)_canAcceptMethod.Invoke(null, new object[] { itemAddress.Container, item });
                })
                .Where(item => {
                    Weapon weapon;
                    return (weapon = item as Weapon) == null || !weapon.MissingVitalParts.Any<Slot>();
                })
                .OrderByDescending(item => item.TemplateId);

            ____placeHolder.SetActive(!__result.Any<Item>());
        }
    }
}
