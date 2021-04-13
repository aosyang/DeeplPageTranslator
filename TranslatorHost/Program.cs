using System;
using System.Windows.Forms;
using System.IO;

namespace TranslatorHost
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //TranslatorService deepl = new TranslatorService();
            //deepl.TranslateHtml("<a href=\"https://policy.medium.com/medium-terms-of-service-9db0094a1e0f?source=post_page-----9a0bff37854e--------------------------------\" class=\"cx cy bb bc bd be bf bg bh bi tr bl tk tl\" rel=\"noopener\">Legal</a>");

            // Disable default console output
            Console.SetOut(TextWriter.Null);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
