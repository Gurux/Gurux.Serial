//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// More information of Gurux products: http://www.gurux.org
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Android.Hardware.Usb;
using Gurux.Serial.Enums;

namespace Gurux.Serial
{

    /**
     * GXCp21xx (Silicon Labs) chipset settings.
     * https://www.silabs.com/Support%20Documents/TechnicalDocs/AN571.pdf
     */
    class GXCP21xx : GXChipset
    {

        /// <inheritdoc />
        public override Chipset Chipset
        {
            get
            {
                return Chipset.Cp21xx;
            }
        }

        public new static bool IsUsing(string manufacturer, int vendor, int product)
        {
            /* Renesas RX610 RX-Stick */
            if ((vendor == 0x045B && product == 0x0053)
                    /* AKTAKOM ACE-1001 cable */
                    || (vendor == 0x0471 && product == 0x066A)
                    /* Pirelli Broadband S.p.A, DP-L10 SIP/GSM Mobile */
                    || (vendor == 0x0489 && product == 0xE000)
                    /* Pirelli Broadband S.p.A, DP-L10 SIP/GSM Mobile */
                    || (vendor == 0x0489 && product == 0xE003)
                    /* CipherLab USB CCD Barcode Scanner 1000 */
                    || (vendor == 0x0745 && product == 0x1000)
                    /* NetGear Managed Switch M4100 series, M5300 series, M7100 series */
                    || (vendor == 0x0846 && product == 0x1100)
                    /* Gemalto Prox-PU/CU contactless smartcard reader */
                    || (vendor == 0x08e6 && product == 0x5501)
                    /* Digianswer A/S , ZigBee/802.15.4 MAC Device */
                    || (vendor == 0x08FD && product == 0x000A)
                    /* Siemens RUGGEDCOM USB Serial Console */
                    || (vendor == 0x0908 && product == 0x01FF)
                    /* MEI (TM) Cashflow-SC Bill/Voucher Acceptor */
                    || (vendor == 0x0BED && product == 0x1100)
                    /* MEI series 2000 Combo Acceptor */
                    || (vendor == 0x0BED && product == 0x1101)
                    /* Dynastream ANT development board */
                    || (vendor == 0x0FCF && product == 0x1003)
                    /* Dynastream ANT2USB */
                    || (vendor == 0x0FCF && product == 0x1004)
                    /* Dynastream ANT development board */
                    || (vendor == 0x0FCF && product == 0x1006)
                    /* OWL Wireless Electricity Monitor CM-160 */
                    || (vendor == 0x0FDE && product == 0xCA05)
                    /* Knock-off DCU-11 cable */
                    || (vendor == 0x10A6 && product == 0xAA26)
                    /* Siemens MC60 Cable */
                    || (vendor == 0x10AB && product == 0x10C5)
                    /* Nokia CA-42 USB */
                    || (vendor == 0x10B5 && product == 0xAC70)
                    /* Vstabi */
                    || (vendor == 0x10C4 && product == 0x0F91)
                    /* Arkham Technology DS101 Bus Monitor */
                    || (vendor == 0x10C4 && product == 0x1101)
                    /* Arkham Technology DS101 Adapter */
                    || (vendor == 0x10C4 && product == 0x1601)
                    /* SPORTident BSM7-D-USB main station */
                    || (vendor == 0x10C4 && product == 0x800A)
                    /* Pololu USB-serial converter */
                    || (vendor == 0x10C4 && product == 0x803B)
                    /* Cygnal Debug Adapter */
                    || (vendor == 0x10C4 && product == 0x8044)
                    /* Software Bisque Paramount ME build-in converter */
                    || (vendor == 0x10C4 && product == 0x804E)
                    /* Enfora EDG1228 */
                    || (vendor == 0x10C4 && product == 0x8053)
                    /* Enfora GSM2228 */
                    || (vendor == 0x10C4 && product == 0x8054)
                    /* Argussoft In-System Programmer */
                    || (vendor == 0x10C4 && product == 0x8066)
                    /* IMS USB to RS422 Converter Cable */
                    || (vendor == 0x10C4 && product == 0x806F)
                    /* Crumb128 board */
                    || (vendor == 0x10C4 && product == 0x807A)
                    /* Cygnal Integrated Products, Inc., Optris infrared thermometer */
                    || (vendor == 0x10C4 && product == 0x80C4)
                    /* Degree Controls Inc */
                    || (vendor == 0x10C4 && product == 0x80CA)
                    /* Tracient RFID */
                    || (vendor == 0x10C4 && product == 0x80DD)
                    /* Suunto sports instrument */
                    || (vendor == 0x10C4 && product == 0x80F6)
                    /* Arygon NFC/Mifare Reader */
                    || (vendor == 0x10C4 && product == 0x8115)
                    /* Burnside Telecom Deskmobile */
                    || (vendor == 0x10C4 && product == 0x813D)
                    /* Tams Master Easy Control */
                    || (vendor == 0x10C4 && product == 0x813F)
                    /* West Mountain Radio RIGblaster P&P */
                    || (vendor == 0x10C4 && product == 0x814A)
                    /* West Mountain Radio RIGtalk */
                    || (vendor == 0x10C4 && product == 0x814B)
                    /* West Mountain Radio RIGblaster Advantage */
                    || (vendor == 0x2405 && product == 0x0003)
                    /* B&G H3000 link cable */
                    || (vendor == 0x10C4 && product == 0x8156)
                    /* Helicomm IP-Link 1220-DVM */
                    || (vendor == 0x10C4 && product == 0x815E)
                    /* Timewave HamLinkUSB */
                    || (vendor == 0x10C4 && product == 0x815F)
                    /* AVIT Research USB to TTL */
                    || (vendor == 0x10C4 && product == 0x818B)
                    /* MJS USB Toslink Switcher */
                    || (vendor == 0x10C4 && product == 0x819F)
                    /* ThinkOptics WavIt */
                    || (vendor == 0x10C4 && product == 0x81A6)
                    /* Multiplex RC Interface */
                    || (vendor == 0x10C4 && product == 0x81A9)
                    /* MSD Dash Hawk */
                    || (vendor == 0x10C4 && product == 0x81AC)
                    /* INSYS USB Modem */
                    || (vendor == 0x10C4 && product == 0x81AD)
                    /* Lipowsky Industrie Elektronik GmbH, Baby-JTAG */
                    || (vendor == 0x10C4 && product == 0x81C8)
                    /* IAI Corp. RCB-CV-USB USB to RS485 Adaptor */
                    || (vendor == 0x10C4 && product == 0x81D7)
                    /* Lipowsky Industrie Elektronik GmbH, Baby-LIN */
                    || (vendor == 0x10C4 && product == 0x81E2)
                    /* Aerocomm Radio */
                    || (vendor == 0x10C4 && product == 0x81E7)
                    /* Zephyr Bioharness */
                    || (vendor == 0x10C4 && product == 0x81E8)
                    /* C1007 HF band RFID controller */
                    || (vendor == 0x10C4 && product == 0x81F2)
                    /* Lipowsky Industrie Elektronik GmbH, HARP-1 */
                    || (vendor == 0x10C4 && product == 0x8218)
                    /* Modem EDGE(GSM) Comander 2 */
                    || (vendor == 0x10C4 && product == 0x822B)
                    /* Cygnal Integrated Products, Inc., Fasttrax GPS demonstration module */
                    || (vendor == 0x10C4 && product == 0x826B)
                    /* Nanotec Plug & Drive */
                    || (vendor == 0x10C4 && product == 0x8281)
                    /* Telegesis ETRX2USB */
                    || (vendor == 0x10C4 && product == 0x8293)
                    /* Starizona MicroTouch */
                    || (vendor == 0x10C4 && product == 0x82F4)
                    /* Procyon AVS */
                    || (vendor == 0x10C4 && product == 0x82F9)
                    /* Siemens MC35PU GPRS Modem */
                    || (vendor == 0x10C4 && product == 0x8341)
                    /* Cygnal Integrated Products, Inc. */
                    || (vendor == 0x10C4 && product == 0x8382)
                    /* Amber Wireless AMB2560 */
                    || (vendor == 0x10C4 && product == 0x83A8)
                    /* DekTec DTA Plus VHF/UHF Booster/Attenuator */
                    || (vendor == 0x10C4 && product == 0x83D8)
                    /* Kyocera GPS Module */
                    || (vendor == 0x10C4 && product == 0x8411)
                    /* IRZ Automation Teleport SG-10 GSM/GPRS Modem */
                    || (vendor == 0x10C4 && product == 0x8418)
                    /* BEI USB Sensor Interface (VCP) */
                    || (vendor == 0x10C4 && product == 0x846E)
                    /* Juniper Networks BX Series System Console */
                    || (vendor == 0x10C4 && product == 0x8470)
                    /* Balluff RFID */
                    || (vendor == 0x10C4 && product == 0x8477)
                    /* Starizona Hyperion */
                    || (vendor == 0x10C4 && product == 0x84B6)
                    /* AC-Services IBUS-IF */
                    || (vendor == 0x10C4 && product == 0x85EA)
                    /* AC-Services CIS-IBUS */
                    || (vendor == 0x10C4 && product == 0x85EB)
                    /* Virtenio Preon32 */
                    || (vendor == 0x10C4 && product == 0x85F8)
                    /* AC-Services CAN-IF */
                    || (vendor == 0x10C4 && product == 0x8664)
                    /* AC-Services OBD-IF */
                    || (vendor == 0x10C4 && product == 0x8665)
                    /* CEL EM357 ZigBee USB Stick - LR */
                    || (vendor == 0x10C4 && product == 0x8856)
                    /* CEL EM357 ZigBee USB Stick */
                    || (vendor == 0x10C4 && product == 0x8857)
                    /* MMB Networks ZigBee USB Device */
                    || (vendor == 0x10C4 && product == 0x88A4)
                    /* Planet Innovation Ingeni ZigBee USB Device */
                    || (vendor == 0x10C4 && product == 0x88A5)
                    /* Ketra N1 Wireless Interface */
                    || (vendor == 0x10C4 && product == 0x8946)
                    /* Brim Brothers charging dock */
                    || (vendor == 0x10C4 && product == 0x8962)
                    /* CEL MeshWorks DevKit Device */
                    || (vendor == 0x10C4 && product == 0x8977)
                    /* KCF Technologies PRN */
                    || (vendor == 0x10C4 && product == 0x8998)
                    /* HubZ dual ZigBee and Z-Wave dongle */
                    || (vendor == 0x10C4 && product == 0x8A2A)
                    /* Silicon Labs factory default */
                    || (vendor == 0x10C4 && product == 0xEA60)
                    /* Silicon Labs factory default */
                    || (vendor == 0x10C4 && product == 0xEA61)
                    /* Silicon Labs factory default */
                    || (vendor == 0x10C4 && product == 0xEA70)
                    /* Infinity GPS-MIC-1 Radio Monophone */
                    || (vendor == 0x10C4 && product == 0xEA71)
                    /* Elan Digital Systems USBscope50 */
                    || (vendor == 0x10C4 && product == 0xF001)
                    /* Elan Digital Systems USBwave12 */
                    || (vendor == 0x10C4 && product == 0xF002)
                    /* Elan Digital Systems USBpulse100 */
                    || (vendor == 0x10C4 && product == 0xF003)
                    /* Elan Digital Systems USBcount50 */
                    || (vendor == 0x10C4 && product == 0xF004)
                    /* Silicon Labs MobiData GPRS USB Modem */
                    || (vendor == 0x10C5 && product == 0xEA61)
                    /* Silicon Labs MobiData GPRS USB Modem 100EU */
                    || (vendor == 0x10CE && product == 0xEA6A)
                    /* Link G4 ECU */
                    || (vendor == 0x12B8 && product == 0xEC60)
                    /* Link G4+ ECU */
                    || (vendor == 0x12B8 && product == 0xEC62)
                    /* Baltech card reader */
                    || (vendor == 0x13AD && product == 0x9999)
                    /* Owen AC4 USB-RS485 Converter */
                    || (vendor == 0x1555 && product == 0x0004)
                    /* Clipsal 5500PACA C-Bus Pascal Automation Controller */
                    || (vendor == 0x166A && product == 0x0201)
                    /* Clipsal 5800PC C-Bus Wireless PC Interface */
                    || (vendor == 0x166A && product == 0x0301)
                    /* Clipsal 5500PCU C-Bus USB interface */
                    || (vendor == 0x166A && product == 0x0303)
                    /* Clipsal 5000CT2 C-Bus Black and White Touchscreen */
                    || (vendor == 0x166A && product == 0x0304)
                    /* Clipsal C-5000CT2 C-Bus Spectrum Colour Touchscreen */
                    || (vendor == 0x166A && product == 0x0305)
                    /* Clipsal L51xx C-Bus Architectural Dimmer */
                    || (vendor == 0x166A && product == 0x0401)
                    /* Clipsal 5560884 C-Bus Multi-room Audio Matrix Switcher */
                    || (vendor == 0x166A && product == 0x0101)
                    /* Lunatico Seletek */
                    || (vendor == 0x16C0 && product == 0x09B0)
                    /* Lunatico Seletek */
                    || (vendor == 0x16C0 && product == 0x09B1)
                    /* Jablotron serial interface */
                    || (vendor == 0x16D6 && product == 0x0001)
                    /* W-IE-NE-R Plein & Baus GmbH PL512 Power Supply */
                    || (vendor == 0x16DC && product == 0x0010)
                    /* W-IE-NE-R Plein & Baus GmbH RCM Remote Control for MARATON Power Supply */
                    || (vendor == 0x16DC && product == 0x0011)
                    /* W-IE-NE-R Plein & Baus GmbH MPOD Multi Channel Power Supply */
                    || (vendor == 0x16DC && product == 0x0012)
                    /* W-IE-NE-R Plein & Baus GmbH CML Control, Monitoring and Data Logger */
                    || (vendor == 0x16DC && product == 0x0015)
                    /* Kamstrup Optical Eye/3-wire */
                    || (vendor == 0x17A8 && product == 0x0001)
                    /* Kamstrup M-Bus Master MultiPort 250D */
                    || (vendor == 0x17A8 && product == 0x0005)
                    /* Wavesense Jazz blood glucose meter */
                    || (vendor == 0x17F4 && product == 0xAAAA)
                    /* Vaisala USB Instrument Cable */
                    || (vendor == 0x1843 && product == 0x0200)
                    /* ELV USB-I2C-Interface */
                    || (vendor == 0x18EF && product == 0xE00F)
                    /* ELV Marble Sound Board 1 */
                    || (vendor == 0x18EF && product == 0xE025)
                    /* GE B850 CP2105 Recorder interface */
                    || (vendor == 0x1901 && product == 0x0190)
                    /* GE B650 CP2104 PMC interface */
                    || (vendor == 0x1901 && product == 0x0193)
                    /* GE Healthcare Remote Alarm Box */
                    || (vendor == 0x1901 && product == 0x0194)
                    /* Parrot NMEA GPS Flight Recorder */
                    || (vendor == 0x19CF && product == 0x3000)
                    /* Schweitzer Engineering C662 Cable */
                    || (vendor == 0x1ADB && product == 0x0001)
                    /* Corsair USB Dongle */
                    || (vendor == 0x1B1C && product == 0x1C00)
                    /* Silicon Labs 358x factory default */
                    || (vendor == 0x1BA4 && product == 0x0002)
                    /* WAGO 750-923 USB Service Cable */
                    || (vendor == 0x1BE3 && product == 0x07A6)
                    /* Seluxit ApS RF Dongle */
                    || (vendor == 0x1D6F && product == 0x0010)
                    /* Festo CPX-USB */
                    || (vendor == 0x1E29 && product == 0x0102)
                    /* Festo CMSP */
                    || (vendor == 0x1E29 && product == 0x0501)
                    /* Lake Shore Model 121 Current Source */
                    || (vendor == 0x1FB9 && product == 0x0100)
                    /* Lake Shore Model 218A Temperature Monitor */
                    || (vendor == 0x1FB9 && product == 0x0200)
                    /* Lake Shore Model 219 Temperature Monitor */
                    || (vendor == 0x1FB9 && product == 0x0201)
                    /* Lake Shore Model 233 Temperature Transmitter */
                    || (vendor == 0x1FB9 && product == 0x0202)
                    /* Lake Shore Model 235 Temperature Transmitter */
                    || (vendor == 0x1FB9 && product == 0x0203)
                    /* Lake Shore Model 335 Temperature Controller */
                    || (vendor == 0x1FB9 && product == 0x0300)
                    /* Lake Shore Model 336 Temperature Controller */
                    || (vendor == 0x1FB9 && product == 0x0301)
                    /* Lake Shore Model 350 Temperature Controller */
                    || (vendor == 0x1FB9 && product == 0x0302)
                    /* Lake Shore Model 371 AC Bridge */
                    || (vendor == 0x1FB9 && product == 0x0303)
                    /* Lake Shore Model 411 Handheld Gaussmeter */
                    || (vendor == 0x1FB9 && product == 0x0400)
                    /* Lake Shore Model 425 Gaussmeter */
                    || (vendor == 0x1FB9 && product == 0x0401)
                    /* Lake Shore Model 455A Gaussmeter */
                    || (vendor == 0x1FB9 && product == 0x0402)
                    /* Lake Shore Model 475A Gaussmeter */
                    || (vendor == 0x1FB9 && product == 0x0403)
                    /* Lake Shore Model 465 Three Axis Gaussmeter */
                    || (vendor == 0x1FB9 && product == 0x0404)
                    /* Lake Shore Model 625A Superconducting MPS */
                    || (vendor == 0x1FB9 && product == 0x0600)
                    /* Lake Shore Model 642A Magnet Power Supply */
                    || (vendor == 0x1FB9 && product == 0x0601)
                    /* Lake Shore Model 648 Magnet Power Supply */
                    || (vendor == 0x1FB9 && product == 0x0602)
                    /* Lake Shore Model 737 VSM Controller */
                    || (vendor == 0x1FB9 && product == 0x0700)
                    /* Lake Shore Model 776 Hall Matrix */
                    || (vendor == 0x1FB9 && product == 0x0701)
                    /* Aruba Networks 7xxx USB Serial Console */
                    || (vendor == 0x2626 && product == 0xEA60)
                    /* Link Instruments MSO-19 */
                    || (vendor == 0x3195 && product == 0xF190)
                    /* Link Instruments MSO-28 */
                    || (vendor == 0x3195 && product == 0xF280)
                    /* Link Instruments MSO-28 */
                    || (vendor == 0x3195 && product == 0xF281)
                    /* DW700 GPS USB interface */
                    || (vendor == 0x413C && product == 0x9500))
            {
                return true;
            }
            return false;
        }

