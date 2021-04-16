using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Translator
{
    public class DeepL
    {
        private IWebDriver Driver;
        private int Verbose = 0;

        private readonly string InputSelector = "textarea[dl-test*='source']";
        private readonly string OutputSelector = "button[class='lmt__translations_as_text__text_btn']";

        private bool bUseClipboard = true;

        public DeepL(int InVerbose = 0, bool bHeadless = false)
        {
            Verbose = InVerbose;

            var chromeDriverService = ChromeDriverService.CreateDefaultService();
            chromeDriverService.HideCommandPromptWindow = true;
            chromeDriverService.SuppressInitialDiagnosticInformation = true;

            ChromeOptions options = new ChromeOptions();
            if (bHeadless)
            {
                options.AddArgument("--headless");
                options.AddArgument("--disable-gpu");
            }

            Driver = new ChromeDriver(chromeDriverService, options);
            Log("Opening DeepL in browser");

            Driver.Navigate().GoToUrl("https://www.deepl.com/zh/translator");
            Log("DeepL is ready");
        }

        ~DeepL()
        {
            Driver.Close();
            Log("Finalizing DeepL Translator");
        }

        void Log(string Msg)
        {
            if (Verbose >= 1)
            {
                try
                {
                    Debug.WriteLine(Msg);
                    Console.WriteLine(Msg);
                }
                catch (Exception)
                {
                }
            }
        }

        IWebElement GetTextInput()
        {
            return Driver.FindElement(By.CssSelector(InputSelector));
        }

        IWebElement GetTextOutput()
        {
            return Driver.FindElement(By.CssSelector(OutputSelector));
        }

        public string Translate(string Text)
        {
            // Strip spaces and newlines. If nothing left, return the text as it is.
            if (Regex.Replace(Text, @"\s+", string.Empty) == string.Empty)
            {
                return Text;
            }

            if (Text == "&nbsp;")
            {
                return "&nbsp;";
            }

            // Strip emoji's
            Text = new string((from c in Text where c <= 0xFFFF select c).ToArray());

            string Output = GetTextOutput().GetAttribute("textContent");
            if (Output != string.Empty)
            {
                GetTextInput().Clear();

                // Wait until output has been cleared
                while (Output != string.Empty)
                {
                    Thread.Sleep(1000);
                    Output = GetTextOutput().GetAttribute("textContent");
                }
            }

            int ErrorCount = 0;
            bool bSuccess = false;
            while (!bSuccess)
            {
                try
                {
                    Log("Pasting text to DeepL");

                    if (bUseClipboard)
                    {
                        bool bClipboardError = false;

                        // Copy text and paste it to Deepl. This is much faster than sending text by typing
                        try
                        {
                            Clipboard.SetData(DataFormats.UnicodeText, Text);
                        }
                        catch (Exception)
                        {
                            bClipboardError = true;

                            // Failed to use a clipboard, fallback to simulating key pressing
                            bUseClipboard = false;
                        }

                        if (!bClipboardError)
                        {
                            GetTextInput().SendKeys(Keys.Shift + Keys.Insert);
                        }
                    }

                    if (!bUseClipboard)
                    {
                        GetTextInput().SendKeys(Text);
                    }

                    bSuccess = true;
                }
                catch (Exception e)
                {
                    ErrorCount++;
                    if (ErrorCount >= 10)
                    {
                        throw (e);
                    }

                    Thread.Sleep(1000);
                }
            }

            string OutputBefore = string.Empty;
            while (true)
            {
                ErrorCount = 0;
                bSuccess = false;
                while (!bSuccess)
                {
                    try
                    {
                        Output = GetTextOutput().GetAttribute("textContent");
                        bSuccess = true;
                    }
                    catch (Exception e)
                    {
                        ErrorCount++;
                        if (ErrorCount >= 10)
                        {
                            throw (e);
                        }

                        Thread.Sleep(1000);
                    }
                }

                if (Output == string.Empty)
                {
                    continue;
                }

                if (!bUseClipboard)
                {
                    if (Output.Length <= 1 && Output != Text)
                    {
                        continue;
                    }

                    // When typing in the input, "[...]" indicates the translation is still running.
                    // This check is only needed if the translator is simulating typing in the input text,
                    // not when using clipboards.
                    if (Output.Contains("[...]"))
                    {
                        continue;
                    }
                }

                if (OutputBefore == Output)
                {
                    Log("Translated text is received");
                    break;
                }

                OutputBefore = Output;

                // Wait a bit and get output text again to verify it's no longer changing
                Thread.Sleep(200);
            }

            return Output;
        }
    }
}
