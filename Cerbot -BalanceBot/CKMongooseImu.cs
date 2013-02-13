using System;
using Microsoft.SPOT;

namespace NETMFx.Hardware
{
    public class CKMongooseImu
    {
        // Main computed angles
        private const int RX_ROLL = 1;
        private const int RX_PITCH = 2;
        private const int RX_YAW = 3;
        // Corrected sensor data
        private const int RX_ACC_X = 5;
        private const int RX_ACC_Y = 6;
        private const int RX_ACC_Z = 7;
        private const int RX_GYRO_X = 8;
        private const int RX_GYRO_Y = 9;
        private const int RX_GYRO_Z = 10;
        private const int RX_MAG_X = 11;
        private const int RX_MAG_Y = 12;
        private const int RX_MAG_Z = 13;
        private const int RX_MAG_H = 14;
        private const int RX_TEMP = 15;
        private const int RX_PRESSURE = 16;
        // Raw sensor data
        private const int RX_RAW_ACC_X = 18;
        private const int RX_RAW_ACC_Y = 19;
        private const int RX_RAW_ACC_Z = 20;
        private const int RX_RAW_GYRO_X = 21;
        private const int RX_RAW_GYRO_Y = 22;
        private const int RX_RAW_GYRO_Z = 23;
        private const int RX_RAW_MAG_X = 24;
        private const int RX_RAW_MAG_Y = 25;
        private const int RX_RAW_MAG_Z = 26;

        private string[] _receivedData;

        public bool SetData(string dataReceived)
        {
            if (dataReceived == null) return false;
            var tempData = dataReceived.Split(',');
            if (tempData.Length != 27 || tempData[0] != "!ANG:") return false;
            _receivedData = tempData;
            return true;
        }

        public double Roll
        {
            get { return double.Parse(_receivedData[RX_ROLL]); }
        }

        public double Pitch
        {
            get { return double.Parse(_receivedData[RX_PITCH]); }
        }

        public double Yaw
        {
            get { return double.Parse(_receivedData[RX_YAW]); }
        }

        public double AccelerometerX
        {
            get { return double.Parse(_receivedData[RX_ACC_X]); }
        }

        public double AccelerometerY
        {
            get { return double.Parse(_receivedData[RX_ACC_Y]); }
        }

        public double AccelerometerZ
        {
            get { return double.Parse(_receivedData[RX_ACC_Z]); }
        }

        public double GyroscopeX
        {
            get { return double.Parse(_receivedData[RX_GYRO_X]); }
        }

        public double GyroscopeY
        {
            get { return double.Parse(_receivedData[RX_GYRO_Y]); }
        }

        public double GyroscopeZ
        {
            get { return double.Parse(_receivedData[RX_GYRO_Z]); }
        }

        public double MagnetometerX
        {
            get { return double.Parse(_receivedData[RX_MAG_X]); }
        }

        public double MagnetomerY
        {
            get { return double.Parse(_receivedData[RX_MAG_Y]); }
        }

        public double MagnetometerZ
        {
            get { return double.Parse(_receivedData[RX_MAG_Z]); }
        }

        public double MagnetometerHeading
        {
            get { return double.Parse(_receivedData[RX_MAG_H]); }
        }

        public double Temperature
        {
            get { return double.Parse(_receivedData[RX_TEMP]); }
        }

        public double BarometricPressure
        {
            get { return double.Parse(_receivedData[RX_PRESSURE]); }
        }

        public double RawAccelerometerX
        {
            get { return double.Parse(_receivedData[RX_RAW_ACC_X]); }
        }

        public double RawAccelerometerT
        {
            get { return double.Parse(_receivedData[RX_RAW_ACC_Y]); }
        }

        public double RawAccelerometerZ
        {
            get { return double.Parse(_receivedData[RX_RAW_ACC_Z]); }
        }

        public double RawGyroscopeX
        {
            get { return double.Parse(_receivedData[RX_RAW_GYRO_X]); }
        }

        public double RawGyroscopeY
        {
            get { return double.Parse(_receivedData[RX_RAW_GYRO_Y]); }
        }

        public double RawGyroscopeZ
        {
            get { return double.Parse(_receivedData[RX_RAW_GYRO_Z]); }
        }

        public double RawMagnetometerX
        {
            get { return double.Parse(_receivedData[RX_RAW_MAG_X]); }
        }

        public double RawMagnetometerY
        {
            get { return double.Parse(_receivedData[RX_RAW_MAG_Y]); }
        }

        public double RawMagnetometerZ
        {
            get { return double.Parse(_receivedData[RX_RAW_MAG_Z]); }
        }
    }
}
