using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// 物品槽
/// </summary>
public class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{

    [SerializeField]
    public GameObject itemPrefab;

    [SerializeField]
    private string itemPrefabResourcePath = "Item";

    private ItemUI CurrentItemUI
    {
        get
        {
            if (transform.childCount <= 0)
            {
                return null;
            }

            return transform.GetChild(0).GetComponent<ItemUI>();
        }
    }

    private bool HasItem
    {
        get { return CurrentItemUI != null; }
    }

    /// <summary>
    /// 把item放在自身下面
    /// 如果自身下面已经有item了，amount++
    /// 如果没有 根据itemPrefab去实例化一个item，放在下面
    /// </summary>
    /// <param name="item"></param>
    public void StoreItem(Item item)
    {
        StoreItem(item, 1);
    }

    public void StoreItem(Item item, int amount)
    {
        if (item == null || amount <= 0)
        {
            return;
        }

        ItemUI itemUI = CurrentItemUI;
        if (itemUI == null)
        {
            GameObject itemGameObject = CreateItemUIObject();
            if (itemGameObject == null)
            {
                return;
            }

            itemGameObject.transform.SetParent(this.transform, false);
            itemGameObject.transform.localScale = Vector3.one;
            itemGameObject.transform.localPosition = Vector3.zero;
            itemUI = itemGameObject.GetComponent<ItemUI>();
            itemUI.SetItem(item, amount);
            return;
        }

        if (itemUI.Item != null && itemUI.Item.ID == item.ID)
        {
            itemUI.AddAmount(amount);
        }
        else
        {
            itemUI.SetItem(item, amount);
        }
    }

    private GameObject CreateItemUIObject()
    {
        GameObject prefab = itemPrefab;
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>(itemPrefabResourcePath);
        }

        if (prefab == null)
        {
            return CreateRuntimeItemUIObject();
        }

