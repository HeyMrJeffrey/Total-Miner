using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Only doing anything if player is looking at inventory/container/toolbet
public class UIItemSlot : MonoBehaviour
{
    public bool isLinked = false;
    public ItemSlot itemSlot;
    public Image slotImage;
    public Image slotIcon;
    public Text slotAmount; //TextMeshPro might be worth using?
    World world;

    private void Awake()
    {
        world = GameObject.Find("World")?.GetComponent<World>();
    }

    public bool HasItem
    {
        get
        {
            if (itemSlot == null)
            {
                return false;
            }
            else
            {
                return itemSlot.HasItem;
            }
        }
    }

    public void Link(ItemSlot _itemSlot)
    {
        itemSlot = _itemSlot;
        isLinked = true;
        itemSlot.LinkUISlot(this);
        UpdateSlot();
    }

    public void Unlink()
    {
        itemSlot.UnlinkUISlot();
        itemSlot = null;
        UpdateSlot();
    }

    public void UpdateSlot()
    {
        if(itemSlot != null && world != null && itemSlot.HasItem)
        {
            slotIcon.sprite = world.blockTypes[itemSlot.stack.id].icon;
            slotAmount.text = itemSlot.stack.amount.ToString();
            slotIcon.enabled = true;
            slotAmount.enabled = true;
        }
        else
        {
            Clear();
        }
    }

    public void Clear()
    {
        slotIcon.sprite = null;
        slotAmount.text = "";
        slotIcon.enabled = false;
        slotAmount.enabled = false;
    }

    private void OnDestroy()
    {
        if (isLinked)
        {
            itemSlot.UnlinkUISlot();
        }
    }

}

// Backend. ItemSlot stores all data needed for a slot. Wether or not a UI menu is open, this data is stored.
public class ItemSlot
{
    public ItemStack stack = null;
    private UIItemSlot uiItemSlot = null;
    public bool isCreative;

    public ItemSlot(UIItemSlot _uiItemSlot)
    {
        stack = null;
        uiItemSlot = _uiItemSlot;
        uiItemSlot.Link(this);
    }

    public ItemSlot(UIItemSlot _uiItemSlot, ItemStack _itemStack)
    {
        stack = _itemStack;
        uiItemSlot = _uiItemSlot;
        uiItemSlot.Link(this);
    }

    public void LinkUISlot (UIItemSlot uiSlot)
    {
        uiItemSlot = uiSlot;
    }

    public void UnlinkUISlot()
    {
        uiItemSlot = null;
    }

    public void EmptySlot()
    {
        stack = null;

        if(uiItemSlot != null)
        {
            uiItemSlot.UpdateSlot();
        }
    }

    public int Take(int amtToTake)
    {
        if (amtToTake > stack.amount)
        {
            int tempStackAmount = stack.amount;
            EmptySlot();
            return tempStackAmount;
        }
        else if (amtToTake < stack.amount)
        {
            stack.amount -= amtToTake;
            uiItemSlot.UpdateSlot();
            return amtToTake;
        }
        else
        {
            EmptySlot();
            return amtToTake;
        }
    }

    public ItemStack TakeAll()
    {
        ItemStack handOver = new ItemStack(stack.id, stack.amount);
        EmptySlot();
        return handOver;
    }

    public void InsertStack (ItemStack _stack)
    {
        stack = _stack;
        uiItemSlot.UpdateSlot();
    }

    public bool HasItem
    {
        get
        {
            if (stack == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}

