﻿using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
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
        protected static Type _targetClass;
        protected static Type _inventoryControllerType;

        protected static FieldInfo _inventoryControllerClassField;
        protected static FieldInfo _itemAddressField;

        protected static PropertyInfo _inventoryProperty;

        protected static MethodInfo _getNotMergedItemsMethod;
        protected static MethodInfo _canAcceptMethod;

        protected override MethodBase GetTargetMethod()
        {
            _targetClass = PatchConstants.EftTypes.Single(IsTargetClass);

            _inventoryControllerClassField = AccessTools.GetDeclaredFields(_targetClass).Single(x => x.FieldType.Name == nameof(InventoryController));
            _inventoryControllerType = _inventoryControllerClassField.FieldType;
            _inventoryProperty = AccessTools.Property(_inventoryControllerType, "Inventory");
            _itemAddressField = AccessTools.GetDeclaredFields(_targetClass).Single(x => typeof(ItemAddress).IsAssignableFrom(x.FieldType));

            _getNotMergedItemsMethod = AccessTools.Method(PatchConstants.EftTypes.Single(x => AccessTools.GetMethodNames(x).Contains("GetNotMergedItems")), "GetNotMergedItems");

            Type canAcceptClass = PatchConstants.EftTypes.Single(IsCanAcceptClass);
            _canAcceptMethod = AccessTools.Method(canAcceptClass, "CanAccept");

            return AccessTools.GetDeclaredMethods(_targetClass).Single(x => x.ReturnType.Equals(typeof(IEnumerable<Item>)));
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
                if (!inventory.HideoutAreaStashes.ContainsKey(area)) continue;
                var areaStash = inventory.HideoutAreaStashes[area];
                var areaItems = _getNotMergedItemsMethod.Invoke(null, new object[] { areaStash }) as IEnumerable<Item>;
                __result = __result.Concat(areaItems);
            }

            __result = __result
                .Where(item => itemAddress != item.CurrentAddress)
                .Where(item => itemAddress.Container.CanAccept(item))
                .Where(item => {
                    Weapon weapon;
                    return (weapon = item as Weapon) == null || !weapon.MissingVitalParts.Any<Slot>();
                })
                .OrderByDescending(item => item.TemplateId);

            ____placeHolder.SetActive(!__result.Any<Item>());
        }
    }
}
