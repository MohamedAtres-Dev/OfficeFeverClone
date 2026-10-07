using UnityEngine;

/// <summary>
/// Purely visual copier next to a paper source. While the source is producing, the rollers and the side gear turn,
/// the status light pulses green, the feed paper dips and every produced paper slides out of the output slot.
/// When the source is full the machine idles: rollers stop and the light turns amber.
/// Everything is driven from Update with plain maths (no tweens, no allocations). Only the moving parts are
/// dynamic; the body is static so it batches with the rest of the office (dynamic batching is off on mobile).
/// </summary>
public class PaperMachine : MonoBehaviour
{
    [SerializeField] private PaperCollectZone source;

    [Header("Parts")]
    [Tooltip("Paper stack on top of the machine: dips a little each time a sheet is pulled in.")]
    [SerializeField] private Transform feedPaper;
    [Tooltip("Rotate around their local Y axis (cylinders laid along X).")]
    [SerializeField] private Transform[] rollers;
    [Tooltip("Rotates around its local Y axis (a disc facing the camera).")]
    [SerializeField] private Transform gear;
    [SerializeField] private Renderer statusLight;
    [SerializeField] private Transform outputSheet;
    [SerializeField] private Vector3 sheetStart;   // local positions, relative to this transform
    [SerializeField] private Vector3 sheetEnd;

    [Header("Tuning")]
    [SerializeField] private float rollerSpeed = 540f;
    [SerializeField] private float sheetTime = 0.18f;
    [SerializeField] private Color runningColor = new Color(0.25f, 0.95f, 0.45f, 1f);
    [SerializeField] private Color fullColor = new Color(1f, 0.68f, 0.15f, 1f);

    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock block;
    private Vector3 feedBaseScale, sheetBaseScale;
    private float run;            // 0 idle .. 1 producing, eased
    private float sheetT = 1f;    // 0..1 progress of the sheet sliding out; 1 = hidden
    private float flash;          // decaying light flash per produced paper
    private float dip;            // decaying feed-paper dip per produced paper
    private float phase;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        phase = Random.value * 10f;
        if (feedPaper != null) feedBaseScale = feedPaper.localScale;
        if (outputSheet != null) { sheetBaseScale = outputSheet.localScale; outputSheet.gameObject.SetActive(false); }
    }

    private void OnEnable()
    {
        if (source != null) source.PaperSpawned += OnPaperSpawned;
    }

    private void OnDisable()
    {
        if (source != null) source.PaperSpawned -= OnPaperSpawned;
        if (feedPaper != null) feedPaper.localScale = feedBaseScale;
        if (outputSheet != null) outputSheet.gameObject.SetActive(false);
        sheetT = 1f;
    }

    private void OnPaperSpawned()
    {
        flash = 1f;
        dip = 1f;
        sheetT = 0f;
        if (outputSheet != null) outputSheet.gameObject.SetActive(true);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        bool producing = source != null && source.isActiveAndEnabled && !source.IsFull;
        run = Mathf.MoveTowards(run, producing ? 1f : 0f, dt * 3f);
        flash = Mathf.MoveTowards(flash, 0f, dt * 6f);
        dip = Mathf.MoveTowards(dip, 0f, dt * 8f);
        float t = Time.time + phase;

        float spin = rollerSpeed * run * dt;
        for (int i = 0; rollers != null && i < rollers.Length; i++)
            if (rollers[i] != null) rollers[i].Rotate(0f, (i % 2 == 0 ? spin : -spin), 0f, Space.Self);
        if (gear != null) gear.Rotate(0f, spin * 0.35f, 0f, Space.Self);

        if (feedPaper != null)
            feedPaper.localScale = new Vector3(feedBaseScale.x, feedBaseScale.y * (1f - dip * 0.4f), feedBaseScale.z);

        UpdateSheet(dt);
        UpdateLight(t);
    }

    private void UpdateSheet(float dt)
    {
        if (outputSheet == null || sheetT >= 1f) return;

        sheetT = Mathf.Min(1f, sheetT + dt / Mathf.Max(0.01f, sheetTime));
        float p = 1f - (1f - sheetT) * (1f - sheetT);   // ease out
        outputSheet.localPosition = Vector3.LerpUnclamped(sheetStart, sheetEnd, p);
        float shrink = sheetT < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (sheetT - 0.7f) / 0.3f);
        outputSheet.localScale = sheetBaseScale * shrink;
        if (sheetT >= 1f) outputSheet.gameObject.SetActive(false);
    }

    private void UpdateLight(float t)
    {
        if (statusLight == null) return;

        Color baseColor = Color.Lerp(fullColor, runningColor, run);
        float pulse = Mathf.Lerp(0.55f, 0.75f + 0.25f * Mathf.Sin(t * 5f), run);
        Color emission = Color.Lerp(baseColor * pulse, Color.white, flash * 0.6f);

        statusLight.GetPropertyBlock(block);
        block.SetColor(ColorId, baseColor);
        block.SetColor(EmissionId, emission);
        statusLight.SetPropertyBlock(block);
    }
}
