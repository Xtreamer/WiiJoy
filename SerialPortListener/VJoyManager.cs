using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using vJoyInterfaceWrap;

namespace SerialPortListener
{  
    public class VJoyManager
    {
        public vJoy joystick;
        public vJoy.JoystickState iReport;
        public uint deviceId = 1;
        private long maxval;

        public void Initialize()
        {
            // Create one joystick object and a position structure.
            joystick = new vJoy();
            iReport = new vJoy.JoystickState();
                      
            // Get the driver attributes (Vendor ID, Product ID, Version Number)
            if (!joystick.vJoyEnabled())
            {
                Console.WriteLine("vJoy driver not enabled: Failed Getting vJoy attributes.\n");
                return;
            }
            else
                Console.WriteLine("Vendor: {0}\nProduct :{1}\nVersion Number:{2}\n", joystick.GetvJoyManufacturerString(), joystick.GetvJoyProductString(), joystick.GetvJoySerialNumberString());

            // Get the state of the requested device
            VjdStat status = joystick.GetVJDStatus(deviceId);
            switch (status)
            {
                case VjdStat.VJD_STAT_OWN:
                    Console.WriteLine("vJoy Device {0} is already owned by this feeder\n", deviceId);
                    break;
                case VjdStat.VJD_STAT_FREE:
                    Console.WriteLine("vJoy Device {0} is free\n", deviceId);
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    Console.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue\n", deviceId);
                    return;
                case VjdStat.VJD_STAT_MISS:
                    Console.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue\n", deviceId);
                    return;
                default:
                    Console.WriteLine("vJoy Device {0} general error\nCannot continue\n", deviceId);
                    return;
            };

            // Check which axes are supported
            bool AxisX = joystick.GetVJDAxisExist(deviceId, HID_USAGES.HID_USAGE_X);
            bool AxisY = joystick.GetVJDAxisExist(deviceId, HID_USAGES.HID_USAGE_Y);
            bool AxisZ = joystick.GetVJDAxisExist(deviceId, HID_USAGES.HID_USAGE_Z);
            bool AxisRX = joystick.GetVJDAxisExist(deviceId, HID_USAGES.HID_USAGE_RX);
            bool AxisRZ = joystick.GetVJDAxisExist(deviceId, HID_USAGES.HID_USAGE_RZ);
            // Get the number of buttons and POV Hat switchessupported by this vJoy device
            int nButtons = joystick.GetVJDButtonNumber(deviceId);
            int ContPovNumber = joystick.GetVJDContPovNumber(deviceId);
            int DiscPovNumber = joystick.GetVJDDiscPovNumber(deviceId);

            // Print results
            Console.WriteLine("\nvJoy Device {0} capabilities:\n", deviceId);
            Console.WriteLine("Numner of buttons\t\t{0}\n", nButtons);
            Console.WriteLine("Numner of Continuous POVs\t{0}\n", ContPovNumber);
            Console.WriteLine("Numner of Descrete POVs\t\t{0}\n", DiscPovNumber);
            Console.WriteLine("Axis X\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Y\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Z\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Rx\t\t{0}\n", AxisRX ? "Yes" : "No");
            Console.WriteLine("Axis Rz\t\t{0}\n", AxisRZ ? "Yes" : "No");

            // Test if DLL matches the driver
            UInt32 DllVer = 0, DrvVer = 0;
            bool match = joystick.DriverMatch(ref DllVer, ref DrvVer);
            if (match)
                Console.WriteLine("Version of Driver Matches DLL Version ({0:X})\n", DllVer);
            else
                Console.WriteLine("Version of Driver ({0:X}) does NOT match DLL Version ({1:X})\n", DrvVer, DllVer);


            // Acquire the target
            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(deviceId))))
            {
                Console.WriteLine("Failed to acquire vJoy device number {0}.\n", deviceId);
                return;
            }
            else
                Console.WriteLine("Acquired: vJoy device number {0}.\n", deviceId);

            joystick.GetVJDAxisMax(deviceId, HID_USAGES.HID_USAGE_X, ref maxval);
        }

        public void SetPositions(object sender, SerialPortDataParser.PositionDataEventArgs e)
        {
            iReport.bDevice = (byte)deviceId;
            iReport.AxisX = e.Roll;
            iReport.AxisY = e.Pitch;
            iReport.AxisZ = e.Throttle;
            iReport.AxisZRot = e.Yaw;
            iReport.AxisXRot = 0;

            var updateSucceeded = joystick.UpdateVJD(deviceId, ref iReport);
            if (!updateSucceeded)
            {
                Console.WriteLine("Feeding vJoy device number {0} failed - try to enable device then press enter\n", deviceId);                
                joystick.AcquireVJD(deviceId);
            }
        }
    }
}
