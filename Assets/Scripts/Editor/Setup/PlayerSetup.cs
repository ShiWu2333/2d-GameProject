using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 玩家组件配置
/// 负责：PlayerController、AimPivot、WeaponSlotSystem、武器实例化
/// </summary>
public static class PlayerSetup
{
    public static (WeaponSlotSystem, Transform) Run(
        GameObject playerGO,
        Dictionary<string, GameObject> weaponPrefabs,
        int slot1Index, int slot2Index, int meleeIndex)
    {
        Undo.RecordObject(playerGO, "Setup Player");

        // PlayerController
        var pc = EditorHelper.GetOrAdd<PlayerController>(playerGO);

        // AimPivot
        Transform aimPivot = playerGO.transform.Find("AimPivot");
        if (aimPivot == null)
        {
            var ap = new GameObject("AimPivot");
            Undo.RegisterCreatedObjectUndo(ap, "Create AimPivot");
            ap.transform.SetParent(playerGO.transform, false);
            aimPivot = ap.transform;
        }
        if (pc.aimPivot == null)
        {
            Undo.RecordObject(pc, "Set AimPivot");
            pc.aimPivot = aimPivot;
        }

        // WeaponSlotSystem
        var slotSys = EditorHelper.GetOrAdd<WeaponSlotSystem>(playerGO);
        Undo.RecordObject(slotSys, "Setup WeaponSlotSystem");

        // 实例化武器
        slotSys.primary1 = InstantiateWeapon(weaponPrefabs, slot1Index, aimPivot, "Weapon_Slot1");
        slotSys.primary2 = InstantiateWeapon(weaponPrefabs, slot2Index, aimPivot, "Weapon_Slot2");
        slotSys.melee    = InstantiateWeapon(weaponPrefabs, meleeIndex, aimPivot, "Weapon_Melee");

        EditorUtility.SetDirty(playerGO);
        return (slotSys, aimPivot);
    }

    private static WeaponBase InstantiateWeapon(
        Dictionary<string, GameObject> map,
        int defIndex, Transform parent, string slotName)
    {
        if (defIndex < 0 || defIndex >= PrefabBuilder.WeaponDefs.Length) return null;
        var def = PrefabBuilder.WeaponDefs[defIndex];
        if (!map.TryGetValue(def.label, out var prefab) || prefab == null) return null;

        // 如果旧实例还在（清理器可能没覆盖到的情况），强制删除
        var existing = parent.Find(slotName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        inst.name = slotName;
        inst.SetActive(false);
        Undo.RegisterCreatedObjectUndo(inst, "Instantiate " + slotName);
        return inst.GetComponent<WeaponBase>();
    }
}
