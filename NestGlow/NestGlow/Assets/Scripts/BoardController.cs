using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nestglow
{
    public class BoardController : MonoBehaviour
    {
        public const int Width = 6;
        public const int Height = 6;

        [SerializeField] float cellSize = 1.18f;
        [SerializeField] int refillEnergyAmount = 8;
        [SerializeField] int rank2SpawnChancePercent = 15;

        // Цели уровней: ранг предмета, сколько создать, энергия на старт
        static readonly (int rank, int count, int energy)[] Levels =
        {
            (5, 2, 35), // 1: 2 фонаря
            (5, 3, 38), // 2: 3 фонаря
            (6, 1, 40), // 3: 1 маяк
            (6, 2, 42), // 4: 2 маяка
            (7, 1, 45), // 5: 1 созвездие
        };

        readonly BoardItem[,] _grid = new BoardItem[Width, Height];

        int _levelIndex;
        int _goalRank = 5;
        int _goalCount = 2;
        int _startingEnergy = 35;
        int _energy;
        int _createdGoalItems;
        bool _gameOver;
        bool _won;
        TextMesh _levelLabel;

        BoardItem _dragItem;
        Vector3 _dragOffset;
        VisualKit _kit;
        Camera _cam;

        Transform _hudRoot;
        TextMesh _title;
        TextMesh _energyValue;
        TextMesh _goalValue;
        TextMesh _hint;
        TextMesh _spawnLabel;
        SpriteRenderer _spawnButton;

        public void Begin(Camera cam, VisualKit kit)
        {
            _cam = cam;
            _kit = kit;
            _levelIndex = 0;
            ApplyLevelConfig();
            StartRound(rebuildWorld: true);
        }

        void ApplyLevelConfig()
        {
            var level = Levels[Mathf.Clamp(_levelIndex, 0, Levels.Length - 1)];
            _goalRank = level.rank;
            _goalCount = level.count;
            _startingEnergy = level.energy;
        }

        void StartRound(bool rebuildWorld)
        {
            _energy = _startingEnergy;
            _createdGoalItems = 0;
            _gameOver = false;
            _won = false;

            if (rebuildWorld)
            {
                BuildAtmosphere();
                BuildBoardVisual();
                EnsureHud();
            }

            SpawnInitialItems();
            RefreshHud();
        }

        void Update()
        {
            if (WasRestartPressed())
            {
                OnContinuePressed();
                return;
            }

            // После победы/поражения — только кнопка внизу или R
            if (_won || _gameOver)
            {
                if (WasPrimaryPressed() && GetScreenPosition().y < Screen.height * 0.14f)
                {
                    PulseSpawnButton();
                    OnContinuePressed();
                }
                return;
            }

            HandlePointer();
            HandleHotkeys();
        }

        void OnContinuePressed()
        {
            if (_won)
            {
                _levelIndex = _levelIndex < Levels.Length - 1 ? _levelIndex + 1 : 0;
                ApplyLevelConfig();
                RebuildRound();
                return;
            }

            // поражение — тот же уровень
            ApplyLevelConfig();
            RebuildRound();
        }

        void BuildAtmosphere()
        {
            // Hallmark atmospheric: dark paper + ≤2 warm blooms (brass emit)
            CreateSprite("NightSky", _kit.Gradient, new Vector3(0f, 0.15f, 1f), new Vector3(20f, 13f, 1f), -40,
                Color.white);

            // Lumen apparatus: moon as the single engineered light object
            CreateSprite("MoonGlow", _kit.WideGlow, new Vector3(3.55f, 3.45f, 0.8f), Vector3.one * 2.6f, -35,
                NestglowTheme.WithAlpha(NestglowTheme.Accent, 0.18f));
            CreateSprite("Moon", _kit.Moon, new Vector3(3.55f, 3.45f, 0.7f), Vector3.one * 1.05f, -34,
                NestglowTheme.WithAlpha(NestglowTheme.Ink, 0.88f));

            // two blooms only
            CreateSprite("WarmBloom", _kit.WideGlow, new Vector3(0.2f, -0.2f, 0.6f), Vector3.one * 8.8f, -30,
                NestglowTheme.WithAlpha(NestglowTheme.Accent, 0.14f));
            CreateSprite("CoolBloom", _kit.WideGlow, new Vector3(-3.0f, 2.0f, 0.6f), Vector3.one * 5.5f, -29,
                NestglowTheme.WithAlpha(NestglowTheme.Paper3, 0.55f));

            CreateSprite("Hills", _kit.Hill, new Vector3(0f, -4.1f, 0.5f), new Vector3(14f, 2.4f, 1f), -25,
                NestglowTheme.WithAlpha(NestglowTheme.Paper2, 0.95f));
            CreateSprite("HillsFront", _kit.Hill, new Vector3(0.4f, -4.35f, 0.45f), new Vector3(15f, 1.8f, 1f), -24,
                NestglowTheme.WithAlpha(NestglowTheme.Paper, 0.92f));

            CreateSprite("Vignette", _kit.Vignette, new Vector3(0f, 0.1f, -0.2f), new Vector3(13f, 9f, 1f), 25,
                Color.white);

            // sparse dust — not floating-orb spam
            for (int i = 0; i < 14; i++)
            {
                var star = new GameObject($"Dust_{i}");
                star.transform.SetParent(transform, false);
                star.transform.position = new Vector3(Random.Range(-6.5f, 6.5f), Random.Range(-3.5f, 4.2f), 0.3f);
                star.transform.localScale = Vector3.one * Random.Range(0.05f, 0.12f);
                var sr = star.AddComponent<SpriteRenderer>();
                sr.sprite = _kit.Star;
                sr.color = NestglowTheme.WithAlpha(NestglowTheme.Accent, Random.Range(0.12f, 0.32f));
                sr.sortingOrder = -18;
                star.AddComponent<AmbientDrift>().Setup(Random.Range(0.03f, 0.12f), Random.Range(0.25f, 0.7f), twinkle: true);
            }
        }

        void BuildBoardVisual()
        {
            // Elevated card on paper — hairline rim, not thick gold chrome
            CreateSprite("BoardOuterGlow", _kit.WideGlow, Vector3.zero, Vector3.one * 8.6f, -12,
                NestglowTheme.GlowSoft);

            CreateSprite("BoardRim", _kit.RoundPanel, Vector3.zero,
                new Vector3(Width * cellSize + 1.15f, Height * cellSize + 1.15f, 1f), -8,
                NestglowTheme.WithAlpha(NestglowTheme.Rule, 0.85f));

            CreateSprite("BoardPanel", _kit.RoundPanel, Vector3.zero,
                new Vector3(Width * cellSize + 0.92f, Height * cellSize + 0.92f, 1f), -7,
                NestglowTheme.WithAlpha(NestglowTheme.Paper2, 0.97f));

            CreateSprite("BoardInner", _kit.RoundPanel, Vector3.zero,
                new Vector3(Width * cellSize + 0.52f, Height * cellSize + 0.52f, 1f), -6,
                NestglowTheme.WithAlpha(NestglowTheme.Paper3, 0.92f));

            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var cell = new GameObject($"Cell_{x}_{y}");
                cell.transform.SetParent(transform, false);
                cell.transform.position = CellToWorld(x, y);
                cell.transform.localScale = Vector3.one * (cellSize * 0.9f);

                var well = cell.AddComponent<SpriteRenderer>();
                well.sprite = _kit.CellWell;
                well.sortingOrder = 0;
                well.color = (x + y) % 2 == 0 ? NestglowTheme.CellA : NestglowTheme.CellB;

                var gloss = new GameObject("Gloss");
                gloss.transform.SetParent(cell.transform, false);
                gloss.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                gloss.transform.localScale = new Vector3(0.65f, 0.32f, 1f);
                var glossSr = gloss.AddComponent<SpriteRenderer>();
                glossSr.sprite = _kit.GlowHalo;
                glossSr.color = NestglowTheme.WithAlpha(NestglowTheme.Ink, 0.04f);
                glossSr.sortingOrder = 1;
            }
        }

        void EnsureHud()
        {
            _hudRoot = new GameObject("HUDRoot").transform;

            float top = Height * cellSize * 0.5f;

            // Lumen: lowercase display wordmark + mono UPPERCASE labels on floating pills
            CreateHudSprite("TitleGlow", _kit.GlowHalo, new Vector3(0f, top + 1.78f, 0f), Vector3.one * 2.2f, 69,
                NestglowTheme.GlowSoft);

            _title = MakeLabel(_hudRoot, "Title", new Vector3(0f, top + 1.78f, 0f), 0.2f, 56,
                NestglowTheme.Ink);
            _title.text = "nestglow";
            _title.fontStyle = FontStyle.Normal;

            _levelLabel = MakeLabel(_hudRoot, "LevelLabel", new Vector3(0f, top + 1.42f, 0f), 0.075f, 26,
                NestglowTheme.Muted);
            _levelLabel.fontStyle = FontStyle.Normal;

            // energy chip — N5 floating pill energy
            CreateHudSprite("EnergyChip", _kit.Pill, new Vector3(-2.35f, top + 1.0f, 0f), new Vector3(2.65f, 0.68f, 1f), 70,
                NestglowTheme.WithAlpha(NestglowTheme.Paper2, 0.92f));
            CreateHudSprite("EnergyIcon", _kit.CoreOrb, new Vector3(-3.2f, top + 1.0f, 0f), Vector3.one * 0.32f, 71,
                NestglowTheme.Accent);
            var energyCaption = MakeLabel(_hudRoot, "EnergyCaption", new Vector3(-2.05f, top + 1.16f, 0f), 0.07f, 24,
                NestglowTheme.Muted);
            energyCaption.text = "ЭНЕРГИЯ";
            energyCaption.fontStyle = FontStyle.Normal;
            energyCaption.anchor = TextAnchor.MiddleLeft;
            energyCaption.alignment = TextAlignment.Left;
            _energyValue = MakeLabel(_hudRoot, "EnergyValue", new Vector3(-2.05f, top + 0.88f, 0f), 0.125f, 40,
                NestglowTheme.Accent);
            _energyValue.anchor = TextAnchor.MiddleLeft;
            _energyValue.alignment = TextAlignment.Left;

            // goal chip
            CreateHudSprite("GoalChip", _kit.Pill, new Vector3(2.35f, top + 1.0f, 0f), new Vector3(2.65f, 0.68f, 1f), 70,
                NestglowTheme.WithAlpha(NestglowTheme.Paper2, 0.92f));
            CreateHudSprite("GoalIcon", _kit.CoreOrb, new Vector3(1.5f, top + 1.0f, 0f), Vector3.one * 0.32f, 71,
                ItemCatalog.GetColor(_goalRank));
            var goalCaption = MakeLabel(_hudRoot, "GoalCaption", new Vector3(2.65f, top + 1.16f, 0f), 0.07f, 24,
                NestglowTheme.Muted);
            goalCaption.text = "ЦЕЛЬ";
            goalCaption.fontStyle = FontStyle.Normal;
            goalCaption.anchor = TextAnchor.MiddleLeft;
            goalCaption.alignment = TextAlignment.Left;
            _goalValue = MakeLabel(_hudRoot, "GoalValue", new Vector3(2.65f, top + 0.88f, 0f), 0.1f, 34,
                NestglowTheme.Ink);
            _goalValue.fontStyle = FontStyle.Normal;
            _goalValue.anchor = TextAnchor.MiddleLeft;
            _goalValue.alignment = TextAlignment.Left;

            _hint = MakeLabel(_hudRoot, "Hint", new Vector3(0f, top + 0.48f, 0f), 0.095f, 32,
                NestglowTheme.Ink2);
            _hint.fontStyle = FontStyle.Normal;

            // primary CTA pill
            float bottom = -Height * cellSize * 0.5f - 1.15f;
            _spawnButton = CreateHudSprite("SpawnButton", _kit.Pill, new Vector3(0f, bottom, 0f), new Vector3(4.5f, 0.82f, 1f), 70,
                NestglowTheme.WithAlpha(NestglowTheme.Accent, 0.92f));
            CreateHudSprite("SpawnGlow", _kit.GlowHalo, new Vector3(0f, bottom, 0f), Vector3.one * 2.6f, 69,
                NestglowTheme.GlowSoft);
            _spawnLabel = MakeLabel(_hudRoot, "SpawnLabel", new Vector3(0f, bottom, 0f), 0.11f, 36,
                NestglowTheme.AccentInk);
            _spawnLabel.text = "ДОСТАТЬ СВЕТ";
            _spawnLabel.fontStyle = FontStyle.Bold;
        }

        SpriteRenderer CreateSprite(string name, Sprite sprite, Vector3 pos, Vector3 scale, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = color;
            return sr;
        }

        SpriteRenderer CreateHudSprite(string name, Sprite sprite, Vector3 pos, Vector3 scale, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_hudRoot != null ? _hudRoot : transform, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = color;
            return sr;
        }

        static TextMesh MakeLabel(Transform parent, string name, Vector3 pos, float charSize, int fontSize, Color color)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = charSize;
            tm.fontSize = fontSize;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sortingOrder = 80;
            return tm;
        }

        void SpawnInitialItems()
        {
            TrySpawnRank(1);
            TrySpawnRank(1);
            TrySpawnRank(1);
            TrySpawnRank(2);
        }

        void HandlePointer()
        {
            if (WasPrimaryPressed())
            {
                var screen = GetScreenPosition();
                if (screen.y < Screen.height * 0.14f)
                {
                    PulseSpawnButton();
                    OnSpawnButtonPressed();
                    return;
                }

                var item = PickItemAtPointer();
                if (item == null) return;

                _dragItem = item;
                var world = GetPointerWorld();
                _dragOffset = item.transform.position - world;
                item.SetDragging(true);
            }
            else if (IsPrimaryHeld() && _dragItem != null)
            {
                _dragItem.transform.position = GetPointerWorld() + _dragOffset;
            }
            else if (WasPrimaryReleased() && _dragItem != null)
            {
                DropDraggedItem();
            }
        }

        void PulseSpawnButton()
        {
            if (_spawnButton == null) return;
            StartCoroutine(SpawnPulseRoutine());
        }

        System.Collections.IEnumerator SpawnPulseRoutine()
        {
            var t = 0f;
            var baseScale = new Vector3(4.4f, 0.85f, 1f);
            while (t < 1f)
            {
                t += Time.deltaTime * 8f;
                float s = 1f + Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.06f;
                _spawnButton.transform.localScale = baseScale * s;
                yield return null;
            }
            _spawnButton.transform.localScale = baseScale;
        }

        void HandleHotkeys()
        {
            if (WasSpawnPressed())
            {
                PulseSpawnButton();
                OnSpawnButtonPressed();
            }
        }

        void OnSpawnButtonPressed()
        {
            if (_won || _gameOver)
            {
                OnContinuePressed();
                return;
            }

            // Нет энергии — мягкий refill (позже заменим на rewarded-рекламу)
            if (_energy <= 0)
            {
                GiveEnergyRefill();
                return;
            }

            TrySpendEnergyAndSpawn();
        }

        void GiveEnergyRefill()
        {
            _energy += refillEnergyAmount;
            _gameOver = false;
            RefreshHud();
            Debug.Log($"Nestglow: +{refillEnergyAmount} энергии (временный бесплатный refill)");
        }

        BoardItem PickItemAtPointer()
        {
            var world = GetPointerWorld();
            if (TryWorldToCell(world, out int x, out int y))
            {
                var atCell = _grid[x, y];
                if (atCell != null) return atCell;
            }

            BoardItem best = null;
            float bestDist = cellSize * 0.55f;
            for (int yy = 0; yy < Height; yy++)
            for (int xx = 0; xx < Width; xx++)
            {
                var item = _grid[xx, yy];
                if (item == null) continue;
                float d = Vector2.Distance(world, item.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = item;
                }
            }
            return best;
        }

        static bool WasPrimaryPressed()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return Input.GetMouseButtonDown(0);
        }

        static bool IsPrimaryHeld()
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
            return Input.GetMouseButton(0);
        }

        static bool WasPrimaryReleased()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) return true;
            return Input.GetMouseButtonUp(0);
        }

        static bool WasSpawnPressed()
        {
            if (Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
                return true;
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E);
        }

        static bool WasRestartPressed()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) return true;
            return Input.GetKeyDown(KeyCode.R);
        }

        static Vector2 GetScreenPosition()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            return Input.mousePosition;
        }

        Vector3 GetPointerWorld()
        {
            Vector2 screen = GetScreenPosition();
            float depth = Mathf.Abs(_cam.transform.position.z);
            var world = _cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            world.z = 0f;
            return world;
        }

        void DropDraggedItem()
        {
            var item = _dragItem;
            _dragItem = null;
            item.SetDragging(false);

            if (!TryWorldToCell(item.transform.position, out int tx, out int ty))
            {
                SnapToCell(item);
                return;
            }

            if (tx == item.X && ty == item.Y)
            {
                SnapToCell(item);
                return;
            }

            var target = _grid[tx, ty];
            if (target == null)
            {
                MoveItem(item, tx, ty);
                AfterBoardChanged();
                return;
            }

            if (target != item && target.Rank == item.Rank && item.Rank < ItemCatalog.MaxRank)
            {
                SpawnMergeBurst(target.transform.position, ItemCatalog.GetColor(target.Rank + 1));
                MergeInto(target, item);
                AfterBoardChanged();
                return;
            }

            SnapToCell(item);
        }

        void SpawnMergeBurst(Vector3 pos, Color color)
        {
            var ring = new GameObject("MergeRing");
            ring.transform.position = pos;
            ring.transform.localScale = Vector3.one * 0.4f;
            var ringSr = ring.AddComponent<SpriteRenderer>();
            ringSr.sprite = _kit.Ring;
            ringSr.color = new Color(color.r, color.g, color.b, 0.85f);
            ringSr.sortingOrder = 36;
            ring.AddComponent<ExpandingRing>().Setup(2.4f, 0.35f);

            var flash = new GameObject("MergeFlash");
            flash.transform.position = pos;
            flash.transform.localScale = Vector3.one * 1.2f;
            var flashSr = flash.AddComponent<SpriteRenderer>();
            flashSr.sprite = _kit.GlowHalo;
            flashSr.color = new Color(color.r, color.g, color.b, 0.7f);
            flashSr.sortingOrder = 34;
            flash.AddComponent<BurstParticle>().Setup(Vector2.zero, 0.28f);

            for (int i = 0; i < 12; i++)
            {
                var go = new GameObject("Burst");
                go.transform.position = pos;
                go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _kit.Star;
                sr.color = Color.Lerp(color, Color.white, Random.Range(0f, 0.4f));
                sr.sortingOrder = 35;
                var burst = go.AddComponent<BurstParticle>();
                float ang = i / 12f * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
                burst.Setup(new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(1.4f, 2.6f), 0.5f);
            }
        }

        void MergeInto(BoardItem keep, BoardItem consumed)
        {
            int fromRank = keep.Rank;
            _grid[consumed.X, consumed.Y] = null;
            Object.Destroy(consumed.gameObject);

            int newRank = fromRank + 1;
            keep.SetRank(newRank);
            SnapToCell(keep);

            if (newRank == _goalRank)
            {
                _createdGoalItems++;
                RefreshHud();
                if (_createdGoalItems >= _goalCount)
                    Win();
            }
        }

        void MoveItem(BoardItem item, int x, int y)
        {
            _grid[item.X, item.Y] = null;
            item.SetCell(x, y);
            _grid[x, y] = item;
            SnapToCell(item);
        }

        void SnapToCell(BoardItem item) => item.transform.position = CellToWorld(item.X, item.Y);

        public void TrySpendEnergyAndSpawn()
        {
            if (_gameOver || _won) return;
            if (_energy <= 0)
            {
                RefreshHud();
                return;
            }

            int rank = Random.Range(0, 100) < rank2SpawnChancePercent ? 2 : 1;
            if (!TrySpawnRank(rank))
            {
                // Поле полное — не тратим энергию, просто подсказка
                RefreshHud();
                return;
            }

            _energy--;
            RefreshHud();
            AfterBoardChanged();
        }

        bool TrySpawnRank(int rank)
        {
            if (!TryFindEmpty(out int x, out int y)) return false;

            var go = new GameObject($"Item_R{rank}");
            go.transform.SetParent(transform, false);
            var item = go.AddComponent<BoardItem>();
            item.Init(rank, x, y, _kit);
            item.transform.position = CellToWorld(x, y);
            _grid[x, y] = item;
            item.PlayPop();
            return true;
        }

        bool TryFindEmpty(out int x, out int y)
        {
            var empties = new List<(int x, int y)>();
            for (int yy = 0; yy < Height; yy++)
            for (int xx = 0; xx < Width; xx++)
                if (_grid[xx, yy] == null)
                    empties.Add((xx, yy));

            if (empties.Count == 0)
            {
                x = y = 0;
                return false;
            }

            var pick = empties[Random.Range(0, empties.Count)];
            x = pick.x;
            y = pick.y;
            return true;
        }

        void AfterBoardChanged()
        {
            RefreshHud();
            CheckLose();
        }

        void CheckLose()
        {
            // Жёсткий проигрыш только если поле забито и нечего мержить.
            // Если просто кончилась энергия — предлагаем refill, не "убиваем" раунд.
            if (_won || _gameOver) return;
            if (HasAnyMerge()) return;
            if (HasEmptyCell()) return;
            Lose();
        }

        bool HasEmptyCell()
        {
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (_grid[x, y] == null) return true;
            return false;
        }

        bool HasAnyMerge()
        {
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var a = _grid[x, y];
                if (a == null || a.Rank >= ItemCatalog.MaxRank) continue;
                for (int yy = 0; yy < Height; yy++)
                for (int xx = 0; xx < Width; xx++)
                {
                    if (xx == x && yy == y) continue;
                    var b = _grid[xx, yy];
                    if (b != null && b.Rank == a.Rank) return true;
                }
            }
            return false;
        }

        void Win()
        {
            _won = true;
            _gameOver = false;
            RefreshHud();
            Debug.Log($"Nestglow: победа на уровне {_levelIndex + 1}!");
        }

        void Lose()
        {
            _gameOver = true;
            _won = false;
            RefreshHud();
            Debug.Log("Nestglow: поражение — поле заполнено.");
        }

        void RebuildRound()
        {
            if (_dragItem != null)
            {
                _dragItem.SetDragging(false);
                _dragItem = null;
            }

            foreach (Transform child in transform)
                Object.Destroy(child.gameObject);
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                _grid[x, y] = null;

            if (_hudRoot != null) Object.Destroy(_hudRoot.gameObject);
            _hudRoot = null;
            _title = null;
            _energyValue = null;
            _goalValue = null;
            _levelLabel = null;
            _hint = null;
            _spawnLabel = null;
            _spawnButton = null;

            StartRound(rebuildWorld: true);
        }

        void RefreshHud()
        {
            if (_energyValue != null)
                _energyValue.text = _energy.ToString();

            if (_goalValue != null)
                _goalValue.text = $"{ItemCatalog.GetName(_goalRank)}  {_createdGoalItems}/{_goalCount}";

            if (_levelLabel != null)
                _levelLabel.text = $"УРОВЕНЬ  {_levelIndex + 1} / {Levels.Length}";

            bool needRefill = _energy <= 0 && !_won && !_gameOver;

            if (_hint != null)
            {
                if (_won)
                {
                    bool last = _levelIndex >= Levels.Length - 1;
                    _hint.text = last
                        ? "все уровни пройдены"
                        : "победа — можно идти дальше";
                    _hint.color = NestglowTheme.Accent;
                }
                else if (_gameOver)
                {
                    _hint.text = "поле заполнено — попробуй снова";
                    _hint.color = NestglowTheme.Danger;
                }
                else if (needRefill)
                {
                    _hint.text = "свет закончился — продолжи кнопкой внизу";
                    _hint.color = NestglowTheme.Honey;
                }
                else
                {
                    _hint.text = "соединяй одинаковые огоньки";
                    _hint.color = NestglowTheme.Ink2;
                }
            }

            if (_spawnLabel != null)
            {
                if (_won)
                {
                    bool last = _levelIndex >= Levels.Length - 1;
                    _spawnLabel.text = last ? "ИГРАТЬ СНАЧАЛА" : "СЛЕДУЮЩИЙ УРОВЕНЬ";
                    _spawnLabel.color = NestglowTheme.AccentInk;
                }
                else if (_gameOver)
                {
                    _spawnLabel.text = "ЕЩЁ РАЗ";
                    _spawnLabel.color = NestglowTheme.Ink;
                }
                else if (_energy <= 0)
                {
                    _spawnLabel.text = $"ЕЩЁ СВЕТА  +{refillEnergyAmount}";
                    _spawnLabel.color = NestglowTheme.AccentInk;
                }
                else
                {
                    _spawnLabel.text = "ДОСТАТЬ СВЕТ";
                    _spawnLabel.color = NestglowTheme.AccentInk;
                }
            }

            if (_spawnButton != null)
            {
                if (_won)
                    _spawnButton.color = NestglowTheme.WithAlpha(NestglowTheme.Accent, 0.95f);
                else if (_gameOver)
                    _spawnButton.color = NestglowTheme.WithAlpha(NestglowTheme.Accent2, 0.85f);
                else if (_energy <= 0)
                    _spawnButton.color = NestglowTheme.WithAlpha(NestglowTheme.Honey, 0.92f);
                else
                    _spawnButton.color = NestglowTheme.WithAlpha(NestglowTheme.Accent, 0.92f);
            }
        }

        Vector3 GetOrigin()
        {
            return new Vector3(
                -(Width - 1) * cellSize * 0.5f,
                -(Height - 1) * cellSize * 0.5f,
                0f);
        }

        Vector3 CellToWorld(int x, int y)
        {
            var o = GetOrigin();
            return new Vector3(o.x + x * cellSize, o.y + y * cellSize, 0f);
        }

        bool TryWorldToCell(Vector3 world, out int x, out int y)
        {
            var o = GetOrigin();
            x = Mathf.RoundToInt((world.x - o.x) / cellSize);
            y = Mathf.RoundToInt((world.y - o.y) / cellSize);
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }
    }

    public class AmbientDrift : MonoBehaviour
    {
        Vector3 _start;
        float _amp;
        float _speed;
        float _phase;
        bool _twinkle;
        SpriteRenderer _sr;
        float _baseAlpha;

        public void Setup(float amp, float speed, bool twinkle = false)
        {
            _start = transform.position;
            _amp = amp;
            _speed = speed;
            _phase = Random.Range(0f, Mathf.PI * 2f);
            _twinkle = twinkle;
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseAlpha = _sr.color.a;
        }

        void Update()
        {
            _phase += Time.deltaTime * _speed;
            transform.position = _start + new Vector3(
                Mathf.Sin(_phase) * _amp,
                Mathf.Cos(_phase * 0.7f) * _amp * 0.8f,
                0f);

            if (_twinkle && _sr != null)
            {
                var c = _sr.color;
                c.a = _baseAlpha * (0.55f + 0.45f * Mathf.Sin(_phase * 2.3f + _start.x));
                _sr.color = c;
            }
        }
    }

    public class BurstParticle : MonoBehaviour
    {
        Vector2 _vel;
        float _life;
        float _age;
        SpriteRenderer _sr;
        Color _color;

        public void Setup(Vector2 velocity, float life)
        {
            _vel = velocity;
            _life = life;
            _sr = GetComponent<SpriteRenderer>();
            _color = _sr.color;
        }

        void Update()
        {
            _age += Time.deltaTime;
            float t = _age / _life;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += (Vector3)(_vel * Time.deltaTime);
            _vel *= 0.95f;
            transform.localScale *= 0.98f;
            if (_sr != null)
            {
                var c = _color;
                c.a = _color.a * (1f - t);
                _sr.color = c;
            }
        }
    }

    public class ExpandingRing : MonoBehaviour
    {
        float _target;
        float _life;
        float _age;
        SpriteRenderer _sr;
        Color _color;

        public void Setup(float targetScale, float life)
        {
            _target = targetScale;
            _life = life;
            _sr = GetComponent<SpriteRenderer>();
            _color = _sr.color;
        }

        void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _life);
            float s = Mathf.Lerp(0.4f, _target, t);
            transform.localScale = Vector3.one * s;
            if (_sr != null)
            {
                var c = _color;
                c.a = _color.a * (1f - t);
                _sr.color = c;
            }
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
