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

    private float animateCoinsTime = 0.18f;
    private Tween coinCountTween;
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
        coinCountTween?.Kill();
        if (coinsTxt != null) coinsTxt.transform.DOKill(true);
    }


    /// <summary>
    /// Runs the moment coins are credited (money coins credit on landing), so the number and the pop line up with
    /// the coin hitting the player. Earlier count/pop tweens are replaced, so rapid coins never stack tweens.
    /// </summary>
    private void UpdateCoinsUI(int value)
    {
        coinsTxt.transform.DOKill(true); // finish the previous pop so the scale is back at 1 before the next one
        coinsTxt.transform.DOScale(1.15f, 0.08f).SetLoops(2, LoopType.Yoyo).SetTarget(coinsTxt.transform);

        coinCountTween?.Kill();
        coinCountTween = DOTween.To(() => animatedCoinsValue, x =>
        {
            animatedCoinsValue = x;
            coinsTxt.text = MathHelper.FormatNumber(x);
        }, value, animateCoinsTime).SetEase(Ease.OutQuad).SetTarget(this);
    }


    // Start is called before the first frame update
    void Start()
    {
        animatedCoinsValue = CurrencyManager.Instance.GetCoins();
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
