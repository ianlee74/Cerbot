using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;

using IanLee;
using IanLee.Cerbot;
using IanLee.Pid;

namespace Cerbot___Gadgeteer_BalanceBot
{
    public partial class Program
    {
        private const bool MOTORS_ENABLED = true;
        private const double MOTOR_MIN_SPEED = 40;
        private const double MOTOR_MAX_SPEED = 100;

        private const int DIRECTION_HALT = 0;
        private const int DIRECTION_FORWARD = 1;
        private const int DIRECTION_REVERSE = 2;

        private const int PID_FREQ_CALC_PERIOD = 10;
        private const double BALANCED_PITCH = -2.3;
        private const double FALLING_THRESHOLD = 50;

        private static CKMongooseImu _imu;

        void ProgramStarted()
        {
            Debug.Print("Program Started");

            _imu = new CKMongooseImu("COM2", 115200);
            _imu.Open();

            // LED tester
            ushort led = 1;
            for (byte l = 0; l < 16; l++)
            {
                led <<= 1;
                cerbotController.SetLEDBitmask(led);
                Thread.Sleep(20);
            }
            // return;

            var pidThread = new Thread(RunPid);
            pidThread.Start();
        }

        private void RunPid()
        {
            var pid = (IPid)(new Pid() { Kp = 2, Ki = 0, Kd = 0, TargetValue = BALANCED_PITCH });
            var currentDirection = (byte)DIRECTION_HALT;
            var speed = 0;
            var dutyCycle = 0.0;
            var startTime = DateTime.Now;
            var cnt = 0;

            button.ButtonReleased += (sender, state) => pid.Kd += 0.1;

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

                var pitch = _imu.Pitch;
                var pidSpeed = pid.Update(pitch);

                Debug.Print("PIT: " + pitch + " PID: " + pidSpeed);
#if DEBUG
                //Debug.Print("ROLL: " + (int)_ckdevice.Roll + "  YAW: " + (int)_ckdevice.Yaw + "  PITCH: " + pitch + "  ERRS: " + _ckdevice.Errors + "  PID: " + pidSpeed);
#endif
                if (pidSpeed == 0   // balanced
                    || (pitch > FALLING_THRESHOLD || pitch < -FALLING_THRESHOLD))  /* falling */
                {
                    speed = 0;
                    cerbotController.SetMotorSpeed(0, 0);
                    //if(pidSpeed == 0) UpdateDisplay("Look ma!        ", "       No hands!");
                    //else UpdateDisplay("I've fallen and ", " I can't get up!");
                    currentDirection = DIRECTION_HALT;
                }
                else if (pitch > BALANCED_PITCH)
                {
                    speed = (int)(Pid.Constrain(MOTOR_MIN_SPEED + (pidSpeed < 0 ? -pidSpeed : pidSpeed), MOTOR_MIN_SPEED, MOTOR_MAX_SPEED));
                    if(MOTORS_ENABLED) cerbotController.SetMotorSpeed(speed, speed);       // Left LEDs
                    //UpdateDisplay(null, "Kd: " + Kd + " FWD DC: " + dutyCycle);
                    currentDirection = DIRECTION_FORWARD;
                }
                else if (pitch < BALANCED_PITCH)
                {
                    speed = (int)(Pid.Constrain(MOTOR_MIN_SPEED + pidSpeed, MOTOR_MIN_SPEED, MOTOR_MAX_SPEED));
                    if(MOTORS_ENABLED) cerbotController.SetMotorSpeed(-speed, -speed);        // Right LEDs
                    //UpdateDisplay(null, "Kd: " + Kd + " REV DC: " + dutyCycle);
                    currentDirection = DIRECTION_REVERSE;
                }
                //ShowLeds(speed, currentDirection);
            }
        }

        private static void UpdateDisplay(string line1 = null, string line2 = null)
        {
            return;
            //if (line1 != null)
            //{
            //    _display.SetCursor(0,0);
            //    _display.PrintString(line1);
            //}

            //if (line2 != null)
            //{
            //    _display.SetCursor(1,0);
            //    _display.PrintString(line2);
            //}
        }

        private void ShowLeds(int speed, byte direction)
        {
            cerbotController.SetLEDBitmask(0xff);
            return;
            var level = speed / 10;
            level = level < 0 ? -level : level;

#if DEBUG
            Debug.Print("speed = " + speed + "  direction = " + direction + "  level = " + level);
#endif

            var leftLeds = (byte)0;
            var rightLeds = (byte)0;

            if (direction == DIRECTION_FORWARD)
            {
                leftLeds = (byte)(1 << level);
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
            cerbotController.SetLEDBitmask(leftLeds);       // TODO: update this function to use the new single ushort instead of two bytes.
        }
    }
}
