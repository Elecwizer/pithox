using UnityEngine;

public interface IPlayerActiveAbility
{
    void OnEquip(GameObject player);
    void OnUnequip(GameObject player);
    void Use(GameObject player);
}