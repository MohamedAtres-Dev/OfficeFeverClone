using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class HUDManager : Singlton<HUDManager>
{
    public TextMeshProUGUI coinsTxt;

    private float animateCoinsTime = 0.4f;
    private int animatedCoinsValue = 0;

    public GameObject joyStickObject;

    public Transform pausePanel;
    public GameObject pauseObject;

    public bool GetJoysticState()
    {
        return PlayerPrefs.GetInt("Joystick", 1) == 0 ? false : true;
    }

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
        ActivateJoystic(GetJoysticState());
    }


    public void ActivateJoystic(bool state)
    {
        joyStickObject.SetActive(state);

        PlayerPrefs.SetInt("Joystick", state == true ? 1 : 0);
    }


    public void ActivatePausePanel(bool state)
    {
        if (state)
        {
            // Show the panel by setting the scale to Vector3.one
            pauseObject.gameObject.SetActive(true);
            pausePanel.transform.localScale = Vector3.zero;
            pausePanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }
        else
        {
            // Hide the panel by setting the scale to Vector3.zero
            pausePanel.transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack).OnComplete(() => pauseObject.gameObject.SetActive(false));
        }
    }

}
