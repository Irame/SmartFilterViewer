﻿using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SmartFilterViewer
{
    public class SensorDataList : List<SensorData>
    {
        public string FileName { get; }

        private SensorDataList(string fileName, IEnumerable<SensorData> data) : base(data)
        {
            FileName = fileName;
        }

        public static SensorDataList FromFile(string fileName)
        {
            var allLines = File.ReadAllLines(fileName);
            var dataLines = allLines.SkipWhile(x => !x.StartsWith("OADateTime"))
                .Skip(1)
                .Select(x => x.Replace("n. def.", "0"));

            using (var reader = new StringReader(string.Join("\n", dataLines)))
            using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
            {
                csv.Configuration.HasHeaderRecord = false;
                return new SensorDataList(fileName, csv.GetRecords<SensorData>());
            }
        }

        public double GetLerpValue(double idx, PropertyInfo propInfo)
        {
            int idx1 = (int)Math.Floor(idx);
            int idx2 = (int)Math.Ceiling(idx);

            if (idx1 == idx2)
            {
                return (double)propInfo.GetValue(this[idx1]);
            }
            else
            {
                double val1 = (double)propInfo.GetValue(this[idx1]);
                double val2 = (double)propInfo.GetValue(this[idx2]);

                return val1 + (val2 - val1) * (idx - idx1);
            }
        }

        public double FindLerpIndex(DateTime time)
        {
            return FindRecursive(0, Count - 1);

            double FindRecursive(int start, int end)
            {
                if (start > end)
                {
                    if (end < 0)
                    {
                        return 0;
                    }
                    else if (start >= Count)
                    {
                        return Count - 1;
                    }

                    var time1 = this[end].DateTime;
                    var time2 = this[start].DateTime;

                    var factor = (time - time1).TotalMilliseconds / (time2 - time1).TotalMilliseconds;

                    return end + factor;
                }

                int checkIdx = end - (end - start) / 2;

                var checkTime = this[checkIdx].DateTime;

                if (time < checkTime)
                {
                    return FindRecursive(start, checkIdx - 1);
                }
                else if (time > checkTime)
                {
                    return FindRecursive(checkIdx + 1, end);
                }
                else
                {
                    return checkIdx;
                }
            }
        }
    }

    class NiceNameAttribute : Attribute
    {
        public string Name { get; }

        public NiceNameAttribute(string name)
        {
            Name = name;
        }
    }

    public class SensorData
    {
        private double _oaDateTime;

        public DateTime DateTime { get; private set; }

        [Index(0)]
        public double OADateTime
        {
            get => _oaDateTime;
            set
            {
                DateTime = DateTime.FromOADate(value);
                _oaDateTime = value;
            }
        }

        [Index(1)]
        public double Bin00 { get; set; }

        [Index(2)]
        public double Bin01 { get; set; }

        [Index(3)]
        public double Bin02 { get; set; }

        [Index(4)]
        public double Bin03 { get; set; }

        [Index(5)]
        public double Bin04 { get; set; }

        [Index(6)]
        public double Bin05 { get; set; }

        [Index(7)]
        public double Bin06 { get; set; }

        [Index(8)]
        public double Bin07 { get; set; }

        [Index(9)]
        public double Bin08 { get; set; }

        [Index(10)]
        public double Bin09 { get; set; }

        [Index(11)]
        public double Bin10 { get; set; }

        [Index(12)]
        public double Bin11 { get; set; }

        [Index(13)]
        public double Bin12 { get; set; }

        [Index(14)]
        public double Bin13 { get; set; }

        [Index(15)]
        public double Bin14 { get; set; }

        [Index(16)]
        public double Bin15 { get; set; }

        [Index(17)]
        public double Bin16 { get; set; }

        [Index(18)]
        public double Bin17 { get; set; }

        [Index(19)]
        public double Bin18 { get; set; }

        [Index(20)]
        public double Bin19 { get; set; }

        [Index(21)]
        public double Bin20 { get; set; }

        [Index(22)]
        public double Bin21 { get; set; }

        [Index(23)]
        public double Bin22 { get; set; }

        [Index(24)]
        public double Bin23 { get; set; }

        [NiceName("MeanToFBin1(us)")]
        [Index(25)]
        public double MeanToFBin1_us { get; set; }

        [NiceName("MeanToFBin3(us)")]
        [Index(26)]
        public double MeanToFBin3_us { get; set; }

        [NiceName("MeanToFBin5(us)")]
        [Index(27)]
        public double MeanToFBin5_us { get; set; }

        [NiceName("MeanToFBin7(us)")]
        [Index(28)]
        public double MeanToFBin7_us { get; set; }

        [NiceName("Count/s")]
        [Index(29)]
        public double Count_s { get; set; }

        [NiceName("SamplingPeriod(s)")]
        [Index(30)]
        public double SamplingPeriod_s { get; set; }

        [NiceName("SFR(ml/s)")]
        [Index(31)]
        public double SFR_ml_s { get; set; }

        [NiceName("Temperature(C)")]
        [Index(32)]
        public double Temperature_C { get; set; }

        [NiceName("RelativeHumidity(%)")]
        [Index(33)]
        public double RelativeHumidity_percent { get; set; }

        [NiceName("#RejectGlitch")]
        [Index(34)]
        public double RejectGlitch { get; set; }

        [NiceName("#RejectLongTOF")]
        [Index(35)]
        public double RejectLongTOF { get; set; }

        [NiceName("#RejectRatio")]
        [Index(36)]
        public double RejectRatio { get; set; }

        [NiceName("#RejectOutOfRange")]
        [Index(37)]
        public double RejectOutOfRange { get; set; }

        [NiceName("FanRevCount")]
        [Index(38)]
        public double FanRevCount { get; set; }

        [NiceName("LaserStatus")]
        [Index(39)]
        public double LaserStatus { get; set; }

        [NiceName("PM1(ug/m3)")]
        [Index(40)]
        public double PM1_ug_m3 { get; set; }

        [NiceName("PM2,5(ug/m3)")]
        [Index(41)]
        public double PM2_5_ug_m3 { get; set; }

        [NiceName("PM10(ug/m3)")]
        [Index(42)]
        public double PM10_ug_m3 { get; set; }

        [NiceName("RollMean_PM1")]
        [Index(43)]
        public double RollMean_PM1 { get; set; }

        [NiceName("RollMean_PM2,5")]
        [Index(44)]
        public double RollMean_PM2_5 { get; set; }

        [NiceName("RollMean_PM10")]
        [Index(45)]
        public double RollMean_PM10 { get; set; }
    }
}
