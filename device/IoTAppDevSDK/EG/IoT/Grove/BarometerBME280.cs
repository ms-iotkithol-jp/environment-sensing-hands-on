using System;
using System.Collections.Generic;
using System.Text;
using System.Buffers.Binary;
using System.Device.I2c;
using Iot.Device.Common;

namespace EG.IoT.Grove
{
	/// <summary>
	/// Reference:
	/// https://www.mouser.jp/datasheet/2/783/BST-BME280-DS002-1509607.pdf
	/// https://qiita.com/yutoakaut/items/7bef27a40633f81a67dc
	/// </summary>
	public class BarometerBME280 : IDisposable
	{
		private CariblationData calibrationData;
		private I2cDevice i2cDevice;

		private double temperature;
		private double humidity;
		private double pressure;

		public BarometerBME280(int busId)
        {
			var i2cConnectionString = new I2cConnectionSettings(busId, 0x76);
			i2cDevice = I2cDevice.Create(i2cConnectionString);
			calibrationData = new CariblationData();
		}

		private readonly byte BME280_REG_DIG_T1 = 0x88;
		private readonly byte BME280_REG_DIG_T2 = 0x8A;
		private readonly byte BME280_REG_DIG_T3 = 0x8C;
//		private readonly byte BME280_REG_CHIPID = 0xD0;
		private readonly byte BME280_REG_CONTROLHUMID = 0xF2;
		private readonly byte BME280_REG_CONTROL = 0xF4;
		private readonly byte BME280_REG_TEMPDATA = 0xFA;

		private readonly byte BME280_REG_DIG_P1 = 0x8E;
		private readonly byte BME280_REG_DIG_P2 = 0x90;
		private readonly byte BME280_REG_DIG_P3 = 0x92;
		private readonly byte BME280_REG_DIG_P4 = 0x94;
		private readonly byte BME280_REG_DIG_P5 = 0x96;
		private readonly byte BME280_REG_DIG_P6 = 0x98;
		private readonly byte BME280_REG_DIG_P7 = 0x9A;
		private readonly byte BME280_REG_DIG_P8 = 0x9C;
		private readonly byte BME280_REG_DIG_P9 = 0x9E;
		private readonly byte BME280_REG_PRESSUREDATA = 0xF7;

		private readonly byte BME280_REG_DIG_H1 = 0xA1;
		private readonly byte BME280_REG_DIG_H2 = 0xE1;
		private readonly byte BME280_REG_DIG_H3 = 0xE3;
		private readonly byte BME280_REG_DIG_H4 = 0xE4;
		private readonly byte BME280_REG_DIG_H5 = 0xE5;
		private readonly byte BME280_REG_DIG_H6 = 0xE7;
		private readonly byte BME280_REG_HUMIDITYDATA = 0xFD;

