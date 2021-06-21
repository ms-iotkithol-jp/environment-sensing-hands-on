using System;
using System.Collections.Generic;
using System.Text;

namespace EG.IoT.Grove
{
    public class GrovePiLightSensor
    {
        GrovePiPlus grovePiPlus;
        GrovePiPlus.Pin sensorPin;

        public GrovePiLightSensor(GrovePiPlus shield, int sensorPin)
        {
            this.grovePiPlus = shield;
            this.sensorPin = (GrovePiPlus.Pin)sensorPin;
            this.grovePiPlus.SetPinMode(this.sensorPin, GrovePiPlus.PinMode.Output);
        }
        
        public int SensorValue()
        {
            return grovePiPlus.AnalogRead(sensorPin);
        }

        public double Resitance(int sensorValue)
        {
            return (double)(1023 - sensorValue) * 10 / sensorValue;
        }
    }
}
