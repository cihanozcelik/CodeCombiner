using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Nopnag.CodeCombiner
{
  public class CodeCombiner : EditorWindow
  {
    const string RootFolder = "Assets";
    readonly List<string> _currentFiles = new();
    readonly Dictionary<string, bool> _fileSelectionStates = new();
    readonly Dictionary<string, List<string>> _folderFilesCache = new();

    readonly Dictionary<string, bool>
      _folderSelectionCache = new(); // Cache for folder selection states

    readonly Dictionary<string, bool> _foldoutStates = new();
    bool _needsRefresh = true;
    Vector2 _scrollPos;
    bool _stateChanged;

    string FileSelectionPrefsKey =>
      $"CodeCombiner_FileSelectionStates_{Application.dataPath.GetHashCode()}";

    string FoldoutPrefsKey => $"CodeCombiner_FoldoutStates_{Application.dataPath.GetHashCode()}";

    void OnEnable()
    {
      LoadState();
      RefreshFileList();
      EditorApplication.update += OnEditorUpdate;
      CustomAssetPostprocessor.assetChanged += OnAssetsChanged;
    }

    void OnDisable()
    {
      SaveState();
      EditorApplication.update -= OnEditorUpdate;
      CustomAssetPostprocessor.assetChanged -= OnAssetsChanged;
    }

    void OnGUI()
    {
      EditorGUILayout.LabelField("Select Files to Merge", EditorStyles.boldLabel);
      _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

      RenderFolder(RootFolder, 0);

      EditorGUILayout.EndScrollView();

      int totalLines = GetTotalSelectedLines();
      int totalFiles = GetTotalSelectedFiles();
      EditorGUILayout.LabelField($"Total Selected Files: {totalFiles}");
      EditorGUILayout.LabelField($"Total Lines of Selected Files: {totalLines}");

      if (GUILayout.Button("Combine and Copy to Clipboard"))
      {
        MergeFilesAndCopyToClipboard();
      }

      // Only repaint if the state has changed
      if (_stateChanged)
      {
        _stateChanged = false;
        Repaint();
      }
    }

    void OnAssetsChanged()
    {
      _needsRefresh = true;
    }

    void OnEditorUpdate()
    {
      if (_needsRefresh)
      {
        _needsRefresh = false;
        RefreshFileList();
        Repaint();
      }
    }

    [MenuItem("Tools/CodeCombiner")]
    public static void ShowWindow()
    {
      GetWindow<CodeCombiner>("Code Combiner");
    }

    void RefreshFileList()
    {
      _currentFiles.Clear();
      _folderFilesCache.Clear();
      _folderSelectionCache.Clear(); // Clear the folder selection cache
      LoadFolder(RootFolder);

      // Remove non-existent files from selection states
      var keysToRemove = new List<string>();
      foreach (string key in _fileSelectionStates.Keys)
      {
        if (!_currentFiles.Contains(key))
        {
          keysToRemove.Add(key);
        }
      }

      foreach (string key in keysToRemove)
      {
        _fileSelectionStates.Remove(key);
      }
    }

    void LoadFolder(string path)
    {
      if (!_foldoutStates.ContainsKey(path))
      {
        _foldoutStates[path] = true;
      }

      if (_folderFilesCache.ContainsKey(path))
      {
        return;
      }

      string[] directories = Directory.GetDirectories(path);
      string[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);

      var folderFiles = new List<string>();

      foreach (string dir in directories)
      {
        LoadFolder(dir); // Lazy loading: Load subfolders recursively as needed
      }

      foreach (string file in files)
      {
        if (Path.GetExtension(file) == ".cs" || Path.GetExtension(file) == ".uxml" ||
            Path.GetExtension(file) == ".uss")
        {
          _currentFiles.Add(file);
          folderFiles.Add(file);
          if (!_fileSelectionStates.ContainsKey(file))
          {
            _fileSelectionStates[file] = false;
          }
        }
      }

      _folderFilesCache[path] = folderFiles;
      UpdateFolderSelectionCache(path);
    }

    void RenderFolder(string path, int indentLevel)
    {
      // Only load folder contents if the folder is expanded
      if (_foldoutStates.ContainsKey(path) && _foldoutStates[path])
      {
        LoadFolder(path);
      }

      if (!_folderFilesCache.ContainsKey(path) || _folderFilesCache[path].Count == 0)
      {
        string[] directories = Directory.GetDirectories(path);
        bool hasFiles = false;

        foreach (string dir in directories)
        {
          if (_folderFilesCache.ContainsKey(dir) && _folderFilesCache[dir].Count > 0)
          {
            hasFiles = true;
            break;
          }
        }

        if (!hasFiles)
        {
          return;
        }
      }

      EditorGUILayout.BeginHorizontal();

      // Render toggle next to folder name
      if (indentLevel > 0)
      {
        GUILayout.Space(indentLevel * 20);
      }

      bool folderSelected = _folderSelectionCache.ContainsKey(path) && _folderSelectionCache[path];
      bool newFolderSelected = EditorGUILayout.Toggle(folderSelected, GUILayout.Width(15));
      if (newFolderSelected != folderSelected)
      {
        SetFolderSelection(path, newFolderSelected);
        _stateChanged = true; // Mark state as changed
      }

      _foldoutStates[path] = EditorGUILayout.Foldout(_foldoutStates[path], Path.GetFileName(path));
      EditorGUILayout.EndHorizontal();

      if (_foldoutStates[path])
      {
        string[] directories = Directory.GetDirectories(path);

        foreach (string dir in directories)
        {
          RenderFolder(dir, indentLevel + 1);
        }

        foreach (string file in _folderFilesCache[path])
        {
          EditorGUILayout.BeginHorizontal();
          GUILayout.Space((indentLevel + 1) * 20);
          bool fileSelected = EditorGUILayout.ToggleLeft(
            Path.GetFileName(file),
            _fileSelectionStates[file]
          );
          if (fileSelected != _fileSelectionStates[file])
          {
            _fileSelectionStates[file] = fileSelected;
            _stateChanged = true; // Mark state as changed
            UpdateFolderSelectionCacheForFile(
              file,
              fileSelected
            ); // Update the folder selection cache for the file
          }

          EditorGUILayout.EndHorizontal();
        }
      }
    }

    bool AreAllFilesSelected(string path)
    {
      if (!_folderFilesCache.ContainsKey(path))
      {
        return false;
      }

      // Check files directly in this folder
      foreach (string file in _folderFilesCache[path])
      {
        if (!_fileSelectionStates.ContainsKey(file) || !_fileSelectionStates[file])
        {
          return false;
        }
      }

      // Recursively check subfolders
      string[] directories = Directory.GetDirectories(path);
      foreach (string dir in directories)
      {
        if (!AreAllFilesSelected(dir))
        {
          return false;
        }
      }

      return true;
    }

    void SetFolderSelection(string path, bool selected)
    {
      if (!_folderFilesCache.ContainsKey(path))
      {
        return;
      }

      foreach (string file in _folderFilesCache[path])
      {
        _fileSelectionStates[file] = selected;
      }

      // Recursively set the selection for subfolders
      string[] directories = Directory.GetDirectories(path);
      foreach (string dir in directories)
      {
        SetFolderSelection(dir, selected);
      }

      UpdateFolderSelectionCache(path);
    }

    void MergeFilesAndCopyToClipboard()
    {
      StringBuilder mergedContent = new();

      foreach (var entry in _fileSelectionStates)
      {
        if (entry.Value)
        {
          string content = File.ReadAllText(entry.Key);
          mergedContent.AppendLine($"//--------------{entry.Key}----------------");
          mergedContent.AppendLine(content);
          mergedContent.AppendLine();
        }
      }

      if (mergedContent.Length > 0)
      {
        EditorGUIUtility.systemCopyBuffer = mergedContent.ToString();
        Debug.Log("Merged content copied to clipboard.");
      }
      else
      {
        Debug.LogWarning("No files selected.");
      }
    }

    void SaveState()
    {
      EditorPrefs.SetString(
        FileSelectionPrefsKey,
        JsonUtility.ToJson(new SerializableDictionary<string, bool>(_fileSelectionStates))
      );
      EditorPrefs.SetString(
        FoldoutPrefsKey,
        JsonUtility.ToJson(new SerializableDictionary<string, bool>(_foldoutStates))
      );
    }

    void LoadState()
    {
      if (EditorPrefs.HasKey(FileSelectionPrefsKey))
      {
        string json = EditorPrefs.GetString(FileSelectionPrefsKey);
        _fileSelectionStates.Clear();
        var loadedFileSelectionStates =
          JsonUtility.FromJson<SerializableDictionary<string, bool>>(json);
        foreach (var pair in loadedFileSelectionStates)
        {
          _fileSelectionStates[pair.Key] = pair.Value;
        }
      }

      if (EditorPrefs.HasKey(FoldoutPrefsKey))
      {
        string json = EditorPrefs.GetString(FoldoutPrefsKey);
        _foldoutStates.Clear();
        var loadedFoldoutStates = JsonUtility.FromJson<SerializableDictionary<string, bool>>(json);
        foreach (var pair in loadedFoldoutStates)
        {
          _foldoutStates[pair.Key] = pair.Value;
        }
      }
    }

    void UpdateFolderSelectionCache(string path)
    {
      bool allFilesSelected = AreAllFilesSelected(path);
      _folderSelectionCache[path] = allFilesSelected;

      // Update parent folder selection cache recursively
      string parentDirectory = Path.GetDirectoryName(path);
      if (!string.IsNullOrEmpty(parentDirectory))
      {
        UpdateFolderSelectionCache(parentDirectory);
      }
    }

    void UpdateFolderSelectionCacheForFile(string file, bool selected)
    {
      string folder = Path.GetDirectoryName(file);
      if (folder != null)
      {
        bool allFilesSelected = AreAllFilesSelected(folder);
        _folderSelectionCache[folder] = allFilesSelected;

        // Update parent folder selection cache recursively
        string parentDirectory = Path.GetDirectoryName(folder);
        if (!string.IsNullOrEmpty(parentDirectory))
        {
          UpdateFolderSelectionCache(parentDirectory);
        }
      }
    }

    int GetTotalSelectedLines()
    {
      int totalLines = 0;
      foreach (var entry in _fileSelectionStates)
      {
        if (entry.Value)
        {
          string[] lines = File.ReadAllLines(entry.Key);
          totalLines += lines.Length;
        }
      }

      return totalLines;
    }

    int GetTotalSelectedFiles()
    {
      int totalFiles = 0;
      foreach (var entry in _fileSelectionStates)
      {
        if (entry.Value)
        {
          totalFiles++;
        }
      }

      return totalFiles;
    }

    [Serializable]
    class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>,
      ISerializationCallbackReceiver
    {
      [SerializeField] List<TKey> keys = new();

      [SerializeField] List<TValue> values = new();

      public SerializableDictionary()
      {
      }

      public SerializableDictionary(IDictionary<TKey, TValue> dict) : base(dict)
      {
      }

      public void OnBeforeSerialize()
      {
        keys.Clear();
        values.Clear();
        foreach (var pair in this)
        {
          keys.Add(pair.Key);
          values.Add(pair.Value);
        }
      }

      public void OnAfterDeserialize()
      {
        Clear();

        if (keys.Count != values.Count)
        {
          throw new Exception("there are not same number of keys and values");
        }

        for (int i = 0; i < keys.Count; i++)
        {
          this[keys[i]] = values[i];
        }
      }
    }
  }

  public class CustomAssetPostprocessor : AssetPostprocessor
  {
    static void OnPostprocessAllAssets(
      string[] importedAssets,
      string[] deletedAssets,
      string[] movedAssets,
      string[] movedFromAssetPaths
    )
    {
      assetChanged?.Invoke();
    }

    public static event Action assetChanged;
  }
}