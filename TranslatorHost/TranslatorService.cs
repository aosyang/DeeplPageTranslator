using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace TranslatorHost
{
    class TranslatorService
    {
        bool bReady = false;
        Translator.DeepL Translator;

        Dictionary<string, string> CachedTranslations = new Dictionary<string, string>();

        public TranslatorService()
        {
            Thread t = new Thread(StartService);
            t.Start();
        }

        private void StartService()
        {
            try
            {
                Translator = new Translator.DeepL();
                bReady = true;
            }
            catch (ThreadAbortException)
            {
                // Service stopped
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "An exception occurred!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public string Translate(string Text)
        {
            while (!bReady)
            {
                Thread.Sleep(500);
            }

            return Translator.Translate(Text);
        }

        public string TranslateHtml(string Text)
        {
            Text = Text.Trim();

            // Check empty string
            if (Text.Length == 0)
            {
                return Text;
            }

            // Check if string contains any letters
            if (!HasAnyLetters(Text))
            {
                return Text;
            }

            bool bCacheTranslation = false;

            // Cache short sentences
            if (Text.Length < 32)
            {
                string CachedText;
                if (CachedTranslations.TryGetValue(Text, out CachedText))
                {
                    return CachedText;
                }

                bCacheTranslation = true;
            }

            // Check if string is wrapped with any html tags
            if (Text[0] == '<' && Text[Text.Length - 1] == '>')
            {
                string FirstTag = StripFirstTag(ref Text);
                string LastTag = StripLastTag(ref Text);

                if (FirstTag.Length > 0 || LastTag.Length > 0)
                {
                    string HtmlResult = FirstTag + TranslateHtml(Text) + LastTag;
                    if (bCacheTranslation)
                    {
                        CachedTranslations[Text] = HtmlResult;
                    }

                    return HtmlResult;
                }
            }

            // Trim any whitespace between html tags
            string OldText = Text;
            Text = RemoveDuplicateSpansForMedium(Text);
            Text = TrimHtmlSpaceAndNewLines(Text);

            // DeepL input length check. Can not exceed 5000 characters.
            string Result = Text.Length >= 5000 ? Text : Translate(Text);
            if (bCacheTranslation)
            {
                CachedTranslations[Text] = Result;
            }

            return Result;
        }

        string StripFirstTag(ref string Text)
        {
            int Start = -1;
            int End = -1;

            for (int i = 0; i < Text.Length; i++)
            {
                if (Text[i] == '<' && Start == -1)
                {
                    Start = i;
                }

                if (Text[i] == '>' && End == -1)
                {
                    End = i;
                }

                if (Start != -1 && End != -1)
                {
                    if (Start < End)
                    {
                        int Length = End - Start + 1;
                        string Result = Text.Substring(Start, Length);
                        Text = Text.Remove(Start, Length);
                        return Result;
                    }

                    break;
                }
            }

            return string.Empty;
        }

        string StripLastTag(ref string Text)
        {
            int Start = -1;
            int End = -1;
            for (int i = Text.Length - 1; i >= 0; i--)
            {
                if (Text[i] == '<' && Start == -1)
                {
                    Start = i;
                }

                if (Text[i] == '>' && End == -1)
                {
                    End = i;
                }

                if (Start != -1 && End != -1)
                {
                    if (Start < End)
                    {
                        int Length = End - Start + 1;
                        string Result = Text.Substring(Start, Length);
                        Text = Text.Remove(Start, Length);
                        return Result;
                    }

                    break;
                }
            }

            return string.Empty;
        }

        bool HasAnyLetters(string Text)
        {
            for (int i = 0; i < Text.Length; i++)
            {
                if (char.IsLetter(Text[i]))
                {
                    return true;
                }
            }

            return false;
        }

        string TrimHtmlSpaceAndNewLines(string Text)
        {
            string Result = string.Empty;

            while (Text.Length != 0)
            {
                Text = Text.Trim();

                int OpenTag = Text.IndexOf('<');
                int CloseTag = Text.IndexOf('>');

                if (OpenTag != -1 && CloseTag != -1)
                {
                    if (OpenTag == 0)
                    {
                        int Count = CloseTag - OpenTag + 1;
                        Result += Text.Substring(OpenTag, Count);
                        Text = Text.Remove(OpenTag, Count);
                    }
                    else
                    {
                        Result += TrimParagraph(Text.Substring(0, OpenTag));
                        Text = Text.Remove(0, OpenTag);
                    }
                }
                else
                {
                    Result += TrimParagraph(Text);
                    break;
                }
            }

            return Result;
        }

        string TrimParagraph(string Text)
        {
            string Paragraph = string.Empty;
            foreach (string SubString in Text.Split('\n'))
            {
                if (Paragraph != string.Empty)
                {
                    Paragraph += " ";
                }

                Paragraph += SubString.Trim();
            }

            return Paragraph;
        }

        // Hack for medium.com - Remove patterns of '<span id="rmm"><span id="rmm">...</span></span>'
        public string RemoveDuplicateSpansForMedium(string Text)
        {
            const string Pattern = "<span id=\"rmm\">";
            while (Text.Contains(Pattern))
            {
                Text = ReplaceFirstOccurance(Text, Pattern, "");
                Text = ReplaceFirstOccurance(Text, "</span>", "");
            }

            return Text;
        }

        public string ReplaceFirstOccurance(string Text, string oldValue, string newValue)
        {
            int Index = Text.IndexOf(oldValue);
            if (Index != -1)
            {
                string Prefix = Text.Substring(0, Index);
                string Suffix = Text.Substring(Index + oldValue.Length);
                return Prefix + newValue + Suffix;
            }

            return Text;
        }

        public string FixMissingPunctuation(string Text)
        {
            // For some reason DeepL drops initial open punctuations in a paragraph. Let's add them back.
            int closeIndex = Text.IndexOf('》');
            if (closeIndex != -1)
            {
                int openIndex = Text.IndexOf('《');
                if (openIndex == -1 || openIndex > closeIndex)
                {
                    int periodIndex = Text.IndexOf('。');
                    if (periodIndex != -1 && periodIndex < closeIndex)
                    {
                        // FIXME: This may put an open punctuation before tags
                        Text.Insert(periodIndex + 1, "《");
                        return Text;
                    }
                    else
                    {
                        return '《' + Text;
                    }
                }
            }

            return Text;
        }
    }
}
