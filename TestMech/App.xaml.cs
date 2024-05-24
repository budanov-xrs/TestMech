using System.Configuration;
using System.Data;
using System.Net;
using System.Windows;
using TestMech.Models;

namespace TestMech;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

    public static ModBusTcp Modbus = new ModBusTcp(IPAddress.Parse("192.168.88.100"));
}