using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragAndDropHandler : MonoBehaviour
{
    [SerializeField] private UIItemSlot cursorSlot = null;
    private ItemSlot cursorItemSlot;

    [SerializeField] private GraphicRaycaster m_Raycaster = null;
    private PointerEventData m_PointerEventData;

    [SerializeField] private EventSystem m_EventSystem = null;

    World world;

    private void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        cursorItemSlot = new ItemSlot(cursorSlot);
    }

    private void Update()
    {
        // Dont do anything unless we are actually in the UI
        if (!world.inUI)
            return;

        cursorSlot.transform.position = Input.mousePosition;


        // Left Click
        if (Input.GetMouseButtonDown(0))
        {
            if (CheckForSlot() != null)
            {
                HandleSlotClick(CheckForSlot());
            }
        }
    }

    private void HandleSlotClick(UIItemSlot clickedSlot)
    {
        //If we are not clicking on a slot
        if (clickedSlot == null)
        {
            return;
        }

        //If the slot has nothing in it, and we are not holding anything
        if (!cursorSlot.HasItem && !clickedSlot.HasItem)
        {
            return;
        }

        if(clickedSlot.itemSlot.isCreative)
        {
            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.stack);
            return;
        }

        //If the slot has nothing in it, but we are holding something
        if (!cursorSlot.HasItem && clickedSlot.HasItem)
        {
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
            return;
        }

        //If the slot has something in it, and we are not holding anything
        if (cursorSlot.HasItem && !clickedSlot.HasItem)
        {
            clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());
            return;
        }

        //If the slot has something in it, and we are holding something
        if (cursorSlot.HasItem && clickedSlot.HasItem)
        {
            //If they are 2 different block/items, swap them
            if (cursorSlot.itemSlot.stack.id != clickedSlot.itemSlot.stack.id)
            {
                ItemStack oldCursorSlot = cursorSlot.itemSlot.TakeAll();
                ItemStack oldSlot = clickedSlot.itemSlot.TakeAll();

                clickedSlot.itemSlot.InsertStack(oldCursorSlot);
                cursorSlot.itemSlot.InsertStack(oldSlot);
            }

            /// TODO: Same blocks (i.e 32 sand blocks + 12 sand block = 40 sandblocks but only 1 stack
            /// Need to account for stack size (TM uses 100 blocks per stack)
        }
    }

    private UIItemSlot CheckForSlot()
    {
        m_PointerEventData = new PointerEventData(m_EventSystem);
        m_PointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();

        m_Raycaster.Raycast(m_PointerEventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.tag == "UIItemSlot")
            {
                return result.gameObject.GetComponent<UIItemSlot>();
            }
        }

        return null;
    }
}
