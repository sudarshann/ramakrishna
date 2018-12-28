using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.IO.Ports;
using Modbus.Device;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Background;
using System.Threading;
using Windows.UI.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409


namespace Renomate
{


    public class Recording : INotifyPropertyChanged
    {
        private string _RegisterAddress;
        private string _RegisterValue;

        public string RegisterAddress
        {
            get => _RegisterAddress;
            set
            {
                if (_RegisterAddress != value)
                {
                    _RegisterAddress = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public string RegisterValue
        {
            get => _RegisterValue;
            set
            {
                if (_RegisterValue != value)
                {
                    _RegisterValue = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        SerialPort _port;
        IModbusMaster _master;
        DispatcherTimer dispatcherTimer;
        DateTimeOffset startTime;
        DateTimeOffset lastTime;
        DateTimeOffset stopTime;

        public ObservableCollection<Recording> Recordings { get; set; } = new ObservableCollection<Recording>();

        /**
        * Holding register address for set temperatuer, set1, set2, set3
        */
        //184
        //187
        //190
        // divide by 10 is celcious with fraction
        // ex 188 / 10 = 18.8 degree

        /**
         * Input register values for reading current temperature value from censor
         * 
         * Address: 6
         * divide by 10 is celcious with fraction
         * ex 188 / 10 = 18.8 degree
         * 
         */

        public MainPage()
        {
            this.InitializeComponent();
        }

        void dispatcherTimer_Tick(object sender, object e)
        {
            _master = ModbusSerialMaster.CreateRtu(_port);
            // Read the current state of the output
            ReadInput(1, 6, 1);
            // ReadHolding(1, 184, 1);
            //  ReadHolding(1, 187, 1);
            // ReadHolding(1, 190, 1);
        }

        private void ReadHolding(byte SLAVE_ADDRESS, ushort START_ADDRESS, ushort NUM_REGISTER)
        {

                try
                {
                    ushort[] registers = _master.ReadHoldingRegisters(SLAVE_ADDRESS, START_ADDRESS, NUM_REGISTER);

                    for (int i = 0; i < NUM_REGISTER; i++)
                    {
                        
                        
                            this.Recordings.Add(new Recording()
                            {
                                RegisterAddress = (START_ADDRESS + i).ToString(),
                                RegisterValue = registers[i].ToString()
                            });
                        

                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            
        }

        private void ReadInput(byte SLAVE_ADDRESS, ushort START_ADDRESS, ushort NUM_REGISTER)
        {

            try
            {
                ushort[] registers = _master.ReadInputRegisters(SLAVE_ADDRESS, START_ADDRESS, NUM_REGISTER);

                for (int i = 0; i < NUM_REGISTER; i++)
                {
                    currentReading.Text = registers[i].ToString();
                    this.Recordings.Add(new Recording()
                    {
                        RegisterAddress = (START_ADDRESS + i).ToString(),
                        RegisterValue = registers[i].ToString()
                    });

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void WriteInput(double heaterInput)
        {
            byte SLAVE_ADDRESS = 1;
            ushort START_ADDRESS = 184;

            try
            {

                ushort[] registers = new ushort[] { (ushort)heaterInput, 0, 0, (ushort)heaterInput,0,0, (ushort)heaterInput };

                // write three registers
                _master.WriteMultipleRegisters(SLAVE_ADDRESS, START_ADDRESS, registers);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void Hamburger_Click(object sender, RoutedEventArgs e)
        {
            view1.IsPaneOpen = !view1.IsPaneOpen;
        }

        private void IconsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (shared.IsSelected)
            {
                RightsideBlock.Text = "shared";
            }
            else if (Important.IsSelected)
            {
                RightsideBlock.Text = "Important";
            }
            else if (Details.IsSelected)
            {
                RightsideBlock.Text = "Details";
            }
        }

        private async void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    var portName = "COM3";
                    string aqs = SerialDevice.GetDeviceSelector();
                    var deviceCollection = await DeviceInformation.FindAllAsync(aqs);
                    List<string> portNamesList = new List<string>();
                    foreach (var item in deviceCollection)
                    {
                        var serialDevice = await SerialDevice.FromIdAsync(item.Id);
                        if(serialDevice.PortName != "")
                        {
                            portName = serialDevice.PortName;
                        }
                    }
                    
                    _port = new SerialPort(portName, 9600);
                    // configure serial port
                    _port.BaudRate = 9600;
                    _port.DataBits = 8;
                    _port.Parity = Parity.None;
                    _port.StopBits = StopBits.One;
                    try
                    {
                        _port.Open();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                    dispatcherTimer = new DispatcherTimer();
                    dispatcherTimer.Tick += dispatcherTimer_Tick;
                    dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                    startTime = DateTimeOffset.Now;
                    lastTime = startTime;
                    dispatcherTimer.Start();
                    
                }
                else
                {
                    try
                    {
                        DateTimeOffset time = DateTimeOffset.Now;
                        stopTime = time;
                        dispatcherTimer.Stop();
                        _port.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (e.NewValue > 0)
            {
                WriteInput(e.NewValue);
            }
        }

    }
}
