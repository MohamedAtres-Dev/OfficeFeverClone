using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>Small HUD chip showing carried papers / capacity. Turns orange and reads MAX when the stack is full.</summary>
public class CapacityChip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Transform root;
    [SerializeField] private Color normalColor = new Color(0.17f, 0.23f, 0.33f, 1f);
    [SerializeField] private Color fullColor = new Color(0.93f, 0.45f, 0.10f, 1f);

    private int lastCount = -1, lastMax = -1;

    private void OnEnable()
    {
        PlayerManager.onPaperCountChanged += OnChanged;
        var pm = FindFirstObjectByType<PlayerManager>();
        if (pm != null) OnChanged(pm.playerData.currentPaperStackCount, pm.MaxPaperStack);
    }

    private void OnDisable()
    {
        PlayerManager.onPaperCountChanged -= OnChanged;
        root.DOKill();
        root.localScale = Vector3.one;
    }

    private void OnChanged(int count, int max)
    {
        if (count == lastCount && max == lastMax) return;
        bool becameFull = count >= max && max > 0 && !(lastCount >= lastMax && lastMax > 0 && max == lastMax);
        bool capacityGrew = lastMax >= 0 && max > lastMax;
        lastCount = count; lastMax = max;

        bool full = count >= max && max > 0;
        label.text = full ? "MAX" : count + "/" + max;
        label.color = full ? fullColor : normalColor;

        if (becameFull || capacityGrew)
        {
            root.DOKill(true);
            root.DOPunchScale(Vector3.one * 0.16f, 0.25f, 6, 0.6f).SetTarget(root);
        }
    }
}
