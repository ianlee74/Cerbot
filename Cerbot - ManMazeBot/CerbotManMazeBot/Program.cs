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

namespace CerbotManMazeBot
{
    public partial class Program
    {
        private enum Direction
        {
            Unknown,
            Forward,
            Reverse,
            Left,
            Right
        }

        private Direction[] _directions = new Direction[20];
        private byte _dirCnt = 0;

        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            Debug.Print("Program Started");

            GTM.GHIElectronics.Joystick.Position pos;
            var joyTimer = new Gadgeteer.Timer(100);
            joyTimer.Tick += t =>
                {
                    pos = joystick.GetJoystickPosition();
                    Debug.Print("X: " + pos.X + "   Y: " + pos.Y);
                };
            //joyTimer.Start();

            // When the button is pushed, add a new direction.
            Direction direction;
            button.ButtonReleased += (b, s) =>
                {
                    direction = GetDirection();
                    _directions[_dirCnt] = direction;
                    Debug.Print(DirectionToString(_directions[_dirCnt]));
                    _dirCnt++;
                };

            // When the joystick button is pressed, play the moves.
            joystick.JoystickReleased += (j, s) =>
                {
                    for (var i = 0; i < _dirCnt; i++ )
                    {
                        Debug.Print(DirectionToString(_directions[i]));
                        Move(_directions[i]);
                    }
                };
        }

        private string DirectionToString(Direction direction)
        {
            switch (direction)
            {
                case Direction.Forward:
                    return "Forward";
                case Direction.Reverse:
                    return "Reverse";
                case Direction.Left:
                    return "Left";
                case Direction.Right:
                    return "Right";
                default:
                    return "Unknown";
            }
        }

        private void Move(Direction direction)
        {
            const int SPEED = 100;

            switch (direction)
            {
                case Direction.Forward:
                    cerbotController.SetMotorSpeed(10, SPEED);
                    break;
                case Direction.Reverse:
                    cerbotController.SetMotorSpeed(-SPEED, -SPEED);
                    break;
                case Direction.Left:
                    TurnLeft();
                    Move(Direction.Forward);
                    return;
                case Direction.Right:
                    TurnRight();
                    Move(Direction.Forward);
                    return;
            }
            Thread.Sleep(1000);
            cerbotController.SetMotorSpeed(0,0);
        }

        private void TurnLeft()
        {
            cerbotController.SetMotorSpeed(0, 100);
            Thread.Sleep(500);
            cerbotController.SetMotorSpeed(0, 0);
        }

        private void TurnRight()
        {
            
        }

        private Direction GetDirection()
        {
            var pos = joystick.GetJoystickPosition();
            if (pos.X > .75) return Direction.Forward;
            if (pos.X < .25) return Direction.Reverse;
            if(pos.Y > .75) return Direction.Left;
            if(pos.Y < .25) return Direction.Right;
            return Direction.Unknown;
        }

    }
}
