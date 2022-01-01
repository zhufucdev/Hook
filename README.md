# Hook
![Logo](Hook/Assets/Square44x44Logo.altform-lightunplated_targetsize-48.png)

Hook is a read-only MS Word implemented in [WebView2](https://docs.microsoft.com/en-us/microsoft-edge/webview2/) and
[UWP](https://docs.microsoft.com/en-us/windows/uwp/get-started/).
## Features
- [x] Docx to HTML Converter
- [x] Modern Look and Multilanguage Support
- [ ] Plugin System using JavaScript for Automation and UI Extension
- [ ] External Converter Support
## Supported Languages
- [x] ¼òÌåÖÐÎÄ
- [ ] English (US)

# Build Environment
Build via Visual Studio 2022

Dependencies:
- [Jint](https://github.com/sebastienros/jint)
- Micosoft.UI.Xaml
- OpenXMLPowerTools
- Newtonsoft.Json

# Plugin Structure
- ../MyPlugin.hplugin
  - plugin.json
  - main.js
  - logo.ico
  - callable.js(register in plugin.json to take effect)
## .hplugin File
Actually ZIP archive, the plugin container
## plugin.json
Plugin info containter
```json
{
    "name": "MyPlugin",
    "description": "Sample plugin",
    "version": "0.8 alpha",
    "author": "Sample Author",
    "embed": ["callable.js"]
}
```
## main.js
Entry to the plugin.
## logo.ico
**Optional:** Avator to display in the plugin list.