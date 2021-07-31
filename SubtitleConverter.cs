using System;
using System.Text;
using System.Text.RegularExpressions;

namespace TencentVideoTool
{
    public class SubtitleConverter
    {
        private static readonly Regex RgxCueId = new Regex(@"^\d+$");
        private static readonly Regex RgxTimeFrame = new Regex(@"(\d\d:\d\d:\d\d(?:[,.]\d\d\d)?) --> (\d\d:\d\d:\d\d(?:[,.]\d\d\d)?)");

        public static string ConvertSrtToVtt(string srtSubtitle)
        {
            var vttSubtitle = new StringBuilder();

            vttSubtitle.AppendLine("WEBVTT");
            vttSubtitle.AppendLine("");

            foreach (var srtLine in srtSubtitle.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                if (RgxCueId.IsMatch(srtLine)) // Ignore cue ID number lines
                {
                    continue;
                }

                Match match = RgxTimeFrame.Match(srtLine);
                if (match.Success) // Format the time frame to VTT format (and handle offset)
                {
                    var startTime = TimeSpan.Parse(match.Groups[1].Value.Replace(',', '.'));
                    var endTime = TimeSpan.Parse(match.Groups[2].Value.Replace(',', '.'));

                    vttSubtitle.AppendLine(
                        startTime.ToString(@"hh\:mm\:ss\.fff") +
                        " --> " +
                        endTime.ToString(@"hh\:mm\:ss\.fff"));
                }
                else
                {
                    vttSubtitle.AppendLine(srtLine);
                }
            }

            return vttSubtitle.ToString();
        }
    }
}