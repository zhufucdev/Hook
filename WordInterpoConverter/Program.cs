using System;
using System.IO;
using Windows.Storage;
using Word = Microsoft.Office.Interop.Word;

namespace WordInteropConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var target = ApplicationData.Current.LocalSettings.Values[KEY_TARGET_PATH];
            var output = ApplicationData.Current.LocalSettings.Values[KEY_OUTPUT_PATH];
            if (string.IsNullOrEmpty(output as string) || string.IsNullOrEmpty(target as string))
            {
                throw new ArgumentException("target or output path is not set");
            }

            var app = new Word.Application();
            var doc = app.Documents.Open(ref target, ReadOnly: true);
            object format = Word.WdSaveFormat.wdFormatHTML;

            doc.SaveAs2(ref output, ref format);
            doc.Close(Word.WdSaveOptions.wdDoNotSaveChanges);
        }

        public const string KEY_TARGET_PATH = "WICTargetPath";
        public const string KEY_OUTPUT_PATH = "WICOutputPath";
    }
}
