using UnityEngine;
using System.Collections;

public class Chest : Inventory
{
    #region 单例模式
    private static Chest _instance;
    public static Chest Instance
    {
        get
        {
            return _instance;
        }
    }
    #endregion

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }
}
