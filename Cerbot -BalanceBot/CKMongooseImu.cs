using System;
using System.IO.Ports;
using System.Text;
using Microsoft.SPOT;
using System.Threading;
using Cerbot.Extensions;

namespace IanLee
{
    class CKMongooseImu
    {
        public delegate void ReceiveData(object rool, object pitch, object yaw, object temp, object pressure);
        public event ReceiveData ReceiveDataEvent;

        private readonly SerialPort _serialPort;

        public double Roll { get; private set; }
        public double Pitch { get; private set; }
        public double Yaw { get; private set; }
        public double Temp { get; private set; }
        public double Pressure { get; private set; }
        public long Errors { get; private set; }

        public CKMongooseImu(string com, int baund)
        {
            _serialPort = new SerialPort(com, baund);
            //_serialPort.DataReceived += OnDataReceived;
            var t = new Thread(PollingParser);
            t.Start();
        }

        public void Open()
        {
            _serialPort.Open();
        }

        public void Close()
        {
            _serialPort.Close();
        }

        public bool IsOpen()
        {
            return (_serialPort.IsOpen);
        }

        public int UpdateFreqency { get; set; }

        private readonly byte[] _buffer = new byte[1024];
        private int _countRead = 0;
        private readonly byte[] _tBuffer = new byte[1024];
        private bool _find;
        private DateTime _startTime = DateTime.Now;
        private int _readingFreqCnt;
        private const int FREQ_CALC_PERIOD = 10;

        private void PollingParser()
        {
            const byte MAX_VAL_SIZE = 8;

            var buffer1 = new byte[1];
            var buffer2 = new byte[1];
            var valBuffer = new byte[MAX_VAL_SIZE];
            //byte[] valBuffer2;
            var valLen = 0;

            while (true)
            {
                if (!_serialPort.IsOpen || _serialPort.BytesToRead < MAX_VAL_SIZE + 1) continue;      // We're looking for something like "!-17.14" (begins with '!', ends with newline)

                // Look for an '!'
                if (buffer2[0] != (byte) '!')
                {
                    _serialPort.Read(buffer1, 0, 1);
                    if (buffer1[0] != (byte) '!') continue;
                }

                // Read value.
                valLen = 0;
                while (true)
                {                    
                    _serialPort.Read(buffer2, 0, 1);

                    // If we get to an '!' before the newline then something is corrupt.
                    if (buffer2[0] == (byte) '!') break;

                    // Are we at the end (newline)?
                    if (buffer2[0] == 13) break;

                    valBuffer[valLen] = buffer2[0];
                    valLen++;
                }
                if (buffer2[0] == (byte) '!') continue;

                //valBuffer2 = new byte[valLen];
                //Array.Copy(valBuffer, 0, valBuffer2, 0, valLen);
                //valBuffer.CopyTo(valBuffer2, 0, valLen);
                Pitch = double.Parse(new string(Encoding.UTF8.GetChars(valBuffer, 0, valLen)));

                // Count the update frequency metric.
                if (_startTime.AddSeconds(FREQ_CALC_PERIOD) < DateTime.Now)
                {
                    UpdateFreqency = _readingFreqCnt / FREQ_CALC_PERIOD;
                    _readingFreqCnt = 0;
                    _startTime = DateTime.Now;
                }
                _readingFreqCnt++;
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            const byte MAX_VAL_SIZE = 6;

            var inBuffer = new byte[1];
            var valBuffer = new byte[MAX_VAL_SIZE];

            while (true)
            {
                if (_serialPort.BytesToRead < MAX_VAL_SIZE + 1) continue; // We're looking for something like "!-17.14" (begins with '!', ends with newline)

                // Look for an '!'
                _serialPort.Read(inBuffer, 0, 1);
                if (inBuffer[0] != (byte) '!') continue;
            }



            var byteread = _serialPort.BytesToRead;
            _serialPort.Read(_tBuffer, 0, byteread);

            _find = false;

            for (var i = 0; i < byteread; i++)
            {
                if (_tBuffer[i] != 10) continue;

                Array.Copy(_tBuffer, 0, _buffer, _countRead, byteread);
                _find = true;
                break;
            }

            if (_find)
            {
                _countRead = 0;
                if ((_buffer[0] == (byte)'!')) //&& (_buffer[1] == (byte)'A') && (_buffer[2] == (byte)'N') && (_buffer[3] == (byte)'G') && (_buffer[4] == (byte)':'))
                {
                    // Count the update frequency metric.
                    if (_startTime.AddSeconds(FREQ_CALC_PERIOD) < DateTime.Now)
                    {
                        UpdateFreqency = _readingFreqCnt / FREQ_CALC_PERIOD;
                        _readingFreqCnt = 0;
                        _startTime = DateTime.Now;
                    }
                    _readingFreqCnt++;

                    try
                    {
                        var str = new string(Encoding.UTF8.GetChars(_buffer));
                        var values = str.Split(',');

                        Roll = double.Parse(values[1]);
                        Pitch = double.Parse(values[2]);
                        Yaw = double.Parse(values[3]);
                        Temp = double.Parse(values[15]);
                        Pressure = double.Parse(values[16]);

                        if (ReceiveDataEvent != null) ReceiveDataEvent(Roll, Pitch, Yaw, Temp, Pressure);
                    }
                    catch
                    {
                        Errors++;
                    }
                }
            }
            else
            {
                Array.Copy(_tBuffer, 0, _buffer, _countRead, byteread);
                _countRead = _countRead + byteread;
            }
        }
    }
}
