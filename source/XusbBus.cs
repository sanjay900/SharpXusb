using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using PInvoke;

namespace SharpXusb
{
    using static Kernel32;

    public sealed class XusbBus : IDisposable
    {
        private SafeObjectHandle m_handle = null;
        private SafeObjectHandle m_handleAsync = null;
        private XusbDeviceVersion m_version;

        public XusbDeviceVersion Version
        {
            get
            {
                Debug.Assert(m_version != XusbDeviceVersion.ProcNotSupported);
                return m_version;
            }
        }

        public string DevicePath { get; }

        internal SafeObjectHandle Handle
        {
            get
            {
                if (m_handle is null)
                {
                    m_handle = Utilities.CreateFile(DevicePath);
                }

                return m_handle;
            }
        }

        internal SafeObjectHandle AsyncHandle
        {
            get
            {
                if (m_handleAsync is null)
                {
                    m_handleAsync = Utilities.CreateFile(DevicePath, CreateFileFlags.FILE_ATTRIBUTE_NORMAL | CreateFileFlags.FILE_FLAG_OVERLAPPED);
                }

                return m_handleAsync;
            }
        }

        internal XusbBus(string path)
        {
            DevicePath = path;
            m_handle = Utilities.CreateFile(path);
            m_handleAsync = Utilities.CreateFile(path, CreateFileFlags.FILE_ATTRIBUTE_NORMAL | CreateFileFlags.FILE_FLAG_OVERLAPPED);
            m_version = (XusbDeviceVersion)GetInformation().Version;
            Debug.Assert(m_version != XusbDeviceVersion.ProcNotSupported);
        }

        ~XusbBus()
        {
            Dispose(false);
        }

        public XusbBusInfo GetInformation()
        {
            int result = XusbCore.Bus_GetInformation(Handle, out var info);
            Utilities.ThrowOnError(result);
            return info;
        }

        public bool TryGetInformation(out XusbBusInfo info)
        {
            int result = XusbCore.Bus_GetInformation(Handle, out info);
            return result == 0;
        }

        public XusbBusInfoEx GetInformationEx(XusbBusInformationExType type = XusbBusInformationExType.Basic)
        {
           int result = XusbCore.Bus_GetInformationEx(Handle, Version, type, out var info);
            Utilities.ThrowOnError(result);
            return info;
        }

        public XusbInputState GetDeviceInputState(byte userIndex)
        {
            int result = XusbCore.Device_GetInputState(Handle, Version, userIndex, out var state);
            Utilities.ThrowOnError(result);
            return state;
        }

        public bool TryGetDeviceInputState(byte userIndex, out XusbInputState state)
        {
            int result = XusbCore.Device_GetInputState(Handle, Version, userIndex, out state);
            return result == 0;
        }

        public void SetDeviceState(byte userIndex, XusbLedSetting ledState, XusbVibration vibration)
        {
            int result = XusbCore.Device_SetState(Handle, userIndex, ledState, vibration);
            Utilities.ThrowOnError(result);
        }

        public void SetDeviceState(byte userIndex, XusbLedSetting ledState, XusbVibration vibration, XusbSetStateFlags flags)
        {
            int result = XusbCore.Device_SetState(Handle, userIndex, ledState, vibration, flags);
            Utilities.ThrowOnError(result);
        }

        public void SetDeviceState(byte userIndex, XusbVibration vibration)
        {
            int result = XusbCore.Device_SetState(Handle, userIndex, vibration);
            Utilities.ThrowOnError(result);
        }

        public void SetDeviceState(byte userIndex, XusbLedSetting ledState)
        {
            int result = XusbCore.Device_SetState(Handle, userIndex, ledState);
            Utilities.ThrowOnError(result);
        }

        public bool TryGetDeviceLedState(byte userIndex, out XusbLedState ledState)
        {
            int result = XusbCore.Device_GetLedState(Handle, Version, userIndex, out ledState);
            return result == 0;
        }

        public XusbLedState GetDeviceLedState(byte userIndex)
        {
            int result = XusbCore.Device_GetLedState(Handle, Version, userIndex, out var ledState);
            Utilities.ThrowOnError(result);
            return ledState;
        }

        public XusbCapabilities GetDeviceCapabilities(byte userIndex)
        {
            int result = XusbCore.Device_GetCapabilities(Handle, Version, userIndex, out var capabilities);
            Utilities.ThrowOnError(result);
            return capabilities;
        }

        public XusbBatteryInformation GetDeviceBatteryInformation(byte userIndex, XusbSubDevice subDevice = XusbSubDevice.Gamepad)
        {
            int result = XusbCore.Device_GetBatteryInformation(Handle, Version, userIndex, out var batteryInfo, subDevice);
            Utilities.ThrowOnError(result);
            return batteryInfo;
        }

        public XusbAudioDeviceInformation GetDeviceAudioDeviceInformation(byte userIndex)
        {
            int result = XusbCore.Device_GetAudioDeviceInformation(Handle, Version, userIndex, out var audioInfo);
            Utilities.ThrowOnError(result);
            return audioInfo;
        }

        public string GetDeviceAudioDeviceString(byte userIndex)
        {
            var audioInfo = GetDeviceAudioDeviceInformation(userIndex);
            return $"USB\\VID_{audioInfo.VendorId}&PID_{audioInfo.ProductId}&IA_{audioInfo.unk}";
        }

        public XusbInputState WaitForDeviceGuideButton(byte userIndex)
        {
            int result = XusbCore.Device_WaitForGuideButton(AsyncHandle,
                userIndex, out var inputState);
            Utilities.ThrowOnError(result);
            return inputState;
        }

        public async Task<XusbInputState> WaitForDeviceGuideButtonAsync(byte userIndex)
        {
            return await Task.Run(() => WaitForDeviceGuideButton(userIndex));
        }

        public XusbInputState WaitForDeviceInput(byte userIndex)
        {
            int result = XusbCore.Device_WaitForInput(AsyncHandle,
                userIndex, out var inputState);
            Utilities.ThrowOnError(result);
            return inputState;
        }

        public async Task<XusbInputState> WaitForDeviceInputAsync(byte userIndex)
        {
            return await Task.Run(() => WaitForDeviceInput(userIndex));
        }

        public void CancelWait(byte userIndex)
        {
            XusbCore.Device_CancelWait(userIndex);
        }

        public void PowerOffDevice(byte userIndex)
        {
            int result = XusbCore.Device_PowerOff(Handle, Version, userIndex);
            if (result != Win32Error.DeviceNotConnected)
            {
                Utilities.ThrowOnError(result);
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_handle?.Dispose();
                m_handle = null;
            }
        }
    }
}