        return Instantiate(prefab) as GameObject;
    }

    private static GameObject CreateRuntimeItemUIObject()
    {
        GameObject itemObject = new GameObject("Item", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(ItemUI));
        RectTransform rectTransform = itemObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        UnityEngine.UI.Image image = itemObject.GetComponent<UnityEngine.UI.Image>();
        image.raycastTarget = true;

        GameObject amountObject = new GameObject("Amount", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
        amountObject.transform.SetParent(itemObject.transform, false);
        RectTransform amountRect = amountObject.GetComponent<RectTransform>();
        amountRect.anchorMin = Vector2.zero;
        amountRect.anchorMax = Vector2.one;
        amountRect.offsetMin = new Vector2(2f, 2f);
        amountRect.offsetMax = new Vector2(-2f, -2f);

        UnityEngine.UI.Text amountText = amountObject.GetComponent<UnityEngine.UI.Text>();
        amountText.font = Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf");
        amountText.fontSize = 14;
        amountText.alignment = TextAnchor.LowerRight;
        amountText.color = Color.white;
        amountText.raycastTarget = false;
        return itemObject;
    }


    /// <summary>
    /// 得到当前物品槽存储的物品类型
    /// </summary>
    /// <returns></returns>
    public Item.ItemType GetItemType()
    {
        return CurrentItemUI.Item.Type;
    }

    /// <summary>
    /// 得到物品的id
    /// </summary>
    /// <returns></returns>
    public int GetItemId()
    {
        return CurrentItemUI.Item.ID;
    }

    public bool IsFilled()
    {
        ItemUI itemUI = CurrentItemUI;
        return itemUI != null && itemUI.Amount >= itemUI.Item.Capacity;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (HasItem)
        {
            InventoryManager.Instance.HideToolTip();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (HasItem)
        {
            string toolTipText = CurrentItemUI.Item.GetToolTipText();
            InventoryManager.Instance.ShowToolTip(toolTipText);
        }

    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick();
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left) return;

        ItemUI currentItem = CurrentItemUI;
        if (currentItem != null)
        {
            if (InventoryManager.Instance.IsPickedItem == false)
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    int amountPicked = (currentItem.Amount + 1) / 2;
                    InventoryManager.Instance.PickupItem(currentItem.Item, amountPicked);
                    int amountRemained = currentItem.Amount - amountPicked;
                    if (amountRemained <= 0)
                    {
                        Destroy(currentItem.gameObject);
                    }
                    else
                    {
                        currentItem.SetAmount(amountRemained);
                    }
                }
                else
                {
                    InventoryManager.Instance.PickupItem(currentItem.Item, currentItem.Amount);
                    Destroy(currentItem.gameObject);
                }
            }
            else
            {
                if (currentItem.Item.ID == InventoryManager.Instance.PickedItem.Item.ID)
                {
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        if (currentItem.Amount < currentItem.Item.Capacity)
                        {
                            currentItem.AddAmount();
                            InventoryManager.Instance.RemoveItem();
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (currentItem.Amount < currentItem.Item.Capacity)
                        {
                            int amountRemain = currentItem.Item.Capacity - currentItem.Amount;
                            if (amountRemain >= InventoryManager.Instance.PickedItem.Amount)
                            {
                                currentItem.SetAmount(currentItem.Amount + InventoryManager.Instance.PickedItem.Amount);
                                InventoryManager.Instance.RemoveItem(InventoryManager.Instance.PickedItem.Amount);
                            }
                            else
                            {
                                currentItem.SetAmount(currentItem.Amount + amountRemain);
                                InventoryManager.Instance.RemoveItem(amountRemain);
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                else
                {
                    Item item = currentItem.Item;
                    int amount = currentItem.Amount;
                    currentItem.SetItem(InventoryManager.Instance.PickedItem.Item, InventoryManager.Instance.PickedItem.Amount);
                    InventoryManager.Instance.PickedItem.SetItem(item, amount);
                }

            }
        }
        else
        {
            if (InventoryManager.Instance.IsPickedItem == true)
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    this.StoreItem(InventoryManager.Instance.PickedItem.Item);
                    InventoryManager.Instance.RemoveItem();
                }
                else
                {
                    for (int i = 0; i < InventoryManager.Instance.PickedItem.Amount; i++)
                    {
                        this.StoreItem(InventoryManager.Instance.PickedItem.Item);
                    }
                    InventoryManager.Instance.RemoveItem(InventoryManager.Instance.PickedItem.Amount);
                }
            }
            else
            {
                return;
            }

        }
    }

    private void HandleRightClick()
    {
        if (InventoryManager.Instance.IsPickedItem)
        {
            return;
        }

        ItemUI currentItemUI = CurrentItemUI;
        if (currentItemUI == null)
        {
            return;
        }

        if (currentItemUI.Item is Consumable consumable)
        {
            if (!UseConsumable(consumable))
            {
                return;
            }

            currentItemUI.ReduceAmount(1);
            if (currentItemUI.Amount <= 0)
            {
                Destroy(currentItemUI.gameObject);
                InventoryManager.Instance.HideToolTip();
            }

            return;
        }

        if (currentItemUI.Item is Equipment || currentItemUI.Item is Weapon)
        {
            currentItemUI.ReduceAmount(1);
            Item currentItem = currentItemUI.Item;
            if (currentItemUI.Amount <= 0)
            {
                Destroy(currentItemUI.gameObject);
                InventoryManager.Instance.HideToolTip();
            }
            CharacterPanel.Instance.PutOn(currentItem);
        }
    }

    private static bool UseConsumable(Consumable consumable)
    {
        if (consumable == null)
        {
            return false;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return false;
        }

        Bagsys.RogueLike.PlayerStats playerStats = playerObject.GetComponent<Bagsys.RogueLike.PlayerStats>();
        if (playerStats == null)
        {
            return false;
        }

        if (consumable.HP > 0)
        {
            playerStats.Heal(consumable.HP);
        }

        return consumable.HP > 0 || consumable.MP > 0;
    }
}
