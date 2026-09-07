using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ScoringSceneController : MonoBehaviour
{
    private UIDocument _document;
    private VisualElement _root;
    private VisualElement _outcomeTab;
    private VisualElement _metricsTab;
    private VisualElement _reactionsTab;
    private Button _outcomeButton;
    private Button _metricsButton;
    private Button _reactionsButton;

    private void OnEnable()
    {
        _document = GetComponent<UIDocument>();

        if (_document == null)
        {
            Debug.LogError("[ScoringSceneController] UIDocument is missing.");
            return;
        }

        _root = _document.rootVisualElement;
        if (_root == null)
        {
            Debug.LogError("[ScoringSceneController] rootVisualElement is missing.");
            return;
        }

        BuildUi();
        ShowOutcomeTab();
    }

    private void OnDisable()
    {
        if (_outcomeButton != null)
            _outcomeButton.clicked -= ShowOutcomeTab;

        if (_metricsButton != null)
            _metricsButton.clicked -= ShowMetricsTab;

        if (_reactionsButton != null)
            _reactionsButton.clicked -= ShowReactionsTab;
    }

    private void BuildUi()
    {
        _root.Clear();
        _root.style.flexGrow = 1;
        _root.style.width = Length.Percent(100);
        _root.style.height = Length.Percent(100);
        _root.style.overflow = Overflow.Hidden;
        _root.style.backgroundColor = UiColor(10, 28, 49);

        var screenRoot = CreateElement("screen_root");
        screenRoot.AddToClassList("screen-root");
        screenRoot.style.flexGrow = 1;
        screenRoot.style.width = Length.Percent(100);
        screenRoot.style.height = Length.Percent(100);
        screenRoot.style.overflow = Overflow.Hidden;
        screenRoot.style.flexDirection = FlexDirection.Column;
        screenRoot.style.position = Position.Relative;
        _root.Add(screenRoot);

        var overlay = CreateElement();
        overlay.AddToClassList("screen-overlay");
        ApplyAbsoluteFill(overlay, UiColor(6, 19, 34, 92));
        screenRoot.Add(overlay);

        var glowLeft = CreateElement();
        glowLeft.AddToClassList("bg-glow");
        glowLeft.AddToClassList("bg-glow-left");
        ApplyGlow(glowLeft, UiColor(101, 183, 255, 30), -8, 12, 34, 34);
        screenRoot.Add(glowLeft);

        var glowRight = CreateElement();
        glowRight.AddToClassList("bg-glow");
        glowRight.AddToClassList("bg-glow-right");
        ApplyGlow(glowRight, UiColor(63, 132, 215, 40), null, null, 42, 42, true);
        screenRoot.Add(glowRight);

        var frame = CreateElement("screen_frame");
        frame.AddToClassList("screen-frame");
        frame.style.flexGrow = 1;
        frame.style.width = Length.Percent(100);
        frame.style.maxWidth = 1600;
        frame.style.minWidth = 960;
        frame.style.alignSelf = Align.Center;
        frame.style.flexDirection = FlexDirection.Column;
        frame.style.paddingTop = 18;
        frame.style.paddingBottom = 18;
        frame.style.paddingLeft = 20;
        frame.style.paddingRight = 20;
        screenRoot.Add(frame);

        var hero = CreateHero();
        hero.style.marginBottom = 14;
        frame.Add(hero);

        var judgeRow = CreateJudgeRow();
        judgeRow.style.marginBottom = 14;
        frame.Add(judgeRow);

        frame.Add(CreateTabPanel());
    }

    private VisualElement CreateHero()
    {
        var hero = PanelShell("hero", UiColor(14, 39, 67, 209), UiColor(141, 203, 255, 128), 18);
        hero.style.paddingTop = 14;
        hero.style.paddingBottom = 12;
        hero.style.paddingLeft = 18;
        hero.style.paddingRight = 18;

        var eyebrow = CreateLabel("Scenario Debrief", 11, UiColor(182, 205, 226), true);
        eyebrow.AddToClassList("eyebrow");
        eyebrow.style.marginBottom = 10;
        hero.Add(eyebrow);

        var main = CreateElement("hero_main");
        main.AddToClassList("hero-main");
        main.style.flexDirection = FlexDirection.Row;
        main.style.justifyContent = Justify.SpaceBetween;
        main.style.alignItems = Align.FlexEnd;
        main.style.flexWrap = Wrap.Wrap;
        var title = CreateLabel("CITY SCORING REPORT", 48, UiColor(238, 247, 255), true);
        title.AddToClassList("hero-title");
        main.Add(title);

        var scoreChip = PanelShell("score_chip", UiColor(101, 183, 255, 30), UiColor(101, 183, 255, 102), 14);
        scoreChip.AddToClassList("score-chip");
        scoreChip.style.minWidth = 180;
        scoreChip.style.paddingTop = 10;
        scoreChip.style.paddingBottom = 10;
        scoreChip.style.paddingLeft = 14;
        scoreChip.style.paddingRight = 14;
        scoreChip.style.alignItems = Align.FlexEnd;

        scoreChip.Add(CreateLabel("Final City Grade", 11, UiColor(182, 205, 226), true));
        var grade = CreateLabel("B+", 40, UiColor(101, 183, 255), true);
        grade.style.marginTop = 4;
        scoreChip.Add(grade);
        main.Add(scoreChip);
        hero.Add(main);
        return hero;
    }

    private VisualElement CreateJudgeRow()
    {
        var row = CreateElement("judge_row");
        row.AddToClassList("judge-row");
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginTop = 18;
        var first = CreateJudgeCard("judge_card_resilience", "Resilience Judge", "Dr. Vale", 2);
        first.style.marginRight = 18;
        row.Add(first);

        var second = CreateJudgeCard("judge_card_trust", "Public Trust Judge", "Mira Quill", 3);
        second.style.marginRight = 18;
        row.Add(second);

        row.Add(CreateJudgeCard("judge_card_systems", "Systems Judge", "Unit Sol-9", 4));
        return row;
    }

    private VisualElement CreateJudgeCard(string name, string role, string judgeName, int filledDots)
    {
        var card = PanelShell(name, UiColor(14, 39, 67, 209), UiColor(110, 179, 241, 71), 18);
        card.AddToClassList("judge-card");
        card.style.flexGrow = 1;
        card.style.flexBasis = 0;
        card.style.flexDirection = FlexDirection.Row;
        card.style.alignItems = Align.Center;
        card.style.paddingTop = 12;
        card.style.paddingBottom = 12;
        card.style.paddingLeft = 14;
        card.style.paddingRight = 14;
        var avatar = CreateElement();
        avatar.AddToClassList("avatar");
        avatar.style.width = 72;
        avatar.style.height = 72;
        avatar.style.borderTopLeftRadius = 999;
        avatar.style.borderTopRightRadius = 999;
        avatar.style.borderBottomLeftRadius = 999;
        avatar.style.borderBottomRightRadius = 999;
        avatar.style.borderTopWidth = 4;
        avatar.style.borderBottomWidth = 4;
        avatar.style.borderLeftWidth = 4;
        avatar.style.borderRightWidth = 4;
        avatar.style.borderTopColor = UiColor(255, 255, 255, 209);
        avatar.style.borderBottomColor = UiColor(255, 255, 255, 209);
        avatar.style.borderLeftColor = UiColor(255, 255, 255, 209);
        avatar.style.borderRightColor = UiColor(255, 255, 255, 209);
        avatar.style.backgroundColor = UiColor(74, 147, 222);
        avatar.style.position = Position.Relative;
        avatar.style.justifyContent = Justify.Center;
        avatar.style.alignItems = Align.Center;

        var avatarLabel = CreateLabel("AI", 21, Color.white, true);
        avatar.Add(avatarLabel);

        var badge = CreateElement();
        badge.AddToClassList("avatar-badge");
        badge.style.position = Position.Absolute;
        badge.style.right = 6;
        badge.style.bottom = 6;
        badge.style.width = 22;
        badge.style.height = 22;
        badge.style.borderTopLeftRadius = 999;
        badge.style.borderTopRightRadius = 999;
        badge.style.borderBottomLeftRadius = 999;
        badge.style.borderBottomRightRadius = 999;
        badge.style.backgroundColor = UiColor(255, 250, 255, 240);
        badge.style.borderTopWidth = 1;
        badge.style.borderBottomWidth = 1;
        badge.style.borderLeftWidth = 1;
        badge.style.borderRightWidth = 1;
        badge.style.borderTopColor = UiColor(101, 183, 255, 66);
        badge.style.borderBottomColor = UiColor(101, 183, 255, 66);
        badge.style.borderLeftColor = UiColor(101, 183, 255, 66);
        badge.style.borderRightColor = UiColor(101, 183, 255, 66);
        badge.style.justifyContent = Justify.Center;
        badge.style.alignItems = Align.Center;
        badge.Add(CreateLabel("AI", 9, UiColor(101, 183, 255), true));
        avatar.Add(badge);
        card.Add(avatar);

        var meta = CreateElement();
        meta.AddToClassList("judge-meta");
        meta.style.flexGrow = 1;
        meta.style.minWidth = 0;
        meta.Add(CreateLabel(role, 11, UiColor(182, 205, 226), true));
        var judgeNameLabel = CreateLabel(judgeName, 18, UiColor(238, 247, 255), true);
        judgeNameLabel.style.marginTop = 4;
        meta.Add(judgeNameLabel);
        card.Add(meta);

        var rating = CreateElement();
        rating.AddToClassList("judge-rating");
        rating.style.width = 76;
        rating.style.alignItems = Align.FlexEnd;

        var dots = CreateElement();
        dots.AddToClassList("rating-dots");
        dots.style.flexDirection = FlexDirection.Row;
        dots.style.justifyContent = Justify.FlexEnd;

        for (var i = 0; i < 4; i++)
        {
            var dot = CreateElement();
            dot.AddToClassList("rating-dot");
            dot.style.width = 10;
            dot.style.height = 10;
            dot.style.marginLeft = 3;
            dot.style.marginRight = 3;
            dot.style.borderTopLeftRadius = 999;
            dot.style.borderTopRightRadius = 999;
            dot.style.borderBottomLeftRadius = 999;
            dot.style.borderBottomRightRadius = 999;
            dot.style.backgroundColor = i < filledDots ? UiColor(101, 183, 255) : UiColor(182, 205, 226, 89);
            dots.Add(dot);
        }

        rating.Add(dots);
        card.Add(rating);
        return card;
    }

    private VisualElement CreateTabPanel()
    {
        var panel = PanelShell("tab_panel", UiColor(14, 39, 67, 209), UiColor(110, 179, 241, 71), 18);
        panel.AddToClassList("tab-panel");
        panel.style.flexGrow = 1;
        panel.style.minHeight = 0;
        panel.style.paddingTop = 16;
        panel.style.paddingBottom = 16;
        panel.style.paddingLeft = 16;
        panel.style.paddingRight = 16;
        panel.style.flexDirection = FlexDirection.Column;

        var header = CreateElement("panel_header");
        header.AddToClassList("panel-header");
        header.style.flexDirection = FlexDirection.Column;

        header.Add(CreateLabel("SCENARIO DEBRIEF", 20, UiColor(238, 247, 255), true));
        var subtitle = CreateLabel("Tabbed scoring categories keep the whole scene inside a single game screen while preserving report-style detail.", 12, UiColor(182, 205, 226), false);
        subtitle.style.marginTop = 6;
        header.Add(subtitle);

        var tabs = CreateElement("tabs");
        tabs.AddToClassList("tabs");
        tabs.style.flexDirection = FlexDirection.Row;
        tabs.style.flexWrap = Wrap.Wrap;
        tabs.style.marginTop = 10;

        _outcomeButton = CreateTabButton("OUTCOME", ShowOutcomeTab, true);
        _metricsButton = CreateTabButton("METRICS", ShowMetricsTab, false);
        _reactionsButton = CreateTabButton("REACTIONS", ShowReactionsTab, false);
        _outcomeButton.style.marginRight = 10;
        _metricsButton.style.marginRight = 10;
        tabs.Add(_outcomeButton);
        tabs.Add(_metricsButton);
        tabs.Add(_reactionsButton);
        header.Add(tabs);
        panel.Add(header);

        var content = CreateElement("tab_content");
        content.AddToClassList("tab-content");
        content.style.flexGrow = 1;
        content.style.minHeight = 0;
        content.style.marginTop = 14;
        content.style.flexDirection = FlexDirection.Column;

        _outcomeTab = CreateOutcomeTab();
        _metricsTab = CreateMetricsTab();
        _reactionsTab = CreateReactionsTab();

        content.Add(_outcomeTab);
        content.Add(_metricsTab);
        content.Add(_reactionsTab);
        panel.Add(content);
        return panel;
    }

    private VisualElement CreateOutcomeTab()
    {
        var tab = CreateElement("tab-outcome");
        tab.AddToClassList("tab-view");
        tab.style.flexGrow = 1;
        tab.style.flexDirection = FlexDirection.Column;
        tab.Add(CreateLabel("SCENARIO OUTCOME", 20, UiColor(238, 247, 255), true));
        var summary = CreateLabel("The city prevented catastrophic collapse, but several districts still suffered visible flood damage and delayed service restoration.", 12, UiColor(182, 205, 226), false);
        summary.style.marginTop = 6;
        tab.Add(summary);
        tab.Add(CreateSummaryBand());
        return tab;
    }

    private VisualElement CreateMetricsTab()
    {
        var tab = CreateElement("tab-metrics");
        tab.AddToClassList("tab-view");
        tab.style.flexGrow = 1;
        tab.style.flexDirection = FlexDirection.Column;
        tab.style.display = DisplayStyle.None;

        tab.Add(CreateLabel("CITY METRICS", 20, UiColor(238, 247, 255), true));
        var subtitle = CreateLabel("Core end-of-scenario scoring categories presented in a compact, game-friendly overview.", 12, UiColor(182, 205, 226), false);
        subtitle.style.marginTop = 6;
        tab.Add(subtitle);
        tab.Add(CreateMetricsGrid());
        return tab;
    }

    private VisualElement CreateReactionsTab()
    {
        var tab = CreateElement("tab-reactions");
        tab.AddToClassList("tab-view");
        tab.style.flexGrow = 1;
        tab.style.flexDirection = FlexDirection.Column;
        tab.style.display = DisplayStyle.None;

        tab.Add(CreateLabel("FACTION REACTIONS AND FINAL STATUS", 20, UiColor(238, 247, 255), true));
        var subtitle = CreateLabel("Political reaction and city condition share one switchable debrief tab to reduce panel clutter.", 12, UiColor(182, 205, 226), false);
        subtitle.style.marginTop = 6;
        tab.Add(subtitle);
        tab.Add(CreateReactionColumns());
        return tab;
    }

    private VisualElement CreateSummaryBand()
    {
        var band = CreateElement();
        band.AddToClassList("summary-band");
        band.style.flexDirection = FlexDirection.Row;
        band.style.flexWrap = Wrap.Wrap;
        band.style.marginTop = 12;

        var pill1 = CreateSummaryPill("Districts Secured", "8 / 11");
        pill1.style.marginRight = 12;
        band.Add(pill1);

        var pill2 = CreateSummaryPill("Emergency Funds Left", "$1.2M");
        pill2.style.marginRight = 12;
        band.Add(pill2);

        band.Add(CreateSummaryPill("Recovery Outlook", "Stable"));
        return band;
    }

    private VisualElement CreateSummaryPill(string label, string value)
    {
        var pill = PanelShell(null, UiColor(15, 43, 73, 199), UiColor(101, 183, 255, 41), 14);
        pill.AddToClassList("summary-pill");
        pill.style.flexGrow = 1;
        pill.style.flexBasis = 0;
        pill.style.paddingTop = 12;
        pill.style.paddingBottom = 12;
        pill.style.paddingLeft = 12;
        pill.style.paddingRight = 12;
        pill.Add(CreateLabel(label, 11, UiColor(182, 205, 226), true));
        var valueLabel = CreateLabel(value, 21, UiColor(101, 183, 255), true);
        valueLabel.style.marginTop = 6;
        pill.Add(valueLabel);
        return pill;
    }

    private VisualElement CreateMetricsGrid()
    {
        var grid = CreateElement();
        grid.AddToClassList("stats-grid");
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;
        grid.style.marginTop = 12;

        var stat1 = CreateStatCard("Damage Stats", "$48.6M", "Low-lying residential corridors and two industrial blocks flooded, but downtown barriers prevented a full breach.", "Damage Severity High", false);
        stat1.style.marginRight = 12;
        stat1.style.marginBottom = 12;
        grid.Add(stat1);

        var stat2 = CreateStatCard("Reputation Stats", "+12", "Public approval rose after timely warnings, though business groups pushed back on late road closures.", "Net Reputation Gain", true);
        stat2.style.marginBottom = 12;
        grid.Add(stat2);

        var stat3 = CreateStatCard("Infrastructure Survival", "83%", "Power, pump, and transit assets held through the peak surge with localized service interruptions.", "Above Scenario Target", true);
        stat3.style.marginRight = 12;
        grid.Add(stat3);

        grid.Add(CreateStatCard("Civilian Impact", "Moderate", "Shelters remained functional and evacuation coverage stayed strong despite prolonged standing water.", "Recovery Strain Visible", null));
        return grid;
    }

    private VisualElement CreateStatCard(string label, string value, string detail, string chipText, bool? positive)
    {
        var card = PanelShell(null, UiColor(20, 56, 93, 230), UiColor(101, 183, 255, 46), 14);
        card.AddToClassList("stat-card");
        card.style.flexGrow = 1;
        card.style.flexBasis = new Length(49, LengthUnit.Percent);
        card.style.paddingTop = 13;
        card.style.paddingBottom = 13;
        card.style.paddingLeft = 13;
        card.style.paddingRight = 13;

        card.Add(CreateLabel(label, 11, UiColor(182, 205, 226), true));
        var valueLabel = CreateLabel(value, 28, UiColor(101, 183, 255), true);
        valueLabel.style.marginTop = 8;
        card.Add(valueLabel);

        var detailLabel = CreateLabel(detail, 13, UiColor(211, 231, 248), false);
        detailLabel.style.marginTop = 8;
        card.Add(detailLabel);

        var chip = CreateLabel(chipText, 11, GetChipColor(positive), true);
        chip.style.marginTop = 8;
        chip.style.paddingTop = 5;
        chip.style.paddingBottom = 5;
        chip.style.paddingLeft = 8;
        chip.style.paddingRight = 8;
        chip.style.alignSelf = Align.FlexStart;
        chip.style.borderTopLeftRadius = 999;
        chip.style.borderTopRightRadius = 999;
        chip.style.borderBottomLeftRadius = 999;
        chip.style.borderBottomRightRadius = 999;
        chip.style.backgroundColor = GetChipBackground(positive);
        card.Add(chip);
        return card;
    }

    private VisualElement CreateReactionColumns()
    {
        var columns = CreateElement();
        columns.AddToClassList("reaction-columns");
        columns.style.flexDirection = FlexDirection.Row;
        columns.style.flexGrow = 1;
        columns.style.minHeight = 0;
        columns.style.marginTop = 12;

        var left = CreateElement();
        left.AddToClassList("reaction-column");
        left.style.flexGrow = 1;
        left.style.flexBasis = 0;
        left.style.minWidth = 0;
        left.style.marginRight = 12;
        left.Add(CreateReactionList());

        var right = CreateElement();
        right.AddToClassList("status-column");
        right.style.flexGrow = 1;
        right.style.flexBasis = 0;
        right.style.minWidth = 0;
        right.Add(CreateStatusList());

        columns.Add(left);
        columns.Add(right);
        return columns;
    }

    private VisualElement CreateReactionList()
    {
        var list = CreateElement();
        list.AddToClassList("reaction-list");
        list.style.flexDirection = FlexDirection.Column;
        var item1 = CreateTextCard("reaction-item", "Residents", "Support improved thanks to fast alerts and visible emergency deployment.", true);
        item1.style.marginBottom = 10;
        list.Add(item1);

        var item2 = CreateTextCard("reaction-item", "Business Coalition", "Mixed response after logistics routes were cut to protect flood-prone corridors.", true);
        item2.style.marginBottom = 10;
        list.Add(item2);

        list.Add(CreateTextCard("reaction-item", "City Leadership", "Positive overall. The council views the response as competent, but not flawless.", true));
        return list;
    }

    private VisualElement CreateStatusList()
    {
        var list = CreateElement();
        list.AddToClassList("status-list");
        list.style.flexDirection = FlexDirection.Column;
        var item1 = CreateTextCard("status-item", "Grid Stability", "Maintained with emergency rerouting and backup generator coverage.", true);
        item1.style.marginBottom = 10;
        list.Add(item1);

        var item2 = CreateTextCard("status-item", "Drainage Network", "Overloaded in southern districts but recovered before total system failure.", true);
        item2.style.marginBottom = 10;
        list.Add(item2);

        list.Add(CreateTextCard("status-item", "Civic Confidence", "Trending upward, with strong trust in response crews and warning systems.", true));
        return list;
    }

    private VisualElement CreateTextCard(string className, string title, string body, bool wrap)
    {
        var card = PanelShell(null, UiColor(20, 56, 93, 230), UiColor(101, 183, 255, 41), 14);
        card.AddToClassList(className);
        card.style.paddingTop = 12;
        card.style.paddingBottom = 12;
        card.style.paddingLeft = 13;
        card.style.paddingRight = 13;

        card.Add(CreateLabel(title, 13, UiColor(238, 247, 255), true));
        var bodyLabel = CreateLabel(body, 13, UiColor(214, 232, 247), false);
        bodyLabel.style.marginTop = 4;
        card.Add(bodyLabel);
        return card;
    }

    private Button CreateTabButton(string text, System.Action onClick, bool active)
    {
        var button = new Button();
        button.text = text;
        button.clicked += onClick;

        ApplyTabButtonStyle(button, active);
        return button;
    }

    private void ShowOutcomeTab()
    {
        SetTabState(_outcomeTab, _outcomeButton, true);
        SetTabState(_metricsTab, _metricsButton, false);
        SetTabState(_reactionsTab, _reactionsButton, false);
    }

    private void ShowMetricsTab()
    {
        SetTabState(_outcomeTab, _outcomeButton, false);
        SetTabState(_metricsTab, _metricsButton, true);
        SetTabState(_reactionsTab, _reactionsButton, false);
    }

    private void ShowReactionsTab()
    {
        SetTabState(_outcomeTab, _outcomeButton, false);
        SetTabState(_metricsTab, _metricsButton, false);
        SetTabState(_reactionsTab, _reactionsButton, true);
    }

    private static void SetTabState(VisualElement tab, Button button, bool isVisible)
    {
        if (tab != null)
            tab.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;

        if (button != null)
            ApplyTabButtonStyle(button, isVisible);
    }

    private static void ApplyTabButtonStyle(Button button, bool isActive)
    {
        button.style.paddingTop = 9;
        button.style.paddingBottom = 9;
        button.style.paddingLeft = 12;
        button.style.paddingRight = 12;
        button.style.borderTopWidth = 1;
        button.style.borderBottomWidth = 1;
        button.style.borderLeftWidth = 1;
        button.style.borderRightWidth = 1;
        button.style.borderTopLeftRadius = 999;
        button.style.borderTopRightRadius = 999;
        button.style.borderBottomLeftRadius = 999;
        button.style.borderBottomRightRadius = 999;
        button.style.fontSize = 12;
        button.style.unityFontStyleAndWeight = FontStyle.Bold;
        button.style.color = isActive ? UiColor(101, 183, 255) : UiColor(182, 205, 226);
        button.style.borderTopColor = isActive ? UiColor(166, 223, 255, 160) : UiColor(123, 183, 226, 150);
        button.style.borderLeftColor = isActive ? UiColor(95, 150, 207, 175) : UiColor(75, 117, 160, 165);
        button.style.borderRightColor = isActive ? UiColor(95, 150, 207, 175) : UiColor(75, 117, 160, 165);
        button.style.borderBottomColor = isActive ? UiColor(43, 84, 127, 230) : UiColor(31, 63, 96, 230);
        button.style.borderBottomWidth = isActive ? 4 : 3;
        button.style.backgroundColor = isActive ? UiColor(101, 183, 255, 68) : UiColor(18, 49, 81, 212);
        button.style.unityTextAlign = TextAnchor.MiddleCenter;
    }

    private static VisualElement PanelShell(string name, Color background, Color border, float radius)
    {
        var element = CreateElement(name);
        element.style.backgroundColor = background;
        element.style.borderTopWidth = 1;
        element.style.borderBottomWidth = 1;
        element.style.borderLeftWidth = 1;
        element.style.borderRightWidth = 1;
        element.style.borderTopColor = border;
        element.style.borderBottomColor = border;
        element.style.borderLeftColor = border;
        element.style.borderRightColor = border;
        element.style.borderTopLeftRadius = radius;
        element.style.borderTopRightRadius = radius;
        element.style.borderBottomLeftRadius = radius;
        element.style.borderBottomRightRadius = radius;
        return element;
    }

    private static VisualElement CreateElement(string name = null)
    {
        var element = new VisualElement();
        if (!string.IsNullOrEmpty(name))
            element.name = name;

        return element;
    }

    private static Label CreateLabel(string text, int size, Color color, bool bold)
    {
        var label = new Label(text);
        label.style.fontSize = size;
        label.style.color = color;
        label.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
        return label;
    }

    private static void ApplyAbsoluteFill(VisualElement element, Color color)
    {
        element.style.position = Position.Absolute;
        element.style.left = 0;
        element.style.top = 0;
        element.style.right = 0;
        element.style.bottom = 0;
        element.style.backgroundColor = color;
    }

    private static void ApplyGlow(VisualElement element, Color color, float? leftPercent, float? bottomPercent, float widthPercent, float heightPercent, bool rightSide = false)
    {
        element.style.position = Position.Absolute;
        element.style.width = new Length(widthPercent, LengthUnit.Percent);
        element.style.height = new Length(heightPercent, LengthUnit.Percent);
        element.style.borderTopLeftRadius = 999;
        element.style.borderTopRightRadius = 999;
        element.style.borderBottomLeftRadius = 999;
        element.style.borderBottomRightRadius = 999;
        element.style.backgroundColor = color;
        element.style.opacity = 1;
        if (rightSide)
        {
            element.style.right = new Length(-10, LengthUnit.Percent);
            element.style.top = new Length(10, LengthUnit.Percent);
        }
        else
        {
            if (leftPercent.HasValue)
                element.style.left = new Length(leftPercent.Value, LengthUnit.Percent);

            if (bottomPercent.HasValue)
                element.style.bottom = new Length(bottomPercent.Value, LengthUnit.Percent);
        }
    }

    private static Color GetChipColor(bool? positive)
    {
        return positive switch
        {
            true => UiColor(123, 221, 155),
            false => UiColor(255, 141, 162),
            _ => UiColor(211, 231, 248),
        };
    }

    private static Color GetChipBackground(bool? positive)
    {
        return positive switch
        {
            true => UiColor(66, 130, 79, 31),
            false => UiColor(154, 69, 95, 31),
            _ => UiColor(101, 183, 255, 20),
        };
    }

    private static Color UiColor(byte r, byte g, byte b, byte a = 255)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }

}
