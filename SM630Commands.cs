using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutomaticArduinoSerialMonitor
{
    class Sm630Commands
    {
        //define fingerprint command code
        const byte ADD_FINGERPRINT = 0x40; const byte DELETE_FINGERPRINT = 0x42;
        const byte SearchFingerprint = 0x44; const byte EMPTY_DATABASE = 0x46;
        const byte Matching = 0x4B; const byte DownloadFp = 0x50;
        const byte UploadFp = 0x52; const byte ReadId = 0x60;
        const byte RdFlash = 0x62; const byte WrFlash = 0x64;
        const byte RdLogo = 0x80;

        //define fingerprint response code
        const byte RxCorrect = 0x01; const byte RxError = 0x02;
        const byte OpSuccess = 0x31; const byte FpDetected = 0x32;
        const byte TimeOut = 0x33; const byte ProcessFailed = 0x34;
        const byte ParameterErr = 0x35; const byte Match = 0x37;
        const byte NoMatch = 0x38; const byte FpFound = 0x39;
        const byte FpUnfound = 0x3A;

        public SerialPort ScannerSerialPort { get; set; }
        bool _isSent;
        int _fprintId;
        byte _feedback;
        public bool AddFingerprint(int id)
        {
            Cmd(ADD_FINGERPRINT, id);
            Resp();
            if (_feedback == RxError) return false;
            Resp();
            if (_feedback != OpSuccess) return false;
            Resp();
            if (_feedback != OpSuccess) return false;
            return true;
        }

        public bool DeleteFingerprint(int id)
        {
            Cmd(DELETE_FINGERPRINT, id);
            Resp();
            if (_feedback == RxError) return false;
            Resp();
            if (_feedback != OpSuccess) return false;
            return true;
        }

        public bool EmptyDatabase()
        {
            Cmd(EMPTY_DATABASE);
            Resp();
            if (_feedback == RxError) return false;
            Resp();
            if (_feedback != OpSuccess) return false;
            else return true;
        }

        public bool UploadTemplate(int id, byte[] templ)
        {

            if (id > 768)
            {
                _feedback = ParameterErr;
                return false;
            }

            Cmd(UploadFp, id);
            Resp();
            if (_feedback == RxError) return false;

            try
            {
                for (int i = 0; i < 2; i++)
                {
                    byte[] rx = new byte[133];

                    //while (ScannerSerialPort.BytesToRead < 133) { }
                    if (ScannerSerialPort.BytesToRead > 0)
                    {
                        ScannerSerialPort.Read(rx, 0, ScannerSerialPort.BytesToRead);
                    }

                    for (int j = 0; j < 128; j++)
                    {
                        templ[i * 128 + j] = rx[j + 4];
                    }

                    if (i == 1) break;

                    int[] tx = { 0x4D, 0x58, 0x30, 0x01, 0x01, 0xD7 };

                    byte[] txByte = new byte[tx.Length];

                    for (int j = 0; j < tx.Length; j++)
                    {
                        txByte[j] = Convert.ToByte(tx[j]);
                    }

                    ScannerSerialPort.Write(txByte, 0, txByte.Length);

                    Delay5Sec();
                    //int k = 0;  //A form of delay to allow for serial buffer to load
                    //do { k++; } while (k < 2000);
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }

            _feedback = OpSuccess;
            return true;
        }

        private async void Delay5Sec()
        {
            await Task.Delay(5000);
        }

        private void Cmd(byte cmdCode, int number)
        {
            try
            {

                int packetContent = 0;
                int[] sendCmd;
                byte[] sendCmdByte;

                switch (cmdCode)
                {
                    case SearchFingerprint:
                        packetContent = 5; sendCmd = new int[10]; break;
                    case EMPTY_DATABASE:
                    case ReadId:
                        packetContent = 1; sendCmd = new int[6]; break;
                    default:
                        packetContent = 3; sendCmd = new int[8]; break;
                }

                int hbyte = (byte)(number / 256);
                int lbyte = (byte)(number % 256);
                int checksum = 0;

                for (int i = 0; i < sendCmd.Length; i++) sendCmd[i] = 0;

                sendCmd[0] = 0x4D; sendCmd[1] = 0x58;
                sendCmd[2] = 0x10; sendCmd[3] = packetContent;
                sendCmd[4] = cmdCode;

                for (int i = 0; i < sendCmd.Length; i++)
                {
                    checksum += sendCmd[i];
                }

                if (packetContent >= 3)
                {
                    sendCmd[5] = hbyte; sendCmd[6] = lbyte;
                    checksum += sendCmd[5];
                    checksum += sendCmd[6];

                    if (cmdCode == SearchFingerprint)
                    {
                        for (byte i = 7; i > 4; i--)
                        {
                            sendCmd[i + 2] = sendCmd[i];
                        }
                        sendCmd[5] = 0; sendCmd[6] = 0;
                        checksum += sendCmd[5];
                        checksum += sendCmd[6];
                    }
                }

                if (checksum >= 256) checksum -= 256;

                sendCmd[packetContent + 5 - 1] = checksum;

                sendCmdByte = new byte[sendCmd.Length];

                for (int i = 0; i < sendCmd.Length; i++)
                {
                    sendCmdByte[i] = Convert.ToByte(sendCmd[i]);
                }

                ScannerSerialPort.Write(sendCmdByte, 0, sendCmd.Length);

                if (cmdCode == ADD_FINGERPRINT)
                {
                    do
                    {
                    } while (ScannerSerialPort.BytesToRead != 20);
                }

                _isSent = true;
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }

        }

        private void Cmd(byte cmdCode)
        {
            int packetContent;
            byte[] sendCmd;

            switch (cmdCode)
            {
                case SearchFingerprint:
                    packetContent = 5; sendCmd = new byte[10]; break;
                case EMPTY_DATABASE:
                case ReadId:
                    packetContent = 1; sendCmd = new byte[6]; break;
                default:
                    packetContent = 3; sendCmd = new byte[8]; break;
            }

            byte checksum = 0;

            for (int i = 0; i < sendCmd.Length; i++) sendCmd[i] = 0;

            sendCmd[0] = 0x4D; sendCmd[1] = 0x58;
            sendCmd[2] = 0x10; sendCmd[3] = (byte)packetContent;
            sendCmd[4] = cmdCode;

            for (int i = 0; i < sendCmd.Length; i++)
            {
                checksum += sendCmd[i];
            }

            sendCmd[packetContent + 5 - 1] = checksum;

            ScannerSerialPort.Write(sendCmd, 0, sendCmd.Length);
            _isSent = true;
            _isSent = true;

        }

        private void Resp()
        {
            _fprintId = -1;

            if (_isSent)
            {
                _isSent = false;

                int[] rx = new int[6];

                if (ScannerSerialPort.BytesToRead >= 0)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        rx[i] = (int)ScannerSerialPort.ReadByte();
                    }
                }

                _feedback = rx[3] == 0x01 && rx[4] == RxCorrect ? RxCorrect : RxError;
                return;
            }

            int packetLength = 9;
            int[] resp = new int[packetLength];

            for (int j = 0; j < packetLength; j++)
            {
                if (ScannerSerialPort.BytesToRead > 0)
                {
                    resp[j] = (int)ScannerSerialPort.ReadByte();
                }

                if (j == 3) packetLength = resp[3] + 5;
            }

            if (resp[5] == FpFound) _fprintId = resp[6] * 256 + resp[7];
            _feedback = Convert.ToByte(resp[5]);
        }

        public static string ToHex(int value)
        {
            return String.Format("0x{0:X}", value);
        }

        public static int FromHex(string value)
        {
            // strip the leading 0x
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(2);
            }
            return Int32.Parse(value, NumberStyles.HexNumber);
        }
    }
}