		public void Initialize()
		{
			calibrationData.dig_T1 = ReadUnsigned16BitsFromRegisterBE(BME280_REG_DIG_T1);
			calibrationData.dig_T2 = ReadSigned16BitsFromRegisterBE(BME280_REG_DIG_T2);
			calibrationData.dig_T3 = ReadSigned16BitsFromRegisterBE(BME280_REG_DIG_T3);

			calibrationData.dig_P1 = ReadUnsigned16BitsFromRegisterBE(BME280_REG_DIG_P1);
			calibrationData.dig_P2 = ReadSigned16BitsFromRegisterBE(BME280_REG_DIG_P2);
			calibrationData.dig_P3 = ReadSigned16BitsFromRegisterBE(BME280_REG_DIG_P3);
			calibrationData.dig_P4 = ReadSigned16BitsFromRegisterBE(BME280_REG_DIG_P4);
			calibrationData.dig_P5 = ReadSigned16BitsFromRegisterBE(BME280_REG_DIG_P5);
			calibrationData.dig_P6 = ReadSigned16BitsFromRegisterBE(BME280_REG_DIG_P6);
			calibrationData.dig_P7 = ReadSigned16BitsFromRegisterBE(BME280_REG_DIG_P7);
			calibrationData.dig_P8 = ReadSigned16BitsFromRegisterBE(BME280_REG_DIG_P8);
			calibrationData.dig_P9 = ReadSigned16BitsFromRegisterBE(BME280_REG_DIG_P9);

			calibrationData.dig_H1 = ReadUnsigned8BitsFromRegister(BME280_REG_DIG_H1);
			calibrationData.dig_H2 = ReadSigned16BitsFromRegisterBE(BME280_REG_DIG_H2);
			calibrationData.dig_H3 = ReadUnsigned8BitsFromRegister(BME280_REG_DIG_H3);

			byte temp8h, temp8l;
			temp8h = ReadUnsigned8BitsFromRegister(BME280_REG_DIG_H4);
			temp8l = ReadUnsigned8BitsFromRegister((byte)((int)BME280_REG_DIG_H4 + 1));
			calibrationData.dig_H4 = (short)((temp8h << 4) | (temp8l & 0x0f));
			temp8h = ReadUnsigned8BitsFromRegister((byte)((int)BME280_REG_DIG_H5 + 1));
			temp8l = ReadUnsigned8BitsFromRegister(BME280_REG_DIG_H5);
			calibrationData.dig_H5 = (short)((temp8h << 4) | (0x0f & temp8l >> 4));
			calibrationData.dig_H6 = ReadSigned8BitsFromRegister(BME280_REG_DIG_H6);

			Span<byte> command05 = stackalloc byte[]
			{
				BME280_REG_CONTROLHUMID,
				0x05
			};
			i2cDevice.Write(command05);
			Span<byte> commandb7 = stackalloc byte[]
			{
				BME280_REG_CONTROL,
				0xb7
			};
			i2cDevice.Write(commandb7);
		}

		public void Read()
        {
			this.temperature = double.NaN;
			ushort dig_T1;
			short dig_T2;
			short dig_T3;
			dig_T1 = ReadUnsigned16BitsFromRegisterBE(BME280_REG_DIG_T1);
			dig_T2 = ReadSigned16BitsFromRegisterBE(BME280_REG_DIG_T2);
			dig_T3 = ReadSigned16BitsFromRegisterBE(BME280_REG_DIG_T3);

			int adc_T;
			adc_T = ReadSigned24BitsFromRegisterBE(BME280_REG_TEMPDATA);

			// adc_T >>= 4;
			int var1 = (((adc_T >> 3) - ((int)(dig_T1 << 1))) * ((int)dig_T2)) >> 11;
			int var2 = (((((adc_T >> 4) - ((int)dig_T1)) * ((adc_T >> 4) - ((int)dig_T1))) >> 12) * ((int)dig_T3)) >> 14;

			int t_fine = var1 + var2;
			this.temperature = (double)((t_fine * 5 + 128) >> 8) / 100;



			// Read Pressure
			int adc_P;
			this.pressure = double.NaN;
			adc_P = ReadSigned24BitsFromRegisterBE(BME280_REG_PRESSUREDATA);
			// adc_P >>= 4;

			long var64_1, var64_2, p;
			var64_1 = ((long)t_fine) - 128000;
			var64_2 = var64_1 * var64_1 * (long)this.calibrationData.dig_P6;
			var64_2 = var64_2 + ((var64_1 * (long)this.calibrationData.dig_P5) << 17);
			var64_2 = var64_2 + (((long)this.calibrationData.dig_P4) << 35);
			var64_1 = ((var64_1 * var64_1 * (long)this.calibrationData.dig_P3) >> 8) + ((var64_1 * (long)this.calibrationData.dig_P2) << 12);
			var64_1 = (((((long)1) << 47) + var64_1)) * ((long)this.calibrationData.dig_P1) >> 33;
			if (var64_1 == 0)
			{
				return; // avoid exception caused by division by zero
			}
			p = 1048576 - adc_P;
			p = (((p << 31) - var64_2) * 3125) / var64_1;
			var64_1 = (((long)this.calibrationData.dig_P9) * (p >> 13) * (p >> 13)) >> 25;
			var64_2 = (((long)this.calibrationData.dig_P8) * p) >> 19;
			p = ((p + var64_1 + var64_2) >> 8) + (((long)this.calibrationData.dig_P7) << 4);
			this.pressure = (double)p / (double)256;

			// For Humidity
			this.humidity = double.NaN;
			int adc_H, v_x1_u32r;
			ushort adc_H16;
			adc_H16 = ReadUnsigned16BitsFromRegisterBE(BME280_REG_HUMIDITYDATA);
			var t = adc_H16 << 8;
			adc_H16 = (ushort)((adc_H16 << 8) | (adc_H16 >> 8));
			adc_H = adc_H16;
			v_x1_u32r = (t_fine - ((int)76800));
			v_x1_u32r = (((((adc_H << 14) - (((int)this.calibrationData.dig_H4) << 20) - (((int)this.calibrationData.dig_H5) * v_x1_u32r)) + ((
				int)16384)) >> 15) * (((((((v_x1_u32r * ((int)this.calibrationData.dig_H6)) >> 10) * (((v_x1_u32r * ((int)this.calibrationData.dig_H3)) >> 11) + ((
					int)32768))) >> 10) + ((int)2097152)) * ((int)this.calibrationData.dig_H2) + 8192) >> 14));
			v_x1_u32r = (v_x1_u32r - (((((v_x1_u32r >> 15) * (v_x1_u32r >> 15)) >> 7) * ((int)this.calibrationData.dig_H1)) >> 4));
			v_x1_u32r = (v_x1_u32r < 0 ? 0 : v_x1_u32r);
			v_x1_u32r = (v_x1_u32r > 419430400 ? 419430400 : v_x1_u32r);
			this.humidity = (double)((uint)(v_x1_u32r >> 12)) / (double)1024;
		}

