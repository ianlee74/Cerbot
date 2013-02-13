using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using GHI.OSHW.Hardware;

namespace Cerbot
{
    public static class Cerbot
    {
        // Motors
        private static PWM leftMotor;
        private static PWM rightMotor;
        private static OutputPort leftMotorPolarity;
        private static OutputPort rightMotorPolarity;
        private static bool isCerbotMovingForward;

        // Reflective sensors
        private static AnalogInput leftSensor;
        private static AnalogInput rightSensor;
        private static OutputPort leftIRLED;
        private static OutputPort rightIRLED;

        // Servo motor control
        private static PWM servo;
        private const uint SERVO_FACTOR_IN_us = 19;
        private const uint SERVO_BEGINNING_PULSE_OFFSET_IN_us = 500;
        private static double InvertedSpeed;

        // Forward LEDs
        private static PWM enableFaderPin;
        private static OutputPort shiftRegisterLatch;
        private static SPI.Configuration forwardLEDsConfiguration;
        private static SPI forwardLEDs;
        private static byte[] forwardLEDsDataArray;

        // Buzzer
        private static PWM buzzer;

        public static class InitializeCerbot
        {
            // Motors
            public static void Motors()
            {
                leftMotor = new PWM(Cpu.PWMChannel.PWM_4, 1000, 0.5, false);
                rightMotor = new PWM(Cpu.PWMChannel.PWM_5, 1000, 0.5, false);
                leftMotorPolarity = new OutputPort(FEZCerberus.Pin.PA6, false);
                rightMotorPolarity = new OutputPort(FEZCerberus.Pin.PC4, false);
                isCerbotMovingForward = false;
            }

            // Reflective sensors
            public static void ReflectiveSensors()
            {
                leftSensor = new AnalogInput((Cpu.AnalogChannel)8);
                rightSensor = new AnalogInput(Cpu.AnalogChannel.ANALOG_6);
                leftIRLED = new OutputPort(FEZCerberus.Pin.PB13, true);
                rightIRLED = new OutputPort(FEZCerberus.Pin.PB14, true);
            }

            // Servo motor control
            public static void Servo()
            {
                servo = new PWM(Cpu.PWMChannel.PWM_3, 22500, 500, PWM.ScaleFactor.Microseconds, false);
                InvertedSpeed = 0;
            }

            public static SPI SPI1
            {
                get{ return forwardLEDs; }
            }

            // Forward LEDs
            public static void ForwardLEDs()
            {
                enableFaderPin = new PWM((Cpu.PWMChannel)11, 500, 500, PWM.ScaleFactor.Microseconds, true);
                enableFaderPin.Start();
                shiftRegisterLatch = new OutputPort(FEZCerberus.Pin.PB2, false);
                forwardLEDsConfiguration = new SPI.Configuration(Cpu.Pin.GPIO_NONE, false, 1000, 1000, false, true, 500, SPI.SPI_module.SPI1);
                forwardLEDs = new SPI(forwardLEDsConfiguration);
                forwardLEDsDataArray = new byte[3];
            }

            // Buzzer
            public static void Buzzer()
            {
                buzzer = new PWM((Cpu.PWMChannel)12, 1000, 0.0, false);
            }
            //buzzer.DutyCycle = 0.8;
        }

        public static class ReflectiveSensors
        {
            public static double ReadLeftSensorVoltage()
            {
                return (leftSensor.Read() * 3.3);
            }

            public static double ReadRightSensorVoltage()
            {
                return (rightSensor.Read() * 3.3);
            }
        }

        public static class ForwardLEDs
        {
            public static void SetLeftLEDArray(byte LeftLEDArray)
            {
                forwardLEDsDataArray[1] = LeftLEDArray;
                forwardLEDs.Write(forwardLEDsDataArray);
                Thread.Sleep(40);
                shiftRegisterLatch.Write(true);
                shiftRegisterLatch.Write(false);
            }

            public static void SetRightLEDArray(byte RightLEDArray)
            {
                forwardLEDsDataArray[2] = RightLEDArray;
                forwardLEDs.Write(forwardLEDsDataArray);
                Thread.Sleep(40);
                shiftRegisterLatch.Write(true);
                shiftRegisterLatch.Write(false);
            }

