using System;
using Microsoft.SPOT;
using IanLee.Pid;

namespace IanLee.Cerbot
{
    public class Pid : IPid
    {
        public double GuardGain = 10;
        public double PTerm;
        public double IntegratedError;
        public double ITerm;
        public double DTerm;
        public double LastError;
        public double K = 1;

        public double Kp { get; set; }      // 3
        public double Ki { get; set; }      // 0
        public double Kd { get; set; }      // 0
        public double TargetValue { get; set; }
        public double PidValue {get; set; }

        // PID function from http://www.x-firm.com/?page_id=193
        public double Update(double currentPosition)
        {
            //if (currentPosition < 0) currentPosition = -currentPosition;
            var error = TargetValue - currentPosition;
            PTerm = Kp * error;
            IntegratedError += error;
            ITerm = Ki * Constrain(IntegratedError, -GuardGain, GuardGain);
            DTerm = Kd * (error - LastError);
            LastError = error;
            PidValue = Constrain(K * (PTerm + ITerm + DTerm), -255, 255);
//            _pid = Constrain(_k * (_pTerm + _iTerm + _dTerm), -255, 255);

#if DEBUG
            Debug.Print("K = " + K + " pTerm = " + PTerm + " iTerm = " + ITerm + " dTerm = " + DTerm);
#endif
            return PidValue;
        }

        public static double Constrain(double value, double min, double max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }
    }
}
