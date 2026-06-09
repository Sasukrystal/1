using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Bagsys.RogueLike;

public class Player : MonoBehaviour
{

    #region basic property
    private int basicStrength = 10;
    private int basicIntellect = 10;
    private int basicAgility = 10;
    private int basicStamina = 10;
    private int basicDamage = 10;

    public int BasicStrength
    {
        get
        {
            return basicStrength;
        }
    }
    public int BasicIntellect
    {
        get
        {
            return basicIntellect;
        }
    }
    public int BasicAgility
    {
        get
        {
            return basicAgility;
        }
    }
    public int BasicStamina
    {
        get
        {
            return basicStamina;
        }
    }
    public int BasicDamage
    {
        get
        {
            return basicDamage;
        }
    }
    #endregion

    private int coinAmount = 100;

    private Text coinText;

    public int CoinAmount
    {
        get
        {
            return coinAmount;
        }
        set
        {
            coinAmount = value;
            if (coinText != null)
            {
                coinText.text = coinAmount.ToString();
            }
        }
    }

    void Start()
    {
        EnsureCharacterPanelController();

        GameObject coinObject = GameObject.Find("Coin");
        if (coinObject != null)
        {
            coinText = coinObject.GetComponentInChildren<Text>();
        }

        if (coinText != null)
        {
            coinText.text = coinAmount.ToString();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (ModernRogue.UIManager.Instance != null)
        {
            return;
        }

        //G 随机得到一个物品放到背包里面
        if (Input.GetKeyDown(KeyCode.G))
        {
            int id = Random.Range(1, 19);
            Knapsack.Instance.StoreItem(id);
        }

        //B 控制背包的显示和隐藏
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ToggleKnapsackPanel();
            }
        }

        //E 控制角色属性面板的显示和隐藏
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ToggleCharacterPanel();
            }
        }

        //T/Y/U/O 保留为旧快捷键，避免破坏现有习惯
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (Knapsack.Instance != null)
            {
                Knapsack.Instance.DisplaySwitch();
            }
        }
        //Y 控制箱子的显示和隐藏
        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (Chest.Instance != null)
            {
                Chest.Instance.DisplaySwitch();
            }
        }
        //U 控制角色面板的 显示和隐藏
        if (Input.GetKeyDown(KeyCode.U))
        {
            if (CharacterPanel.Instance != null)
            {
                CharacterPanel.Instance.DisplaySwitch();
            }
        }
        //I 控制商店显示和隐藏 
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (Vendor.Instance != null)
            {
                Vendor.Instance.DisplaySwitch();
            }
        }
        //O 控制锻造界面显示和隐藏 
        if (Input.GetKeyDown(KeyCode.O))
        {
           if (Forge.Instance != null)
           {
               Forge.Instance.DisplaySwitch();
           }
        }



    }

    /// <summary>
    /// 消费
    /// </summary>
    public bool ConsumeCoin(int amount)
    {
        if (coinAmount >= amount)
        {
            coinAmount -= amount;
            if (coinText != null)
            {
                coinText.text = coinAmount.ToString();
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 赚取金币
    /// </summary>
    /// <param name="amount"></param>
    public void EarnCoin(int amount)
    {
        this.coinAmount += amount;
        if (coinText != null)
        {
            coinText.text = coinAmount.ToString();
        }
    }

    private void EnsureCharacterPanelController()
    {
        if (Object.FindObjectOfType<CharacterPanelController>() != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("CharacterPanelController");
        controllerObject.AddComponent<CharacterPanelController>();
    }
}
