using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Demo_Phone81_CSharp
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }


        private async void button1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IAsyncAction a = null;
                if (a != null) await a.Log();
                await TestAsync().Log();
            }
            catch (Exception ex)
            {
                label1.Text = ex.StackTraceEx();
                if (await PromptToSendEmailAsync())
                {
                    await SendEmailAsync(ex.Message, ex.StackTraceEx());
                }
            }
        }

        async Task<bool> PromptToSendEmailAsync()
        {
            var md = new Windows.UI.Popups.MessageDialog("A error occured. Do you want to send a problem report?", "Error");
            bool? r = null;
            md.Commands.Add(new Windows.UI.Popups.UICommand("Yes", delegate { r = true; }));
            md.Commands.Add(new Windows.UI.Popups.UICommand("No", delegate { r = false; }));
            await md.ShowAsync();
            return (r.HasValue && r.Value);
        }

        async Task SendEmailAsync(string message, string details)
        {
            var emailTo = "lu@wischik.com";
            var emailSubject = "DemoApp problem report";
            var emailBody = "I encountered a problem with AsyncStackTraceEx...\r\n\r\n" + message + "\r\n\r\nDetails:\r\n" + details;
            var url = "mailto:?to=" + emailTo + "&subject=" + emailSubject + "&body=" + Uri.EscapeDataString(emailBody);
            await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
        }

        static async Task<int> TestAsync()
        {
            await FooAsync(0).Log("FooAsync", 0);
            await FooAsync(3).Log("FooAsync", 3);
            return 1;
        }

        static async Task FooAsync(int i)
        {
            if (i <= 1) await BarAsync(i == 0).Log("BarAsync", i == 0);
            else await FooAsync(i - 1).Log("FooAsync", i - 1);
        }

        static async Task BarAsync(bool b)
        {
            await Task.Delay(1).Log("Delay", 1);
            if (!b) throw new InvalidOperationException("oops");
        }

    }
}
