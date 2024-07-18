using System.Collections;
using System.Windows.Input;
using ShMeco.Infrastructure.ViewModel;
using TestMech.Infrastructure.Commands;
using TestMech.Models;

namespace TestMech.ViewModels;

public class MainWindowViewModel : BaseViewModel
{
    #region Properties

    #region AxisY1 : AxisControlViewModel -

    private AxisControlViewModel _axisY1;

    /// <summary>  </summary>
    public AxisControlViewModel AxisY1
    {
        get => _axisY1;
        set => SetField(ref _axisY1, value);
    }

    #endregion AxisY1

    #region AxisY2 : AxisControlViewModel -

    private AxisControlViewModel _axisY2;

    /// <summary>  </summary>
    public AxisControlViewModel AxisY2
    {
        get => _axisY2;
        set => SetField(ref _axisY2, value);
    }

    #endregion AxisY2

    #region AxisX1 : AxisControlViewModel -

    private AxisControlViewModel _axisX1;

    /// <summary>  </summary>
    public AxisControlViewModel AxisX1
    {
        get => _axisX1;
        set => SetField(ref _axisX1, value);
    }

    #endregion AxisX1

    #region AxisU1 : AxisControlViewModel -

    private AxisControlViewModel _axisU1;

    /// <summary>  </summary>
    public AxisControlViewModel AxisU1
    {
        get => _axisU1;
        set => SetField(ref _axisU1, value);
    }

    #endregion AxisU1

    #region AxisU2 : AxisControlViewModel -

    private AxisControlViewModel _axisU2;

    /// <summary>  </summary>
    public AxisControlViewModel AxisU2
    {
        get => _axisU2;
        set => SetField(ref _axisU2, value);
    }

    #endregion AxisU2

    #region AxisV1 : AxisControlViewModel -

    private AxisControlViewModel _axisV1;

    /// <summary>  </summary>
    public AxisControlViewModel AxisV1
    {
        get => _axisV1;
        set => SetField(ref _axisV1, value);
    }

    #endregion AxisV1

    #region AxisZ1 : AxisControlViewModel -

    private AxisControlViewModel _axisZ1;

    /// <summary>  </summary>
    public AxisControlViewModel AxisZ1
    {
        get => _axisZ1;
        set => SetField(ref _axisZ1, value);
    }

    #endregion AxisZ1

    #region AxisZ2 : AxisControlViewModel -

    private AxisControlViewModel _axisZ2;

    /// <summary>  </summary>
    public AxisControlViewModel AxisZ2
    {
        get => _axisZ2;
        set => SetField(ref _axisZ2, value);
    }

    #endregion AxisZ2

    #region Speed : int -

    private int _speed;

    /// <summary>  </summary>
    public int Speed
    {
        get => _speed;
        set => SetField(ref _speed, value);
    }

    #endregion AxisZ2

    #region EmergencyStop : bool - Description

    private bool _emergencyStop;

    /// <summary> Description </summary>
    public bool EmergencyStop
    {
        get => _emergencyStop;
        set => SetField(ref _emergencyStop, value);
    }

    #endregion EmergencyStop

    #region SystemReady : bool - Description

    private bool _systemReady;

    /// <summary> Description </summary>
    public bool SystemReady
    {
        get => _systemReady;
        set => SetField(ref _systemReady, value);
    }

    #endregion SystemReady

    #endregion Properties

    private ModBusTcp _modbus;
    private readonly Thread _readStatus;

    #region ChangeSpeed - Переход домой

    ///<summary> Переход домой </summary>
    public ICommand ChangeSpeedCommand { get; }

    private void OnChangeSpeedCommandExecuted(object parameter)
    {
        if (int.TryParse((string)parameter, out var value))
        {
            SetSpeed(value);
        }
    }

    private bool CanChangeSpeedCommandExecute(object parameter)
    {
        return true;
    }

    #endregion
    
    #region ChangeSpeed - Переход домой

    ///<summary> Переход домой </summary>
    public ICommand StopMotorsCommand { get; }

    private void OnStopMotorsCommandExecuted(object parameter)
    {
        Bools[1] = true;
    }

    private bool CanStopMotorsCommandExecute(object parameter)
    {
        return true;
    }

    #endregion

    #region Move - Description

    ///<summary> Description </summary>
    public ICommand MoveCommand { get; }

    private void OnMoveCommandExecuted(object parameter)
    {
        Bools[0] = true;
    }

    private bool CanMoveCommandExecute(object parameter)
    {
        return true;
    }

    #endregion Move
    
    public List<bool> Bools;
    
