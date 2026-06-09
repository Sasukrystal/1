using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using System.Runtime.Remoting.Channels;
using UnityEngine.UI;


public class InventoryManager : MonoBehaviour
{

    #region 单例模式
    public static InventoryManager Instance
    {
        get; private set;
    }
    #endregion
    
    /// <summary>
    ///  物品信息的列表（集合）
    /// </summary>
    private List<Item> itemList;

    #region ToolTip
    private ToolTip toolTip;

    private bool isToolTipShow = false;

    private Vector2 toolTipPosionOffset = new Vector2(10, -10);
    //private Vector2 toolTipPosionOffset = new Vector2(0, -20);
    #endregion

    private Canvas canvas;
    private GameObject backpackSummaryRoot;
    private Text backpackSummaryText;
    private int recentPickupItemId = -1;
    private int recentPickupAmount = 0;
    private float recentPickupTime = -999f;

    #region PickedItem
    private bool isPickedItem = false;

    public bool IsPickedItem
    {
        get
        {
            return isPickedItem;
        }
    }

    private ItemUI pickedItem;//鼠标选中的物体

    public ItemUI PickedItem
    {
        get
        {
            return pickedItem;
        }
    }
    #endregion

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ParseItemJson();
    }

    void Start()
    {
        toolTip = GameObject.FindObjectOfType<ToolTip>();
        canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        pickedItem = GameObject.Find("PickedItem").GetComponent<ItemUI>();
        pickedItem.Hide();

        EnsureBackpackSummary();

        HideCorePanelsOnStart();
    }

    private void OnEnable()
    {
        if (canvas == null)
        {
            Canvas foundCanvas = GameObject.FindObjectOfType<Canvas>();
            if (foundCanvas != null)
            {
                canvas = foundCanvas;
            }
        }

        EnsureBackpackSummary();
    }

    private void HideCorePanelsOnStart()
    {
        if (Knapsack.Instance != null)
        {
            Knapsack.Instance.gameObject.SetActive(false);
        }

        if (Chest.Instance != null)
        {
            Chest.Instance.gameObject.SetActive(false);
        }

        if (CharacterPanel.Instance != null)
        {
            CharacterPanel.Instance.gameObject.SetActive(false);
        }

        if (Vendor.Instance != null)
        {
            Vendor.Instance.gameObject.SetActive(false);
        }

        if (Forge.Instance != null)
        {
            Forge.Instance.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (backpackSummaryRoot == null)
        {
            EnsureBackpackSummary();
        }

        if (isPickedItem && pickedItem != null)
        {
            //如果我们捡起了物品，我们就要让物品跟随鼠标
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, Input.mousePosition, null, out position);
            pickedItem.SetLocalPosition(position);
        }else if (isToolTipShow)
        {
            //控制提示面板跟随鼠标
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, Input.mousePosition, null, out position);
            toolTip.SetLocalPotion(position+toolTipPosionOffset);
        }

        RefreshBackpackSummary();

        //物品丢弃的处理
        if (isPickedItem && Input.GetMouseButtonDown(0) && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(-1)==false)
        {
            DropPickedItem();
        }
    }

    /// <summary>
    /// 解析物品信息
    /// </summary>
    void ParseItemJson()
    {
        itemList = new List<Item>();
        //文本为在Unity里面是 TextAsset类型
        TextAsset itemText = Resources.Load<TextAsset>("Items");
        string itemsJson = itemText.text;//物品信息的Json格式
        
        JSONObject j = new JSONObject(itemsJson);
        foreach (JSONObject temp in j.list)
        {
            string typeStr = temp["type"].str;
            Item.ItemType type= (Item.ItemType)System.Enum.Parse(typeof(Item.ItemType), typeStr);

            //下面的事解析这个对象里面的公有属性
            int id = (int)(temp["id"].n);
            string name = temp["name"].str;
            Item.ItemQuality quality = (Item.ItemQuality)System.Enum.Parse(typeof(Item.ItemQuality), temp["quality"].str);
            string description = temp["description"].str;
            int capacity = (int)(temp["capacity"].n);
            int buyPrice = (int)(temp["buyPrice"].n);
            int sellPrice = (int)(temp["sellPrice"].n);
            string sprite = temp["sprite"].str;

            Item item = null;

            switch (type)
            {
                case Item.ItemType.Consumable:
                    int hp = (int)(temp["hp"].n);
                    int mp = (int)(temp["mp"].n);
                    item = new Consumable(id, name, type, quality, description, capacity, buyPrice, sellPrice, sprite, hp, mp);
                    break;
                case Item.ItemType.Equipment:
                    //
                    int strength = (int)temp["strength"].n;
                    int intellect = (int)temp["intellect"].n;
                    int agility = (int)temp["agility"].n;
                    int stamina = (int)temp["stamina"].n;
                    Equipment.EquipmentType equipType = (Equipment.EquipmentType)System.Enum.Parse(typeof(Equipment.EquipmentType), temp["equipType"].str);
                    item = new Equipment(id, name, type, quality, description, capacity, buyPrice, sellPrice, sprite, strength, intellect, agility, stamina, equipType);
                    break;
                case Item.ItemType.Weapon:
                    //
                    int damage = (int)temp["damage"].n;
                    Weapon.WeaponType wpType = (Weapon.WeaponType)System.Enum.Parse(typeof(Weapon.WeaponType), temp["weaponType"].str);
                    item = new Weapon(id, name, type, quality, description, capacity, buyPrice, sellPrice, sprite, damage, wpType);
                    break;
                case Item.ItemType.Material:
                    //
                    item = new Material(id, name, type, quality, description, capacity, buyPrice, sellPrice, sprite);
                    break;
            }
            itemList.Add(item);
            //Debug.Log(item);
            
        }
    }

    public Item GetItemById(int id)
    {
        foreach (Item item in itemList)
        {
            if (item.ID == id)
            {
                return item;
            }
        }
        return null;
    }

    public void ShowToolTip(string content)
    {
        if (this.isPickedItem) return;
        isToolTipShow = true;
        toolTip.Show(content);
    }

    public void HideToolTip()
    {
        isToolTipShow = false;
        toolTip.Hide();
    }

    public void ToggleKnapsackPanel()
    {
        ModernRogue.UIManager modernUi = ModernRogue.UIManager.Instance != null
            ? ModernRogue.UIManager.Instance
            : Object.FindObjectOfType<ModernRogue.UIManager>(true);
        if (modernUi != null)
        {
            modernUi.OpenTab(0);
            return;
        }

        Bagsys.RogueLike.EnhancedBackpackPanel enhancedPanel = Bagsys.RogueLike.EnhancedBackpackPanel.Instance != null
            ? Bagsys.RogueLike.EnhancedBackpackPanel.Instance
            : Object.FindObjectOfType<Bagsys.RogueLike.EnhancedBackpackPanel>(true);
        if (enhancedPanel != null)
        {
            enhancedPanel.TogglePanel();
            return;
        }

        Knapsack knapsack = Knapsack.Instance != null ? Knapsack.Instance : Object.FindObjectOfType<Knapsack>(true);
        if (knapsack != null)
        {
            knapsack.DisplaySwitch();
            return;
        }

        RuntimeBackpackPanel runtimeBackpackPanel = RuntimeBackpackPanel.Instance != null ? RuntimeBackpackPanel.Instance : Object.FindObjectOfType<RuntimeBackpackPanel>(true);
        if (runtimeBackpackPanel != null)
        {
            runtimeBackpackPanel.TogglePanel();
        }
    }

    public void ToggleCharacterPanel()
    {
        ModernRogue.UIManager modernUi = ModernRogue.UIManager.Instance != null
            ? ModernRogue.UIManager.Instance
            : Object.FindObjectOfType<ModernRogue.UIManager>(true);
        if (modernUi != null)
        {
            modernUi.OpenTab(1);
            return;
        }

        Bagsys.RogueLike.CharacterProgressionPanel progressionPanel = Bagsys.RogueLike.CharacterProgressionPanel.Instance != null
            ? Bagsys.RogueLike.CharacterProgressionPanel.Instance
            : Object.FindObjectOfType<Bagsys.RogueLike.CharacterProgressionPanel>(true);
        if (progressionPanel != null)
        {
            progressionPanel.TogglePanel();
            return;
        }

        if (CharacterPanel.Instance != null)
        {
            CharacterPanel.Instance.DisplaySwitch();
            return;
        }

        Bagsys.RogueLike.CharacterPanelController controller = Object.FindObjectOfType<Bagsys.RogueLike.CharacterPanelController>(true);
        if (controller != null)
        {
            controller.TogglePanel();
        }
    }

    public int RecentPickupItemId => recentPickupItemId;
    public int RecentPickupAmount => recentPickupAmount;
    public float RecentPickupAge => Time.time - recentPickupTime;

    public void RecordRecentPickup(int itemId, int amount)
    {
        if (itemId <= 0 || amount <= 0)
        {
            return;
        }

        recentPickupItemId = itemId;
        recentPickupAmount = amount;
        recentPickupTime = Time.time;
    }

    //捡起物品槽指定数量的物品
    public void PickupItem(Item item,int amount)
    {
        if (item == null || amount <= 0)
        {
            return;
        }

        PickedItem.SetItem(item, amount);
        isPickedItem = true;

        PickedItem.Show();
        if (this.toolTip != null)
        {
            this.toolTip.Hide();
        }
        //如果我们捡起了物品，我们就要让物品跟随鼠标
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, Input.mousePosition, null, out position);
        pickedItem.SetLocalPosition(position);
    }

    /// <summary>
    /// 从手上拿掉一个物品放在物品槽里面
    /// </summary>
    public void RemoveItem(int amount=1)
    {
        PickedItem.ReduceAmount(amount);
        if (PickedItem.Amount <= 0)
        {
            DropPickedItem();
        }
    }

    public void DropPickedItem()
    {
        if (PickedItem != null)
        {
            PickedItem.ClearItem();
            PickedItem.Hide();
        }

        isPickedItem = false;
    }

    public void SaveInventory()
    {
        Knapsack.Instance.SaveInventory();
        Chest.Instance.SaveInventory();
        CharacterPanel.Instance.SaveInventory();
        //Vendor.Instance.SaveInventory();
        Forge.Instance.SaveInventory();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        Player player = playerObject != null ? playerObject.GetComponent<Player>() : null;
        if (player != null)
        {
            PlayerPrefs.SetInt("CoinAmount", player.CoinAmount);
        }
        PlayerPrefs.Save();
    }

    public void LoadInventory()
    {
        Knapsack.Instance.LoadInventory();
        Chest.Instance.LoadInventory();
        CharacterPanel.Instance.LoadInventory();
        //Vendor.Instance.LoadInventory();
        Forge.Instance.LoadInventory();
        if (PlayerPrefs.HasKey("CoinAmount"))
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                Player player = playerObject.GetComponent<Player>();
                if (player != null)
                {
                    player.CoinAmount = PlayerPrefs.GetInt("CoinAmount");
                }
            }
        }
    }

    private void EnsureBackpackSummary()
    {
        if (backpackSummaryRoot != null)
        {
            return;
        }

        if (canvas == null)
        {
            canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        }

        backpackSummaryRoot = new GameObject("BackpackSummary", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        backpackSummaryRoot.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = backpackSummaryRoot.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.sizeDelta = new Vector2(300f, 150f);
        rectTransform.anchoredPosition = new Vector2(-16f, -16f);

        Image background = backpackSummaryRoot.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject textObject = new GameObject("SummaryText", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(backpackSummaryRoot.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(14f, 12f);
        textRect.offsetMax = new Vector2(-14f, -12f);

        backpackSummaryText = textObject.GetComponent<Text>();
        backpackSummaryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        backpackSummaryText.fontSize = 16;
        backpackSummaryText.color = Color.white;
        backpackSummaryText.alignment = TextAnchor.UpperLeft;
        backpackSummaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
        backpackSummaryText.verticalOverflow = VerticalWrapMode.Overflow;
        backpackSummaryText.text = string.Empty;

        backpackSummaryRoot.SetActive(false);
    }

    private void RefreshBackpackSummary()
    {
        if (backpackSummaryText == null)
        {
            return;
        }

        Knapsack knapsack = Knapsack.Instance != null ? Knapsack.Instance : Object.FindObjectOfType<Knapsack>(true);
        if (knapsack == null)
        {
            backpackSummaryText.text = "背包状态\n未找到背包对象";
            return;
        }

        Slot[] slots = knapsack.GetComponentsInChildren<Slot>(true);
        int occupiedSlots = 0;
        int totalItemCount = 0;
        string firstItemName = "无";

        for (int i = 0; i < slots.Length; i++)
        {
            Slot slot = slots[i];
            if (slot == null || slot.transform.childCount <= 0)
            {
                continue;
            }

            occupiedSlots++;
            ItemUI itemUI = slot.transform.GetChild(0).GetComponent<ItemUI>();
            if (itemUI != null && itemUI.Item != null)
            {
                totalItemCount += Mathf.Max(1, itemUI.Amount);
                if (firstItemName == "无")
                {
                    firstItemName = itemUI.Item.Name;
                }
            }
        }

        string pickedItemText = isPickedItem && pickedItem != null && pickedItem.Item != null
            ? string.Format("{0} x{1}", pickedItem.Item.Name, pickedItem.Amount)
            : "无";

        backpackSummaryText.text = string.Format(
            "背包状态\n已占用：{0}/{1}\n物品总数：{2}\n首个物品：{3}\n当前拾取：{4}\n\nB 开关背包\nE 角色面板\nF 捡起世界物品",
            occupiedSlots,
            slots.Length,
            totalItemCount,
            firstItemName,
            pickedItemText);
    }

}
