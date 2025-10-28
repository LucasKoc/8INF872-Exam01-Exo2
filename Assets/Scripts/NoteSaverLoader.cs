using System;
using System.IO;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NoteSaverLoader : MonoBehaviour
{
    [Header("UI (références)")]
    public TMP_InputField inputTexte;
    public TMP_Text labelAffichage;
    [Tooltip("Optionnel : si vide => persistentDataPath/note.txt")]
    public TMP_InputField inputChemin;
    [Tooltip("Optionnel : coche pour auto-reload ~1s")]
    public Toggle toggleAutoReload;

    [Header("Boutons")]
    public Button btnEnregistrer;

    public Button btnRecharger;

    [Header("Paramètres")]
    public string defaultFileName = "note.txt";
    public float reloadIntervalSeconds = 1.0f;

    private string _currentPath;
    private DateTime? _lastWriteUtc;
    private float _nextPollTime;
    private bool _loadingSilently;

    void Start()
    {
        var safeDefault = Path.Combine(Application.persistentDataPath, defaultFileName);
        _currentPath = safeDefault;

        if (inputChemin != null && string.IsNullOrWhiteSpace(inputChemin.text))
            inputChemin.text = safeDefault;

        // NEW: logs utiles pour retrouver le fichier
        Debug.Log($"[START] persistentDataPath = {Application.persistentDataPath}");
        Debug.Log($"[START] default note path = {safeDefault}");

        // NEW: auto-câblage des boutons si présents
        if (btnEnregistrer != null)
        {
            btnEnregistrer.onClick.RemoveAllListeners();
            btnEnregistrer.onClick.AddListener(Save);
        }
        if (btnRecharger != null)
        {
            btnRecharger.onClick.RemoveAllListeners();
            btnRecharger.onClick.AddListener(Load);
        }

        if (File.Exists(GetPath()))
            Load();
        else if (labelAffichage != null)
            labelAffichage.text = "(Aucune note encore)";
    }

    void Update()
    {
        if (toggleAutoReload == null || !toggleAutoReload.isOn) return;
        if (Time.unscaledTime >= _nextPollTime)
        {
            _nextPollTime = Time.unscaledTime + Mathf.Max(0.2f, reloadIntervalSeconds);
            TryAutoReload();
        }
    }

    [ContextMenu("Test Save (Inspector)")] // NEW
    private void ContextSave() => Save();

    [ContextMenu("Test Load (Inspector)")] // NEW
    private void ContextLoad() => Load();

    public void Save()
    {
        var path = GetPath();
        try
        {
            EnsureDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, inputTexte != null ? inputTexte.text : string.Empty, Encoding.UTF8);
            _lastWriteUtc = File.GetLastWriteTimeUtc(path);
            Debug.Log($"[SAVE] {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SAVE][ERROR] {path}\n{ex}");
        }
    }

    public void Load()
    {
        var path = GetPath();
        try
        {
            if (!File.Exists(path))
            {
                if (labelAffichage != null) labelAffichage.text = "(Fichier introuvable)";
                Debug.LogWarning($"[LOAD] Fichier introuvable: {path}");
                return;
            }
            var text = File.ReadAllText(path, Encoding.UTF8);
            if (labelAffichage != null) labelAffichage.text = text;
            _lastWriteUtc = File.GetLastWriteTimeUtc(path);
            if (!_loadingSilently) Debug.Log($"[LOAD] {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LOAD][ERROR] {path}\n{ex}");
        }
    }

    private void TryAutoReload()
    {
        var path = GetPath();
        try
        {
            if (!File.Exists(path)) return;
            var lastWrite = File.GetLastWriteTimeUtc(path);
            if (_lastWriteUtc == null || lastWrite > _lastWriteUtc.Value)
            {
                _loadingSilently = true;
                Load();
                _loadingSilently = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AUTO-RELOAD][ERROR] {path}\n{ex}");
        }
    }

    private string GetPath()
    {
        if (inputChemin != null)
        {
            var candidate = inputChemin.text?.Trim();
            if (!string.IsNullOrEmpty(candidate))
            {
                _currentPath = candidate;
                return _currentPath;
            }
        }
        return _currentPath;
    }

    private static void EnsureDirectory(string dir)
    {
        if (string.IsNullOrEmpty(dir)) return;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    private static string ExpandPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;

        // Trim guillemets ou espaces accidentels
        path = path.Trim().Trim('"');

        // Remplacer ~ par le HOME macOS
        if (path.StartsWith("~"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var rest = path.Substring(1).TrimStart('/', '\\');
            path = string.IsNullOrEmpty(rest) ? home : Path.Combine(home, rest);
        }

        // Normaliser en chemin absolu
        try { path = Path.GetFullPath(path); } catch { /* ignore */ }

        return path;
    }

    #if UNITY_EDITOR
    [ContextMenu("Ouvrir le dossier")] // NEW pratique pour Finder
    private void Reveal()
    {
        var dir = Path.GetDirectoryName(GetPath());
        if (!string.IsNullOrEmpty(dir)) UnityEditor.EditorUtility.RevealInFinder(dir);
    }
    #endif
}
