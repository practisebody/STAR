using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json.Linq;
#if NETFX_CORE
using HoloPoseClient.Signalling;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
#endif
using STAR.Nonin;

namespace STAR
{
    public class NoninOximeterConnection : IConnection
    {
        public string Name => "Oximeter";
        public bool Connected { get; protected set; } = false;
        public string StatusInfo { get; protected set; }

        public event MessageHandler OnMessageReceived;

        protected const int SEQ_CNTR_MSB_IDX = 5; // second byte of the sequence counter value
        protected const int SEQ_CNTR_LSB_IDX = 6; // first byte of the sequence counter value

        protected const int Invalid_SpO2 = 127;
        protected const int Invalid_PulseRate = 511;

        // Nonin Oximetry Service UUID
        protected readonly Guid OXI_SERVICE_UUID = new Guid("46a970e0-0d5f-11e2-8b5e-0002a5d5c51b");

        protected int _SpO2 { get; set; } = Invalid_SpO2;
        protected int _PulseRate { get; set; } = Invalid_PulseRate;
        public string SpO2 { get { return _SpO2 == Invalid_SpO2 ? "--" : _SpO2.ToString(); } }
        public string PulseRate { get { return _PulseRate == Invalid_PulseRate ? "--" : _PulseRate.ToString(); } }

        protected float Timer; // heart beat

        public void Start()
        {
#if NETFX_CORE
            ScanDevices();
#endif
        }

        public void Update()
        {
            //float Time.deltaTime
        }

#if NETFX_CORE
        protected async void ScanDevices()
        {
            var devices = await DeviceInformation.FindAllAsync(
                GattDeviceService.GetDeviceSelectorFromUuid(OXI_SERVICE_UUID),
                new string[] { "System.Devices.Aep.ContainerId" });
            if (devices.Count > 1)
            {
                StatusInfo = "Only connect to one Nonin oximeter!";
            }
            else if (devices.Count == 1)
            {
                foreach (DeviceInformation device in devices)
                {
                    InitBLEDevice(device);
                }
            }
            else
            {
                StatusInfo = "Pair Nonin oximeter with HoloLens first!";
            }
        }

        protected async void InitBLEDevice(DeviceInformation device)
        {
            try
            {
                NoninPulseOximetryService.Instance.OxiValueChangeCompleted += Instance_ValueChangeCompleted;
                await NoninPulseOximetryService.Instance.InitializeServiceAsync(device);
            }
            catch (Exception)
            {
                StatusInfo = "Access to oximeter denied!";
            }
        }

        protected void Instance_ValueChangeCompleted(PulseOximetryMeasurement OximetryMeasurementValue)
        {
            byte[] DF19Data = OximetryMeasurementValue.DF19Packet;
            ushort seqCounter = Convert.ToUInt16((Convert.ToUInt16(DF19Data[SEQ_CNTR_LSB_IDX]) | Convert.ToUInt16(DF19Data[SEQ_CNTR_MSB_IDX] << 8)));

            _SpO2 = OximetryMeasurementValue.SpO2Value;
            _PulseRate = OximetryMeasurementValue.PulseRateValue;

            Connected = _SpO2 == Invalid_SpO2 && _PulseRate == Invalid_PulseRate;

            OnMessageReceived?.Invoke(string.Format("{0},{1}", _SpO2, _PulseRate));

            // prepare initialization message
            JObject message = new JObject
            {
                ["type"] = "O",
                ["HR"] = PulseRate,
                ["SpO2"] = SpO2
            };

            JObject container = new JObject
            {
                ["message"] = message
            };
            string jsonString = container.ToString();
            Conductor.Instance.SendMessage(WebRTCConnection.MentorName, Windows.Data.Json.JsonObject.Parse(jsonString));
        }
#endif

    }
}
