using System;
using System.Text;
using GHI.OSHW.Hardware;
using System.Threading;
using System.IO.Ports;
using IanLee;
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

        private const int PID_FREQ_CALC_PERIOD = 10;

        private const int BALANCED_PITCH = -1;

        private static CKMongooseImu _imu;
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
            _imu = new CKMongooseImu("COM3", 115200);
            _imu.Open();
            var pid = new Pid();

            Cerbot.InitializeCerbot.Motors();
            Cerbot.InitializeCerbot.ForwardLEDs();

            _button = new InterruptPort(FEZCerberus.Pin.PC14, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
            _button.OnInterrupt += (data1, data2, time) =>
                {
                    pid.Kd++;
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

            var currentDirection = (byte)DIRECTION_HALT;
            var speed = 0;
            var dutyCycle = 0.0;
            var startTime = DateTime.Now;
            var cnt = 0;

            while (true)
            {
                if (startTime.AddSeconds(PID_FREQ_CALC_PERIOD) < DateTime.Now)
                {
                    //UpdateDisplay("PID FREQ: " + cnt / PID_FREQ_CALC_PERIOD, "IMU FREQ: " + _imu.UpdateFreqency);
                    cnt = 0;
                    startTime = DateTime.Now;
                }
                cnt++;

                const int THRESHOLD = 1;
                const int FALLING_THRESHOLD = 80;

                var pitch = (int)_imu.Pitch;
                var pidSpeed = pid.Update(BALANCED_PITCH, pitch);
                
                UpdateDisplay("PIT: " + pitch + " PID: " + pidSpeed);
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
                    speed = Pid.Constrain(MOTOR_MIN_SPEED + (pidSpeed < 0 ? -pidSpeed : pidSpeed), MOTOR_MIN_SPEED, MOTOR_MAX_SPEED);
                    dutyCycle = Cerbot.Motor.Forward(speed);       // Left LEDs
                    //UpdateDisplay(null, "Kd: " + Kd + " FWD DC: " + dutyCycle);
                    currentDirection = DIRECTION_FORWARD;
                }
                else if (pitch < BALANCED_PITCH)
                {
                    speed = Pid.Constrain(MOTOR_MIN_SPEED + pidSpeed, MOTOR_MIN_SPEED, MOTOR_MAX_SPEED);
                    dutyCycle = Cerbot.Motor.Reverse(speed);        // Right LEDs
                    //UpdateDisplay(null, "Kd: " + Kd + " REV DC: " + dutyCycle);
                    currentDirection = DIRECTION_REVERSE;
                }
                //ShowLeds(speed, currentDirection);
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


        private static void ShowLeds(int speed, byte direction)
        {
            Cerbot.ForwardLEDs.SetLEDArray(0xff, 0xff);
            return;
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
}