            public static void SetLEDArray(byte LeftLEDArray, byte RightLEDArray)
            {
                forwardLEDsDataArray[2] = RightLEDArray;
                forwardLEDsDataArray[1] = LeftLEDArray;
                forwardLEDs.Write(forwardLEDsDataArray);
                Thread.Sleep(40);
                shiftRegisterLatch.Write(true);
                shiftRegisterLatch.Write(false);
            }

            public static void SetLEDFader(uint fader)
            {
                if (fader > 100 || fader < 1)
                    return;

                enableFaderPin.Duration = 5 * fader;
            }
        }

        public static class Motor
        {
            public static double Forward(double speed)
            {
                if (speed < 0) speed = -speed;      // a negative number was provided.  Make it positive.
                if (speed > 1.0) speed /= 100;      // a non-decimal speed was provided.  Convert it to decimal.
                if (speed > 1) speed = 1;           // if speed is still > 1 then force it to 1 or it will be an invalid duty cycle.

                leftMotor.Stop();
                rightMotor.Stop();

                if (!isCerbotMovingForward)
                {
                    leftMotorPolarity.Write(false);
                    rightMotorPolarity.Write(false);

                    isCerbotMovingForward = true;
                }

#if DEBUG
                Debug.Print("Forward DutyCycle = " + speed);
#endif
                leftMotor.DutyCycle = speed;
                rightMotor.DutyCycle = speed;

                leftMotor.Start();
                rightMotor.Start();

                return speed;
            }

            public static double Reverse(double speed)
            {
                leftMotor.Stop();
                rightMotor.Stop();

                if (isCerbotMovingForward)
                {
                    leftMotorPolarity.Write(true);
                    rightMotorPolarity.Write(true);

                    isCerbotMovingForward = false;
                }

                if (speed >= 1.0) speed /= 100;

                _reverseDutyCycle = 1.0 - speed;

                //Debug.Print("Reverse DutyCycle = " + _reverseDutyCycle);

                leftMotor.DutyCycle = _reverseDutyCycle;
                rightMotor.DutyCycle = _reverseDutyCycle;

                leftMotor.Start();
                rightMotor.Start();

                return _reverseDutyCycle;
            }
            private static double _reverseDutyCycle = 0;

            public static void Halt()
            {
                leftMotorPolarity.Write(false);
                rightMotorPolarity.Write(false);
                leftMotor.Stop();
                rightMotor.Stop();
            }

            public static void Left()
            {
                leftMotor.Stop();
                rightMotor.Stop();

                leftMotorPolarity.Write(true);
                rightMotorPolarity.Write(false);

                isCerbotMovingForward = false;

                leftMotor.DutyCycle = 0.8;
                rightMotor.DutyCycle = 0.2;

                leftMotor.Start();
                rightMotor.Start();

                Thread.Sleep(150);

                leftMotor.Stop();
                rightMotor.Stop();
            }

            public static void Right()
            {
                leftMotor.Stop();
                rightMotor.Stop();

                leftMotorPolarity.Write(false);
                rightMotorPolarity.Write(true);

                isCerbotMovingForward = false;

                leftMotor.DutyCycle = 0.2;
                rightMotor.DutyCycle = 0.8;

                leftMotor.Start();
                rightMotor.Start();

                Thread.Sleep(150);

                leftMotor.Stop();
                rightMotor.Stop();
            }
        }

        public static class Buzzer
        {
            public static void Start()
            {
                buzzer.Start();
            }

            public static void Stop()
            {
                buzzer.Stop();
            }

            public static void SetFrequencyDutyCycle(double frequency, double dutyCycle)
            {
                buzzer.Stop();
                buzzer.Frequency = frequency;
                if (dutyCycle > 1.0)
                    dutyCycle /= 100;
                buzzer.DutyCycle = dutyCycle;
                buzzer.Start();
            }
        }

        public static class Servo
        {
            public static void Start()
            {
                servo.Start();
            }

            public static void Stop()
            {
                servo.Stop();
            }

            public static void Set(uint ammountOfTurn)
            {
                if (ammountOfTurn > 100 || ammountOfTurn < 1)
                    return;

                servo.Duration = (SERVO_FACTOR_IN_us * ammountOfTurn) + SERVO_BEGINNING_PULSE_OFFSET_IN_us;
            }
        }
    }
}