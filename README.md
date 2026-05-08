# Unified Explorer
![assets/logo.jpg](assets/logo.jpg)
[![.NET Desktop Publish](https://github.com/mommel/UnifiedExplorer/actions/workflows/dotnet-desktop-publish.yml/badge.svg)](https://github.com/mommel/UnifiedExplorer/actions/workflows/dotnet-desktop-publish.yml)


The native Windows File Explorer has lacks the option to have a correct sorting, so that files and directories get sorted correctly. The windows file explorer sorts so that files are sorted and directories are sorted and they will never be mixed.
That's why this app built in C# (WPF) with a focus on **Unified Sorting** got alive. Unified Explorer sorts files and folders completely uniformly based on your selected column criteria. That's it.

## Develop, Build & Run

Ensure you have the [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed.

```ps
# Clone the repository
git clone <your-repo-url>
cd fileexploerer

# Run the application directly
dotnet run
```

## Shippable Executable (CI/CD)
The project includes a GitHub Actions workflow (`.github/workflows/dotnet-desktop-publish.yml`) builds the project into a single, shippable `.exe` file, signs the exe if a valid cert is setup, uploades and releases a new app version automatically.
