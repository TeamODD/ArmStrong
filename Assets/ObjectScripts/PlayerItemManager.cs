using UnityEngine;

public class PlayerItemManager : MonoBehaviour
{
    private bool hasCardKey;
    private bool hasSaw;

    public bool HasCardKey => hasCardKey;
    public bool HasSaw => hasSaw;


    public void GetCardKey()
    {
        hasCardKey = true;

        Debug.Log("ƒ´µÂ≈∞∏¶ »πµÊ«ﬂΩ¿¥œ¥Ÿ.");
    }


    public void GetSaw()
    {
        hasSaw = true;

        Debug.Log("≈È¿ª »πµÊ«ﬂΩ¿¥œ¥Ÿ.");
    }
}
