# TaskLauncher
A lightweight Windows app to launch your behavioral task (Bonsai, matlab, etc...). Works with Windows 10, and ade by Visual Studio and **WinForms**.


## 🚀 Getting started
1. Copy `TaskLauncher.exe` (and optionally `config.json`) to any folder from here (`ceph/mrsic_flogel/public/projects/SuKu_20231005_AllOpticalManipulation/2p-313/TaskLauncher`).  
2. Double-click `TaskLauncher.exe`.  
   - If `config.json` is missing, a **starter config** is generated.  
   - Edit `config.json` → save → relaunch. (see below for details)

---

## ⚙️ Configuration (`config.json`)

The app reads everything from `config.json` placed next to the EXE.

### Example
```jsonc
{
  "header": {
    "show": true,
    "left": "My BIG Left Title",
    "right": "small right text",
    "y": 30,
    "marginX": 20,
    "leftFontSize": 36,
    "rightFontSize": 9
  },
  "buttons": [
    {
      "text": "App 1 (no args)",
      "exePath": "C:\\Path\\To\\App1\\app1.exe",
      "y": -1, "width": 220, "height": 40
    },
    {
      "text": "App 2 (with args)",
      "exePath": "C:\\Path\\To\\App2\\app2.exe",
      "args": "--flag1 value",
      "y": -1, "width": 220, "height": 40
    },
    {
      "text": "App 3 (args list)",
      "exePath": "C:\\Path\\To\\App3\\app3.exe",
      "argsList": ["--port", "8080", "--mode", "safe value with spaces"],
      "runAsAdmin": true,
      "y": -1, "width": 220, "height": 40
    }
  ]
}
```

## Debugging
Use Visual Studio for debugging. Once the debug is done, you can publish the standalone `.exe` file with
```sh
dotnet publish ".\TaskLauncher.csproj" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=false /p:PublishReadyToRun=false /p:DebugType=none /p:DebugSymbols=false /p:GenerateDocumentationFile=false /p:IncludeNativeLibrariesForSelfExtract=true
```