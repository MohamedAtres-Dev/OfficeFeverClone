using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class HUDManager : MonoBehaviour
{
    public TextMeshProUGUI coinsTxt;

    private float animateCoinsTime = 0.4f;
    private int animatedCoinsValue = 0;

    private void OnEnable()
    {
        CurrencyManager.onUpdateCoins += UpdateCoinsUI;
    }

    private void OnDisable()
    {
        CurrencyManager.onUpdateCoins -= UpdateCoinsUI;
    }


    private void UpdateCoinsUI(int value)
    {
        coinsTxt.transform.DOScale(1.2f, 0.2f).SetLoops(2, LoopType.Yoyo);
        int currentValue = MathHelper.ParseFormattedNumber(coinsTxt.text);

        if (value >= currentValue)
        {
            DOTween.To(() => animatedCoinsValue, x =>
            {
                animatedCoinsValue = x;
                coinsTxt.text = MathHelper.FormatNumber(x);
            }, value, animateCoinsTime).SetEase(Ease.InBounce);
        }
        else
        {
            animatedCoinsValue = currentValue;
            DOTween.To(() => animatedCoinsValue, x =>
            {
                animatedCoinsValue = x;
                coinsTxt.text = MathHelper.FormatNumber(x);
            }, value, animateCoinsTime).SetEase(Ease.InBounce);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        coinsTxt.text = MathHelper.FormatNumber(CurrencyManager.Instance.GetCoins()).ToString();
    }


   


}
