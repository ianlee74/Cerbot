using System;
using Microsoft.SPOT;
using IanLee.Pid;

namespace Cerbot___Gadgeteer_BalanceBot
{
/*
    // Source:  http://ghielectronics.com/community/codeshare/entry/726
    public class ChrisTaylorPid : IPid
    {
        const int MotorSpeedFactor = 15;

        // PID factors
        //const double Kp = 1;
        //const double Ki = 0.5;
        //const double Kd = 0.2;
        public double Kp { get; set; }
        public double Ki { get; set; }
        public double Kd { get; set; }

        // PID state variables
        double _prevError;
        double _errorSum;

        // Target angle to maintain balance
        // Tune this to compensate for the balance offset of the FEZ Cerbot
        //const double TargetAngle = 270.81;
        public double TargetValue { get; set; }

        public double PidValue
        {
            get { return _angle; }
        }

        public double Update(double currentValue)
        {
            return 0;
        }

        DateTime _gyroLastReadTime;
        double _angle = 0;
        bool _balancing = false;

        int _speed = 0;
        int _lastSpeed = 0;

        public void Start()
        {
            if (_balancing)
            {
                // If we were already balancing and the button was pressed again
                // we shut everything down. This is like a safety switch.
                controller.SetMotorSpeed(0, 0);
                gyro.StopContinuousMeasurements();
                _balancing = false;
            }
            else
            {
                _balancing = true;

                // Start a thread to handle the balancing process
                new Thread(() =>
                {
                    // Setup the gyro
                    gyro.ContinuousMeasurementInterval = TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond * 25);
                    gyro.MeasurementComplete += gyro_MeasurementComplete;

                    // Get the initial angle of the cerbot from the accelerometer as a starting point
                    // you should hold the cerbot as still as possible at this stage, but it is not overly
                    // sensitive
                    var acceleration = accelerometer.RequestMeasurement();
                    var accelAngle = (System.Math.Atan2(acceleration.Y, acceleration.Z) + System.Math.PI) * RagToDeg;
                    _angle = accelAngle;

                    // Startup the gyro read timer
                    _gyroLastReadTime = DateTime.Now;
                    gyro.StartContinuousMeasurements();

                    // Spin in a loop while we are trying to balance and adjust the motor speed
                    // if there is a new speed reading from the calculations.
                    while (_balancing)
                    {
                        if (_speed != _lastSpeed)
                        {
                            controller.SetMotorSpeed(_speed, _speed);
                            _lastSpeed = _speed;
                        }
                        Thread.Sleep(0);
                    }
                }).Start();
            }
        }
    }
 */
}
