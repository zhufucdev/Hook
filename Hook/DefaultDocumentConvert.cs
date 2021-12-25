using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Storage;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using DocumentFormat.OpenXml.Packaging;
using OpenXmlPowerTools;
using System.Drawing.Imaging;
using System.Xml.Linq;

namespace Hook
{
    internal class DefaultDocumentConvert : DocumentConvert
    {
        public async Task Convert(string path, string output)
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            if (file == null)
            {
                throw new FileNotFoundException(path);
            }

            var root = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(output));
            var imageDir = Path.GetFileNameWithoutExtension(output) + "_files";

            var folder = await root.TryGetItemAsync(imageDir);
            if (folder != null && folder is StorageFolder)
            {
                // empty existing content (cache or trash)
                foreach (var f in await (folder as StorageFolder).GetFilesAsync())
                {
                    await f.DeleteAsync();
                }
            }
            else
            {
                await root.CreateFolderAsync(imageDir);
            }

            int imageCounter = 0;
            var buffer = (await FileIO.ReadBufferAsync(file)).ToArray();
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(buffer, 0, buffer.Length);
                using (WordprocessingDocument doc = WordprocessingDocument.Open(ms, true))
                {
                    var settings = new HtmlConverterSettings()
                    {
                        PageTitle = "Hooked " + file.Name,
                    };
                    /*
                    settings.ImageHandler = (imageInfo) =>
                    {
                        var extension = imageInfo.ContentType.Split('/')[1].ToLower();
                        ImageFormat format = null;
                        switch (extension)
                        {
                            case "png":
                                format = ImageFormat.Jpeg;
                                extension = "jpeg";
                                break;
                            case "bmp":
                                format = ImageFormat.Bmp;
                                break;
                            case "jpeg":
                                format = ImageFormat.Jpeg;
                                break;
                            case "tiff":
                                format = ImageFormat.Tiff;
                                break;
                        }

                        if (format == null)
                        {
                            return null;
                        }

                        ++ imageCounter;
                        string imageName = Path.Combine(root.Path, imageDir, "image", string.Format("{0}.{1}", imageCounter, extension));
                        try
                        {
                            imageInfo.Bitmap.Save(imageName, format);
                        }
                        catch (System.Runtime.InteropServices.ExternalException)
                        {
                            return null;
                        }

                        var result = new XElement(
                            Xhtml.img,
                            new XAttribute(NoNamespace.src, imageName),
                            imageInfo.ImgStyleAttribute,
                            imageInfo.AltText != null ?
                                new XAttribute(NoNamespace.alt, imageInfo.AltText) : null
                        );
                        return result;
                    };
                    */

                    var html = HtmlConverter.ConvertToHtml(doc, settings);

                    var outName = Path.GetFileName(output);
                    var outFile = await root.TryGetItemAsync(outName);
                    if (outFile == null)
                    {
                        outFile = await root.CreateFileAsync(outName);
                    }
                    await FileIO.WriteTextAsync(outFile as StorageFile, html.ToStringNewLineOnAttributes());
                }
            }
        }

        public Guid GetID() => Guid.Parse("556960f6-6578-11ec-93e4-00155d0ae6e4");
    }
}
