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
        Debug.Log("Value " + value);
        coinsTxt.transform.DOScale(1.2f, 0.2f).SetLoops(2, LoopType.Yoyo);
        int currentValue = ParseFormattedNumber(coinsTxt.text);

        if (value >= currentValue)
        {
            DOTween.To(() => animatedCoinsValue, x =>
            {
                animatedCoinsValue = x;
                coinsTxt.text = FormatNumber(x);
            }, value, animateCoinsTime).SetEase(Ease.InBounce);
        }
        else
        {
            animatedCoinsValue = currentValue;
            DOTween.To(() => animatedCoinsValue, x =>
            {
                animatedCoinsValue = x;
                coinsTxt.text = FormatNumber(x);
            }, value, animateCoinsTime).SetEase(Ease.InBounce);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        coinsTxt.text = FormatNumber(CurrencyManager.Instance.GetCoins()).ToString();
    }


    public static string FormatNumber(int number)
    {
        if (number >= 1000000000)
        {
            return (number / 1000000000f).ToString("0.#") + "T";
        }
        else if (number >= 1000000)
        {
            return (number / 1000000f).ToString("0.#") + "M";
        }
        else if (number >= 1000)
        {
            return (number / 1000f).ToString("0.#") + "K";
        }
        else
        {
            return number.ToString();
        }
    }


    public static int ParseFormattedNumber(string formattedNumber)
    {
        int multiplier = 1;
        if (formattedNumber.EndsWith("K"))
        {
            multiplier = 1000;
            formattedNumber = formattedNumber.Substring(0, formattedNumber.Length - 1);
        }
        else if (formattedNumber.EndsWith("M"))
        {
            multiplier = 1000000;
            formattedNumber = formattedNumber.Substring(0, formattedNumber.Length - 1);
        }
        else if (formattedNumber.EndsWith("T"))
        {
            multiplier = 1000000000;
            formattedNumber = formattedNumber.Substring(0, formattedNumber.Length - 1);
        }

        float result;
        if (float.TryParse(formattedNumber, out result))
        {
            result *= multiplier;
            return Mathf.RoundToInt(result);
        }
        else
        {
            throw new ArgumentException("Invalid formatted number string");
        }
    }


}
