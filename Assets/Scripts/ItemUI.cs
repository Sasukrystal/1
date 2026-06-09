using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{

    #region Data
    public Item Item { get; private set; }
    public int Amount { get; private set; }
    #endregion

    #region UI Component
    private Image itemImage;
    private Text amountText;

    private Image ItemImage
    {
        get
        {
            if (itemImage == null)
            {
                itemImage = GetComponent<Image>();
            }
            return itemImage;
        }
    }
    private Text AmountText
    {
        get
        {
            if (amountText == null)
            {
                amountText = GetComponentInChildren<Text>();
            }
            return amountText;
        }
    }
    #endregion

    private float targetScale = 1f;

    private Vector3 animationScale = new Vector3(1.4f, 1.4f, 1.4f);

    private float smoothing = 4;

    void Update()
    {
        if (transform.localScale.x != targetScale)
        {
            //动画
            float scale = Mathf.Lerp(transform.localScale.x, targetScale,smoothing*Time.deltaTime);
            transform.localScale = new Vector3(scale, scale, scale);
            if (Mathf.Abs(transform.localScale.x - targetScale) < .02f)
            {
                transform.localScale = new Vector3(targetScale, targetScale, targetScale);
            }
        }
    }

    public void SetItem(Item item,int amount = 1)
    {
        transform.localScale = animationScale;
        this.Item = item;
        this.Amount = amount;
        // update ui 
        ItemImage.sprite = ResolveSprite(item);
        if (Item.Capacity > 1)
            AmountText.text = Amount.ToString();
        else
            AmountText.text = "";
    }

    public void AddAmount(int amount=1)
    {
        transform.localScale = animationScale;
        this.Amount += amount;
        //update ui 
        if (Item.Capacity > 1)
            AmountText.text = Amount.ToString();
        else
            AmountText.text = "";
    }
    public void ReduceAmount(int amount = 1)
    {
        transform.localScale = animationScale;
        this.Amount -= amount;
        //update ui 
        if (Item.Capacity > 1)
            AmountText.text = Amount.ToString();
        else
            AmountText.text = "";
    }
    public void SetAmount(int amount)
    {
        transform.localScale = animationScale;
        this.Amount = amount;
        //update ui 
        if (Item.Capacity > 1)
            AmountText.text = Amount.ToString();
        else
            AmountText.text = "";
    }

    public void ClearItem()
    {
        this.Item = null;
        this.Amount = 0;
        if (itemImage == null)
        {
            itemImage = GetComponent<Image>();
        }

        if (amountText == null)
        {
            amountText = GetComponentInChildren<Text>();
        }

        if (itemImage != null)
        {
            itemImage.sprite = null;
        }

        if (amountText != null)
        {
            amountText.text = string.Empty;
        }

        transform.localScale = Vector3.one;
    }

    //当前物品 跟 另一个物品 交换显示
    public void Exchange(ItemUI itemUI)
    {
        Item itemTemp = itemUI.Item;
        int amountTemp = itemUI.Amount;
        itemUI.SetItem(this.Item, this.Amount);
        this.SetItem(itemTemp, amountTemp);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetLocalPosition(Vector3 position)
    {
        transform.localPosition = position;
    }

    private static Sprite ResolveSprite(Item item)
    {
        if (item == null)
        {
            return null;
        }

        Sprite sprite = Resources.Load<Sprite>(item.Sprite);
        if (sprite != null)
        {
            return sprite;
        }

        string fallbackPath = GetFallbackSpritePath(item);
        if (!string.IsNullOrEmpty(fallbackPath))
        {
            sprite = Resources.Load<Sprite>(fallbackPath);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return Resources.Load<Sprite>("Sprites/Items/bag");
    }

    private static string GetFallbackSpritePath(Item item)
    {
        if (item is Weapon weapon)
        {
            if (weapon.WpType == Weapon.WeaponType.Wand)
            {
                return "Sprites/Items/the_great_stick";
            }

            return "Sprites/Items/sword";
        }

        if (item is Equipment equipment)
        {
            switch (equipment.EquipType)
            {
                case Equipment.EquipmentType.Head:
                    return "Sprites/Items/helmets";
                case Equipment.EquipmentType.Chest:
                    return "Sprites/Items/armor";
                case Equipment.EquipmentType.Leg:
                    return "Sprites/Items/pants";
                case Equipment.EquipmentType.Boots:
                    return "Sprites/Items/boots";
                case Equipment.EquipmentType.Shoulder:
                    return "Sprites/Items/shoulders";
                case Equipment.EquipmentType.Belt:
                    return "Sprites/Items/belts";
                case Equipment.EquipmentType.OffHand:
                    return "Sprites/Items/shield";
            }

            return "Sprites/Items/armor";
        }

        switch (item.Type)
        {
            case Item.ItemType.Consumable:
                return "Sprites/Items/hp";
            case Item.ItemType.Material:
                return "Sprites/Items/ingots";
            default:
                return "Sprites/Items/bag";
        }
    }

}
