using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class HUDManager : MonoBehaviour
{
    public TextMeshProUGUI coinsTxt;

    private float animateCoinsTime = 0.2f;

    private void OnEnable()
    {
        CurrencyManager.onUpdateCoins += UpdateCoinsUI;
    }

    private void OnDisable()
    {
        CurrencyManager.onUpdateCoins -= UpdateCoinsUI;
    }


    private void UpdateCoinsUI(int value )
    {
        coinsTxt.transform.DOScale(1.2f, 0.2f).SetLoops(2, LoopType.Yoyo);
        DOTween.To(() => int.Parse(coinsTxt.text), x => coinsTxt.text = x.ToString(), value, animateCoinsTime).SetEase(Ease.Linear);
    }

    // Start is called before the first frame update
    void Start()
    {
        coinsTxt.text = CurrencyManager.Instance.GetCoins().ToString();
    }

}
