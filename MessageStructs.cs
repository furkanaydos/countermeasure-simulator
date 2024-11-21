using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace EH_KTS
{
    public static class MessageStructs
    {
        public struct ThreatInfo
        {
            public int Angle;
            public int Altitude;
        }

        public struct MDF
        {
            public ushort MDF_Number { get; set; }
            /// <summary>
            /// 6 karakter sabit ASCII
            /// </summary>
            public string MDF_ID;
            /// <summary>
            /// 16 adet sabit
            /// </summary>
            public DispenseProgram[] Dispense_Programs;
            /// <summary>
            /// 4 sektör sınırı
            /// </summary>
            public ushort[] Sectors;
            public ThreatMode[] ThreatModes;
        }

        public struct DispenseProgram
        {
            public byte Program_Number;
            public byte Dispenser;
            public DispenseTech Chaff;
            public DispenseTech Flare;
        }

        public struct DispenseTech
        {
            public byte Salvo_Count;
            public ushort Salvo_Interval;
        }

        public struct ThreatMode
        {
            public byte Id;
            public byte Dispense_Program_Number;
            public byte SelectedSectorsAndAltitude { get; set; }
        }

        public static bool[] GetSelectedSectors(this ThreatMode threatMode)
        {
            bool[] sectors = new bool[4];
            for (int i = 0; i < 4; i++)
            {
                sectors[i] = (threatMode.SelectedSectorsAndAltitude & (0b00000001 << i)) > 0;
            }
            return sectors;
        }

        public static bool GetAltitude(this ThreatMode threatMode)
        {
            return (threatMode.SelectedSectorsAndAltitude & (0b00010000)) > 0;
        }

        public static string GetName(this Enum value)
        {
            Type type = value.GetType();
            MemberInfo[] memberInfo = type.GetMember(value.ToString());
            if (memberInfo != null && memberInfo.Length > 0)
            {
                var attributes = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attributes != null && attributes.Length > 0)
                {
                    return ((DescriptionAttribute)attributes[0]).Description;
                }
            }
            return value.ToString();
        }

        public static MDF ByteArrayToMDF(byte[] bytes)
        {
            MDF mdf = new MDF();

            mdf.MDF_Number = BitConverter.ToUInt16(bytes, 0);
            mdf.MDF_ID = Encoding.ASCII.GetString(bytes, 2, 6);

            mdf.Dispense_Programs = new DispenseProgram[16];

            for (int i = 0; i < 16; i++)
            {
                mdf.Dispense_Programs[i].Program_Number = bytes[8 + i * 8];
                mdf.Dispense_Programs[i].Dispenser = bytes[9 + i * 8];
                mdf.Dispense_Programs[i].Chaff = new DispenseTech()
                {
                    Salvo_Count = bytes[10 + i * 8],
                    Salvo_Interval = BitConverter.ToUInt16(bytes, 11 + i * 8)
                };
                mdf.Dispense_Programs[i].Flare = new DispenseTech()
                {
                    Salvo_Count = bytes[13 + i * 8],
                    Salvo_Interval = BitConverter.ToUInt16(bytes, 14 + i * 8)
                };


            }

            mdf.Sectors = new ushort[4];

            for (int i = 0; i < 4; i++)
            {
                mdf.Sectors[i] = BitConverter.ToUInt16(bytes, 136 + i * 2);
            }

            mdf.ThreatModes = new ThreatMode[16];

            for (int i = 0; i < 16; i++)
            {
                mdf.ThreatModes[i].Id = bytes[144 + i * 3];
                mdf.ThreatModes[i].Dispense_Program_Number = bytes[145 + i * 3];
                mdf.ThreatModes[i].SelectedSectorsAndAltitude = bytes[146 + i * 3];
            }

            return mdf;
        }

        public enum MessageType
        {
            SEND_MDF = 1,
            REQUEST_LOG,
            START_SEND_LOG,
            SEND_LOG,
            END_SEND_LOG,
        }

        public enum LogTypes
        {
            [Description("Atım Programı Başlatıldı")]
            ProgramStarted, //0
            [Description("Uçuş Başlatıldı")]
            FlightStarted,  //1
            [Description("Açı ve Yükseklik")]
            AngleAndAltitude,
            [Description("Atım Programı")]
            DispenceProgram,
            [Description("Tehdit Modu")]
            ThreatMode,
            [Description("Uçuş Bitirildi")]
            FlightEnd,
            [Description("GVD'ye Bağlandı")]
            Connection,
            [Description("Karşı Tedbir")]
            Success
        }

    }
}
