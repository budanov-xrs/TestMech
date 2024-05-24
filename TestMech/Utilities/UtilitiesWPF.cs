using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Arion.Data.Utilities
{
    public class MessageSetting
    {
        public string Text = string.Empty;
        public FontFamily FontFamily = new FontFamily("Federation");
        public double FontSize = 12;

        public override string ToString()
        {
            return Text;
        }
    }

    public static partial class NativeMethods
    {
        [DllImport("gdi32")]
        public static extern int DeleteObject(IntPtr o);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }

    public static class GenericsWpf
    {
        private static object _lckWindows = new object();

        public static void SetWaitCursor()
        {
            // MediaTypeNames.Application.Current.Dispatcher.Invoke((Action)(() => Mouse.OverrideCursor = Cursors.Wait));
        }

        public static void ResetCursor()
        {
            // MediaTypeNames.Application.Current.Dispatcher.Invoke((Action)(() => Mouse.OverrideCursor = null));
        }

        public static void DeleteWindowPosition(string xmlFile)
        {
            lock (_lckWindows)
            {
                try
                {
                    File.Delete(xmlFile);
                }
                catch
                {
                    // ignored
                }
            }
        }

        public static void SaveWindowPositionAndSize(System.Windows.Window w, string xmlFile, string windowName = "")
        {
            var r = -1;
            var ds = new DataSet();
            if (windowName?.Length == 0)
                windowName = w.Name;

            lock (_lckWindows)
            {
                try
                {
                    if (File.Exists(xmlFile))
                        ds.ReadXml(xmlFile);
                    else
                        ds.DataSetName = "WindowsData";

                    if (!ds.Tables.Contains("Window"))
                        ds.Tables.Add("Window");

                    DataTable dt = ds.Tables["Window"];

                    if (!dt.Columns.Contains("Name"))
                        dt.Columns.Add("Name");
                    if (!dt.Columns.Contains("Top"))
                        dt.Columns.Add("Top");
                    if (!dt.Columns.Contains("Left"))
                        dt.Columns.Add("Left");
                    if (!dt.Columns.Contains("Width"))
                        dt.Columns.Add("Width");
                    if (!dt.Columns.Contains("Height"))
                        dt.Columns.Add("Height");
                    if (!dt.Columns.Contains("WindowState"))
                        dt.Columns.Add("WindowState");

                    int i;
                    for (i = 0; i < dt.Rows.Count; i++)
                    {
                        if (Generics.GetRowField(dt, i, "name", out string s))
                            if (string.Equals(s, windowName, StringComparison.CurrentCultureIgnoreCase))
                            {
                                r = i; break;
                            }
                    }

                    DataRow dr;
                    if (r == -1)  //non trovato
                        dr = dt.NewRow();
                    else
                        dr = dt.Rows[r];

                    dr["Name"] = windowName;
                    dr["Top"] = w.Top.ToString(CultureInfo.InvariantCulture);
                    dr["Left"] = w.Left.ToString(CultureInfo.InvariantCulture);
                    dr["Width"] = w.Width.ToString(CultureInfo.InvariantCulture);
                    dr["Height"] = w.Height.ToString(CultureInfo.InvariantCulture);
                    dr["WindowState"] = w.WindowState.ToString();

                    if (r == -1)
                        dt.Rows.Add(dr);

                    ds.WriteXml(xmlFile);
                }
                catch { };
            }
        }
        
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child is T dependencyObject)
                    {
                        yield return dependencyObject;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static List<T> GetLogicalChildCollection<T>(object parent) where T : DependencyObject
        {
            List<T> logicalCollection = new List<T>();
            GetLogicalChildCollection(parent as DependencyObject, logicalCollection);
            return logicalCollection;
        }

        public static void GetLogicalChildCollection<T>(DependencyObject parent, List<T> logicalCollection) where T : DependencyObject
        {
            if (parent != null)
            {
                // string s = parent.GetValue(Control.NameProperty).ToString();

                //parent.bas
                //IEnumerable<T> children = LogicalTreeHelper.GetChildren(parent);

                //if (s == "tbManLoadingCycle")
                //    Console.WriteLine("asaa");

                //Console.WriteLine(parent.ToString() + " ---- " + s);

                if (parent is T o)
                    logicalCollection.Add(o);

                var k = VisualTreeHelper.GetChildrenCount(parent);
                for (var i = 0; i < k; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child is DependencyObject dependencyObject)
                    {
                        GetLogicalChildCollection(dependencyObject, logicalCollection);
                    }
                }
            }
        }
    }
}