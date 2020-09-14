using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

namespace RunZbar
{
    public class Zbarimg
    {
        public XmlDocument Xml { get; private set; }

        public List<Found> FoundList { get; } = new List<Found>();

        public class Found
        {
            public int PageIndex { get; set; }
            public string Format { get; set; }
            public string Quality { get; set; }
            public string Text { get; set; }

            public override string ToString() => $"{Format}:{Text}";
        }

        public void ScanAndDecode(
            string imageFile,
            string exePath = null,
            bool gaussianblur = false,
            bool erode = false,
            bool autonorm = false,
            string moreOptions = null
        )
        {
            var psi = new ProcessStartInfo(
                exePath ?? GetEmbeddedExePath(),
                string.Join(" "
                    , gaussianblur ? "--gaussianblur" : ""
                    , erode ? "--erode" : ""
                    , autonorm ? "--autonorm" : ""
                    , "--xml"
                    , moreOptions ?? ""
                    , $"\"{imageFile}\""
                )
            )
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
            };
            var p = Process.Start(psi);
            p.Start();
            var text = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                var message = p.StandardError.ReadToEnd();
                throw new ZbarimgException(p.ExitCode, message);
            }

            Xml = new XmlDocument();
            Xml.LoadXml(text.ToString());
            var xmlns = new XmlNamespaceManager(Xml.NameTable);
            xmlns.AddNamespace("b", "http://zbar.sourceforge.net/2008/barcode");
            foreach (XmlElement indexElement in Xml.SelectNodes("/b:barcodes/b:source/b:index", xmlns))
            {
                int.TryParse(indexElement.GetAttribute("num"), out int pageIndex);

                foreach (XmlElement symbolElement in indexElement.SelectNodes("b:symbol", xmlns))
                {
                    var type = symbolElement.GetAttribute("type") ?? "";
                    var quality = symbolElement.GetAttribute("quality") ?? "";

                    foreach (XmlElement dataElement in symbolElement.SelectNodes("b:data", xmlns))
                    {
                        var format = dataElement.GetAttribute("format") ?? "";

                        foreach (XmlCharacterData textNode in dataElement.SelectNodes("text()", xmlns))
                        {
                            FoundList.Add(
                                new Found
                                {
                                    PageIndex = pageIndex,
                                    Format = type,
                                    Quality = quality,
                                    Text = DecodeTextByFormat(textNode.Value, format),
                                }
                            );
                        }
                    }
                }
            }
        }

        private static string DecodeTextByFormat(string value, string format)
        {
            if (format == "base64")
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            return value;
        }

        public static string GetEmbeddedExePath() => Path.Combine(
            Path.GetDirectoryName(new Uri(typeof(Zbarimg).Assembly.Location).LocalPath),
            "zbarimg.exe"
        );
    }
}
