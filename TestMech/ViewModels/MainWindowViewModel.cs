using System.Windows.Input;
using ShMeco.Infrastructure.ViewModel;
using TestMech.Infrastructure.Commands;

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

    #endregion Properties

    public static App App;

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
        App.Modbus.WriteSingleRegister(12, 2);
    }

    private bool CanStopMotorsCommandExecute(object parameter)
    {
        return true;
    }

    #endregion

    public MainWindowViewModel()
    {
        ChangeSpeedCommand = new RelayCommand(OnChangeSpeedCommandExecuted, CanChangeSpeedCommandExecute);
        StopMotorsCommand = new RelayCommand(OnStopMotorsCommandExecuted, CanStopMotorsCommandExecute);
        
        AxisY1 = new AxisControlViewModel("Y1", readActual: 0, setTarget: 0, move: 12) { Maximum = 1000, Minimum = 0 };
        AxisY2 = new AxisControlViewModel("Y2", readActual: 1, setTarget: 1, move: 12) { Maximum = 1000, Minimum = 0 };
        AxisU1 = new AxisControlViewModel("U1", readActual: 2, setTarget: 2, move: 12) { Maximum = 1000, Minimum = 0 };
        AxisU2 = new AxisControlViewModel("U2", readActual: 3, setTarget: 3, move: 12) { Maximum = 1000, Minimum = 0 };
        AxisZ1 = new AxisControlViewModel("Z1", readActual: 4, setTarget: 4, move: 12) { Maximum = 1000, Minimum = 0 };
        AxisZ2 = new AxisControlViewModel("Z2", readActual: 5, setTarget: 5, move: 12) { Maximum = 1000, Minimum = 0 };
        AxisX1 = new AxisControlViewModel("X", readActual: 6, setTarget: 6, move: 12) { Maximum = 1000, Minimum = 0 };
        AxisV1 = new AxisControlViewModel("V", readActual: 7, setTarget: 7, move: 12) { Maximum = 1000, Minimum = 0 };

        Speed = 100;
        SetSpeed(100);
    }
    
    private void SetSpeed(int value)
    {
        App.Modbus.WriteSingleRegister(11, (ushort)value);
    }
}