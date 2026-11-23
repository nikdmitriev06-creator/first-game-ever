using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelEditorWindow : EditorWindow
{
    RoomDefinition _room;
    LevelDefinition _level;
    LevelLayoutDefinition _layout;
    TerrainLibrary _library;
    Vector2 _paletteScroll;
    Vector2 _sideScroll;
    Vector2 _pan;
    RoomDefinition _framed;
    bool _draggingView;
    float _zoom = 36f;
    readonly List<AuditIssue> _issues = new List<AuditIssue>();

    [MenuItem("Game/Уровни/Редактор уровней")]
    public static void OpenMenu()
    {
        Open(null);
    }

    public static void Open(RoomDefinition room)
    {
        LevelEditorWindow window = GetWindow<LevelEditorWindow>("Уровни");
        if (room != null)
            window._room = room;
        window.Show();
    }

    void OnEnable()
    {
        if (_library == null)
            _library = AssetDatabase.LoadAssetAtPath<TerrainLibrary>("Assets/Data/Levels/TerrainLibrary.asset");
        RoomEditSession.Difficulty = LevelDebugSettings.Difficulty;
        RoomEditSession.Seed = LevelDebugSettings.Seed;
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Редактор уровней", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Сцена и это окно пишут в один и тот же Room asset. Направления выходов — оси изометрической сетки, не WASD.",
            MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawPalette();
            DrawGrid();
            DrawSide();
        }
    }

    void DrawPalette()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(230)))
        {
            _library = (TerrainLibrary)EditorGUILayout.ObjectField("Палитра", _library, typeof(TerrainLibrary), false);
            _room = (RoomDefinition)EditorGUILayout.ObjectField("Комната", _room, typeof(RoomDefinition), false);
            RoomEditSession.Layer = (RoomLayer)EditorGUILayout.EnumPopup("Слой", RoomEditSession.Layer);
            RoomEditSession.Tool = (RoomTool)EditorGUILayout.EnumPopup("Инструмент", RoomEditSession.Tool);
            _paletteScroll = EditorGUILayout.BeginScrollView(_paletteScroll);
            if (_library != null)
            {
                IReadOnlyList<TerrainDefinition> terrains = _library.All;
                for (int i = 0; i < terrains.Count; i++)
                {
                    TerrainDefinition terrain = terrains[i];
                    if (terrain == null)
                        continue;

                    Color previous = GUI.backgroundColor;
                    if (RoomEditSession.Terrain == terrain)
                        GUI.backgroundColor = new Color(0.65f, 0.85f, 0.55f);
                    if (GUILayout.Button(terrain.DisplayName))
                    {
                        RoomEditSession.Terrain = terrain;
                        RoomEditSession.Tool = RoomTool.Paint;
                    }

                    GUI.backgroundColor = previous;
                }
            }
            else
                EditorGUILayout.HelpBox("Сначала создайте демо-контент: Game → Уровни.", MessageType.Info);

            EditorGUILayout.EndScrollView();
        }
    }

    void DrawGrid()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _zoom = EditorGUILayout.Slider("Масштаб", _zoom, 16f, 72f);
                if (GUILayout.Button("По центру", GUILayout.Width(90f)))
                    _pan = Vector2.zero;
            }

            EditorGUILayout.LabelField("Правая кнопка двигает карту. Колёсико меняет масштаб.");
            Rect canvas = GUILayoutUtility.GetRect(10f, 10f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (_room == null)
            {
                GUI.Label(canvas, "Выберите комнату.");
                return;
            }

            if (_framed != _room)
            {
                _framed = _room;
                _pan = Vector2.zero;
            }

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(canvas, new Color(0.15f, 0.15f, 0.15f, 1f));
                DrawRoom(canvas);
            }

            HandleGrid(canvas);
        }
    }

    void DrawRoom(Rect canvas)
    {
        var corners = new Vector3[4];
        for (int y = 0; y < _room.Height; y++)
        {
            for (int x = 0; x < _room.Width; x++)
            {
                TerrainDefinition surface = _room.SurfaceAt(x, y);
                Color fill = surface != null ? surface.Swatch : new Color(0.2f, 0.2f, 0.2f, 1f);
                if (surface != null && !surface.Walkable)
                    fill = Color.Lerp(fill, Color.black, 0.25f);
                TerrainCoordinates.WriteDiamond(x, y, corners);
                Vector3[] gui = new Vector3[4];
                for (int i = 0; i < 4; i++)
                    gui[i] = ToGui(canvas, corners[i]);
                Handles.DrawSolidRectangleWithOutline(gui, fill, new Color(0f, 0f, 0f, 0.35f));
            }
        }

        IReadOnlyList<RoomMarkerData> markers = _room.Markers;
        for (int i = 0; i < markers.Count; i++)
        {
            RoomMarkerData marker = markers[i];
            if (marker == null)
                continue;

            Vector2 at = ToGui(canvas, TerrainCoordinates.CellCenterWorld(marker.x, marker.y));
            bool selected = RoomEditSession.Selected.Contains(marker.id);
            Handles.color = marker.kind == MarkerKind.Socket ? Color.cyan : marker.kind == MarkerKind.Spawn ? new Color(1f, 0.45f, 0.2f) : Color.white;
            Handles.DrawSolidDisc(at, Vector3.forward, selected ? 6f : 4f);
        }
    }

    void HandleGrid(Rect canvas)
    {
        Event current = Event.current;
        bool over = canvas.Contains(current.mousePosition);
        bool viewButton = current.button == 1 || current.button == 2;
        if (current.type == EventType.MouseDown && over && viewButton)
        {
            _draggingView = true;
            current.Use();
            return;
        }

        if (current.type == EventType.MouseDrag && _draggingView)
        {
            _pan += current.delta;
            current.Use();
            Repaint();
            return;
        }

        if (current.type == EventType.MouseUp && _draggingView)
        {
            _draggingView = false;
            current.Use();
            return;
        }

        if (!over)
            return;

        if (current.type == EventType.ScrollWheel)
        {
            _zoom = Mathf.Clamp(_zoom - current.delta.y * 2f, 16f, 72f);
            current.Use();
            Repaint();
            return;
        }

        bool drag = current.type == EventType.MouseDrag && (RoomEditSession.Tool == RoomTool.Paint || RoomEditSession.Tool == RoomTool.Erase);
        bool click = current.type == EventType.MouseDown && current.button == 0;
        if (!drag && !click)
            return;

        Vector2 world = FromGui(canvas, current.mousePosition);
        RoomEdits.Apply(_room, TerrainCoordinates.WorldToCell(world));
        current.Use();
        Repaint();
    }

    Vector2 RoomCenter()
    {
        Vector2 south = TerrainCoordinates.CornerWorld(0, 0);
        Vector2 north = TerrainCoordinates.CornerWorld(_room.Width, _room.Height);
        return (south + north) * 0.5f;
    }

    Vector2 ToGui(Rect canvas, Vector2 world)
    {
        Vector2 delta = world - RoomCenter();
        return new Vector2(
            canvas.x + canvas.width * 0.5f + _pan.x + delta.x * _zoom,
            canvas.y + canvas.height * 0.5f + _pan.y - delta.y * _zoom);
    }

    Vector2 FromGui(Rect canvas, Vector2 gui)
    {
        Vector2 center = RoomCenter();
        float x = (gui.x - canvas.x - canvas.width * 0.5f - _pan.x) / _zoom;
        float y = -(gui.y - canvas.y - canvas.height * 0.5f - _pan.y) / _zoom;
        return new Vector2(x, y) + center;
    }

    void DrawSide()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(280)))
        {
            _sideScroll = EditorGUILayout.BeginScrollView(_sideScroll);
            DrawMarkerSettings();
            DrawAudit();
            DrawDifficulty();
            DrawLevel();
            if (GUILayout.Button("Сохранить"))
            {
                AssetDatabase.SaveAssets();
                if (_room != null)
                    RoomDocument.Find(_room)?.Rebuild();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    void DrawMarkerSettings()
    {
        EditorGUILayout.LabelField("Объекты и спавн", EditorStyles.boldLabel);
        RoomEditSession.Pool = (SpawnPoolDefinition)EditorGUILayout.ObjectField("Пул", RoomEditSession.Pool, typeof(SpawnPoolDefinition), false);
        RoomEditSession.Category = (SpawnCategory)EditorGUILayout.EnumPopup("Категория", RoomEditSession.Category);
        RoomEditSession.MinCount = EditorGUILayout.IntField("Минимум", RoomEditSession.MinCount);
        RoomEditSession.MaxCount = EditorGUILayout.IntField("Максимум", RoomEditSession.MaxCount);
        RoomEditSession.Radius = EditorGUILayout.FloatField("Радиус", RoomEditSession.Radius);
        RoomEditSession.Prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", RoomEditSession.Prefab, typeof(GameObject), false);
        RoomEditSession.Direction = (IsoDirection)EditorGUILayout.EnumPopup("Выход", RoomEditSession.Direction);
        EditorGUILayout.LabelField(IsoSides.Label(RoomEditSession.Direction));
        RoomEditSession.SocketRequired = EditorGUILayout.Toggle("Обязательный выход", RoomEditSession.SocketRequired);
        RoomEditSession.SocketTag = EditorGUILayout.TextField("Метка стыка", RoomEditSession.SocketTag);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Удалить"))
                RoomEdits.DeleteSelected(_room);
            if (GUILayout.Button("Копия"))
                RoomEdits.DuplicateSelected(_room);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("←"))
                RoomEdits.Nudge(_room, -1, 0);
            if (GUILayout.Button("→"))
                RoomEdits.Nudge(_room, 1, 0);
            if (GUILayout.Button("↑"))
                RoomEdits.Nudge(_room, 0, 1);
            if (GUILayout.Button("↓"))
                RoomEdits.Nudge(_room, 0, -1);
        }
    }

    void DrawAudit()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Проверка", EditorStyles.boldLabel);
        RoomAudit.Evaluate(_room, RoomEditSession.Difficulty, _issues);
        if (_issues.Count == 0)
            EditorGUILayout.LabelField("Замечаний нет.");
        for (int i = 0; i < _issues.Count; i++)
            EditorGUILayout.HelpBox(_issues[i].Message, _issues[i].Error ? MessageType.Error : MessageType.Warning);

        if (_room != null && RoomEditSession.Pool != null)
        {
            List<SpawnEntry> available = SpawnResolver.Available(RoomEditSession.Pool, RoomEditSession.Difficulty);
            EditorGUILayout.LabelField("Доступно на сложности " + RoomEditSession.Difficulty + ": " + available.Count);
            for (int i = 0; i < available.Count; i++)
            {
                SpawnEntry entry = available[i];
                string prefab = entry.prefab != null ? entry.prefab.name : "нет prefab";
                EditorGUILayout.LabelField(entry.id + "  вес " + entry.weight + "  " + prefab);
            }
        }
    }

    void DrawDifficulty()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Отладка", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        RoomEditSession.Difficulty = EditorGUILayout.IntSlider("Сложность", RoomEditSession.Difficulty, 1, 10);
        RoomEditSession.Seed = EditorGUILayout.IntField("Seed", RoomEditSession.Seed);
        bool armed = EditorGUILayout.Toggle("Взять сложность в Play", LevelDebugSettings.Armed);
        if (EditorGUI.EndChangeCheck())
        {
            if (armed)
                LevelDebugSettings.Arm(RoomEditSession.Difficulty, RoomEditSession.Seed);
            else
                LevelDebugSettings.Disarm();
        }

        EditorGUILayout.HelpBox("Галочка не записывает прогресс локации. Она только подменяет сложность следующего запуска в редакторе.", MessageType.None);
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Принудительный спавн"))
            {
                RoomDocument document = _room != null ? RoomDocument.Find(_room) : null;
                document?.RunSpawns(true, RoomEditSession.Difficulty);
            }
        }
    }

    void DrawLevel()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Локация", EditorStyles.boldLabel);
        _level = (LevelDefinition)EditorGUILayout.ObjectField("Локация", _level, typeof(LevelDefinition), false);
        _layout = (LevelLayoutDefinition)EditorGUILayout.ObjectField("Макет", _layout, typeof(LevelLayoutDefinition), false);
        if (GUILayout.Button("Собрать по seed"))
            BuildLayout(false);
        if (GUILayout.Button("Проверить макет"))
            BuildLayout(true);
    }

    void BuildLayout(bool authored)
    {
        LayoutResult result = authored && _layout != null
            ? RoomLayoutBuilder.FromPlacements(Copy(_layout), _layout.Seed)
            : RoomLayoutBuilder.Build(_level, RoomEditSession.Seed);
        if (result == null)
            return;

        if (!authored && _layout != null)
        {
            Undo.RecordObject(_layout, "Сборка локации");
            var placements = new List<LayoutPlacement>();
            for (int i = 0; i < result.Nodes.Count; i++)
            {
                placements.Add(new LayoutPlacement
                {
                    room = result.Nodes[i].Room,
                    origin = result.Nodes[i].Origin
                });
            }

            _layout.SetPlacements(placements, RoomEditSession.Seed);
            EditorUtility.SetDirty(_layout);
        }

        string text = (result.Ok ? "Сборка сходится." : "Сборка с ошибками.") + "\n";
        for (int i = 0; i < result.Nodes.Count; i++)
            text += result.Nodes[i].Room.DisplayName + " @ " + result.Nodes[i].Origin + "\n";
        for (int i = 0; i < result.Problems.Count; i++)
            text += result.Problems[i] + "\n";
        EditorUtility.DisplayDialog("Локация", text, "Ок");
    }

    static List<LayoutPlacement> Copy(LevelLayoutDefinition layout)
    {
        var list = new List<LayoutPlacement>();
        IReadOnlyList<LayoutPlacement> placements = layout.Placements;
        for (int i = 0; i < placements.Count; i++)
            list.Add(placements[i]);
        return list;
    }
}
