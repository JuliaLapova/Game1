using UnityEngine;

namespace Nestglow
{
    public class BoardItem : MonoBehaviour
    {
        public int Rank { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        VisualKit _kit;
        SpriteRenderer _wideGlow;
        SpriteRenderer _glow;
        SpriteRenderer _ring;
        SpriteRenderer _ring2;
        SpriteRenderer _core;
        SpriteRenderer _inner;
        SpriteRenderer _sheen;
        TextMesh _label;
        TextMesh _labelShadow;

        float _baseScale = 1f;
        float _pulse;
        float _noiseSeed;
        float _glowBaseA;
        float _wideBaseA;
        float _coreBaseA = 1f;
        Color _coreBaseColor;
        Color _innerBaseColor;
        Color _glowBaseColor;
        Color _wideBaseColor;
        bool _dragging;
        bool _popping;

        public void Init(int rank, int x, int y, VisualKit kit)
        {
            Rank = rank;
            X = x;
            Y = y;
            _kit = kit;
            _noiseSeed = Random.Range(0f, 100f);

            _wideGlow = MakeChild("WideGlow", kit.WideGlow, 6);
            _glow = MakeChild("Glow", kit.GlowHalo, 7);
            _ring2 = MakeChild("Ring2", kit.Ring, 8);
            _ring = MakeChild("Ring", kit.Ring, 9);

            _core = gameObject.AddComponent<SpriteRenderer>();
            _core.sprite = kit.SoftOrb;
            _core.sortingOrder = 10;

            _inner = MakeChild("Inner", kit.CoreOrb, 11);
            _inner.transform.localScale = Vector3.one * 0.55f;

            var sheenGo = new GameObject("Sheen");
            sheenGo.transform.SetParent(transform, false);
            sheenGo.transform.localPosition = new Vector3(-0.14f, 0.18f, 0f);
            sheenGo.transform.localScale = Vector3.one * 0.28f;
            _sheen = sheenGo.AddComponent<SpriteRenderer>();
            _sheen.sprite = kit.SoftOrb;
            _sheen.color = new Color(1f, 1f, 1f, 0.28f);
            _sheen.sortingOrder = 12;

            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.48f;
            col.isTrigger = true;

            _labelShadow = MakeLabel("LabelShadow", new Vector3(0.02f, -0.04f, 0f), NestglowTheme.WithAlpha(NestglowTheme.Paper, 0.45f), 13);
            _label = MakeLabel("Label", new Vector3(0f, -0.01f, 0f), NestglowTheme.AccentInk, 14);

            ApplyVisual();
            // у каждого своя фаза — мерцают не синхронно
            _pulse = Random.Range(0f, Mathf.PI * 2f);
        }

        SpriteRenderer MakeChild(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        TextMesh MakeLabel(string name, Vector3 localPos, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.105f;
            tm.fontSize = 44;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sortingOrder = order;
            return tm;
        }

        void Update()
        {
            // Ранг 1 — медленное «свечное» мерцание; выше — чуть живее, но всё ещё мягко
            float speed = Rank <= 1 ? 0.48f : 0.65f + Rank * 0.06f;
            _pulse += Time.deltaTime * speed;

            float flicker = SampleFlicker();

            if (!_popping)
            {
                float breathe = 1f + (flicker - 1f) * 0.045f;
                float dragBoost = _dragging ? 1.08f : 1f;
                transform.localScale = Vector3.one * (_baseScale * breathe * dragBoost);
            }

            ApplyFlickerToRenderers(flicker);

            // мягкое дыхание ореола (крупнее и спокойнее)
            float glowScale = Rank <= 1
                ? 1.95f + Mathf.Sin(_pulse * 0.9f) * 0.08f
                : 1.85f + Rank * 0.03f + Mathf.Sin(_pulse * 1.1f) * 0.06f;
            if (_glow != null)
                _glow.transform.localScale = Vector3.one * glowScale * (0.96f + (flicker - 1f) * 0.35f);

            if (_wideGlow != null)
            {
                float wide = Rank <= 1 ? 3.1f : 2.9f + Rank * 0.04f;
                _wideGlow.transform.localScale = Vector3.one * wide * (0.97f + (flicker - 1f) * 0.25f);
            }

            if (_ring != null && _ring.enabled)
                _ring.transform.localRotation = Quaternion.Euler(0f, 0f, _pulse * 4f);
            if (_ring2 != null && _ring2.enabled)
                _ring2.transform.localRotation = Quaternion.Euler(0f, 0f, -_pulse * 2.5f);
        }

        float SampleFlicker()
        {
            // Медленная волна + лёгкий шум (как живой огонёк)
            float wave = Mathf.Sin(_pulse);
            float wave2 = Mathf.Sin(_pulse * 1.7f + 1.1f);
            float noise = Mathf.PerlinNoise(_noiseSeed, Time.time * (Rank <= 1 ? 0.35f : 0.55f));
            noise = (noise - 0.5f) * 2f; // -1..1

            float amp = Rank <= 1 ? 0.28f : 0.14f + Rank * 0.012f;
            float flicker = 1f
                            + wave * amp * 0.55f
                            + wave2 * amp * 0.25f
                            + noise * amp * 0.35f;

            return Mathf.Clamp(flicker, 0.72f, 1.28f);
        }

        void ApplyFlickerToRenderers(float flicker)
        {
            if (_glow != null)
            {
                var c = _glowBaseColor;
                c.a = Mathf.Clamp01(_glowBaseA * flicker);
                _glow.color = c;
            }

            if (_wideGlow != null)
            {
                var c = _wideBaseColor;
                c.a = Mathf.Clamp01(_wideBaseA * (0.85f + (flicker - 1f) * 0.9f));
                _wideGlow.color = c;
            }

            if (_core != null)
            {
                // сам шарик чуть «дышит» яркостью, не дёргаясь
                float bright = Mathf.Lerp(0.88f, 1.08f, (flicker - 0.72f) / (1.28f - 0.72f));
                var c = _coreBaseColor * bright;
                c.a = _coreBaseA;
                _core.color = c;
            }

            if (_inner != null)
            {
                var c = _innerBaseColor;
                c.a = Mathf.Clamp01(_innerBaseColor.a * (0.85f + (flicker - 1f) * 0.4f));
                float bright = Mathf.Lerp(0.9f, 1.1f, (flicker - 0.72f) / 0.56f);
                c.r = Mathf.Clamp01(_innerBaseColor.r * bright);
                c.g = Mathf.Clamp01(_innerBaseColor.g * bright);
                c.b = Mathf.Clamp01(_innerBaseColor.b * bright);
                _inner.color = c;
            }
        }

        public void SetCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void SetRank(int rank)
        {
            Rank = rank;
            ApplyVisual();
            PlayPop();
        }

        void ApplyVisual()
        {
            var color = ItemCatalog.GetColor(Rank);
            var glow = ItemCatalog.GetGlowColor(Rank);

            _coreBaseColor = color;
            _coreBaseA = 1f;
            if (_core != null) _core.color = color;

            if (_inner != null)
            {
                _innerBaseColor = Color.Lerp(Color.white, color, 0.35f);
                _innerBaseColor.a = 0.7f;
                _inner.color = _innerBaseColor;
            }

            // мягче базовый glow; у ранга 1 чуть заметнее «живой» ореол
            _glowBaseA = Rank <= 1 ? 0.26f : 0.18f + Rank * 0.018f;
            _wideBaseA = Rank <= 1 ? 0.14f : 0.08f + Rank * 0.01f;

            _glowBaseColor = new Color(glow.r, glow.g, glow.b, _glowBaseA);
            _wideBaseColor = new Color(glow.r, glow.g, glow.b, _wideBaseA);

            if (_glow != null) _glow.color = _glowBaseColor;
            if (_wideGlow != null) _wideGlow.color = _wideBaseColor;

            bool showRing = Rank >= 3;
            bool showRing2 = Rank >= 6;
            if (_ring != null)
            {
                _ring.enabled = showRing;
                _ring.color = new Color(color.r, color.g, color.b, 0.55f);
                _ring.transform.localScale = Vector3.one * (1.05f + Rank * 0.01f);
            }
            if (_ring2 != null)
            {
                _ring2.enabled = showRing2;
                _ring2.color = new Color(1f, 0.95f, 0.8f, 0.28f);
                _ring2.transform.localScale = Vector3.one * 1.28f;
            }

            string text = Rank.ToString();
            if (_label != null) _label.text = text;
            if (_labelShadow != null) _labelShadow.text = text;

            _baseScale = 0.74f + Rank * 0.038f;
            if (!_popping)
                transform.localScale = Vector3.one * _baseScale;
        }

        public void SetDragging(bool dragging)
        {
            _dragging = dragging;
            int order = dragging ? 40 : 10;
            SetOrders(order);
        }

        void SetOrders(int coreOrder)
        {
            if (_wideGlow != null) _wideGlow.sortingOrder = coreOrder - 4;
            if (_glow != null) _glow.sortingOrder = coreOrder - 3;
            if (_ring2 != null) _ring2.sortingOrder = coreOrder - 2;
            if (_ring != null) _ring.sortingOrder = coreOrder - 1;
            if (_core != null) _core.sortingOrder = coreOrder;
            if (_inner != null) _inner.sortingOrder = coreOrder + 1;
            if (_sheen != null) _sheen.sortingOrder = coreOrder + 2;
            if (_labelShadow != null) _labelShadow.GetComponent<MeshRenderer>().sortingOrder = coreOrder + 3;
            if (_label != null) _label.GetComponent<MeshRenderer>().sortingOrder = coreOrder + 4;
        }

        public void PlayPop()
        {
            StopAllCoroutines();
            StartCoroutine(PopRoutine());
        }

        System.Collections.IEnumerator PopRoutine()
        {
            _popping = true;
            float t = 0f;
            float from = _baseScale * 0.65f;
            float peak = _baseScale * 1.18f;
            float to = _baseScale;
            while (t < 1f)
            {
                t += Time.deltaTime * 7.5f;
                float s = t < 0.5f
                    ? Mathf.SmoothStep(from, peak, t / 0.5f)
                    : Mathf.SmoothStep(peak, to, (t - 0.5f) / 0.5f);
                transform.localScale = Vector3.one * s;
                yield return null;
            }
            _popping = false;
        }
    }
}
