using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace Arion.Data.Utilities
{
    public enum WaitReplyOpt : ushort { NO = 0, YES = 1 }

    public enum BitState : byte { OFF = 0, ON = 1, RAISE = 3, FALL = 4 }

    public enum PartResult { REJECTED = 0, ACCEPTED = 1, TBD = 2, UNTESTED = 3, NOTHING = 4 }

    /// <summary>
    /// Расширенная версия результата для общения
    /// </summary>
    //public enum OutputResultEx : byte { NOTHING = 0, ADR_ACCEPTED = 1, ADR_REJECTED = 2, OPERATOR_ACCEPTED = 3, OPERATOR_REJECTED = 4, ACQUISITION_ERROR = 5, TBD = 6 };

    public enum InspectionResultEx { Accepted = 'A', Error = 'E', NoTest = 'N', Rejected = 'R', Suspect = 'S', Absent = 'X' }

    public static class InspectionResultConversion
    {
        // public enum InspectionResultEx { Accepted = 'A', Rejected = 'R', Absent = 'X', NoTest = 'N', Error = 'E', Suspect = 'S' };
        public static string InspectionResultExToString(InspectionResultEx isp)
        {
            return ((char)isp).ToString();
        }

        public static InspectionResultEx StringToInspectionResultEx(string s)
        {
            s = s.ToUpper();
            if (s == "A")
                return InspectionResultEx.Accepted;
            if (s == "R")
                return InspectionResultEx.Rejected;
            if (s == "X")
                return InspectionResultEx.Absent;
            if (s == "E")
                return InspectionResultEx.Error;
            if (s == "S")
                return InspectionResultEx.Suspect;

            return InspectionResultEx.NoTest;
        }
    }
    
    public class IoDefinition
    {
        public string Description = string.Empty;
        public string Name = string.Empty;
    }

    public class PartDataLocation
    {
        public ushort InputByteForPartCodeLsb = 1000;
        public ushort InputByteForPartCodeMsb = 1000;
        public ushort InputBitPartIsControlCasting = 10000; // чтобы знать, что загруженная часть является master / control casting
        public ushort InputByteSnStart = 10000; // начало входящего серийного номера
        public ushort SnLenght = 16; // максимальная длина серийный номер
        public ushort InputByteFurnaceLsb = 10000;
        public ushort InputByteFurnaceMsb = 10000;
        public ushort InputByteCastingMachineLsb = 10000;
        public ushort InputByteCastingMachineMsb = 10000;
        public ushort InputByteMoldLsb = 10000;
        public ushort InputByteMoldMsb = 10000;
        public ushort InputByteCavityLsb = 10000;
        public ushort InputByteCavityMsb = 10000;
        public ushort InputByteShotNumberLsb = 10000;
        public ushort InputByteShotNumberMsb = 10000;
        public ushort OutputByteForPartCodeLsb = 9999;
        public ushort OutputByteForPartCodeMsb = 9999;
        public ushort OutputByteSnStart = 9999; // начало исходящего серийного номера
        public ushort OutputBitPartAccepted = 9999; // сказать, что кусок хорош
        public ushort OutputBitPartRejected = 9999; // сказать, что кусок-это лом
        public ushort OutputBitPartTbd = 9999; // сказать, что кусок подозрительный или "знак вопроса"
        public ushort OutputBitPartUntested = 9999; // non test
        public ushort OutputBitPartIsControlCasting = 9999; // сказать, что заготовка в выхлопе-это мастер / контроль литья
        public ushort OutputByteFurnaceLsb = 9999;
        public ushort OutputByteFurnaceMsb = 9999;
        public ushort OutputByteCastingMachineLsb = 9999;
        public ushort OutputByteCastingMachineMsb = 9999;
        public ushort OutputByteMoldLsb = 9999;
        public ushort OutputByteMoldMsb = 9999;
        public ushort OutputByteCavityLsb = 9999;
        public ushort OutputByteCavityMsb = 9999;
        public ushort OutputByteShotNumberLsb = 9999;
        public ushort OutputByteShotNumberMsb = 9999;

        /// <summary>
        /// Указывает, с чего начать запись результатов отдельных позиций
        /// </summary>
        public ushort OutputBytePositionResultStart = 9999;

        /// <summary>
        /// Максимальное число позиции для записи результат
        /// </summary>
        public ushort PositionResultLenght = 63;

        /// <summary>
        /// Указывает, где написать расширенный результат произведения
        /// </summary>
        public ushort OutputBytePartResult = 9999;
    }

    public static class Network
    {
        /// <summary>
        /// Протяните прибор, чтобы увидеть, достижимо ли оно
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="retryTimes"></param>
        /// <returns></returns>
        public static bool PingIp(IPAddress ip, int retryTimes = 3)
        {
            var pingSender = new Ping();
            var timeout = 100;
            try
            {
                for (var i = 0; i < retryTimes; i++)
                {
                    var reply = pingSender.Send(ip, timeout);
                    if (reply?.Status == IPStatus.Success)
                        return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }
        
        public static bool PingIp(string ip, int retryTimes = 3)
        {
            var pingSender = new Ping();
            var timeout = 100;
            try
            {
                for (int i = 0; i < retryTimes; i++)
                {
                    var reply = pingSender.Send(ip, timeout);
                    if (reply?.Status == IPStatus.Success)
                        return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }
    }

    public static partial class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    }

    public sealed class OrderByNaturalSort : IComparer<string>
    {
        int IComparer<string>.Compare(string x, string y)
        {
            return Generics.CompareNumericString(x, y);
        }
    }

    public static class Generics
    {
        public static BitState CheckIfRaiseOrFall(byte actualState, byte previousState)
        {
            var state = BitState.OFF;

            if (actualState != 0 && previousState == 0)
                state = BitState.RAISE;
            else if (actualState == 0 && previousState != 0)
                state = BitState.FALL;
            else if (actualState == 0 && previousState == 0)
                state = BitState.OFF;
            else if (actualState != 0 && previousState != 0)
                state = BitState.ON;

            return state;
        }

        public static void RemoveSeparatorAndDecimalPointChar(ref string s)
        {
            s = s.Replace(",", "");
            s = s.Replace(".", "");
        }

        public static void FixDecimalPointChar(ref string s)
        {
            string decSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            var p1 = s.IndexOf(".", StringComparison.Ordinal);
            var p2 = s.IndexOf(",", StringComparison.Ordinal);

            if (p1 > -1 && p2 > -1) // если оба существуют, то один-разделитель тысяч!! я удаляю крайний левый
            {
                s = s.Replace(p1 < p2 ? "." : ",", "");
            }

            s = s.Replace(",", decSep);
            s = s.Replace(".", decSep);
        }

        public static bool IsValidPathName(string path)
        {
            var invalidPathChars = Path.GetInvalidPathChars();
            return path.IndexOfAny(invalidPathChars) <= -1;
        }

        /// <summary>
        /// Удалить недопустимые caraterri для путей к папкам из строки
        /// </summary>
        /// <param name="path"></param>
        public static void CleanPath(ref string path)
        {
            int i;
            var invalidPathChars = Path.GetInvalidPathChars();

            for (i = 0; i < path.Length; i++)
                if (Array.IndexOf(invalidPathChars, path[i]) > -1)
                {
                    path = path.Remove(i, 1);
                    i--;
                }
        }

        public static bool IsValidFileName(string file)
        {
            var invalidFileChars = Path.GetInvalidFileNameChars();
            return file.IndexOfAny(invalidFileChars) <= -1;
        }

        public static string FixSnCode(string sn, char replaceWith = '-')
        {
            /*
            string sn_return = "";

            for (int i = 0; i < sn.Length; i++)
                if (CheckIfValidSNCode(sn[i]))
                    sn_return = sn_return + sn[i];

            return sn_return;
            */
            CleanFileName(ref sn, replaceWith);
            return sn.Replace('_', replaceWith); // характер не допущен в общении с фарисом
        }

        /// <summary>
        /// Заменяет недопустимые карат в именах файлов на указанном
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="replaceWith"></param>
        public static void CleanFileName(ref string filename, char replaceWith = '-')
        {
            filename = filename.Trim();

            char[] invalidFileChars = Path.GetInvalidFileNameChars();

            for (int i = 0; i < filename.Length; i++)
            {
                if (filename[i] == '\0')
                {
                    filename = filename.Remove(i, 1);
                    i--;
                }
                else if (Array.IndexOf(invalidFileChars, filename[i]) > -1)
                {
                    filename = filename.Replace(filename[i], replaceWith);
                }
            }
        }

        /// <summary>
        /// Убедитесь, что шрифт приемлем как sn
        /// допустимые символы 0 - >9 a - >z A - >Z - _
        /// </summary>
        /// <param name="sn"></param>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static bool CheckIfValidSnCode(char ch)
        {
            return ch == '-' || ch == '_' || (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
        }

        public static string RestrictToNumber(string s)
        {
            var r = s;
            for (var i = 0; i < r.Length; i++)
            {
                var ch = r[i];
                if ((ch >= '0' && ch <= '9') || ch == '.' || ch == ',' || ch == '-') continue;
                r = r.Remove(i, 1);
                i--;
            }
            return r;
        }

        /// <summary>
        /// Сравните две строки, принимая во внимание числа (3 меньше 21)
        /// </summary>
        /// <param name="s"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static int CompareNumericString(string s, string other)
        {
            if (s != null && other != null
                && (s = s.Replace(" ", string.Empty)).Length > 0
                && (other = other.Replace(" ", string.Empty)).Length > 0)
            {
                int sIndex = 0, otherIndex = 0;

                while (sIndex < s.Length)
                {
                    if (otherIndex >= other.Length)
                        return 1;

                    if (char.IsDigit(s[sIndex]))
                    {
                        if (!char.IsDigit(other[otherIndex]))
                            return -1;

                        // Compare the numbers
                        StringBuilder sBuilder = new StringBuilder(), otherBuilder = new StringBuilder();

                        while (sIndex < s.Length && char.IsDigit(s[sIndex]))
                        {
                            sBuilder.Append(s[sIndex++]);
                        }

                        while (otherIndex < other.Length && char.IsDigit(other[otherIndex]))
                        {
                            otherBuilder.Append(other[otherIndex++]);
                        }

                        long sValue, otherValue;

                        try
                        {
                            sValue = Convert.ToInt64(sBuilder.ToString());
                        }
                        catch (OverflowException) { sValue = Int64.MaxValue; }

                        try
                        {
                            otherValue = Convert.ToInt64(otherBuilder.ToString());
                        }
                        catch (OverflowException) { otherValue = Int64.MaxValue; }

                        if (sValue < otherValue)
                            return -1;
                        if (sValue > otherValue)
                            return 1;
                    }
                    else if (char.IsDigit(other[otherIndex]))
                        return 1;
                    else
                    {
                        int difference = string.Compare(s[sIndex].ToString(), other[otherIndex].ToString(), StringComparison.InvariantCultureIgnoreCase);

                        if (difference > 0)
                            return 1;
                        if (difference < 0)
                            return -1;

                        sIndex++;
                        otherIndex++;
                    }
                }

                if (otherIndex < other.Length)
                    return -1;
            }

            return 0;
        }

        public static bool InRange<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
                return false;

            if (value.CompareTo(max) > 0)
                return false;

            return true;
        }

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;

            return value;
        }

        public static int CountOccurrences(string where, string what)
        {
            int count = 0;
            int n = 0;

            while ((n = where.IndexOf(what, n + 1, StringComparison.Ordinal)) != -1)
                count++;

            return count;
        }

        public static int GetIso8601WeekOfYear(DateTime time)
        {
            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public static bool GetRowField(DataTable dt, int row, string fieldName, out string strOut)
        {
            if (dt.Columns.Contains(fieldName) && row < dt.Rows.Count)
            {
                //str_out = dt.Rows[row][fieldName].ToString();
                strOut = dt.DefaultView[row][fieldName].ToString();
                return true;
            }
            strOut = string.Empty;
            return false;
        }

        public static bool GetRowField(DataTable dt, int row, string fieldName, out int i)
        {
            var r = false;
            i = 0;
            if (GetRowField(dt, row, fieldName, out string s))
            {
                FixDecimalPointChar(ref s);
                r = int.TryParse(s, out i);
            }
            return r;
        }

        public static bool GetRowField(DataTable dt, int row, string fieldName, out double d)
        {
            var r = false;
            d = 0;
            if (GetRowField(dt, row, fieldName, out string s))
            {
                FixDecimalPointChar(ref s);
                r = double.TryParse(s, out d);
            }
            return r;
        }

        public static bool GetField(DataSet ds, string tableName, string fieldName, out string strOut, int rowindex = 0)
        {
            strOut = "";
            try
            {
                if (ds.Tables.Contains(tableName))
                {
                    DataTable dt = ds.Tables[tableName];

                    return GetRowField(dt, rowindex, fieldName, out strOut);
                }

                // Console.WriteLine(string.Format("Table not found: {0} {1}", ds.DataSetName, tableName));
                return false;
            }
            catch { return false; }
        }

        public static bool SetField(string fileName, string tableName, string fieldName, string value, int rowindex = 0)
        {
            try
            {
                DataSet ds = new DataSet();
                if (File.Exists(fileName))
                {
                    ds.ReadXml(fileName);
                    SetField(ds, tableName, fieldName, value, rowindex);
                }
                else
                {
                    ds.Tables.Add(tableName);
                    ds.Tables[tableName].Columns.Add(fieldName);
                    DataRow dr = ds.Tables[tableName].NewRow();
                    dr[fieldName] = value;
                }
                /*
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = Encoding.UTF8;
                using (XmlWriter xmlWriter = XmlWriter.Create(Filename, settings))
                {
                    ds.WriteXml(xmlWriter);
                    xmlWriter.Close();
                }
                 */
                ds.WriteXml(fileName);
                return true;
            }
            catch { return false; }
        }

        public static bool SetField(DataSet ds, string tableName, string fieldName, string value, int rowindex = 0)
        {
            try
            {
                if (!ds.Tables.Contains(tableName))
                    ds.Tables.Add(tableName);

                var dt = ds.Tables[tableName];
                return SetField(dt, fieldName, value, rowindex);
            }
            catch { return false; }
        }

        public static bool SetField(DataTable dt, string fieldName, string value, int row = 0)
        {
            if (!dt.Columns.Contains(fieldName))
                dt.Columns.Add(fieldName);

            if (row < dt.Rows.Count)
            {
                dt.DefaultView[row][fieldName] = value;
                //    return true;
            }
            else
            {
                DataRow dr = dt.NewRow();
                dr[fieldName] = value;
                dt.Rows.Add(dr);
            }
            return true;

            //return false;
        }

        /// <summary>
        /// Читает поле, и если он не существует, добавляет его
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="table"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <param name="rowindex"></param>
        /// <returns>False, если параметр не найден или пуст</returns>
        public static bool GetSetField(DataSet ds, string table, string field, ref double value, int rowindex = 0)
        {
            bool r = true;
            if (GetField(ds, table, field, out var s, rowindex) && s != string.Empty)
            {
                FixDecimalPointChar(ref s);
                double.TryParse(s, out value);
            }
            else
            {
                SetField(ds, table, field, value.ToString(CultureInfo.InvariantCulture), rowindex);
                r = false;
            }
            return r;
        }

        public static bool GetSetField(DataSet ds, string table, string field, ref ushort value, int rowindex = 0)
        {
            bool r = true;
            if (GetField(ds, table, field, out var s, rowindex) && s != string.Empty)
                ushort.TryParse(s, out value);
            else
            {
                SetField(ds, table, field, value.ToString(), rowindex);
                r = false;
            }
            return r;
        }

        public static bool GetSetField(DataSet ds, string table, string field, ref int value, int rowindex = 0)
        {
            bool r = true;
            if (GetField(ds, table, field, out var s, rowindex) && s != string.Empty)
                int.TryParse(s, out value);
            else
            {
                SetField(ds, table, field, value.ToString(), rowindex);
                r = false;
            }
            return r;
        }

        public static bool GetSetField(DataSet ds, string table, string field, ref long value, int rowindex = 0)
        {
            bool r = true;
            if (GetField(ds, table, field, out var s, rowindex) && s != string.Empty)
                long.TryParse(s, out value);
            else
            {
                SetField(ds, table, field, value.ToString(), rowindex);
                r = false;
            }
            return r;
        }

        public static bool GetSetField(DataSet ds, string table, string field, ref bool value, int rowindex = 0)
        {
            bool r = true;
            if (GetField(ds, table, field, out var s, rowindex) && s != string.Empty)
            {
                if (string.Equals(s, "false", StringComparison.CurrentCultureIgnoreCase))
                    value = false;
                else if (string.Equals(s, "true", StringComparison.CurrentCultureIgnoreCase))
                    value = true;
                else
                    value = s == "1";
            }
            else
            {
                SetField(ds, table, field, value ? "1" : "0", rowindex);
                r = false;
            }
            return r;
        }

        public static bool GetSetField(DataSet ds, string table, string field, ref string value, int rowindex = 0)
        {
            bool r = true;
            if (GetField(ds, table, field, out var s, rowindex))
                value = s;
            else
            {
                SetField(ds, table, field, value, rowindex);
                r = false;
            }
            return r;
        }

        public static bool GetSetField(DataSet ds, string table, string field, ref DateTime value, string format = "yyyy-MM-dd HH:mm:ss", int rowindex = 0)
        {
            bool r = true;
            CultureInfo provider = CultureInfo.InvariantCulture;
            if (GetField(ds, table, field, out var s, rowindex) && s != string.Empty)
                DateTime.TryParseExact(s, format, provider, DateTimeStyles.None, out value);
            else
            {
                SetField(ds, table, field, value.ToString(format), rowindex);
                r = false;
            }
            return r;
        }

        /// <summary>
        /// Вычислить BCC между всеми строковыми символами
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        public static int Bcc(string[] ar)
        {
            int ret = 0;
            if (ar != null)
            {
                int k = ar.GetUpperBound(0);
                for (int i = 0; i <= k; i++)
                {
                    byte[] b = Encoding.ASCII.GetBytes(ar[i]);
                    ret = b.Aggregate(ret, (current, t) => current ^ t);
                }
            }
            return ret;
        }

        /// <summary>
        /// вычислить контрольную сумму чисел, содержащихся в массиве
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        public static long CheckSum(long[] ar)
        {
            long ret = 0;
            if (ar != null)
            {
                int k = ar.GetUpperBound(0);
                for (int i = 0; i <= k; i++)
                {
                    ret += ar[i];
                }
            }
            return ret;
        }

        public static long GetFileTimeCheckSum(string path)
        {
            long ret = 0;
            int i;
            try
            {
                string[] filePaths = Directory.GetFiles(path);
                if (filePaths != null)
                {
                    long[] fileDate = new long[filePaths.GetUpperBound(0) + 1];
                    for (i = 0; i <= fileDate.GetUpperBound(0); i++)
                        fileDate[i] = Convert.ToInt64(File.GetLastWriteTime(filePaths[i]).ToString("yyyyMMddHHmmss"));

                    ret = CheckSum(fileDate);
                }
            }
            catch { ret = -1; }
            return ret;
        }

        /// <summary>
        /// Он ищет указанное значение, если оно отсутствует, добавляет его и помещает в таблицу описаний
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="id"></param>
        /// <param name="v"></param>
        /// <param name="ioDef"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static bool GetSetIoMappingById(DataTable dt, string id, ref ushort v, IoDefinition[] ioDef, string lang = "eng")
        {
            bool r = false;
            string dsc = string.Empty;
            if (dt == null)
                return r;
            if (!dt.Columns.Contains("ID"))
                dt.Columns.Add("ID");

            if (!dt.Columns.Contains("MAPPING"))
                dt.Columns.Add("MAPPING");

            if (!dt.Columns.Contains(lang))
                dt.Columns.Add(lang);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (string.Equals(dt.Rows[i]["ID"].ToString(), id, StringComparison.CurrentCultureIgnoreCase))
                {
                    r = ushort.TryParse(dt.Rows[i]["MAPPING"].ToString(), out v);
                    dsc = dt.Rows[i][lang].ToString();
                    break;
                }
            }

            if (!r)
            {
                DataRow dr = dt.NewRow();
                dr["ID"] = id;
                dr["MAPPING"] = v.ToString();
                dt.Rows.Add(dr);
            }

            if (ioDef?.Length > v)
            {
                if (ioDef[v].Name?.Length == 0)
                    ioDef[v].Name = id;
                else
                    ioDef[v].Name += "; " + id;

                if (ioDef[v].Description?.Length == 0)
                    ioDef[v].Description = dsc;
                else
                    ioDef[v].Description += "; " + dsc;
            }

            return r;
        }

        public static bool GetSetIoMappingByFieldName(DataSet ds, string table, string field, ref ushort v, IoDefinition[] ioDef, int rowindex = 0)
        {
            bool r = GetSetField(ds, table, field, ref v, rowindex);

            if (ioDef?.Length > v)
            {
                if (ioDef[v].Name?.Length == 0)
                    ioDef[v].Name = field;
                else
                    ioDef[v].Name += "; " + field;

                /*
                if (IODef[v].Description?.Length == 0)
                    IODef[v].Description = dsc;
                else
                    IODef[v].Description += "; " + dsc;
                */
            }

            return r;
        }

        #region DOT NET

        public enum DOTNETVERSION { NONE = 0, V450 = 378389, V451 = 378675, V452 = 379893, V460 = 393273, V461 = 394254, V462 = 394802, V470 = 460798 }

        public static DOTNETVERSION CheckFor45DotVersion(int releaseKey)
        {
            var v = DOTNETVERSION.NONE;
            var values = EnumUtil.GetValues<DOTNETVERSION>();

            foreach (DOTNETVERSION z in values.Reverse())
            {
                if (releaseKey >= (int)z)
                {
                    v = z;
                    break;
                }
            }
            return v;
        }

        public static int GetDotNetVersion45OrAbove()
        {
            int releaseKey = 0;
            using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
            {
                releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
            }
            return releaseKey;
        }

        public static double GetDotNetVersion4OrBelow()
        {
            RegistryKey installedVersions = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP");
            string[] versionNames = installedVersions.GetSubKeyNames();
            //version names start with 'v', eg, 'v3.5' which needs to be trimmed off before conversion
            double framework = Convert.ToDouble(versionNames[versionNames.Length - 1].Remove(0, 1), CultureInfo.InvariantCulture);
            int sp = Convert.ToInt32(installedVersions.OpenSubKey(versionNames[versionNames.Length - 1]).GetValue("SP", 0));
            return framework;
        }

        #endregion DOT NET
    }

    public static class EnumUtil
    {
        public static IEnumerable<T> GetValues<T>()
        {
            return (T[])Enum.GetValues(typeof(T));
            //return Enum.GetValues(typeof(T)).Cast<T>();
        }
    }

    public class BitStateCheck
    {
        private byte _actualValue;
        private byte _previousValue;
        
        public BitState Value = BitState.OFF;

        public BitState Compute(byte newValue)
        {
            _actualValue = newValue;
            
            Value = Generics.CheckIfRaiseOrFall(_actualValue, _previousValue);
            
            _previousValue = _actualValue;
            
            return Value;
        }
    }

    public class ListManager<T>
    {
        //protected SynchronizedCollection<T> items = new SynchronizedCollection<T>();

        protected List<T> items = new List<T>();
        private const long MaxMessages = 5000;

        private object lockForOpenSave = new object();
        public string filePath;

        public bool Add(T item)
        {
            lock (items)
            {
                if (items.Count < MaxMessages)
                {
                    items.Add(item);
                    return true;
                }
                else
                    return false;
            }
        }

        public void Clear()
        {
            lock (items)
            {
                items.Clear();
            }
        }

        public void Remove(int index)
        {
            lock (items)
            {
                items.RemoveAt(index);
            }
        }

        public List<T> GetList()
        {
            lock (items)
            {
                return new List<T>(items);
            }
        }

        #region LOAD/SAVE

        public bool Load()
        {
            bool r = false;
            if (filePath.Length == 0
                || !File.Exists(filePath))
            {
                return false;
            }

            lock (lockForOpenSave)
            {
                try
                {
                    var deserializer = new XmlSerializer(typeof(List<T>));
                    /*
                    using (TextReader textReader = new StreamReader(filepath))
                    {
                        messages = (List<AlertMessage>)deserializer.Deserialize(textReader);
                    }
                    */
                    var settings = new XmlReaderSettings();

                    lock (items)
                    {
                        using XmlReader xmlReader = XmlReader.Create(filePath, settings);
                        items = (List<T>)deserializer.Deserialize(xmlReader);
                    }
                    r = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return r;
        }

        public bool Save()
        {
            for (int i = 0; i < 3; i++)
            {
                RealSave();
                var tmp = new ListManager<T>();
                tmp.filePath = filePath;
                if (tmp.Load()) //rilegge se ok
                    return true;
            }
            return false;
        }

        private bool RealSave()
        {
            bool r = false;
            if (filePath.Length > 0)
            {
                lock (lockForOpenSave)
                {
                    try
                    {
                        string path = Path.GetDirectoryName(filePath);
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);

                        var serializer = new XmlSerializer(typeof(List<T>));
                        /*
                        using (TextWriter textWriter = new StreamWriter(filepath))
                        {
                            serializer.Serialize(textWriter, messages);
                        }
                        */
                        var settings = new XmlWriterSettings
                        {
                            Encoding = Encoding.UTF8,
                            Indent = true
                        };
                        lock (items)
                        {
                            using XmlWriter xmlWriter = XmlWriter.Create(filePath, settings);
                            //xmlWriter.WriteNode
                            serializer.Serialize(xmlWriter, items);
                        }
                        r = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            return r;
        }

        #endregion LOAD/SAVE
    }

    /// <summary>
    /// Предел движения оси
    /// </summary>
    public class AxisMovementRestriction
    {
        public double To_Min = 0;
        public double To_Max = 0;

        public AxisMovementRestriction()
        { }

        public AxisMovementRestriction(double toMin, double toMax)
        {
            To_Min = toMin;
            To_Max = toMax;
        }
    }

    /// <summary>
    /// Limite movimentazione X e Y
    /// </summary>
    public class MechMovementRestriction
    {
        public AxisMovementRestriction X = new AxisMovementRestriction();
        public AxisMovementRestriction Y = new AxisMovementRestriction();

        public MechMovementRestriction()
        { }

        public MechMovementRestriction(double XtoMin, double XtoMax, double YtoMin, double YtoMax)
        {
            X.To_Min = XtoMin;
            X.To_Max = XtoMax;
            Y.To_Min = YtoMin;
            Y.To_Max = YtoMax;
        }
    }

    internal class WaitLoopTime
    {
        private double time = 5; //ms default

        private DateTime dt = DateTime.UtcNow;

        public WaitLoopTime()
        {
        }

        public WaitLoopTime(double c)
        {
            time = c;
        }

        public uint Inc()
        {
            if ((DateTime.UtcNow - dt).TotalMilliseconds >= time)
            {
                Thread.Sleep(1);
                Reset();
                return 1;
            }
            return 0;
        }

        public void Reset()
        {
            DateTime dt = DateTime.UtcNow;
        }
    }
}