using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace UWP_USB_Communication
{
    public class Communication
    {
        private SerialDevice _serialDevice;
        private DataWriter _dataWriter;
        private DataReader _dataReader;
        private StringBuilder _serialBuffer = new StringBuilder(); // Buffer for received data

        public event Action<string> Serial_NewSerialData;

        private Communication(SerialDevice serialDevice)
        {
            _serialDevice = serialDevice;
            _dataWriter = new DataWriter(_serialDevice.OutputStream);
            _dataReader = new DataReader(_serialDevice.InputStream);

            _serialDevice.BaudRate = 115200;
            _serialDevice.DataBits = 8;
            _serialDevice.StopBits = SerialStopBitCount.One;
            _serialDevice.Parity = SerialParity.None;

            ReadSerialDataAsync();
        }

        public static async Task<Communication> CreateAsync()
        {
            string deviceSelector = SerialDevice.GetDeviceSelector();
            var devices = await DeviceInformation.FindAllAsync(deviceSelector);

            if (devices.Count == 0)
            {
                Debug.WriteLine("No serial devices found.");
                return null;
            }

            SerialDevice serialDevice = await SerialDevice.FromIdAsync(devices[0].Id);
            return serialDevice != null ? new Communication(serialDevice) : null;
        }

        private async void ReadSerialDataAsync()
        {
            try
            {
                while (true)
                {
                    uint bytesToRead = await _dataReader.LoadAsync(256);
                    if (bytesToRead > 0)
                    {
                        string receivedData = _dataReader.ReadString(bytesToRead);
                        _serialBuffer.Append(receivedData); // Append to buffer

                        while (_serialBuffer.ToString().Contains("\n")) // Process complete JSON messages
                        {
                            int index = _serialBuffer.ToString().IndexOf("\n");
                            string jsonData = _serialBuffer.ToString().Substring(0, index).Trim();
                            _serialBuffer.Remove(0, index + 1);

                            if (IsValidJson(jsonData))
                            {
                                Serial_NewSerialData?.Invoke(jsonData);
                            }
                            else
                            {
                                Debug.WriteLine("Invalid JSON received.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading serial data: {ex.Message}");
            }
        }

        private bool IsValidJson(string data)
        {
            try
            {
                JObject.Parse(data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task SendCommandAsync(string command)
        {
            if (_serialDevice != null)
            {
                try
                {
                    string jsonCommand = $"{{\"command\":\"{command}\"}}\n"; // Append newline
                    _dataWriter.WriteString(jsonCommand);
                    await _dataWriter.StoreAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error sending command: {ex.Message}");
                }
            }
        }
    }
}
