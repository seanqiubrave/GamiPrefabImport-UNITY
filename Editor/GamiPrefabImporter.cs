// GamiPrefabImporter.cs  v3.0.1
// Place in: Assets/GamiPrefabImport/Editor/
//
// Free Edition — Brave Games
//
// v3.0.1 — Fixed Unity 2022.3 compatibility (TextMeshPro 3.0.6 API).
// v3.0.0 — Free Edition. Removed all account / credit / payment logic.
//           Pure local ZIP-to-Prefab converter. No network calls. No tracking.
// v2.0.x — (legacy, removed)

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BraveGames.GamiPrefabImport.Editor
{
    public class GamiPrefabImporter : EditorWindow
    {
        private const string VERSION = "3.0.1";

        [MenuItem("Tools/Gami Prefab Importer")]
        public static void ShowWindow()
        {
            var w = GetWindow<GamiPrefabImporter>("Gami Prefab Importer");
            w.minSize = new Vector2(460, 420);
        }

        // ── Import state ──────────────────────────────────────────────────
        private string  _zipPath   = "";
        private bool    _debugMode = false;
        private Vector2 _logScroll;
        private string  _log       = "";

        // ── Output paths ──────────────────────────────────────────────────
        private const string OUT_DIR     = "Assets/GamiPrefabImport";
        private const string SPRITES_DIR = "Assets/GamiPrefabImport/Sprites";
        private const string BTN_DIR     = "Assets/GamiPrefabImport/Buttons";
        private const string PREFAB_NAME = "layout_export";
        private const string LOG_PATH    = "Assets/GamiPrefabImport/import_log.txt";

        // ── Layout ────────────────────────────────────────────────────────
        private const float BTN_WIDTH = 370f;
        private const float PAD_L = 28f, PAD_R = 28f, PAD_T = 14f, PAD_B = 34f;

        // ── Website (informational only — no API calls) ───────────────────
        private const string WEBSITE_URL = "https://arteditor.art";

        // ── Reusable GUIStyles ────────────────────────────────────────────
        private GUIStyle _sTitle;
        private GUIStyle _sVer;
        private GUIStyle _sMuted;
        private GUIStyle _sBold;
        private bool _stylesReady = false;

        private void OnEnable()
        {
            // Styles are created lazily in OnGUI to avoid issues with EditorStyles
            // not being available during OnEnable on some Unity versions.
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _sTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.95f, 0.95f, 1.00f) }
            };
            _sVer = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.65f, 0.70f, 0.80f) }
            };
            _sMuted = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.45f, 0.50f, 0.60f) },
                wordWrap = true
            };
            _sBold = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            _stylesReady = true;
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();
            GUILayout.Space(6);
            DrawImportSection();
            GUILayout.Space(6);
            DrawFooter();
            DrawLog();
        }

        // ─────────────────────────────────────────────────────────────────
        //  HEADER
        // ─────────────────────────────────────────────────────────────────
        private void DrawHeader()
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.07f, 0.10f, 0.20f, 1f);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = prev;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label("Gami Prefab Importer", _sTitle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("v" + VERSION, _sVer);
            GUILayout.Space(6);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.Label("by Brave Games · Free Edition", _sMuted);
            GUILayout.Space(6);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────────────────────────
        //  IMPORT
        // ─────────────────────────────────────────────────────────────────
        private void DrawImportSection()
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.07f, 0.10f, 0.20f, 1f);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = prev;
            GUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUILayout.Label("Import your ZIP from arteditor.art", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.75f, 0.85f, 1.00f) }
            });
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
            EditorGUILayout.EndVertical();

            GUILayout.Space(6);

            // ZIP path row
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            _zipPath = EditorGUILayout.TextField(_zipPath);
            if (GUILayout.Button("Browse", GUILayout.Width(64), GUILayout.Height(20)))
            {
                string p = EditorUtility.OpenFilePanel("Select ZIP", "", "zip");
                if (!string.IsNullOrEmpty(p)) _zipPath = p;
            }
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Debug toggle
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            _debugMode = EditorGUILayout.Toggle("Debug log", _debugMode, GUILayout.Width(140));
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Big import button
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.10f, 0.40f, 1.00f);
            var importBtn = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                normal = { textColor = Color.white },
                hover = { textColor = Color.white },
                active = { textColor = Color.white }
            };
            if (GUILayout.Button("Import and Generate Prefab", importBtn, GUILayout.Height(40)))
            {
                _log = "";
                try { RunImport(); }
                catch (Exception e) { ALog($"EXCEPTION: {e}"); }
                Directory.CreateDirectory(OUT_DIR);
                File.WriteAllText(LOG_PATH, _log);
                AssetDatabase.Refresh();
            }
            GUI.backgroundColor = oldBg;
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
        }

        // ─────────────────────────────────────────────────────────────────
        //  FOOTER
        // ─────────────────────────────────────────────────────────────────
        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUILayout.Label("Need source ZIPs? Visit arteditor.art (free, no signup required)", _sMuted);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open Website", GUILayout.Width(110), GUILayout.Height(22)))
                Application.OpenURL(WEBSITE_URL);
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
        }

        // ─────────────────────────────────────────────────────────────────
        //  LOG
        // ─────────────────────────────────────────────────────────────────
        private void DrawLog()
        {
            if (string.IsNullOrEmpty(_log)) return;
            GUILayout.Space(4);
            var r2 = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r2, new Color(0.07f, 0.10f, 0.20f, 1f));
            GUILayout.Space(2);
            _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.ExpandHeight(true), GUILayout.MinHeight(72));
            var ls = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 10,
                normal = { textColor = new Color(0.22f, 0.26f, 0.34f) }
            };
            GUILayout.TextArea(_log, ls, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
        }

        // ─────────────────────────────────────────────────────────────────
        //  LOG HELPERS
        // ─────────────────────────────────────────────────────────────────
        private void ALog(string s) { _log += s + "\n"; Debug.Log("[GamiPrefab] " + s); }
        private void WLog(string s) { _log += "WARN: " + s + "\n"; Debug.LogWarning("[GamiPrefab] " + s); }
        private void DLog(string s) { if (_debugMode) { _log += "  · " + s + "\n"; Debug.Log("[GamiPrefab] " + s); } }

        // ─────────────────────────────────────────────────────────────────
        //  IMPORT CORE
        // ─────────────────────────────────────────────────────────────────
        private void RunImport()
        {
            if (string.IsNullOrEmpty(_zipPath) || !File.Exists(_zipPath))
            {
                ALog("No valid ZIP selected.");
                return;
            }

            ALog($"=== Import START  ({DateTime.Now:HH:mm:ss}) ===");
            ALog($"ZIP: {_zipPath}");

            byte[] raw;
            try { raw = File.ReadAllBytes(_zipPath); }
            catch (Exception e) { ALog($"Cannot read file: {e.Message}"); return; }

            Dictionary<string, byte[]> outerEntries;
            try { outerEntries = ParseZipBinary(raw); }
            catch (Exception e) { ALog($"ZIP parse error: {e.Message}"); return; }

            DLog($"Outer entries: {outerEntries.Count}");
            foreach (var k in outerEntries.Keys) DLog($"  entry: {k}  ({outerEntries[k].Length}B)");

            bool hasLayoutJson = outerEntries.ContainsKey("layout.json");
            bool hasInnerZips = AnyEndsWith(outerEntries, ".zip");

            if (hasLayoutJson && !hasInnerZips)
            {
                string jsonText = Encoding.UTF8.GetString(outerEntries["layout.json"]);
                if (jsonText.Contains("\"layers\"") && !jsonText.Contains("\"rootNode\":{\"id\":\"root_000\""))
                {
                    ALog("Format detected: GamiPrefabEditor");
                    RunGamiImport(_zipPath);
                    return;
                }
                ALog("Format detected: Batch Button single _ui.zip");
                Directory.CreateDirectory(BTN_DIR);
                AssetDatabase.Refresh();
                ImportBatchButtonEntries(outerEntries, Path.GetFileNameWithoutExtension(_zipPath).Replace("_ui", ""));
                AssetDatabase.Refresh();
                ALog("=== Import COMPLETE ===");
                return;
            }

            if (!hasLayoutJson && hasInnerZips)
            {
                ALog("Format detected: Batch Button bundle ZIP");
                Directory.CreateDirectory(BTN_DIR);
                AssetDatabase.Refresh();
                int ok = 0, fail = 0;
                foreach (var kv in outerEntries)
                {
                    if (!kv.Key.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;
                    string stem = Path.GetFileNameWithoutExtension(kv.Key).Replace("_ui", "");
                    try
                    {
                        ImportBatchButtonEntries(ParseZipBinary(kv.Value), stem);
                        ok++;
                    }
                    catch (Exception e) { WLog($"Failed {kv.Key}: {e.Message}"); fail++; }
                }
                AssetDatabase.Refresh();
                ALog($"Bundle import: {ok} prefab{(ok != 1 ? "s" : "")} created" + (fail > 0 ? $", {fail} failed" : ""));
                ALog("=== Import COMPLETE ===");
                return;
            }

            ALog("Format: attempting GamiPrefabEditor pipeline");
            RunGamiImport(_zipPath);
        }

        // ─────────────────────────────────────────────────────────────────
        //  BATCH BUTTON IMPORT
        // ─────────────────────────────────────────────────────────────────
        private void ImportBatchButtonEntries(Dictionary<string, byte[]> entries, string stem)
        {
            if (!entries.ContainsKey("layout.json")) throw new Exception("layout.json missing");
            var layout = MiniJson.Deserialize(Encoding.UTF8.GetString(entries["layout.json"])) as Dictionary<string, object>
                         ?? throw new Exception("JSON parse failed");
            var rootNode = GetDict(layout, "rootNode") ?? throw new Exception("rootNode missing");
            var kids = GetList(rootNode, "children");
            var btnNode = (kids != null && kids.Count > 0) ? (kids[0] as Dictionary<string, object> ?? rootNode) : rootNode;

            var csz = GetDict(layout, "canvasSize");
            float ow = csz != null ? GetF(csz, "width", 68f) : 68f;
            float oh = csz != null ? GetF(csz, "height", 178f) : 178f;
            float btnW = BTN_WIDTH, btnH = ow > oh ? Mathf.Round(oh / ow * btnW) : oh;

            var assetPaths = new Dictionary<string, string>();
            foreach (var kv in entries)
            {
                if (!kv.Key.StartsWith("assets/") || !kv.Key.EndsWith(".png")) continue;
                string id = Path.GetFileNameWithoutExtension(kv.Key);
                string dest = $"{BTN_DIR}/{id}.png";
                File.WriteAllBytes(Path.GetFullPath(dest), kv.Value);
                assetPaths[id] = dest;
                DLog($"Saved PNG → {dest}");
            }
            AssetDatabase.Refresh();
            ApplyBatchTextureSettings(btnNode, assetPaths);
            AssetDatabase.Refresh();

            var go = BuildBatchButtonPrefab(stem, btnNode, btnW, btnH, assetPaths);
            string prefabPath = $"{BTN_DIR}/{stem}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            ALog($"Button prefab: {prefabPath}  ({btnW}×{btnH})");
        }

        private void ApplyBatchTextureSettings(Dictionary<string, object> node, Dictionary<string, string> assetPaths)
        {
            var asset = GetDict(node, "asset");
            if (asset != null)
            {
                string id = GetStr(asset, "assetId");
                if (id != null && assetPaths.TryGetValue(id, out string pp))
                {
                    var ti = AssetImporter.GetAtPath(pp) as TextureImporter;
                    if (ti != null)
                    {
                        ti.textureType = TextureImporterType.Sprite;
                        ti.spriteImportMode = SpriteImportMode.Single;
                        ti.alphaIsTransparency = true;
                        ti.mipmapEnabled = false;
                        var smd = GetDict(node, "spriteMetaData");
                        ti.spritePixelsPerUnit = smd != null ? GetF(smd, "pixelsPerUnit", 100f) : 100f;
                        if (smd != null && smd.ContainsKey("border"))
                        {
                            var b = smd["border"] as List<object>;
                            if (b != null && b.Count == 4)
                            {
                                float L = ToF(b[0]), B2 = ToF(b[1]), R = ToF(b[2]), T = ToF(b[3]);
                                if (L > 0 || B2 > 0 || R > 0 || T > 0)
                                {
                                    ti.spriteBorder = new Vector4(L, B2, R, T);
                                    DLog($"  9-slice {id}: L={L} B={B2} R={R} T={T}");
                                }
                            }
                        }
                        ti.SaveAndReimport();
                    }
                }
            }
            var k2 = GetList(node, "children");
            if (k2 != null)
                foreach (var c in k2)
                    if (c is Dictionary<string, object> ch)
                        ApplyBatchTextureSettings(ch, assetPaths);
        }

        private GameObject BuildBatchButtonPrefab(string name, Dictionary<string, object> btnNode, float w, float h, Dictionary<string, string> assetPaths)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);

            var asset = GetDict(btnNode, "asset");
            string aid = asset != null ? GetStr(asset, "assetId") : null;
            string sm = asset != null ? (GetStr(asset, "scaleMode") ?? "simple") : "simple";

            if (aid != null && assetPaths.TryGetValue(aid, out string pp))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(pp);
                if (spr != null)
                {
                    var img = go.AddComponent<Image>();
                    img.sprite = spr;
                    img.color = Color.white;
                    img.raycastTarget = true;
                    img.type = sm == "sliced" ? Image.Type.Sliced : Image.Type.Simple;
                    img.preserveAspect = sm != "sliced";
                    var btn = go.AddComponent<Button>();
                    btn.targetGraphic = img;
                    ApplyBatchButtonColors(btn);
                }
            }
            else
            {
                go.AddComponent<Button>();
                ApplyBatchButtonColors(go.GetComponent<Button>());
            }

            var tgo = new GameObject("Text (TMP)");
            tgo.transform.SetParent(go.transform, false);
            var trt = tgo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.offsetMin = new Vector2(PAD_L, PAD_B);
            trt.offsetMax = new Vector2(-PAD_R, -PAD_T);

            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = "Text";
            tmp.color = Color.white;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18f;
            tmp.fontSizeMax = 75f;
            tmp.raycastTarget = false;
            tmp.alignment = TextAlignmentOptions.Center;
            return go;
        }

        private static void ApplyBatchButtonColors(Button btn)
        {
            if (btn == null) return;
            var so = new SerializedObject(btn);
            void SC(string p, Color c) { var x = so.FindProperty(p); if (x != null) x.colorValue = c; }
            SC("m_Colors.m_NormalColor", new Color(1, 1, 1, 1));
            SC("m_Colors.m_HighlightedColor", new Color(.92f, .92f, .92f, 1));
            SC("m_Colors.m_PressedColor", new Color(.7f, .7f, .7f, 1));
            SC("m_Colors.m_SelectedColor", new Color(1, 1, 1, 1));
            SC("m_Colors.m_DisabledColor", new Color(.5f, .5f, .5f, .5f));
            var mp = so.FindProperty("m_Colors.m_ColorMultiplier");
            if (mp != null) mp.floatValue = 1f;
            var fp = so.FindProperty("m_Colors.m_FadeDuration");
            if (fp != null) fp.floatValue = 0.08f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ─────────────────────────────────────────────────────────────────
        //  GAMI PREFAB EDITOR IMPORT
        // ─────────────────────────────────────────────────────────────────
        private void RunGamiImport(string zipPath)
        {
            string jsonText = null;
            var pngBytes = new Dictionary<string, byte[]>();
            using (var zip = ZipFile.OpenRead(zipPath))
            {
                foreach (var e in zip.Entries)
                {
                    if (e.Name == "layout.json")
                    {
                        using var sr = new StreamReader(e.Open());
                        jsonText = sr.ReadToEnd();
                        DLog("layout.json read OK");
                    }
                    else if (e.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        using var ms = new MemoryStream();
                        e.Open().CopyTo(ms);
                        pngBytes[e.Name] = ms.ToArray();
                        DLog($"PNG entry: {e.Name}  ({ms.Length}B)");
                    }
                }
            }
            if (jsonText == null) { ALog("ERROR: layout.json not found."); return; }
            var rawJson = MiniJson.Deserialize(jsonText) as Dictionary<string, object>;
            if (rawJson == null) { ALog("ERROR: JSON parse failed."); return; }

            string jsonVersion = GetStr(rawJson, "version") ?? "?";
            var rawRootNode = GetDict(rawJson, "rootNode");
            string rootId = rawRootNode != null ? (GetStr(rawRootNode, "id") ?? GetStr(rawRootNode, "name") ?? "?") : "?";
            ALog($"layout.json version: {jsonVersion}  root: {rootId}");

            var rawRootRect = rawRootNode != null ? GetDict(rawRootNode, "rect") : null;
            float rootW = rawRootRect != null ? GetF(rawRootRect, "width", 1080f) : 1080f;
            float rootH = rawRootRect != null ? GetF(rawRootRect, "height", 180f) : 180f;

            var allRawLayers = new List<Dictionary<string, object>>();
            var rawBtnLayers = new List<Dictionary<string, object>>();
            float hlgSpacing = -3f;
            int hlgPT = 0, hlgPB = 0, hlgPL = 0, hlgPR = 0;

            if (rawRootNode != null)
            {
                var rl = GetList(rawRootNode, "layers");
                if (rl != null)
                {
                    foreach (var item in rl)
                    {
                        var layer = item as Dictionary<string, object>;
                        if (layer == null) continue;
                        string lid = GetStr(layer, "id") ?? "";
                        string lname = GetStr(layer, "name") ?? "";
                        string ltype = GetStr(layer, "type") ?? "";
                        string lpid = GetStr(layer, "parentId") ?? "";
                        DLog($"  Layer: id={lid} name={lname} type={ltype} parentId={lpid}");
                        allRawLayers.Add(layer);
                        if (string.IsNullOrEmpty(lpid) || lpid == rootId)
                        {
                            var lg = GetDict(layer, "layoutGroup");
                            if (lg != null)
                            {
                                hlgSpacing = GetF(lg, "spacing", -3f);
                                var lgPad = GetDict(lg, "padding");
                                if (lgPad != null)
                                {
                                    hlgPT = (int)GetF(lgPad, "top", 0f);
                                    hlgPB = (int)GetF(lgPad, "bottom", 0f);
                                    hlgPL = (int)GetF(lgPad, "left", 0f);
                                    hlgPR = (int)GetF(lgPad, "right", 0f);
                                }
                                DLog($"  HLG: spacing={hlgSpacing} pad=T{hlgPT}/B{hlgPB}/L{hlgPL}/R{hlgPR}");
                            }
                        }
                        bool isBtn = ltype == "button" || ltype == "btn" || lname.StartsWith("Btn_", StringComparison.OrdinalIgnoreCase);
                        if (isBtn) rawBtnLayers.Add(layer);
                    }
                }
                else WLog("rootNode.layers is null.");
            }

            var childrenByPID = new Dictionary<string, List<Dictionary<string, object>>>();
            foreach (var layer in allRawLayers)
            {
                string pid = GetStr(layer, "parentId") ?? "";
                if (string.IsNullOrEmpty(pid)) continue;
                if (!childrenByPID.ContainsKey(pid)) childrenByPID[pid] = new List<Dictionary<string, object>>();
                childrenByPID[pid].Add(layer);
            }

            var sliceBySN = new Dictionary<string, SliceJson>();
            foreach (var layer in allRawLayers)
            {
                string lname = (GetStr(layer, "name") ?? "").ToLowerInvariant();
                if (lname != "bg") continue;
                string spName = GetStr(layer, "sprite") ?? "";
                var sa = GetList(layer, "slice");
                if (!string.IsNullOrEmpty(spName) && sa != null && sa.Count == 4 && !sliceBySN.ContainsKey(spName))
                    sliceBySN[spName] = new SliceJson { left = ToF(sa[0]), top = ToF(sa[1]), right = ToF(sa[2]), bottom = ToF(sa[3]) };
            }

            var btnLayers = new List<LayerJson>();
            foreach (var rb in rawBtnLayers)
            {
                string bid = GetStr(rb, "id") ?? "";
                var ch = childrenByPID.TryGetValue(bid, out var c2) ? c2 : null;
                btnLayers.Add(RawToLayerJson(rb, ch));
            }

            var layout = new LayoutJson { version = jsonVersion, rootNode = new LayerJson { rect = new RectJson { width = rootW, height = rootH } } };
            Directory.CreateDirectory(OUT_DIR);
            Directory.CreateDirectory(SPRITES_DIR);
            AssetDatabase.Refresh();
            var spriteMap = new Dictionary<string, Sprite>();

            foreach (var kv in pngBytes)
            {
                string filename = kv.Key, assetPath = $"{SPRITES_DIR}/{filename}";
                bool isPlaceholder = kv.Value.Length < 5120;
                if (isPlaceholder)
                {
                    DLog($"{filename}: placeholder ({kv.Value.Length}B)");
                    string bn = Path.GetFileNameWithoutExtension(filename);
                    string found = FindProjectSprite(bn);
                    if (found != null)
                    {
                        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(found);
                        if (sp != null) spriteMap[bn] = sp;
                        continue;
                    }
                    WLog($"{filename}: placeholder and no project sprite found");
                }
                File.WriteAllBytes(Path.GetFullPath(assetPath), kv.Value);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                var ti = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                if (ti != null)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.SaveAndReimport();
                }
                string baseSN = Path.GetFileNameWithoutExtension(filename);
                if (sliceBySN.TryGetValue(baseSN, out var si) && ti != null)
                {
                    ti.spriteBorder = new Vector4(si.left, si.bottom, si.right, si.top);
                    ti.SaveAndReimport();
                    DLog($"  9-slice {baseSN}");
                }
                else ApplySliceIfPresent(layout, filename, assetPath, ti);

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null) { spriteMap[baseSN] = sprite; ALog($"Sprite ready: {baseSN}"); }
                else WLog($"Failed to load sprite: {assetPath}");
            }

            DLog($"Total button layers parsed: {btnLayers.Count}");
            string prefabAssetPath = $"{OUT_DIR}/{PREFAB_NAME}.prefab";
            GameObject root;
            bool existed = File.Exists(prefabAssetPath);
            if (existed)
            {
                root = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath)) as GameObject;
                while (root.transform.childCount > 0) UnityEngine.Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
            }
            else root = new GameObject(PREFAB_NAME);

            var rootRT = EnsureComponent<RectTransform>(root);
            rootRT.anchorMin = rootRT.anchorMax = rootRT.pivot = new Vector2(0.5f, 0.5f);
            rootRT.sizeDelta = new Vector2(rootW, rootH);
            rootRT.anchoredPosition = Vector2.zero;

            var hlg = EnsureComponent<HorizontalLayoutGroup>(root);
            hlg.childAlignment = TextAnchor.LowerCenter;
            hlg.spacing = hlgSpacing;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;
            var hlgP = hlg.padding;
            hlgP.top = hlgPT; hlgP.bottom = hlgPB; hlgP.left = hlgPL; hlgP.right = hlgPR;
            hlg.padding = hlgP;

            Type ntgType = FindRuntimeType("GamiTabGroup");
            if (ntgType != null) { if (root.GetComponent(ntgType) == null) root.AddComponent(ntgType); }
            else WLog("GamiTabGroup type not found (optional runtime component).");

            ALog($"Buttons to create: {btnLayers.Count}");
            Type aebType = FindRuntimeType("GamiButton");
            if (aebType == null) WLog("GamiButton type not found (optional runtime component).");

            foreach (var btnLayer in btnLayers)
            {
                var btnGO = new GameObject(btnLayer.name);
                btnGO.transform.SetParent(root.transform, false);
                var btnRT = EnsureComponent<RectTransform>(btnGO);
                btnRT.anchorMin = Vector2.zero;
                btnRT.anchorMax = Vector2.one;
                btnRT.sizeDelta = Vector2.zero;
                var le = EnsureComponent<LayoutElement>(btnGO);
                le.preferredHeight = btnLayer.height > 0 ? btnLayer.height : 175f;
                le.flexibleWidth = 1f;
                le.flexibleHeight = 1f;
                var btn = EnsureComponent<Button>(btnGO);
                bool isED = btnLayer.drive_mode == "editor_driver";
                btn.transition = isED ? Selectable.Transition.None : Selectable.Transition.ColorTint;
                if (!isED)
                {
                    var cb = btn.colors;
                    cb.normalColor = HexToColor("#FFFFFF");
                    cb.highlightedColor = HexToColor("#F5F5F5");
                    cb.pressedColor = HexToColor("#C8C8C8");
                    cb.selectedColor = HexToColor("#FFFFFF");
                    cb.disabledColor = HexToColor("#C8C8C880");
                    cb.colorMultiplier = 1f;
                    cb.fadeDuration = 0.1f;
                    btn.colors = cb;
                }
                MonoBehaviour aebComp = null;
                if (aebType != null && isED)
                {
                    aebComp = btnGO.AddComponent(aebType) as MonoBehaviour;
                    DLog($"  Added GamiButton to {btnLayer.name}");
                }

                var bgGO = new GameObject("Bg");
                bgGO.transform.SetParent(btnGO.transform, false);
                var bgRT = EnsureComponent<RectTransform>(bgGO);
                bgRT.anchorMin = Vector2.zero;
                bgRT.anchorMax = Vector2.one;
                bgRT.sizeDelta = Vector2.zero;
                bgRT.offsetMin = Vector2.zero;
                bgRT.offsetMax = Vector2.zero;
                var bgImg = EnsureComponent<Image>(bgGO);
                bgImg.type = Image.Type.Sliced;
                bgImg.fillCenter = true;
                string bgSN = btnLayer.bg_sprite_name;
                if (!string.IsNullOrEmpty(bgSN) && spriteMap.TryGetValue(bgSN, out var bgSpr)) bgImg.sprite = bgSpr;
                else
                {
                    if (spriteMap.TryGetValue("Tab_BottomFlush_01_White_Bg", out var fb)) bgImg.sprite = fb;
                    else WLog($"  Bg sprite not found: {bgSN}");
                }
                btn.targetGraphic = bgImg;

                string iconSN = btnLayer.icon_sprite_name;
                if (string.IsNullOrEmpty(iconSN))
                {
                    string btnStem = btnLayer.name.Replace("Btn_", "");
                    var aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Store", new[] { "Shop", "Store", "Market" } },
                        { "Shop", new[] { "Shop", "Store", "Market" } },
                        { "Character", new[] { "Character", "Hero", "Char" } },
                        { "Battle", new[] { "Battle", "Fight", "Combat" } },
                        { "Gear", new[] { "Gear", "Armor", "Equipment" } },
                        { "Talent", new[] { "Talent", "Skill", "Ability" } },
                        { "Home", new[] { "Home", "Main", "Lobby" } }
                    };
                    string[] terms = aliases.TryGetValue(btnStem, out var al) ? al : new[] { btnStem };
                    foreach (var term in terms)
                    {
                        bool found = false;
                        foreach (var sk in spriteMap.Keys)
                        {
                            if (sk.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                iconSN = sk;
                                DLog($"  Icon fallback: {btnLayer.name} → '{term}' matched '{sk}'");
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                }
                var iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(btnGO.transform, false);
                var iconRT = EnsureComponent<RectTransform>(iconGO);
                iconRT.anchorMin = new Vector2(0.5f, 1f);
                iconRT.anchorMax = new Vector2(0.5f, 1f);
                iconRT.pivot = new Vector2(0.5f, 0.5f);
                float iW = btnLayer.icon_w > 0 ? btnLayer.icon_w : 80f;
                float iH = btnLayer.icon_h > 0 ? btnLayer.icon_h : 80f;
                iconRT.sizeDelta = new Vector2(iW, iH);
                float jPY = btnLayer.icon_pos_y;
                float bH = btnLayer.height > 0 ? btnLayer.height : 175f;
                float iOY = (jPY == 0f) ? 2f : (-jPY - iH * 0.5f);
                float fPY = -(bH * 0.5f - iOY);
                iconRT.anchoredPosition = new Vector2(0f, fPY);
                DLog($"  {btnLayer.name} icon: sprite={iconSN} jsonPosY={jPY} iconOffY={iOY} finalPosY={fPY} btnH={bH}");
                var iconImg = EnsureComponent<Image>(iconGO);
                iconImg.type = Image.Type.Simple;
                iconImg.preserveAspect = true;
                if (!string.IsNullOrEmpty(iconSN) && spriteMap.TryGetValue(iconSN, out var iSpr)) iconImg.sprite = iSpr;
                else
                {
                    WLog($"  Icon sprite not found: '{iconSN}' for {btnLayer.name}");
                    DLog($"    Available: {string.Join(", ", spriteMap.Keys)}");
                }

                var textGO = new GameObject("Text");
                textGO.transform.SetParent(btnGO.transform, false);
                var tmp2 = textGO.AddComponent<TextMeshProUGUI>();
                tmp2.text = btnLayer.label ?? "";
                tmp2.fontSize = (btnLayer.font_size > 0 ? btnLayer.font_size : 36f) * 0.75f;
                tmp2.fontStyle = FontStyles.Bold;
                tmp2.alignment = TextAlignmentOptions.Center;
                tmp2.enableWordWrapping = false;
                tmp2.overflowMode = TextOverflowModes.Overflow;
                var tRT = textGO.GetComponent<RectTransform>();
                float tH = btnLayer.text_height > 0 ? btnLayer.text_height : 50f;
                tRT.anchorMin = new Vector2(0, 0);
                tRT.anchorMax = new Vector2(1, 0);
                tRT.pivot = new Vector2(0.5f, 0);
                tRT.offsetMin = new Vector2(0, 0);
                tRT.offsetMax = new Vector2(0, tH);
                textGO.SetActive(false);
                DLog($"  Built: {btnGO.name}");
            }

            if (existed) PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabAssetPath, InteractionMode.AutomatedAction);
            else PrefabUtility.SaveAsPrefabAsset(root, prefabAssetPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            ALog($"Prefab saved (pass 1): {prefabAssetPath}");

            var pc = PrefabUtility.LoadPrefabContents(prefabAssetPath);
            try { ApplyGamiColors(pc, btnLayers, spriteMap, aebType); PrefabUtility.SaveAsPrefabAsset(pc, prefabAssetPath); ALog("Post-process (colours) done."); }
            finally { PrefabUtility.UnloadPrefabContents(pc); }

            pc = PrefabUtility.LoadPrefabContents(prefabAssetPath);
            try { WireGamiTabGroup(pc, aebType); PrefabUtility.SaveAsPrefabAsset(pc, prefabAssetPath); ALog("Post-process (GamiTabGroup wired) done."); }
            finally { PrefabUtility.UnloadPrefabContents(pc); }

            AssetDatabase.Refresh();
            ALog($"=== Import COMPLETE ===  Prefab: {prefabAssetPath}");
        }

        private void ApplyGamiColors(GameObject pc, List<LayerJson> btnLayers, Dictionary<string, Sprite> spriteMap, Type aebType)
        {
            for (int i = 0; i < pc.transform.childCount; i++)
            {
                var btnT = pc.transform.GetChild(i);
                var layer = i < btnLayers.Count ? btnLayers[i] : null;
                if (layer == null) continue;
                var bgT = btnT.Find("Bg");
                if (bgT != null)
                {
                    var img = bgT.GetComponent<Image>();
                    if (img != null && !string.IsNullOrEmpty(layer.bg_color))
                    {
                        img.color = HexToColor(layer.bg_color);
                        DLog($"  {btnT.name}/Bg color={layer.bg_color}");
                    }
                }
                var btn = btnT.GetComponent<Button>();
                bool isED = layer.drive_mode == "editor_driver";
                if (btn != null && !isED && layer.unity_button != null)
                {
                    var cb = btn.colors;
                    if (!string.IsNullOrEmpty(layer.unity_button.normal_color)) cb.normalColor = HexToColor(layer.unity_button.normal_color);
                    if (!string.IsNullOrEmpty(layer.unity_button.highlighted)) cb.highlightedColor = HexToColor(layer.unity_button.highlighted);
                    if (!string.IsNullOrEmpty(layer.unity_button.pressed)) cb.pressedColor = HexToColor(layer.unity_button.pressed);
                    if (!string.IsNullOrEmpty(layer.unity_button.selected)) cb.selectedColor = HexToColor(layer.unity_button.selected);
                    if (!string.IsNullOrEmpty(layer.unity_button.disabled)) cb.disabledColor = HexToColor(layer.unity_button.disabled);
                    btn.colors = cb;
                    DLog($"  {btnT.name} ColorBlock applied");
                }
                if (aebType != null && isED && layer.logic_data?.states != null)
                {
                    var aebComp = btnT.GetComponent(aebType);
                    if (aebComp != null)
                    {
                        var so = new SerializedObject(aebComp);
                        var sdp = so.FindProperty("stateData");
                        sdp.arraySize = layer.logic_data.states.Length;
                        for (int s = 0; s < layer.logic_data.states.Length; s++)
                        {
                            var sj = layer.logic_data.states[s];
                            var sp2 = sdp.GetArrayElementAtIndex(s);
                            sp2.FindPropertyRelative("stateName").stringValue = sj.stateName;
                            var cp = sp2.FindPropertyRelative("children");
                            int cc = sj.children?.Length ?? 0;
                            cp.arraySize = cc;
                            for (int c = 0; c < cc; c++)
                            {
                                var cj = sj.children[c];
                                var cpE = cp.GetArrayElementAtIndex(c);
                                cpE.FindPropertyRelative("childName").stringValue = cj.childName;
                                cpE.FindPropertyRelative("color").colorValue = HexToColor(cj.colorHex);
                            }
                        }
                        so.ApplyModifiedPropertiesWithoutUndo();
                        DLog($"  {btnT.name} AEB stateData written");
                    }
                }
            }
        }

        private void WireGamiTabGroup(GameObject pc, Type aebType)
        {
            Type ntgType = FindRuntimeType("GamiTabGroup");
            if (ntgType == null) { WLog("GamiTabGroup not found."); return; }
            var ntgComp = pc.GetComponent(ntgType);
            if (ntgComp == null) { WLog("GamiTabGroup component not found."); return; }
            var so = new SerializedObject(ntgComp);
            var bp = so.FindProperty("buttons");
            var abp = so.FindProperty("aebButtons");
            int count = pc.transform.childCount;
            bp.arraySize = count; abp.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                var child = pc.transform.GetChild(i);
                bp.GetArrayElementAtIndex(i).objectReferenceValue = child.GetComponent<Button>();
                abp.GetArrayElementAtIndex(i).objectReferenceValue = aebType != null ? child.GetComponent(aebType) : null;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            ALog($"GamiTabGroup wired: {count} tabs.");
        }

        // ─────────────────────────────────────────────────────────────────
        //  ZIP PARSER (binary, no temp files)
        // ─────────────────────────────────────────────────────────────────
        private static Dictionary<string, byte[]> ParseZipBinary(byte[] data)
        {
            var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            int len = data.Length;
            int eocd = -1;
            for (int i = len - 22; i >= Math.Max(0, len - 65557); i--)
                if (data[i] == 0x50 && data[i + 1] == 0x4B && data[i + 2] == 0x05 && data[i + 3] == 0x06)
                { eocd = i; break; }
            if (eocd < 0) throw new Exception("Not a valid ZIP");
            int cdCount = U16(data, eocd + 8), cdOffset = U32s(data, eocd + 16), pos = cdOffset;
            for (int i = 0; i < cdCount; i++)
            {
                if (U32s(data, pos) != 0x02014B50) throw new Exception($"CD sig missing at {pos}");
                int method = U16(data, pos + 10), compSize = U32s(data, pos + 20), uncompSize = U32s(data, pos + 24);
                int nameLen = U16(data, pos + 28), extraLen = U16(data, pos + 30), commentLen = U16(data, pos + 32);
                int lhOffset = U32s(data, pos + 42);
                string name = Encoding.UTF8.GetString(data, pos + 46, nameLen);
                pos += 46 + nameLen + extraLen + commentLen;
                if (string.IsNullOrEmpty(name) || name.EndsWith("/")) continue;
                if (U32s(data, lhOffset) != 0x04034B50) throw new Exception($"LH sig missing at {lhOffset}");
                int lNL = U16(data, lhOffset + 26), lEL = U16(data, lhOffset + 28), ds = lhOffset + 30 + lNL + lEL;
                byte[] fd;
                if (method == 0) { fd = new byte[uncompSize]; Array.Copy(data, ds, fd, 0, uncompSize); }
                else if (method == 8)
                {
                    using var ms = new MemoryStream(data, ds, compSize, false);
                    using var dstr = new DeflateStream(ms, CompressionMode.Decompress);
                    using var buf = new MemoryStream(uncompSize);
                    dstr.CopyTo(buf);
                    fd = buf.ToArray();
                }
                else { Debug.LogWarning($"[GamiPrefab] Skipping '{name}': compression {method}"); continue; }
                result[name] = fd;
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────────
        private static int U16(byte[] b, int o) => b[o] | (b[o + 1] << 8);
        private static int U32s(byte[] b, int o) => b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24);
        private static bool AnyEndsWith(Dictionary<string, byte[]> d, string suffix)
        { foreach (var k in d.Keys) if (k.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return true; return false; }
        private static float ToF(object v) { try { return Convert.ToSingle(v); } catch { return 0f; } }

        private void ApplySliceIfPresent(LayoutJson layout, string filename, string assetPath, TextureImporter ti)
        {
            if (layout.rootNode?.layers == null) return;
            string bn = Path.GetFileNameWithoutExtension(filename);
            foreach (var btn in layout.rootNode.layers)
            {
                if (btn.bg_sprite_name == bn && btn.bg_sprite_slice != null)
                {
                    var sl = btn.bg_sprite_slice;
                    if (ti == null) return;
                    ti.spriteBorder = new Vector4(sl.left, sl.bottom, sl.right, sl.top);
                    ti.SaveAndReimport();
                    DLog($"  9-slice applied to {bn}");
                    return;
                }
            }
        }

        private string FindProjectSprite(string bn)
        {
            var guids = AssetDatabase.FindAssets($"{bn} t:Sprite");
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p).Equals(bn, StringComparison.OrdinalIgnoreCase)) return p;
            }
            return null;
        }

        private LayerJson RawToLayerJson(Dictionary<string, object> d, List<Dictionary<string, object>> children = null)
        {
            var lj = new LayerJson
            {
                id = GetStr(d, "id") ?? "",
                name = GetStr(d, "name") ?? "",
                type = GetStr(d, "type") ?? "",
                parentId = GetStr(d, "parentId") ?? ""
            };
            string rawDM = GetStr(d, "drive_mode") ?? "";
            lj.drive_mode = rawDM == "Editor Driver" ? "editor_driver" : "native";
            var brt = GetDict(d, "rectTransform");
            if (brt != null)
            {
                var sz = GetDict(brt, "size");
                if (sz != null) lj.height = GetF(sz, "height", 175f);
            }
            if (lj.height <= 0) lj.height = 175f;
            var rawUB = GetDict(d, "unityButton");
            if (rawUB != null)
            {
                var rc = GetDict(rawUB, "colors");
                if (rc != null) lj.unity_button = new UBJson
                {
                    normal_color = GetStr(rc, "normal") ?? "#FFFFFF",
                    highlighted = GetStr(rc, "highlighted") ?? "#F5F5F5",
                    pressed = GetStr(rc, "pressed") ?? "#C8C8C8",
                    selected = GetStr(rc, "selected") ?? "#FFFFFF",
                    disabled = GetStr(rc, "disabled") ?? "#C8C8C880"
                };
            }
            var rawLogic = GetDict(d, "logic_data");
            if (rawLogic != null)
            {
                var rs = GetDict(rawLogic, "states");
                if (rs != null)
                {
                    var sl2 = new List<AEBStateJson>();
                    foreach (var sn in new[] { "Normal", "Selected", "Disabled" })
                    {
                        var sd = GetDict(rs, sn);
                        if (sd == null) continue;
                        var cl = new List<AEBChildJson>();
                        foreach (var cn in new[] { "Bg", "Icon", "Text" })
                        {
                            var cd = GetDict(sd, cn);
                            if (cd == null) continue;
                            string hex = GetStr(cd, "hex") ?? "#FFFFFF";
                            float alpha = GetF(cd, "alpha", 1f);
                            cl.Add(new AEBChildJson { childName = cn, colorHex = hex + ((int)(alpha * 255)).ToString("X2") });
                        }
                        sl2.Add(new AEBStateJson { stateName = sn, children = cl.ToArray() });
                    }
                    lj.logic_data = new LogicJson { states = sl2.ToArray() };
                }
            }
            var childList = children ?? new List<Dictionary<string, object>>();
            var nested = GetList(d, "layers");
            if (nested != null)
                foreach (var ni in nested)
                    if (ni is Dictionary<string, object> nd && !childList.Contains(nd))
                        childList.Add(nd);
            foreach (var child in childList)
            {
                if (child == null) continue;
                string cname = (GetStr(child, "name") ?? "").ToLowerInvariant().Trim();
                if (cname == "bg")
                {
                    lj.bg_sprite_name = GetStr(child, "sprite") ?? "";
                    lj.bg_color = GetStr(child, "colorHex") ?? GetStr(child, "color") ?? "";
                    var sa = GetList(child, "slice");
                    if (sa != null && sa.Count == 4)
                        lj.bg_sprite_slice = new SliceJson { left = ToF(sa[0]), top = ToF(sa[1]), right = ToF(sa[2]), bottom = ToF(sa[3]) };
                    DLog($"  Bg: sprite={lj.bg_sprite_name} color={lj.bg_color}");
                }
                else if (cname == "icon")
                {
                    lj.icon_sprite_name = GetStr(child, "sprite") ?? "";
                    var irt = GetDict(child, "rectTransform");
                    if (irt != null)
                    {
                        var isz = GetDict(irt, "size");
                        if (isz != null) { lj.icon_w = GetF(isz, "width", 80f); lj.icon_h = GetF(isz, "height", 80f); }
                        var ipos = GetDict(irt, "position");
                        if (ipos != null) lj.icon_pos_y = GetF(ipos, "y", 0f);
                    }
                    DLog($"  Icon: sprite={lj.icon_sprite_name} w={lj.icon_w} h={lj.icon_h} posY={lj.icon_pos_y}");
                }
                else if (cname == "text (tmp)" || cname == "text")
                {
                    var to = GetDict(child, "text");
                    if (to != null)
                    {
                        lj.label = GetStr(to, "content") ?? "";
                        lj.font_size = GetF(to, "fontSize", 36f);
                    }
                    var trt = GetDict(child, "rectTransform");
                    if (trt != null)
                    {
                        var tsz = GetDict(trt, "size");
                        if (tsz != null) lj.text_height = GetF(tsz, "height", 50f);
                    }
                    DLog($"  Text: label='{lj.label}' fontSize={lj.font_size} textH={lj.text_height}");
                }
            }
            if (lj.icon_w <= 0) lj.icon_w = 80f;
            if (lj.icon_h <= 0) lj.icon_h = 80f;
            if (lj.font_size <= 0) lj.font_size = 36f;
            if (lj.text_height <= 0) lj.text_height = 50f;
            return lj;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        { var c = go.GetComponent<T>(); return c != null ? c : go.AddComponent<T>(); }

        private static Type FindRuntimeType(string n)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = a.GetType(n);
                if (t != null) return t;
            }
            return null;
        }

        private static Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            hex = hex.TrimStart('#');
            if (hex.Length == 6) hex += "FF";
            if (hex.Length != 8) return Color.white;
            return new Color32(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16),
                Convert.ToByte(hex.Substring(6, 2), 16));
        }

        private static Dictionary<string, object> GetDict(Dictionary<string, object> d, string k)
        { if (d != null && d.TryGetValue(k, out var v)) return v as Dictionary<string, object>; return null; }
        private static List<object> GetList(Dictionary<string, object> d, string k)
        { if (d != null && d.TryGetValue(k, out var v)) return v as List<object>; return null; }
        private static string GetStr(Dictionary<string, object> d, string k)
        { if (d != null && d.TryGetValue(k, out var v)) return v as string; return null; }
        private static float GetF(Dictionary<string, object> d, string k, float def = 0f)
        { if (d != null && d.TryGetValue(k, out var v)) try { return Convert.ToSingle(v); } catch { } return def; }

        // ─────────────────────────────────────────────────────────────────
        //  JSON DATA CLASSES
        // ─────────────────────────────────────────────────────────────────
        [Serializable]
        private class LayoutJson
        {
            public string version;
            public LayerJson rootNode;
            public float hlg_spacing;
            public int hlg_padding_top, hlg_padding_bottom, hlg_padding_left, hlg_padding_right;
        }
        [Serializable]
        private class LayerJson
        {
            public string id, name, type, parentId, drive_mode;
            public RectJson rect;
            public float height, icon_w, icon_h, icon_pos_y, font_size, text_height;
            public string bg_color, bg_sprite_name, icon_sprite_name, label;
            public SliceJson bg_sprite_slice;
            public UBJson unity_button;
            public LogicJson logic_data;
            public LayerJson[] layers;
        }
        [Serializable] private class RectJson { public float x, y, width, height; }
        [Serializable] private class SliceJson { public float left, right, top, bottom; }
        [Serializable] private class UBJson { public string normal_color, highlighted, pressed, selected, disabled; }
        [Serializable] private class LogicJson { public AEBStateJson[] states; }
        [Serializable] private class AEBStateJson { public string stateName; public AEBChildJson[] children; }
        [Serializable] private class AEBChildJson { public string childName, colorHex; }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  MINI JSON PARSER (no external dependency)
    // ─────────────────────────────────────────────────────────────────────
    public static class MiniJson
    {
        public static object Deserialize(string json) => json == null ? null : new Parser(json).Parse();

        private sealed class Parser : IDisposable
        {
            private readonly StringReader _r;
            public Parser(string s) { _r = new StringReader(s); }
            public void Dispose() { _r.Dispose(); }
            public object Parse() { Skip(); return Val(); }

            private object Val()
            {
                int c = _r.Peek();
                if (c == '{') return Obj();
                if (c == '[') return Arr();
                if (c == '"') return Str();
                if (c == 't') { Eat("true"); return true; }
                if (c == 'f') { Eat("false"); return false; }
                if (c == 'n') { Eat("null"); return null; }
                return Num();
            }

            private Dictionary<string, object> Obj()
            {
                var d = new Dictionary<string, object>();
                _r.Read(); Skip();
                while (_r.Peek() != '}')
                {
                    string k = Str(); Skip(); _r.Read(); Skip();
                    d[k] = Val(); Skip();
                    if (_r.Peek() == ',') { _r.Read(); Skip(); }
                }
                _r.Read();
                return d;
            }

            private List<object> Arr()
            {
                var l = new List<object>();
                _r.Read(); Skip();
                while (_r.Peek() != ']')
                {
                    l.Add(Val()); Skip();
                    if (_r.Peek() == ',') { _r.Read(); Skip(); }
                }
                _r.Read();
                return l;
            }

            private string Str()
            {
                var sb = new StringBuilder();
                _r.Read();
                for (; ; )
                {
                    int c = _r.Read();
                    if (c == '"') break;
                    if (c != '\\') { sb.Append((char)c); continue; }
                    int e = _r.Read();
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            char[] h = new char[4];
                            for (int i = 0; i < 4; i++) h[i] = (char)_r.Read();
                            sb.Append((char)Convert.ToInt32(new string(h), 16));
                            break;
                    }
                }
                return sb.ToString();
            }

            private object Num()
            {
                var sb = new StringBuilder();
                bool isF = false;
                for (; ; )
                {
                    int c = _r.Peek();
                    if (c < 0) break;
                    char ch = (char)c;
                    if (ch == '.' || ch == 'e' || ch == 'E') isF = true;
                    if (ch == '.' || ch == 'e' || ch == 'E' || ch == '+' || ch == '-' || char.IsDigit(ch))
                    { sb.Append(ch); _r.Read(); }
                    else break;
                }
                string s = sb.ToString();
                if (isF) return double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
                if (long.TryParse(s, out long ll)) return ll;
                return double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            }

            private void Eat(string s) { foreach (char c in s) _r.Read(); }
            private void Skip() { while (_r.Peek() >= 0 && char.IsWhiteSpace((char)_r.Peek())) _r.Read(); }
        }
    }
}
