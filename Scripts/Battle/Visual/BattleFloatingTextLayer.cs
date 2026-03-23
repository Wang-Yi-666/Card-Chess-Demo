using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using CardChessDemo.Battle.Board;

namespace CardChessDemo.Battle.Visual;

public partial class BattleFloatingTextLayer : Node2D
{
    [Export] public Font? FloatingTextFont { get; set; } = GD.Load<Font>("res://Assets/Fonts/unifont_t-17.0.04.otf");
    [Export(PropertyHint.Range, "8,32,1")] public int FontSize { get; set; } = 16;
    [Export(PropertyHint.Range, "0.1,2.0,0.05")] public float LifetimeSeconds { get; set; } = 0.65f;
    [Export(PropertyHint.Range, "4,48,1")] public float RiseDistancePixels { get; set; } = 18.0f;
    [Export(PropertyHint.Range, "0,24,1")] public float HorizontalSpreadPixels { get; set; } = 11.0f;
    [Export(PropertyHint.Range, "0,18,1")] public float BurstLaneOffsetPixels { get; set; } = 8.0f;
    [Export(PropertyHint.Range, "0.0,0.25,0.01")] public float ImpactStaggerSeconds { get; set; } = 0.045f;
    [Export(PropertyHint.Range, "0,14,1")] public float RandomJitterPixels { get; set; } = 4.0f;
    [Export(PropertyHint.Range, "0.1,1.0,0.05")] public float MinScale { get; set; } = 0.72f;
    [Export(PropertyHint.Range, "0.1,1.0,0.05")] public float ScalePopDurationRatio { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0,12,1")] public int OutlineSize { get; set; } = 2;
    [Export] public Color HealthDamageColor { get; set; } = new(0.98f, 0.24f, 0.24f, 1.0f);
    [Export] public Color ShieldDamageColor { get; set; } = new(0.76f, 0.78f, 0.84f, 1.0f);
    [Export] public Color HealthHealColor { get; set; } = new(0.34f, 0.96f, 0.42f, 1.0f);
    [Export] public Color ShieldGainColor { get; set; } = new(0.42f, 0.84f, 1.0f, 1.0f);
    [Export] public Color OutlineColor { get; set; } = new(0.10f, 0.08f, 0.08f, 0.92f);

    private readonly List<FloatingTextEntry> _entries = new();
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Randomize();
    }

    public void ShowImpacts(string anchorKey, Vector2 localPosition, IReadOnlyList<CombatImpact> impacts)
    {
        if (impacts.Count == 0)
        {
            return;
        }

        int activeBurstCount = _entries.Count(entry => string.Equals(entry.AnchorKey, anchorKey, StringComparison.Ordinal));
        float laneOffset = activeBurstCount * BurstLaneOffsetPixels;
        int impactCount = impacts.Count;

        for (int index = 0; index < impactCount; index++)
        {
            CombatImpact impact = impacts[index];
            float horizontalOffset = impactCount <= 1
                ? 0.0f
                : (index - (impactCount - 1) * 0.5f) * HorizontalSpreadPixels;
            Vector2 jitter = new(
                _rng.RandfRange(-RandomJitterPixels, RandomJitterPixels),
                _rng.RandfRange(-RandomJitterPixels * 0.4f, RandomJitterPixels * 0.4f));

            _entries.Add(new FloatingTextEntry(
                anchorKey,
                localPosition + new Vector2(horizontalOffset, -laneOffset) + jitter,
                impact,
                GetNowSeconds() + index * ImpactStaggerSeconds));
        }

        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_entries.Count == 0)
        {
            return;
        }

        double nowSeconds = GetNowSeconds();
        _entries.RemoveAll(entry => nowSeconds - entry.StartTimeSeconds >= LifetimeSeconds);
        QueueRedraw();
    }

    public override void _Draw()
    {
        Font font = FloatingTextFont ?? ThemeDB.FallbackFont;
        if (font == null)
        {
            return;
        }

        double nowSeconds = GetNowSeconds();
        foreach (FloatingTextEntry entry in _entries)
        {
            float progress = Mathf.Clamp((float)((nowSeconds - entry.StartTimeSeconds) / Math.Max(LifetimeSeconds, 0.001f)), 0.0f, 1.0f);
            if (progress <= 0.0f)
            {
                continue;
            }

            float eased = EaseOutCubic(progress);
            Vector2 drawPosition = entry.BasePosition + new Vector2(0.0f, -RiseDistancePixels * eased);
            Color textColor = ResolveTextColor(entry.Impact.ImpactType);
            textColor.A *= 1.0f - progress;

            string text = entry.Impact.Amount.ToString();
            Vector2 stringSize = font.GetStringSize(text, HorizontalAlignment.Left, -1, FontSize);
            float scale = ResolveScale(progress);

            DrawSetTransform(drawPosition, 0.0f, new Vector2(scale, scale));
            Vector2 textPosition = new(-stringSize.X * 0.5f, 0.0f);
            DrawTextWithOutline(font, textPosition, text, textColor);
            DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
        }
    }

    private Color ResolveTextColor(CombatImpactType impactType)
    {
        return impactType switch
        {
            CombatImpactType.HealthDamage => HealthDamageColor,
            CombatImpactType.ShieldDamage => ShieldDamageColor,
            CombatImpactType.HealthHeal => HealthHealColor,
            CombatImpactType.ShieldGain => ShieldGainColor,
            _ => Colors.White,
        };
    }

    private static float EaseOutCubic(float t)
    {
        float inverse = 1.0f - t;
        return 1.0f - inverse * inverse * inverse;
    }

    private float ResolveScale(float progress)
    {
        float popDuration = Mathf.Clamp(ScalePopDurationRatio, 0.05f, 1.0f);
        if (progress < popDuration)
        {
            float popT = progress / popDuration;
            return Mathf.Lerp(MinScale, 1.16f, EaseOutBack(popT));
        }

        float settleT = (progress - popDuration) / Math.Max(1.0f - popDuration, 0.001f);
        return Mathf.Lerp(1.16f, 1.0f, EaseOutCubic(settleT));
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1.0f;
        float inverse = t - 1.0f;
        return 1.0f + c3 * inverse * inverse * inverse + c1 * inverse * inverse;
    }

    private void DrawTextWithOutline(Font font, Vector2 position, string text, Color textColor)
    {
        int outline = Math.Max(OutlineSize, 0);
        if (outline > 0)
        {
            DrawString(font, position + new Vector2(-outline, 0), text, HorizontalAlignment.Left, -1.0f, FontSize, OutlineColor);
            DrawString(font, position + new Vector2(outline, 0), text, HorizontalAlignment.Left, -1.0f, FontSize, OutlineColor);
            DrawString(font, position + new Vector2(0, -outline), text, HorizontalAlignment.Left, -1.0f, FontSize, OutlineColor);
            DrawString(font, position + new Vector2(0, outline), text, HorizontalAlignment.Left, -1.0f, FontSize, OutlineColor);
        }

        DrawString(font, position, text, HorizontalAlignment.Left, -1.0f, FontSize, textColor);
    }

    private static double GetNowSeconds()
    {
        return Time.GetTicksMsec() / 1000.0d;
    }

    private readonly record struct FloatingTextEntry(
        string AnchorKey,
        Vector2 BasePosition,
        CombatImpact Impact,
        double StartTimeSeconds);
}
