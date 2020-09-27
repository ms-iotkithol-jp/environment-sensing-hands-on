using System;
using System.Collections.Generic;
using System.Text;

namespace EG.IoT.Grove
{
    public class GrovePiPlusBlueLEDButton : IDisposable
    {
        private GrovePiPlus grovePiPlus;
        private GrovePiPlus.Pin ledPin;
        private GrovePiPlus.Pin buttonPin;

        public GrovePiPlusBlueLEDButton(GrovePiPlus shield, int ledPin, int buttonPin)
        {
            grovePiPlus = shield;
            this.ledPin = (GrovePiPlus.Pin)ledPin;
            this.buttonPin = (GrovePiPlus.Pin)buttonPin;
            grovePiPlus.SetPinMode(this.ledPin, GrovePiPlus.PinMode.Output);
            grovePiPlus.SetPinMode(this.buttonPin, GrovePiPlus.PinMode.Input);
        }

        public void TurnOn()
        {
            grovePiPlus.DigitalWrite(this.ledPin, 1);
        }
        public void TurnOff()
        {
            grovePiPlus.DigitalWrite(this.ledPin, 0);
        }

        public int GetButtonStatus()
        {
            return grovePiPlus.DigitalRead(this.buttonPin);
        }

        public void Dispose()
        {
            grovePiPlus.Dispose();
        }
    }
}
