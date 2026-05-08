# Unified Explorer
![assets/logo.jpg](assets/logo.jpg)

A native Windows File Explorer clone built in C# (WPF) with a focus on **Unified Sorting**. Unlike the standard Windows File Explorer which always groups folders at the top, Unified Explorer sorts files and folders completely uniformly based on your selected column criteria.

## 🚀 Key Features

### 1. Unified Sorting
- Click any column header (Name, Date Modified, Size, etc.) and files/folders will be shuffled together purely alphabetically or numerically.

### 2. Dual-View Interface
- Toggle between classic **Details View** and a modern **Large Icons View** using the bottom right status bar buttons.
- The Large Icons view features wrapping grids with massive folder/file icons perfectly matching the Windows 11 aesthetic.

### 3. Dynamic Bilingual Support
- Features a zero-dependency `LocalizationManager` that automatically translates the entire application (headers, context menus, right-pane details) into **German** if your Windows OS language is set to German. Defaults to **English**.

### 4. Custom Sidebar Navigation
- **Quick access (Favorites)**: Add folders by dragging and dropping them from the main file list. Remove them by clicking the Pin icon on the far right.
- **This PC**: An auto-updating tree view of all connected drives and recursive folder expansion.

### 5. Column Customization
- Right-click any column header to open the Context Menu.
- Toggle visibility for: Name, Date Modified, Type, Size, Creation Date, and Image Dimensions.
- Click "Size all columns to fit" to auto-fit columns to their content length.

### 6. Standard File Operations
- Fully supports Context Menu operations: Open, Copy, Cut, Paste, Rename, Delete, New Folder, and New File.

## 🛠️ Build & Run

Ensure you have the [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed.

```bash
# Clone the repository
git clone <your-repo-url>
cd fileexploerer

# Run the application directly
dotnet run
```

## 📦 Shippable Executable (CI/CD)
The project includes a GitHub Actions workflow (`.github/workflows/build.yml`) that automatically compiles the project into a single, shippable `.exe` file whenever code is pushed to the `main` branch. You can download the artifact directly from the "Actions" tab in your GitHub repository.
