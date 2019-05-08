// Copyright (C) Nonin Medical, Inc. All rights reserved.
// Adopted from Nonin sample UWP code, modified by Chengyuan

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NETFX_CORE
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;
using System.ComponentModel;
using Windows.Storage.Streams;
#endif
using UnityEngine;

namespace STAR
{
    namespace Nonin
    {
        public class PulseOximetryMeasurement
        {
            public ushort PulseRateValue { get; set; }
            public ushort SpO2Value { get; set; }
            public byte[] DF19Packet { get; set; }
        }

#if NETFX_CORE
        public class NoninPulseOximetryService
        {
            public delegate void Data_ValueChangeCompletedHandler(PulseOximetryMeasurement OximetryMeasurementValue);

            // Nonin's proprietary pulse oximetry service and characteristic UUIDs (measurement and control point)
            protected readonly Guid OXI_SERVICE_UUID = new Guid("46a970e0-0d5f-11e2-8b5e-0002a5d5c51b");
            protected readonly Guid MM_CHARACTERISTIC_UUID = new Guid("0aad7ea0-0d60-11e2-8e3c-0002a5d5c51b");
            protected readonly Guid CP_CHARACTERISTIC_UUID = new Guid("1447af80-0d60-11e2-88b6-0002a5d5c51b");

            // Nonin's pulse oximetry service has only one pulse oximetry measurement and 
            // one control point characteristic - set index to 0
            protected const int CHARACTERISTIC_INDEX = 0;

            //  Turn on notification for the pulse oximetry measurement characteristic.
            protected const GattClientCharacteristicConfigurationDescriptorValue CHARACTERISTIC_NOTIFICATION =
                GattClientCharacteristicConfigurationDescriptorValue.Notify;

            // Variable deviceCollect contains all the paired 3230 devices
            protected DeviceInformationCollection deviceCollect;

            // Declare service and characteristics as protected since it is only used within this module
            protected GattDeviceService Service;
            protected GattCharacteristic Characteristic;

            // Declare public events of delegate type
            public event Data_ValueChangeCompletedHandler OxiValueChangeCompleted;

            public static NoninPulseOximetryService Instance { get; } = new NoninPulseOximetryService();

            public async Task InitializeServiceAsync(DeviceInformation device)
            {
                // Get all paired Nonin 3230 devices
                deviceCollect = await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(OXI_SERVICE_UUID));

                int index = 0;
                for (index = 0; index < deviceCollect.Count; index++)
                {
                    if (deviceCollect[index].Name == device.Name)
                    {
                        Service = await GattDeviceService.FromIdAsync(deviceCollect[index].Id);
                    }
                }

                if (Service != null)
                {
                    await ConfigureServiceForNotificationsAsync();
                }
                else
                {
                }
            }

            private async Task ConfigureServiceForNotificationsAsync()
            {
                // Get the characteristic of Nonin Oximetry measurement 
                var result = await Service.GetCharacteristicsForUuidAsync(MM_CHARACTERISTIC_UUID);
                Characteristic = result.Characteristics[CHARACTERISTIC_INDEX];

                // Enable Link Layer encryption for connections between Windows OS and Nonin 3230 oximeter 
                Characteristic.ProtectionLevel = GattProtectionLevel.EncryptionRequired;

                // Configure the pulse oximetry measurement event handler
                Characteristic.ValueChanged += OxiCharacteristic_ValueChanged;

                //Turn on Notification 
                await Characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(CHARACTERISTIC_NOTIFICATION);
            }

            private void OxiCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
            {
                try
                {
                    var data = new byte[args.CharacteristicValue.Length];

                    DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

                    // Process the raw data received from the device.
                    var value = ProcessDF19Data(data);

                    OxiValueChangeCompleted?.Invoke(value);
                }
                catch (Exception)
                {
                }
            }

            private PulseOximetryMeasurement ProcessDF19Data(byte[] data)
            {
                int PR_MSB_IDX = 8;     //second byte of PR value 
                int PR_LSB_IDX = 9;     //first byte of PR value 
                int SPO2_IDX = 7;
                int DF19_LEN = 10;

                byte flags = data[PR_MSB_IDX];
                bool isPulseRateValueSizeLong = ((flags & 0x01) != 0);

                ushort PRMeasurementValue = 0;
                ushort SpO2MeasurementValue = 0;

                SpO2MeasurementValue = data[SPO2_IDX];

                if (isPulseRateValueSizeLong)
                {
                    PRMeasurementValue = (ushort)((data[PR_MSB_IDX] << 8) + data[PR_LSB_IDX]);
                    //increment PR_MSB_IDX byte packet length in case there are multiple packets received
                    PR_MSB_IDX += DF19_LEN;
                }
                else
                {
                    PRMeasurementValue = data[PR_LSB_IDX];
                    //increment PR_MSB_IDX byte packet length in case there are multiple packets
                    PR_MSB_IDX += DF19_LEN;
                }

                return new PulseOximetryMeasurement
                {
                    PulseRateValue = PRMeasurementValue,
                    SpO2Value = SpO2MeasurementValue,
                    DF19Packet = data,
                };
            }
        }
#endif
    }
}
