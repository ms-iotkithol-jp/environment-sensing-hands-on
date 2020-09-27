using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;

namespace EG.IoT.Grove
{
    public class BlueLEDButton : IDisposable
    {
        GpioController gpioController;
        int ledPin;
        int buttonPin;

        public BlueLEDButton(int ledPin, int buttonPin)
        {
            gpioController = new GpioController();
            
            gpioController.OpenPin(ledPin, PinMode.Output);
            gpioController.OpenPin(buttonPin, PinMode.Input);
            this.ledPin = ledPin;
            this.buttonPin = buttonPin;
        }

        public void TurnOnLed()
        {
            gpioController.Write(ledPin, PinValue.High);
        }
        public void TurnOffLed()
        {
            gpioController.Write(ledPin, PinValue.Low);
        }

        public PinValue ReadButtonStatus()
        {
            var status = gpioController.Read(buttonPin);
            return status;
        }

        public void Dispose()
        {
            gpioController.Dispose();
        }
    }
}
