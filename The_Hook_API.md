# The Hook API
API is a way to communicate with Hook, which can be accessed in the main.js script
## Guideline
The API should be a consistent, event-based and full-function coding experience.

**Note:** The Hook API hasn't been fully constructed yet. Changes expected.
## functions
### addEventListener(eventName, callback)
*parameters*
- **Function callback:** triggered when the given event happens
- **String eventName:** can be one of the followings

|Name|Description|Callback Parameter|
|:---|:----------|:-----------------|
|documentLoaded|When a document is ready to be shown|[JSDocumentView](Hook/Plugin/Interpret/JSDocumentView.cs)|
|documentClosed|When a document is about to be closed|[JSDocumentView](Hook/Plugin/Interpret/JSDocumentView.cs)|
|unload|When the plugin is about to be unloaded, usually app shutingdown or user uninstalling the plugin|nothing|
|systemStartup|When the plugin is loaded because of system starting up|nothing|

### getOpenedDocuments()
*return:* a read-only array containing each document shown in the tab view

*see:* [DocumentView](Hook/Plugin/Interpret/JSDocumentView.cs)
### getRecentDocuments()
*return:* a read-only array containing each document touched recently, sorted from most recent to least

*see:* [IDcument](API/IDocument.cs)

### download(uri: string, [rename: string, [callback: function]])
#### param: uri
HTTP address to the desired file.

*see:* [HttpClient](https://docs.microsoft.com/en-us/windows/uwp/networking/httpclient)
#### param: rename
Desired file name, or the one provided by server if empty.
#### param: callback(path: string|integer|null)
Called when the operation is done.

If the file was successfully downloaded, *path* will be its absolute path to the local filesystem.
If it's an http error, it's state code will be given.
Otherwise, a null parameter is given in a system exception.

### httpAsString(uri: string, callback: function)
Download a HTTP site as string.
#### param: uri
HTTP address to the desired website.
*see:* [HttpClient](https://docs.microsoft.com/en-us/windows/uwp/networking/httpclient)
#### param: callback(content: string|integer|null)
Called when the operation is done.
If the file was successfully downloaded, *content* will be the desired string content.
If it's an http error, it's state code will be given.
Otherwise, a null parameter is given in a system exception.

### openDucment(path: string)
Open a document and bring it to user.

#### param: path
The file path to desired document.

### writeline(content: any)
Write some text into the Debug section.

### showInfoBar(title: string, message: string, severity: string = "")
Write some text into the InfoBar section.
#### param: severity
Different severity corresponds to different background colors and icons.

Can be one of the followings:
`error`,`warning`,`success`,`default`

*see:* [Guide by Microsoft](https://docs.microsoft.com/en-us/windows/apps/design/controls/infobar#severity)

## Properties
### window: [JSWindow](Hook/Plugin/Interpret/JSWindow.cs)
Represents the window of the app.
#### activate: function
Activate the application.
#### tryEnterFullscreen: function
Try to eneter fullscreen mode for the app.
### plugin: [JSPluginWrapper](Hook/Plugin/Interpret/JSPluginWrapper.cs)
#### createShortcut: function(name, description, [iconSymbol, ]path)
##### param name: string

Title of the shortcut, displayed as the document name.

##### param description: string
Subtitle of the shortcut, displayed like the document last touched date.

##### param iconSymbol: string
Icon to be displayed.

*See:* [Symbol by Microsoft](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.symbol?view=winrt-22000)

##### param path: function(progressUpdater, resultInvoker)|string
This parameter can either be a string constant, a function that returns a string const or a function that
returns nothing and invokes its second argument with a string const. All you need to do is to provide the path
to the file to be opened by this method.

The **progressUpdater** parameter is a function that receives a double as current progress, which ranges from 0 to 100
as a percentage.