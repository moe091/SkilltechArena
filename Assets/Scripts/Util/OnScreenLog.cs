using FishNet;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI; // or swap for TMP if you prefer

public class OnScreenLog : MonoBehaviour
{
    [Header("UI")]
    public Text text; // Assign a Text (or swap to TMP_Text) in the scene
    public KeyCode toggleKey = KeyCode.F1;
    public int fontSize = 14;

    [Header("Behavior")]
    public int maxLines = 120;          // ring buffer size
    public bool startVisible = false;   // start hidden in release by default
    public bool collapseDuplicates = true;  // like the Unity console
    public bool showTimestamps = true;

    struct Line
    {
        public string msg;
        public string stack;
        public LogType type;
        public int count;
        public double time;
    }

    readonly LinkedList<Line> _lines = new LinkedList<Line>();
    readonly StringBuilder _sb = new StringBuilder(16_384);
    bool _visible;
    RectTransform _rt;


    void Awake()
    {
        if (text == null)
        {
            // Make a minimal canvas/Text if none assigned
            var canvasGO = new GameObject("OnScreenLog_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue; // on top

            var textGO = new GameObject("OnScreenLog_Text", typeof(Text));
            textGO.transform.SetParent(canvasGO.transform, false);
            text = textGO.GetComponent<Text>();
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.alignment = TextAnchor.LowerLeft;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.raycastTarget = false;

            _rt = text.GetComponent<RectTransform>();
            _rt.anchorMin = new Vector2(0, 0);
            _rt.anchorMax = new Vector2(0.6f, 0.4f); // bottom-left area
            _rt.offsetMin = new Vector2(8, 8);
            _rt.offsetMax = new Vector2(-8, -8);

            var bg = new GameObject("OnScreenLog_BG", typeof(Image)).GetComponent<Image>();
            bg.transform.SetParent(text.transform, false);
            bg.rectTransform.anchorMin = new Vector2(0, 0);
            bg.rectTransform.anchorMax = new Vector2(1, 1);
            bg.rectTransform.offsetMin = new Vector2(-6, -6);
            bg.rectTransform.offsetMax = new Vector2(6, 6);
            bg.color = new Color(0, 0, 0, 0.45f); // translucent background
            bg.raycastTarget = false;

            text.transform.SetAsLastSibling(); // text on top of bg
        }
        else
        {
            _rt = text.GetComponent<RectTransform>();
        }

        _visible = startVisible;
        text.gameObject.SetActive(_visible);
        Application.logMessageReceivedThreaded += HandleLog;
    }

    void OnDestroy() => Application.logMessageReceivedThreaded -= HandleLog;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            _visible = !_visible;
            text.gameObject.SetActive(_visible);
            if (_visible) RebuildText();
        }
    }

    void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Warning)
            return;

        lock (_lines)
        {
            string lineText = condition;
            if (showTimestamps)
                lineText = $"[{System.DateTime.Now:HH:mm:ss.fff}] {lineText}";

            if (collapseDuplicates && _lines.First != null && _lines.First.Value.msg == lineText && _lines.First.Value.type == type)
            {
                var top = _lines.First.Value;
                top.count++;
                _lines.First.Value = top;
            }
            else
            {
                if (_lines.Count >= maxLines)
                    _lines.RemoveLast();

                _lines.AddFirst(new Line
                {
                    msg = lineText,
                    stack = stackTrace,
                    type = type,
                    count = 1,
                    time = Time.realtimeSinceStartupAsDouble
                });
            }
        }
        if (_visible) RebuildText();
    }

    void RebuildText()
    {
        lock (_lines)
        {
            _sb.Clear();
            foreach (var node in _lines)
            {
                // Color tags are cheap; remove if you dislike
                switch (node.type)
                {
                    case LogType.Error:
                    case LogType.Exception: _sb.Append("<color=#FF6B6B>"); break;
                    case LogType.Assert:
                    case LogType.Warning: _sb.Append("<color=#FFD166>"); break;
                    default: _sb.Append("<color=#FFFFFF>"); break;
                }

                _sb.Append(node.msg);
                if (node.count > 1) _sb.Append($"  x{node.count}");
                _sb.Append("</color>\n");

                if ((node.type == LogType.Error || node.type == LogType.Exception) && !string.IsNullOrEmpty(node.stack))
                {
                    _sb.Append("<color=#AAAAAA>").Append(node.stack).Append("</color>\n");
                }
            }
            text.supportRichText = true;
            text.text = _sb.ToString();
        }
    }
}
