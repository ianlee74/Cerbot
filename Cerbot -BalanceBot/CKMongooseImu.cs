using System;
using System.IO.Ports;
using System.Text;
using Microsoft.SPOT;

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
            _serialPort.DataReceived += OnDataReceived;
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
        private int _cnt;

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
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
                if ((_buffer[0] == (byte)'!') && (_buffer[1] == (byte)'A') && (_buffer[2] == (byte)'N') && (_buffer[3] == (byte)'G') && (_buffer[4] == (byte)':'))
                {
                    // Count the update frequency metric.
                    if (_startTime.AddSeconds(5) < DateTime.Now)
                    {
                        UpdateFreqency = _cnt / 5;
                        _cnt = 0;
                        _startTime = DateTime.Now;
                    }
                    _cnt++;

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
