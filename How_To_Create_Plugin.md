# Guideline on How to Create a Hook Plugin
## STEP1: Project Structure
- ../MyPlugin.hplugin
  - plugin.json
  - main.js
  - logo.png
  - callable.js(register in plugin.json to take effect)

Let's take a deeper dive into each item
### .hplugin File
Actually ZIP archive, the plugin container
### plugin.json
Plugin manifest

Example be like:
```json
{
    "name": "MyPlugin",
    "description": "Sample plugin",
    "version": "0.8 alpha",
    "author": "Sample Author",
    "embed": ["callable.js"],
    "require": ["startWithSystem"]
}
```
### main.js
Entry to the plugin where API is available. Shall be executed after Hook starts up

A common practice is to register to the event listener
```javascript
addEventListener("documentLoaded", (v) => {
    // v is short for DocumentView, a wrapped object from C#
    v.ZoomFactor = 3
})
```
### logo.png
**Optional:** Avator to display in the plugin list

## STEP2: The Hook API
API is a way to communicate with Hook, which can be accessed in the main.js script

**Notice:** The Hook API hasn't been fully constructed yet. Changes expected.
#### addEventListener(eventName, callback)
*parameters*
- **Function callback**: triggered when the given event happens
- **String eventName:** can be one of the followings

|Name|Description|Callback Parameter|
|:---|:----------|:-----------------|
|documentLoaded|When a document is ready to be shown|[DocumentView](Hook/Plugin/JSDocumentView.cs)|
|documentClosed|When a document is about to be closed|[DocumentView](Hook/Plugin/JSDocumentView.cs)|
|unload|When the plugin is about to be unloaded, usually app shutingdown or user uninstalling the plugin|nothing|
|systemStartup|When the plugin is loaded because of system starting up|nothing|

#### getOpenedDocuments()
*return:* a read-only array containing each document shown in the tab view