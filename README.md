# Unity Texture Atlas Maker

A lightweight, UI Toolkit-based Unity Editor tool that allows you to combine multiple textures into texture atlases. 


*Note: Because this tool uses `UnityEditor` and UI Toolkit (`UnityEngine.UIElements`), it must reside in an `Editor` folder so it is not included in your final build.*

## How to Use

1. **Open the Tool:** In the top menu bar of the Unity Editor, navigate to **Tools > Texture Atlas Maker**.
2. **Add Textures:** Drag and drop `Texture2D` assets from your Project window into the blank drop zone.
3. **Configure Options:** 
   * Use the dropdowns at the top to select your desired padding and maximum atlas resolution.
   * Review the generated pages in the preview grid.
4. **Remove Textures (Optional):** Right-click any texture thumbnail in the grid and select "Remove" to delete it from the current batch.
5. **Export:** Click the **Export Atlas** button, choose a destination folder within your `Assets` directory, and save. The tool will generate the `.png` files and automatically refresh the Asset Database.

## Requirements

* **Unity Version:** Unity 2020.1 or newer (requires support for UI Elements / UI Toolkit).
