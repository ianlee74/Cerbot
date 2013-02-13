using System;
using System.Text;
using GHI.OSHW.Hardware;
using System.Threading;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;

namespace Cerbot
{
    public class Program
    {
        private const int MOTOR_MIN_SPEED = 40;
        private const int MOTOR_MAX_SPEED = 100;

        const int DIRECTION_HALT = 0;
        const int DIRECTION_FORWARD = 1;
        const int DIRECTION_REVERSE = 2;

        private const int BALANCED_PITCH = -7;

        private static MongooseImu _ckdevice;
        private static InterruptPort _button;

        private static HD44780_Display _display;
        private static OutputPort _lcdRS;
        private static OutputPort _lcdE;
        private static OutputPort _lcdD4;
        private static OutputPort _lcdD5;
        private static OutputPort _lcdD6;
        private static OutputPort _lcdD7;
        private static OutputPort _lcdBacklight;

        public static void Main()
        {
            // Initialize display (can't use until Cerbot gets +5V)
            _lcdRS = new OutputPort(FEZCerberus.Pin.PC1, false);            // Socket.Pin.Four
            _lcdE = new OutputPort(FEZCerberus.Pin.PC0, false);             // Socket.Pin.Three
            _lcdD4 = new OutputPort(FEZCerberus.Pin.PA4, false);            // Socket.Pin.Five
            _lcdD5 = new OutputPort(FEZCerberus.Pin.PC6, false);            // Socket.Pin.Seven
            _lcdD6 = new OutputPort(FEZCerberus.Pin.PC7, false);            // Socket.Pin.Nine
            _lcdD7 = new OutputPort(FEZCerberus.Pin.PC5, false);            // Socket.Pin.Six
            _lcdBacklight = new OutputPort(FEZCerberus.Pin.PA7, true);
            _display = new HD44780_Display(_lcdRS, _lcdE, _lcdD4, _lcdD5, _lcdD6, _lcdD7, _lcdBacklight);
            UpdateDisplay("Cerbot says,", "    hello world!");

            // Initialize IMU
            _ckdevice = new MongooseImu("COM3", 115200);
            _ckdevice.open();

            Cerbot.InitializeCerbot.Motors();
            Cerbot.InitializeCerbot.ForwardLEDs();

            _button = new InterruptPort(FEZCerberus.Pin.PC14, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
            _button.OnInterrupt += (data1, data2, time) =>
                {
                    Kd++;
                };

            // LED tester
            byte led = 1;
            for (byte l = 0; l < 8; l++)
            {
                led <<= 1;
                Cerbot.ForwardLEDs.SetLEDArray((byte)led, (byte)led);
                //Cerbot.ForwardLEDs.SetRightLEDArray((byte)(led << l));
                Thread.Sleep(200);
            }
            //return;

            _display.Clear();

            var currentDirection = DIRECTION_HALT;
            var speed = 0;
            var dutyCycle = 0.0;
            var startTime = DateTime.Now;
            var cnt = 0;

            while (true)
            {
                if (startTime.AddSeconds(5) < DateTime.Now)
                {
                    UpdateDisplay("FREQ: " + cnt/5);
                    cnt = 0;
                    startTime = DateTime.Now;
                }
                cnt++;

                const int THRESHOLD = 1;
                const int FALLING_THRESHOLD = 80;

                var pitch = (int)_ckdevice.Pitch;
                var pidSpeed = UpdatePid(BALANCED_PITCH, pitch);
                
                //UpdateDisplay("PIT: " + pitch + " PID: " + pidSpeed);
#if DEBUG
                Debug.Print("ROLL: " + (int)_ckdevice.Roll + "  YAW: " + (int)_ckdevice.Yaw + "  PITCH: " + pitch + "  ERRS: " + _ckdevice.Errors + "  PID: " + pidSpeed);
#endif
                if (pidSpeed == 0   // balanced
                    ||(pitch > FALLING_THRESHOLD || pitch < -FALLING_THRESHOLD))  /* falling */
                {
                    speed = 0;
                    Cerbot.Motor.Halt();
                    //if(pidSpeed == 0) UpdateDisplay("Look ma!        ", "       No hands!");
                    //else UpdateDisplay("I've fallen and ", " I can't get up!");
                    currentDirection = DIRECTION_HALT;
                }
                else if (pitch > BALANCED_PITCH)
                {
                    speed = Constrain(MOTOR_MIN_SPEED + (pidSpeed < 0 ? -pidSpeed : pidSpeed), MOTOR_MIN_SPEED, MOTOR_MAX_SPEED);
                    //dutyCycle = Cerbot.Motor.Forward(speed);       // Left LEDs
                    //UpdateDisplay(null, "Kd: " + Kd + " FWD DC: " + dutyCycle);
                    currentDirection = DIRECTION_FORWARD;
                }
                else if (pitch < BALANCED_PITCH)
                {
                    speed = Constrain(MOTOR_MIN_SPEED + pidSpeed, MOTOR_MIN_SPEED, MOTOR_MAX_SPEED);
                    //dutyCycle = Cerbot.Motor.Reverse(speed);        // Right LEDs
                    //UpdateDisplay(null, "Kd: " + Kd + " REV DC: " + dutyCycle);
                    currentDirection = DIRECTION_REVERSE;
                }
                ShowLeds((byte)speed, (byte)currentDirection);
                //Thread.Sleep(250);
            }
        }

        private static void UpdateDisplay(string line1 = null, string line2 = null)
        {
            if (line1 != null)
            {
                _display.SetCursor(0,0);
                _display.PrintString(line1);
            }

            if (line2 != null)
            {
                _display.SetCursor(1,0);
                _display.PrintString(line2);
            }
        }

        private const int GUARD_GAIN = 10;
        private static int pTerm;
        private static int Kp = 3;
        private static int integrated_error;
        private static int iTerm;
        private static int Ki = 0;
        private static int dTerm ;
        private static int Kd = 0;
        private static int last_error;
        private static int K = 1;
        private static int pid = 0;

        // PID function from http://www.x-firm.com/?page_id=193
        private static int UpdatePid(int targetPosition, int currentPosition)
        {
            //if (currentPosition < 0) currentPosition = -currentPosition;
            var error = targetPosition - currentPosition;
            pTerm = Kp * error;
            integrated_error += error;
            iTerm = Ki * Constrain(integrated_error, -GUARD_GAIN, GUARD_GAIN);
            dTerm = Kd * (error - last_error);
            last_error = error;
            pid = Constrain(K * (pTerm + iTerm + dTerm), -255, 255);

#if DEBUG
            Debug.Print("K = " + K + " pTerm = " + pTerm + " iTerm = " + iTerm + " dTerm = " + dTerm);
#endif
            return pid;
            //return -Constrain(K * (pTerm + iTerm + dTerm), -255, 255);
        }

        private static int Constrain(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static void ShowLeds(int speed, byte direction)
        {
            var level = speed/10;
            level = level < 0 ? -level : level;

#if DEBUG
            Debug.Print("speed = " + speed + "  direction = " + direction + "  level = " + level);
#endif

            var leftLeds = (byte) 0;
            var rightLeds = (byte) 0;

            if (direction == DIRECTION_FORWARD)
            {
                leftLeds = (byte) (1 << level);
                rightLeds = 0x00;
            }
            else if (direction == DIRECTION_REVERSE)
            {
                leftLeds = 0x00;
                rightLeds = (byte)(1 << level);
            }
            else            // Balanced
            {
                leftLeds = 0xff;
                rightLeds = 0xff;
            }
            Cerbot.ForwardLEDs.SetLEDArray(leftLeds, rightLeds);
        }
    }

    class MongooseImu
    {
        public delegate void ReceiveData(object _rool, object _pitch, object _yaw, object _temp, object _pressure);
        public event ReceiveData ReceiveDataEvent;

        private readonly SerialPort _serialPort;

        public double Roll { get; private set; }
        public double Pitch { get; private set; }
        public double Yaw { get; private set; }
        public double Temp { get; private set; }
        public double Pressure { get; private set; }
        public long Errors { get; private set; }

        public MongooseImu(string com, int baund)
        {
            _serialPort = new SerialPort(com, baund);
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(SERIALRECEIVE);
        }

        public void open()
        {
            _serialPort.Open();
        }

        public void close()
        {
            _serialPort.Close();
        }
        public bool IsOpen()
        {
            return (_serialPort.IsOpen);
        }



        private byte[] buffer = new byte[1024];
        private int countRead = 0;
        private byte[] Tbuffer = new byte[1024];
        private bool find = false;


        private void SERIALRECEIVE(object sender, SerialDataReceivedEventArgs e)
        {
            var byteread = _serialPort.BytesToRead;
            _serialPort.Read(Tbuffer, 0, byteread);

            find = false;

            for (int i = 0; i < byteread; i++)
            {
                if (Tbuffer[i] == 10)
                {
                    Array.Copy(Tbuffer, 0, buffer, countRead, byteread);
                    find = true;
                    break;
                }
            }

            if (find)
            {
                countRead = 0;
                if ((buffer[0] == (byte)'!') && (buffer[1] == (byte)'A') && (buffer[2] == (byte)'N') && (buffer[3] == (byte)'G') && (buffer[4] == (byte)':'))
                {
                    try
                    {
                        var str = new string(Encoding.UTF8.GetChars(buffer));
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
                Array.Copy(Tbuffer, 0, buffer, countRead, byteread);
                countRead = countRead + byteread;
            }
        }
    }
    /*
    public class Program
    {
        static SerialPort _imuSerialPort;
        private static CKMongooseImu _imu;

        public static void Main()
        {
            Debug.Print("Program started.");

            _imuSerialPort = new SerialPort("COM3", 9600, Parity.None, (int)StopBits.One);
            _imuSerialPort.Open();
            //_imuSerialPort.DataReceived += OnImuSerialPortDataReceived;

            _imu = new CKMongooseImu();

            //Cerbot.InitializeCerbot.Motors();
            //Cerbot.Motor.Forward(70);
            //Thread.Sleep(500);
            //Cerbot.Motor.Reverse(50);
            //Thread.Sleep(500);
            //Cerbot.Motor.Halt();

            while (true)
            {
                if (_imuSerialPort.BytesToRead <= 0) continue;
                _imuSerialPort.Read(__buffer, 0, 1);
                __charBuffer = Encoding.UTF8.GetChars(__buffer);
                Debug.Print(__charBuffer[0].ToString());
            }

            // Monitor the IMU.
            while (true)
            {
                if (_imuLastSentence != "")
                {
                    _imu.SetData(_imuLastSentence);
                    Debug.Print(_imu.Roll.ToString());
                }
                Thread.Sleep(200);
            }
        }

        private static byte[] __buffer = new byte[1];
        private static char[] __charBuffer = new char[1];

        private static string _imuBuffer = "";
        private static string _imuLastSentence = "";
        private static int _imuBytesReceived;
        private static char[] _inputBuffer = new char[500];
        private static byte[] _byteBuffer = new byte[1024];

        private static void OnImuSerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            const string START_MARKER = "!ANG:";

            Thread.Sleep(20);
            // Read all data and buffer to a string.
            if (!_imuSerialPort.IsOpen) return;
            _imuBytesReceived = ((SerialPort)sender).BytesToRead;
            _byteBuffer = new byte[_imuBytesReceived];
            ((SerialPort)sender).Read(_byteBuffer, 0, _imuBytesReceived);
            _inputBuffer = Encoding.UTF8.GetChars(_byteBuffer);
            var strBuffer = new string(_inputBuffer);
            // See if the buffer contains a start marker.
            if (strBuffer == null) return;
            var start = strBuffer.IndexOf(START_MARKER);
            if (start >= 0)
            {
                if (_imuBuffer == "")
                {
                    // Assumes there are not more than one start marker in a single message.
                    _imuBuffer = strBuffer.Substring(start, strBuffer.Length - start);
                }
                else
                {
                    if (start > 0)          // start marker is not the first thing in the message.
                    {
                        _imuBuffer += strBuffer.Substring(0, start);
                    }
                    _imuLastSentence = _imuBuffer;
                    _imuBuffer = "";
                }
            }
#if DEBUG
            //            Debug.Print(_imuLastSentence);
#endif
        }
    }
    */
}
