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
    v.zoomFactor = 3
})
```
### logo.png
**Optional:** Avator to display in the plugin list

## STEP2: The Hook API
API is a way to communicate with Hook, which can be accessed in the main.js script
### Design
The API should be a consistent, event-based and full-function coding experience.

**Note:** The Hook API hasn't been fully constructed yet. Changes expected.
### functions
#### addEventListener(eventName, callback)
*parameters*
- **Function callback:** triggered when the given event happens
- **String eventName:** can be one of the followings

|Name|Description|Callback Parameter|
|:---|:----------|:-----------------|
|documentLoaded|When a document is ready to be shown|[DocumentView](Hook/Plugin/JSDocumentView.cs)|
|documentClosed|When a document is about to be closed|[DocumentView](Hook/Plugin/JSDocumentView.cs)|
|unload|When the plugin is about to be unloaded, usually app shutingdown or user uninstalling the plugin|nothing|
|systemStartup|When the plugin is loaded because of system starting up|nothing|

#### getOpenedDocuments()
*return:* a read-only array containing each document shown in the tab view

*see:* [DocumentView](Hook/Plugin/JSDocumentView.cs)
#### getRecentDocuments()
*return:* a read-only array containing each document touched recently, sorted from most recent to least

*see:* [IDcument](API/IDocument.cs)

#### download(uri: string, [rename: string, [callback: function]])
##### param: uri
HTTP address to the desired file.

*see:* [HttpClient](https://docs.microsoft.com/en-us/windows/uwp/networking/httpclient)
##### param: rename
Desired file name, or the one provided by server if empty.
##### param: callback(path: string|integer|null)
Called when the operation is done.

If the file was successfully downloaded, *path* will be its absolute path to the local filesystem.
If it's an http error, it's state code will be given.
Otherwise, a null parameter is given in a system exception.

#### httpAsString(uri: string, callback: function)
Cast a HTTP site as string.
##### param: uri
HTTP address to the desired website.
*see:* [HttpClient](https://docs.microsoft.com/en-us/windows/uwp/networking/httpclient)
##### param: callback(content: string|integer|null)
Called when the operation is done.
If the file was successfully downloaded, *content* will be the desired string content.
If it's an http error, it's state code will be given.
Otherwise, a null parameter is given in a system exception.

#### openDucment(path: string)
Open a document and bring it to user.

##### param: path
The file path to desired document.

#### writeline(content: any)
Write some text into the Debug section.

#### showInfoBar(title: string, message: string, severity: string = "")
Write some text into the InfoBar section.
##### param: severity
Different severity corresponds to different background colors and icons.

Can be one of the followings:
`error`,`warning`,`success`,`default`

*see:* [Guidline by Microsoft](https://docs.microsoft.com/en-us/windows/apps/design/controls/infobar#severity)

### Properties
#### window: [JSWindow](Hook/Plugin/Interpret/JSWindow.cs)
Represents the window of the app.
##### activate: function
Activate the application.
##### tryEnterFullscreen: function
Try to eneter fullscreen mode for the app.
#### plugin: [JSPluginWrapper](Hook/Plugin/Interpret/JSPluginWrapper.cs)
##### createShortcut: function(name, description, [iconSymbol, ]path)
**param** name: string

Title of the shortcut, displayed as the document name.

**param** description: string
Subtitle of the shortcut, displayed like the document last touched date.

**param** iconSymbol: string
Icon to be displayed.

*See:* [Symbol by Microsoft](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.symbol?view=winrt-22000)

**param** path: function(progressUpdater, resultInvoker)|string
This parameter can either be a string constant, a function that returns a string const or a function that
returns nothing and invokes its second argument with a string const. All you need to do is to provide the path
to the file to be opened.

The **progressUpdater** parameter is a function that receives a double as current progress, which ranges from 0 to 100
as a percentage.