		public double ReadTemperature()
        {
			return temperature;
        }
		public double ReadHumidity()
        {
			return humidity;
        }
		public double ReadPressure()
        {
			return pressure;
        }
		internal ushort ReadUnsigned16BitsFromRegisterBE(byte register)
		{
			Span<byte> address = stackalloc byte[1] { register };
			Span<byte> bytes = stackalloc byte[2];
			i2cDevice.WriteRead(address, bytes);

			// return BinaryPrimitives.ReadUInt16BigEndian(bytes);
			return BinaryPrimitives.ReadUInt16LittleEndian(bytes);
		}
		internal short ReadSigned16BitsFromRegisterBE(byte register)
		{
			Span<byte> address = stackalloc byte[1] { register };
			Span<byte> bytes = stackalloc byte[2];
			i2cDevice.WriteRead(address, bytes);

			// return BinaryPrimitives.ReadInt16BigEndian(bytes);
			return BinaryPrimitives.ReadInt16LittleEndian(bytes);
		}
		internal int ReadSigned24BitsFromRegisterBE(byte register)
		{
			Span<byte> address = stackalloc byte[1] { register };
			Span<byte> bytes = stackalloc byte[3];
			i2cDevice.WriteRead(address, bytes);
			int xlsb = (int)bytes[2] >> 4;
			// 0->19:12, 1->11:4, 2->3:0
			int data = xlsb | (int)bytes[1] << 4 | (int)bytes[0] << 12;

			// return BinaryPrimitives.ReadInt32BigEndian(data);
			return data;
		}
		internal byte ReadUnsigned8BitsFromRegister(byte register)
		{
			Span<byte> address = stackalloc byte[1] { register };
			Span<byte> buf = stackalloc byte[1];
			i2cDevice.WriteRead(address, buf);

			return buf[0];
		}
		internal sbyte ReadSigned8BitsFromRegister(byte register)
		{
			Span<byte> address = stackalloc byte[1] { register };
			Span<byte> data = stackalloc byte[1];
			i2cDevice.WriteRead(address, data);

			return (sbyte)data[0];
		}
		public void Dispose()
		{
			i2cDevice.Dispose();
		}

		struct CariblationData
		{
			public ushort dig_T1;
			public short dig_T2;
			public short dig_T3;
			public ushort dig_P1;
			public short dig_P2;
			public short dig_P3;
			public short dig_P4;
			public short dig_P5;
			public short dig_P6;
			public short dig_P7;
			public short dig_P8;
			public short dig_P9;
			public byte dig_H1;
			public short dig_H2;
			public byte dig_H3;
			public short dig_H4;
			public short dig_H5;
			public sbyte dig_H6;
//			public int t_fine;
		}
	}


}