        /*
         * Configuration Request Types
         */
        private static int HOST_TO_DEVICE_REQUEST_TYPE = 0x41;
        private static int ENABLE_INTERFACE = 0x0;
        private static int SET_BAUDRATE = 0x1E;
        private static int SET_LINE_CTL = 0x03;
        private static int SET_MHS = 0x07;
        private static int CONTROL_DTR = 0x01;

        public override bool Open(GXSerial serial, UsbDeviceConnection connection, byte[] rawDescriptors)
        {
            int ret;
            int tmp;
            byte[] data = new byte[4];
            ret = connection.ControlTransfer((UsbAddressing)HOST_TO_DEVICE_REQUEST_TYPE, ENABLE_INTERFACE,
                    1, 0, null, 0,
                    serial.WriteTimeout);
            if (ret != 0)
            {
                throw new System.Exception("Failed to enable interface.");
            }

            int baudRate = serial.BaudRate;
            data[0] = (byte)(baudRate & 0xff);
            data[1] = (byte)((baudRate >> 8) & 0xff);
            data[2] = (byte)((baudRate >> 16) & 0xff);
            data[3] = (byte)((baudRate >> 24) & 0xff);
            ret = connection.ControlTransfer(HOST_TO_DEVICE_REQUEST_TYPE, SET_BAUDRATE,
                    0, 0x0, data, 4,
                    serial.WriteTimeout);
            if (ret != 4)
            {
                throw new System.Exception("Failed to set baudrate: " + ret);
            }

            int value = (serial.DataBits << 8);
            value |= ((int)serial.Parity << 4);
            value |= (int)serial.StopBits;
            ret = connection.ControlTransfer(HOST_TO_DEVICE_REQUEST_TYPE,
                    SET_LINE_CTL, value,
                    0, null, 0, serial.WriteTimeout);
            if (ret != 0)
            {
                throw new System.Exception("Failed to set parity.");
            }

            tmp = CONTROL_DTR << 8 | CONTROL_DTR;
            ret = connection.ControlTransfer(HOST_TO_DEVICE_REQUEST_TYPE, SET_MHS,
                    tmp,
                    0, null, 0, serial.WriteTimeout);
            if (ret != 0)
            {
                throw new System.Exception("Data terminal ready failed.");
            }
            return true;
        }
        public override bool GetDtrEnable(UsbDeviceConnection connection)
        {
            throw new System.NotImplementedException();
        }

        public override void SetDtrEnable(UsbDeviceConnection connection, bool value)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override bool GetRtsEnable(UsbDeviceConnection connection)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override void SetRtsEnable(UsbDeviceConnection connection, bool value)
        {
            throw new System.NotImplementedException();
        }
    }
}