using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TextureAtlasMaker : EditorWindow
{
    private readonly List<Texture2D> _sourceTextures = new();
    private VisualElement _dropZone;
    private VisualElement _texGrid;

    private int _maxAtlasSize = 2048;
    private int _padding = 2;

    [MenuItem("Tools/Texture Atlas Maker")]
    public static void ShowWindow()
    {
        GetWindow<TextureAtlasMaker>("Texture Atlas Maker");
    }

    public void CreateGUI()
    {
        _sourceTextures.Clear();

        VisualElement root = rootVisualElement;
        root.Add(CreateHeader());
        root.Add(CreateDropZone());
    }


    private VisualElement CreateHeader()
    {
        VisualElement header = new()
        {
            style =
            {
                flexGrow = 0,
                flexDirection = FlexDirection.Row,
                paddingBottom = 10, paddingTop = 10, paddingLeft = 5, paddingRight = 5
            }
        };

        DropdownField texResolution = new(new List<string> { "512", "1024", "2048", "4096" }, 2);
        texResolution.RegisterValueChangedCallback(evt =>
        {
            if (int.TryParse(evt.newValue, out int newSize))
            {
                _maxAtlasSize = newSize;
                RefreshGrid();
            }
        });

        DropdownField paddingField = new(new List<string> { "0", "2", "4", "8" }, 1);
        paddingField.RegisterValueChangedCallback(evt =>
        {
            if (int.TryParse(evt.newValue, out int newPadding))
            {
                _padding = newPadding;
                RefreshGrid();
            }
        });

        Button exportButton = new() { text = "Export Atlas" };
        exportButton.RegisterCallback<ClickEvent>(OnExportClicked);

        header.Add(paddingField);
        header.Add(texResolution);
        header.Add(exportButton);

        return header;
    }

    private VisualElement CreateDropZone()
    {
        _dropZone = new VisualElement { style = { flexGrow = 1 } };

        ScrollView scrollView = new();
        _texGrid = scrollView.contentContainer;
        _texGrid.style.flexDirection = FlexDirection.Column;

        _dropZone.Add(scrollView);

        _dropZone.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
        _dropZone.RegisterCallback<DragPerformEvent>(OnDragPerform);

        return _dropZone;
    }

    private List<List<Texture2D>> CalculatePages()
    {
        List<List<Texture2D>> pages = new();
        if (_sourceTextures.Count == 0) return pages;

        List<Texture2D> currentPage = new();
        pages.Add(currentPage);

        List<Texture2D> sorted = new List<Texture2D>(_sourceTextures);
        sorted.Sort((a, b) => b.height.CompareTo(a.height));

        int currentX = 0;
        int currentY = 0;
        int rowHeight = 0;

        foreach (Texture2D tex in sorted)
        {
            if (tex.width > _maxAtlasSize || tex.height > _maxAtlasSize)
            {
                Debug.LogWarning($"Texture {tex.name} is larger than the Max Atlas Size!");
            }

            if (currentX + tex.width > _maxAtlasSize)
            {
                currentX = 0;
                currentY += rowHeight + _padding;
                rowHeight = 0;
            }

            if (currentY + tex.height > _maxAtlasSize)
            {
                currentPage = new List<Texture2D>();
                pages.Add(currentPage);
                currentX = 0;
                currentY = 0;
                rowHeight = 0;
            }

            currentPage.Add(tex);
            currentX += tex.width + _padding;
            rowHeight = Mathf.Max(rowHeight, tex.height);
        }

        return pages;
    }

    private void RefreshGrid()
    {
        _texGrid.Clear();
        List<List<Texture2D>> pages = CalculatePages();

        for (int i = 0; i < pages.Count; i++)
        {
            Label pageTitle = new($"Atlas Page {i + 1}")
            {
                style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 15, marginBottom = 5, marginLeft = 5 }
            };
            _texGrid.Add(pageTitle);

            VisualElement pageContainer = new()
            {
                style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 10 }
            };

            foreach (Texture2D tex in pages[i])
            {
                pageContainer.Add(CreateGridItem(tex));
            }

            _texGrid.Add(pageContainer);
        }
    }

    private VisualElement CreateGridItem(Texture2D texture)
    {
        VisualElement item = new()
        {
            style =
            {
                flexGrow = 0, flexShrink = 0, width = 120, height = 120, marginRight = 5, marginBottom = 5,
                borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                borderTopColor = Color.gray, borderBottomColor = Color.gray, borderLeftColor = Color.gray, borderRightColor = Color.gray
            }
        };

        Image img = new() { image = texture, scaleMode = ScaleMode.ScaleToFit };
        img.style.flexGrow = 1;

        item.Add(img);

        item.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button == 1) 
            {
                GenericMenu menu = new();
                menu.AddItem(new GUIContent("Remove"), false, () =>
                {
                    _sourceTextures.Remove(texture);
                    RefreshGrid();
                });
                menu.ShowAsContext();
            }
        });

        return item;
    }

    private void OnDragUpdated(DragUpdatedEvent evt)
    {
        bool containsTexture = false;
        foreach (Object obj in DragAndDrop.objectReferences)
        {
            if (obj is Texture2D) { containsTexture = true; break; }
        }
        DragAndDrop.visualMode = containsTexture ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
    }

    private void OnDragPerform(DragPerformEvent evt)
    {
        DragAndDrop.AcceptDrag();
        bool added = false;

        foreach (Object obj in DragAndDrop.objectReferences)
        {
            if (obj is Texture2D texture && !_sourceTextures.Contains(texture))
            {
                _sourceTextures.Add(texture);
                added = true;
            }
        }

        if (added) RefreshGrid();
    }


    private void OnExportClicked(ClickEvent evt)
    {
        if (_sourceTextures.Count == 0)
        {
            EditorUtility.DisplayDialog("No Textures", "Please add some textures to export.", "OK");
            return;
        }

        string basePath = EditorUtility.SaveFilePanel("Save Texture Atlas", "Assets", "TextureAtlas.png", "png");
        if (string.IsNullOrEmpty(basePath)) return;

        string directory = Path.GetDirectoryName(basePath);
        string fileName = Path.GetFileNameWithoutExtension(basePath);
        string extension = Path.GetExtension(basePath);

        List<List<Texture2D>> pages = CalculatePages();

        for (int i = 0; i < pages.Count; i++)
        {
            string savePath = pages.Count > 1
                ? Path.Combine(directory, $"{fileName}_{i + 1}{extension}")
                : basePath;

            ExportSinglePage(pages[i], savePath);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", $"Exported {pages.Count} Atlas Page(s) successfully!", "OK");
    }

    private void ExportSinglePage(List<Texture2D> pageTextures, string path)
    {
        Texture2D[] tempTextures = new Texture2D[pageTextures.Count];

        for (int i = 0; i < pageTextures.Count; i++)
        {
            tempTextures[i] = CreateUncompressedCopyForPacking(pageTextures[i]);
        }

        Texture2D finalAtlas = new Texture2D(_maxAtlasSize, _maxAtlasSize);
        finalAtlas.PackTextures(tempTextures, _padding, _maxAtlasSize);

        byte[] pngData = finalAtlas.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        Debug.Log($"Saved Atlas Page to {path}");

        foreach (Texture2D temp in tempTextures)
        {
            if (temp != null) DestroyImmediate(temp);
        }
        DestroyImmediate(finalAtlas);
    }

    public Texture2D CreateUncompressedCopyForPacking(Texture2D originalTex)
    {
        string path = AssetDatabase.GetAssetPath(originalTex);
        if (string.IsNullOrEmpty(path)) return null;

        byte[] fileData = File.ReadAllBytes(path);
        Texture2D tempTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tempTex.LoadImage(fileData);

        return tempTex;
    }
}