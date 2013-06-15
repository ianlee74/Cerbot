using System;
using Microsoft.SPOT;

namespace IanLee.Pid
{
    public delegate double DoubleDelegate();
    public delegate void PidCalculatedDelegate(double pidValue);

    public interface IPid
    {
        double Kp { get; set; }
        double Ki { get; set; }
        double Kd { get; set; }
        double TargetValue { get; set; }
        double PidValue { get; }
        double Update(double currentValue);
    }
}
