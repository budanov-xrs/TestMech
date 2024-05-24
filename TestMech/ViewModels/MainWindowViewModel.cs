﻿using ShMeco.Infrastructure.ViewModel;

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

    #endregion Properties

    public static App App;
    
    public MainWindowViewModel()
    {
        AxisY1 = new AxisControlViewModel("Y1", readActual: 1, setTarget: 1, move: 1);
        AxisY2 = new AxisControlViewModel("Y2", readActual: 1, setTarget: 1, move: 1);
        AxisX1 = new AxisControlViewModel("X1", readActual: 1, setTarget: 1, move: 1);
        AxisU1 = new AxisControlViewModel("U1", readActual: 1, setTarget: 1, move: 1);
        AxisU2 = new AxisControlViewModel("U2", readActual: 1, setTarget: 1, move: 1);
        AxisV1 = new AxisControlViewModel("V1", readActual: 1, setTarget: 1, move: 1);
        AxisZ1 = new AxisControlViewModel("Z1", readActual: 1, setTarget: 1, move: 1);
        AxisZ2 = new AxisControlViewModel("Z2", readActual: 1, setTarget: 1, move: 1);
    }
}