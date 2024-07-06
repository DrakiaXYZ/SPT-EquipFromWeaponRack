using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace DrakiaXYZ.EquipFromWeaponRack.Patches
{
    // NOTE: Since this patch and EquipItemWindowListPath both patch the same class, I'm being lazy
    //       here and inheriting. This allows me to not have to re-fetch a bunch of references
    internal class TagWeaponRackPatch : EquipItemWindowListPatch
    {
        protected static FieldInfo _itemViewListField;
        protected static MethodInfo _isChildOfMethod;

        protected override MethodBase GetTargetMethod()
        {
            // Call the parent GetTargetMethod to setup the static fields, but discard the result since we don't need it
            base.GetTargetMethod();

            _itemViewListField = AccessTools.GetDeclaredFields(_targetClass).Single(field => typeof(IEnumerable<KeyValuePair<Item, ItemView>>).IsAssignableFrom(field.FieldType));

            Type isChildOfClass = PatchConstants.EftTypes.Single(x => x.GetMethod("ParentRecursiveCheck") != null);
            _isChildOfMethod = AccessTools.Method(isChildOfClass, "IsChildOf", new Type[] { typeof(Item), typeof(Item) });

            // Find the third method_* method that takes no parameters, and returns void
            return AccessTools.GetDeclaredMethods(_targetClass).Where(x => 
                x.GetParameters().Length == 0 && 
                x.ReturnType == typeof(void) &&
                x.Name.StartsWith("method_")
            ).ElementAt(2);
        }

        [PatchPostfix]
        public static void PatchPostfix(object __instance)
        {
            object inventoryControllerClass = _inventoryControllerClassField.GetValue(__instance);
            var inventory = _inventoryProperty.GetValue(inventoryControllerClass) as Inventory;

            var items = _itemViewListField.GetValue(__instance) as IEnumerable<KeyValuePair<Item, ItemView>>;
            foreach (KeyValuePair<Item, ItemView> keyValuePair in items)
            {
                Item item = keyValuePair.Key;
                ItemView itemView = keyValuePair.Value;
                if (item == null || itemView == null) continue;

                var tagPanel = itemView.transform.Find("TagPanel");
                if (tagPanel == null) continue;

                var tagName = tagPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (tagName == null) continue;

                bool inRack = false;
                foreach (var area in new EAreaType[] { EAreaType.WeaponStand, EAreaType.WeaponStandSecondary })
                {
                    if (!inventory.HideoutAreaStashes.ContainsKey(area)) continue;
                    var areaStash = inventory.HideoutAreaStashes[area];

                    if ((bool)_isChildOfMethod.Invoke(null, new object[] { item, areaStash }))
                    {
                        inRack = true;
                        break;
                    }
                }

                tagName.text = inRack ? "Rack" : "Stash";

                tagPanel.RectTransform().sizeDelta = new Vector2(60, 14);
                tagPanel.gameObject.SetActive(true);
            }
        }
    }
}
