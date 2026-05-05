using System.Collections.Generic;
using UnityEngine;

public class PlayerActiveAbilitySlots : MonoBehaviour
{
    [Header("Current Active Slots")]
    [SerializeField] MonoBehaviour activeSlot1;
    [SerializeField] MonoBehaviour activeSlot2;

    [Header("Spawned Ability Holder")]
    [SerializeField] Transform abilityHolder;

    [Header("Owned Active Prefabs")]
    [SerializeField] List<GameObject> ownedActivePrefabs = new List<GameObject>();

    IPlayerActiveAbility slot1;
    IPlayerActiveAbility slot2;

    public bool HasFreeSlot => slot1 == null || slot2 == null;

    void Awake()
    {
        if (abilityHolder == null)
            abilityHolder = transform;

        RefreshSlots();
    }

    void Update()
    {
        if (PlayerInputRouter.GetActive1Down())
            UseSlot1();

        if (PlayerInputRouter.GetActive2Down())
            UseSlot2();
    }

    void RefreshSlots()
    {
        slot1 = GetAbilityFromBehaviour(activeSlot1);
        slot2 = GetAbilityFromBehaviour(activeSlot2);
    }

    public bool HasActiveAbility(GameObject abilityPrefab)
    {
        if (abilityPrefab == null)
            return false;

        return ownedActivePrefabs.Contains(abilityPrefab);
    }

    public bool CanAddActiveAbility(GameObject abilityPrefab)
    {
        if (abilityPrefab == null)
            return false;

        if (!HasFreeSlot)
            return false;

        if (HasActiveAbility(abilityPrefab))
            return false;

        return true;
    }

    public bool TryAddActiveAbility(GameObject abilityPrefab)
    {
        if (!CanAddActiveAbility(abilityPrefab))
            return false;

        GameObject spawned = Instantiate(abilityPrefab, abilityHolder);
        IPlayerActiveAbility ability = spawned.GetComponent<IPlayerActiveAbility>();

        if (ability == null)
        {
            Destroy(spawned);
            return false;
        }

        MonoBehaviour behaviour = ability as MonoBehaviour;

        if (behaviour == null)
        {
            Destroy(spawned);
            return false;
        }

        bool added = TryAddActiveAbility(behaviour);

        if (!added)
        {
            Destroy(spawned);
            return false;
        }

        ownedActivePrefabs.Add(abilityPrefab);
        return true;
    }

    public bool TryAddActiveAbility(MonoBehaviour abilityBehaviour)
    {
        IPlayerActiveAbility ability = GetAbilityFromBehaviour(abilityBehaviour);

        if (ability == null)
            return false;

        if (slot1 == null)
        {
            activeSlot1 = abilityBehaviour;
            slot1 = ability;
            slot1.OnEquip(gameObject);
            return true;
        }

        if (slot2 == null)
        {
            activeSlot2 = abilityBehaviour;
            slot2 = ability;
            slot2.OnEquip(gameObject);
            return true;
        }

        return false;
    }

    public bool TrySetSlot(int slotNumber, MonoBehaviour abilityBehaviour)
    {
        IPlayerActiveAbility ability = GetAbilityFromBehaviour(abilityBehaviour);

        if (ability == null)
            return false;

        if (slotNumber == 1)
        {
            if (slot1 != null)
                slot1.OnUnequip(gameObject);

            activeSlot1 = abilityBehaviour;
            slot1 = ability;
            slot1.OnEquip(gameObject);
            return true;
        }

        if (slotNumber == 2)
        {
            if (slot2 != null)
                slot2.OnUnequip(gameObject);

            activeSlot2 = abilityBehaviour;
            slot2 = ability;
            slot2.OnEquip(gameObject);
            return true;
        }

        return false;
    }

    public void UseSlot1()
    {
        if (slot1 == null)
            return;

        slot1.Use(gameObject);
    }

    public void UseSlot2()
    {
        if (slot2 == null)
            return;

        slot2.Use(gameObject);
    }

    public void ClearSlot1()
    {
        if (slot1 != null)
            slot1.OnUnequip(gameObject);

        activeSlot1 = null;
        slot1 = null;
    }

    public void ClearSlot2()
    {
        if (slot2 != null)
            slot2.OnUnequip(gameObject);

        activeSlot2 = null;
        slot2 = null;
    }

    IPlayerActiveAbility GetAbilityFromBehaviour(MonoBehaviour behaviour)
    {
        if (behaviour == null)
            return null;

        return behaviour as IPlayerActiveAbility;
    }
}