    public MainWindowViewModel()
    {
        _modbus = App.Modbus;
        ChangeSpeedCommand = new RelayCommand(OnChangeSpeedCommandExecuted, CanChangeSpeedCommandExecute);
        StopMotorsCommand = new RelayCommand(OnStopMotorsCommandExecuted, CanStopMotorsCommandExecute);
        MoveCommand = new RelayCommand(OnMoveCommandExecuted, CanMoveCommandExecute);
        
        AxisY1 = new AxisControlViewModel("Y1", readActual: 0, setTarget: 0, move: 12) { Maximum = 1000, Minimum = 0 };
        AxisY2 = new AxisControlViewModel("Y2", readActual: 1, setTarget: 1, move: 12) { Maximum = 1000, Minimum = 0 };
        AxisU1 = new AxisControlViewModel("U1", readActual: 2, setTarget: 2, move: 12) { Maximum = 1000, Minimum = 0 };
        AxisU2 = new AxisControlViewModel("U2", readActual: 3, setTarget: 3, move: 12) { Maximum = 1000, Minimum = 0 };
        AxisZ1 = new AxisControlViewModel("Z1", readActual: 4, setTarget: 4, move: 12) { Maximum = 1000, Minimum = 0 };
        AxisZ2 = new AxisControlViewModel("Z2", readActual: 5, setTarget: 5, move: 12) { Maximum = 1000, Minimum = 0 };
        AxisX1 = new AxisControlViewModel("X", readActual: 6, setTarget: 6, move: 12) { Maximum = 1000, Minimum = 0 };
        AxisV1 = new AxisControlViewModel("V", readActual: 7, setTarget: 7, move: 12) { Maximum = 1000, Minimum = 0 };

        Bools = new List<bool>(32);
        
        Speed = 100;
        SetSpeed(Speed);
        
        _readStatus = new Thread(ReadStatus);
        _readStatus.Start();
    }
    
    private void SetSpeed(int value)
    {
        App.Modbus.WriteSingleRegister(11, (ushort)value);
    }
    
    private List<bool> _status;
    private bool[] _lastBools = new bool[32];

    public bool Exit;
    private void ReadStatus()
    {
        while (Exit == false)
        {
            var registers = new byte[1];
            _modbus.ReadInputRegister(11, 1, registers);
        
            _status = ConvertHexToBitMask(ConvertIntToHex(registers[0])).ToList();

            for (int i = 0; i < Bools.Count; i++)
            {
                if (_lastBools[i] != Bools[i])
                {
                    _lastBools = Bools.ToArray();
                    var number = ConvertBitsMaskToInt(Bools.ToArray());
                    _modbus.WriteSingleRegister(12, (ushort)number);
                    break;
                }
            }
            Thread.Sleep(10);
        }
    }

    public void Unload()
    {
        Bools[2] = true;
    }

    public void Load()
    {
        Bools[3] = true;
    }

    public bool MotorsInPositions()
    {
        var registers = new byte[1];
        _modbus.ReadInputRegister(11, 1, registers);

        var array = ConvertHexToBitMask(ConvertIntToHex(registers[0])).ToList();
        if (array[6])
        {
            return false;
        }
        
        return true;
    }
    
    private static string ConvertIntToHex(int number)
    {
        var hexString = "";

        var main = number / 16;
        var sub = number % 16;

        hexString += GetIntToHex(main);
        hexString += GetIntToHex(sub);

        return hexString.PadLeft(4, '0');
    }
    
    private static string GetIntToHex(int number) => number < 10 ? number.ToString() : ((char)('A' + number - 10)).ToString();

    private static IEnumerable<bool> ConvertHexToBitMask(string hex)
    {
        var longValue = Convert.ToInt64(hex.Trim(), 16);
        var binRepresentation = Convert.ToString(longValue, 2);
        var mask = new bool[32];
        for (var i = 0; i < binRepresentation.Length; i++)
            mask[i] = binRepresentation[i] == '1';

        return mask;
    }

    private static int ConvertBitsMaskToInt(bool[] bools)
    {
        BitArray bits = new BitArray(bools);
        byte[] bytes = new byte[bits.Length / 8];
        bits.CopyTo(bytes, 0);

        return BitConverter.ToInt32(bytes, 0);
    }

    public void WaitLoad()
    {
        while (_status[3] == false) { }
    }
    
    public void WaitUnload()
    {
        while (_status[4] == false) { }
    }

    public void WaitCloseDoor()
    {
        while (_status[7] == false) { }
    }

    public void EnableJoysticks()
    {
        Bools[6] = true;
    }
    
    public void DisableJoysticks()
    {
        Bools[6] = false;
    }
}