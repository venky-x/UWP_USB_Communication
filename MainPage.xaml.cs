using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json;
using System.Diagnostics;

namespace UWP_USB_Communication
{
    public sealed partial class MainPage : Page
    {
        private Communication _serialCommunication;
        private string ledStatus = "off";  // Initially, the LED status is off

        public MainPage()
        {
            InitializeComponent();
            InitializeCommunication();
        }

        private async void InitializeCommunication()
        {
            _serialCommunication = await Communication.CreateAsync();
            if (_serialCommunication != null)
            {
                _serialCommunication.Serial_NewSerialData += Serial_NewSerialData;
                Debug.WriteLine("Serial communication initialized.");
            }
            else
            {
                Debug.WriteLine("Failed to initialize serial communication.");
            }
        }

        private async void Serial_NewSerialData(string jsonData)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    dynamic data = JsonConvert.DeserializeObject(jsonData);
                    TemperatureText.Text = $"Temperature: {data.temperature} °C";

                    // Update LED status only if the value exists in the response
                    if (data.led_status != null)
                    {
                        ledStatus = data.led_status;
                        LedStatusText.Text = $"LED Status: {ledStatus.ToUpper()}";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"JSON Parsing Error: {ex.Message}");
                }
            });
        }

        private async void ToggleLedButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serialCommunication != null)
            {
                // Immediately update the LED status in the UI
                ledStatus = (ledStatus == "on") ? "off" : "on";  // Toggle the LED status
                LedStatusText.Text = $"LED Status: {ledStatus.ToUpper()}";  // Update the UI immediately

                // Send the command to toggle the LED
                await _serialCommunication.SendCommandAsync("toggle_led");
                Debug.WriteLine("Sent: {\"command\": \"toggle_led\"}");
            }
            else
            {
                Debug.WriteLine("Serial communication not initialized.");
            }
        }
    }
}
