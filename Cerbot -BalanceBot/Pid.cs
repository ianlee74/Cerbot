namespace Cerbot
{
    public class Pid
    {
        public int GuardGain = 10;
        public int PTerm;
        public int Kp = 3;
        public int IntegratedError;
        public int ITerm;
        public int Ki = 0;
        public int DTerm;
        public int Kd = 0;
        public int LastError;
        public int K = 1;
        public int PidValue = 0;

        // PID function from http://www.x-firm.com/?page_id=193
        public int Update(int targetPosition, int currentPosition)
        {
            //if (currentPosition < 0) currentPosition = -currentPosition;
            var error = targetPosition - currentPosition;
            PTerm = Kp * error;
            IntegratedError += error;
            ITerm = Ki * Constrain(IntegratedError, -GuardGain, GuardGain);
            DTerm = Kd * (error - LastError);
            LastError = error;
            PidValue = Constrain(K * (PTerm + ITerm + DTerm), -255, 255);
//            _pid = Constrain(_k * (_pTerm + _iTerm + _dTerm), -255, 255);

#if DEBUG
            Debug.Print("K = " + K + " pTerm = " + pTerm + " iTerm = " + iTerm + " dTerm = " + dTerm);
#endif
            return PidValue;
        }

        public static int Constrain(int value, int min, int max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }
    }
}
