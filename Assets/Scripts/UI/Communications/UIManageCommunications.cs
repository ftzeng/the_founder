using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class UIManageCommunications : UIFullScreenPager {
    public GameObject promoPrefab;

    void OnEnable() {
        LoadPromos();
    }

    private void LoadPromos() {
        ClearGrid();
        foreach (Promo p in Promo.LoadAll().OrderBy(p => p.cost)) {
            GameObject promoItem = NGUITools.AddChild(grid.gameObject, promoPrefab);
            promoItem.GetComponent<UIPromo>().promo = p;
        }
        Adjust();
    }

    public GameObject historyPrefab;
    public void OpenHistory() {
        UIManager.Instance.CloseAndOpenPopup(historyPrefab);
    }
}
