using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One tooltip panel. It follows the pointer and stays inside the screen.
/// The lines come from ItemReadout, which reads the item data.
/// </summary>
public class ItemTooltip
{
    const float Width = 320f;
    const float Line = 22f;
    const float Pad = 12f;
    const float Margin = 8f;
    const float Gap = 18f;

    static readonly Color Panel = new Color(0.10f, 0.07f, 0.05f, 0.96f);
    static readonly Color Ink = new Color(0.97f, 0.93f, 0.84f, 1f);
    static readonly Color Warn = new Color(0.95f, 0.55f, 0.42f, 1f);

    readonly List<ReadoutLine> _lines = new List<ReadoutLine>();
    readonly List<TMP_Text> _labels = new List<TMP_Text>();
    RectTransform _root;
    Image _frame;

    public void Ensure(Transform parent)
    {
        if (_root != null)
            return;

        var go = new GameObject("Tooltip", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        _root = go.GetComponent<RectTransform>();
        _root.anchorMin = new Vector2(0.5f, 0.5f);
        _root.anchorMax = new Vector2(0.5f, 0.5f);
        _root.pivot = new Vector2(0f, 1f);
        _root.sizeDelta = new Vector2(Width, 48f);

        var image = go.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = Panel;
        _frame = image;
        _root.gameObject.SetActive(false);
    }

    public void Hide()
    {
        if (_root != null)
            _root.gameObject.SetActive(false);
    }

    public void Show(ItemDefinition item, ItemInstance stack, Vector2 screen)
    {
        if (_root == null)
            return;

        ItemReadout.Write(item, stack, _lines);
        if (_lines.Count == 0)
        {
            Hide();
            return;
        }

        Layout(screen, ItemRarityColors.For(item));
    }

    public void ShowEmpty(Vector2 screen)
    {
        if (_root == null)
            return;

        _lines.Clear();
        _lines.Add(new ReadoutLine { Text = UiGlyphs.Pick("Пустой слот", "Empty slot"), Emphasis = ItemReadout.Title });
        Layout(screen, Color.white);
    }

    void Layout(Vector2 screen, Color title)
    {
        _root.gameObject.SetActive(true);
        while (_labels.Count < _lines.Count)
            _labels.Add(MakeLine());

        float y = Pad;
        float width = Width - Pad * 2f;
        for (int i = 0; i < _labels.Count; i++)
        {
            bool show = i < _lines.Count;
            _labels[i].gameObject.SetActive(show);
            if (!show)
                continue;

            ReadoutLine line = _lines[i];
            TMP_Text label = _labels[i];
            label.text = line.Text;
            UiGlyphs.Prepare(label, line.Text);
            if (line.Emphasis == ItemReadout.Title || line.Emphasis == ItemReadout.Rarity)
                label.color = title;
            else if (line.Emphasis == ItemReadout.Warn)
                label.color = Warn;
            else
                label.color = Ink;

            label.ForceMeshUpdate();
            float height = Mathf.Max(Line, label.preferredHeight);
            RectTransform rect = label.rectTransform;
            rect.anchoredPosition = new Vector2(Pad, -y);
            rect.sizeDelta = new Vector2(width, height);
            y += height + 2f;
        }

        float panelHeight = y + Pad - 2f;
        _root.sizeDelta = new Vector2(Width, panelHeight);
        Place(screen, panelHeight);
    }

    void Place(Vector2 screen, float height)
    {
        Canvas canvas = _root.GetComponentInParent<Canvas>();
        float scale = canvas != null && canvas.scaleFactor > 0.01f ? canvas.scaleFactor : 1f;
        float screenWidth = _root.rect.width * scale;
        float screenHeight = height * scale;
        Vector2 topLeft = UiScreen.TooltipTopLeft(screen, screenWidth, screenHeight, Screen.width, Screen.height, Margin, Gap);
        _root.position = new Vector3(topLeft.x, topLeft.y, 0f);
    }

    TMP_Text MakeLine()
    {
        var go = new GameObject("Line", typeof(RectTransform));
        go.transform.SetParent(_root, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(Width - Pad * 2f, Line);
        return UiGlyphs.Place(go, string.Empty, 18f, TextAnchor.UpperLeft, Ink, UiGlyphs.Wrap.Paragraph);
    }
}
