using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TestMech.Models;
using TestMech.Infrastructure.Commands;

namespace TestMech.ViewModels;

public class AxisControlViewModel : INotifyPropertyChanged
{
    #region Private Properties

    private ModBusTcp _modbus;

    #endregion Private Properties
    
    #region Properties

    #region Axis : string - Ось

    private string _axis;

    /// <summary> Ось </summary>
    public string Axis
    {
        get => _axis;
        set => SetField(ref _axis, value);
    }

    #endregion Axis

    #region ActualValue : int - Актуальное значение

    /// <summary> Актуальное значение </summary>
    public double ActualPosition => Math.Round(AxisModel.ActualPosition, 1);

    #endregion ActualValue

    #region TargetValue : double - Целевое значение

    /// <summary> Целевое значение </summary>
    public double TargetPosition
    {
        get => (double)AxisModel.TargetPosition;
        set
        {
            if(value > Maximum) value = Maximum;
            else if(value < Minimum) value = Minimum;
            AxisModel.TargetPosition = value;
        }
    }

    #endregion TargetValue

    #region AxisModel : Axis - Ось логика

    private Axis _axisModel;

    /// <summary> Ось логика </summary>
    public Axis AxisModel
    {
        get => _axisModel;
        set => SetField(ref _axisModel, value);
    }

    #endregion AxisModel

    #region ActualSpeed : int - Актуальное значение скорости

    /// <summary> Актуальное значение скорости </summary>
    public ushort ActualSpeed => AxisModel.Speed;

    #endregion ActualSpeed

    #region TargetSpeed : int - Целевое значение скорости

    private ushort _targetSpeed;

    /// <summary> Целевое значение скорости </summary>
    public ushort TargetSpeed
    {
        get => _targetSpeed;
        set
        {
            if (SetField(ref _targetSpeed, value))
                AxisModel.Speed = value;
        }
    }

    #endregion TargetSpeed

    #region RelativeSpeed : int - Целевое значение скорости

    private ushort _relativeSpeed = 100;

    /// <summary> Целевое значение скорости </summary>
    public ushort RelativeSpeed
    {
        get => _relativeSpeed;
        set
        {
            if (value > 100) value = 100;
            else if (value < 1) value = 1;

            if (SetField(ref _relativeSpeed, value))
                AxisModel.RelativeSpeed = value;
        }
    }

    #endregion TargetSpeed

    #region CanHoming : bool - Может возвращаться домой

    private bool _canHoming;

    public bool CanHoming
    {
        get => _canHoming;
        set => SetField(ref _canHoming, value);
    }

    #endregion CanHoming

    #region CanSetZero : bool - Может устанавливать 0

    private bool _canSetZero;

    public bool CanSetZero
    {
        get => _canSetZero;
        set => SetField(ref _canSetZero, value);
    }

    #endregion CanHoming

    #region Maximum : int - Максимальное положение

    private int _maximum;
    public int Maximum
    {
        get => _maximum;
        set => SetField(ref _maximum, value);
    }

    #endregion Maximum : int - Максимальное положение

    #region Minimum : int - Минимальное положение

    private int _minimum;
    public int Minimum
    {
        get => _minimum;
        set => SetField(ref _minimum, value);
    }

    #endregion Minimum : int - Минимальное положение

    #endregion Properties

    #region Commands

    #region Move - Перейти на указанную позицию

    ///<summary> Перейти на указанную позицию </summary>
    public ICommand MoveCommand { get; }

    private void OnMoveCommandExecuted(object parameter)
    {
        AxisModel.Move();
    }

    #endregion Move

    #region SetSpeed - Установка значения скорости

    ///<summary> Установка значения скорости </summary>
    public ICommand SetSpeedCommand { get; }

    private void OnSetSpeedCommandExecuted(object parameter)
    {
        AxisModel.SetRelativeSpeed();
    }

    private bool CanSetSpeedCommandExecute(object parameter)
    {
        return true;
    }

    #endregion SetSpeed

    #region Homing - Переход домой

    ///<summary> Переход домой </summary>
    public ICommand HomingCommand { get; }

    private void OnHomingCommandExecuted(object parameter)
    {
        AxisModel.Homing();
    }

    private bool CanHomingCommandExecute(object parameter)
    {
        return true;
    }

    #endregion

    #region SetHoming - Установка нуля

    /// <summary> Установка нуля </summary>
    public ICommand SetZero { get; }

    private void OnSetZeroCommandExecuted(object parameter)
    {
        AxisModel.SetZero();
    }

    private bool CanSetZeroCommandExecute(object parameter)
    {
        return true;
    }

    #endregion SetHoming
    
    #region TargetChange - Изменение значения Target

    /// <summary> Установка нуля </summary>
    public ICommand TargetChange { get; }

    private void OnTargetChangeCommandExecuted(object parameter)
    {
        AxisModel.SetTargetPosition(TargetPosition);
    }

    private bool CanTargetChangeCommandExecute(object parameter)
    {
        return true;
    }

    #endregion TargetChange

    #endregion Commands
    
    #region Constructor

    public AxisControlViewModel(string axis, ushort readActual, ushort setTarget, ushort move)
    {
        MoveCommand = new RelayCommand(OnMoveCommandExecuted);
        SetSpeedCommand = new RelayCommand(OnSetSpeedCommandExecuted, CanSetSpeedCommandExecute);
        HomingCommand = new RelayCommand(OnHomingCommandExecuted, CanHomingCommandExecute);
        SetZero = new RelayCommand(OnSetZeroCommandExecuted, CanSetZeroCommandExecute);
        TargetChange = new RelayCommand(OnTargetChangeCommandExecuted, CanTargetChangeCommandExecute);
        
        Axis = axis;
        AxisModel = new Axis(App.Modbus, readActual, setTarget, move);
        AxisModel.PropertyChanged += AxisModelOnPropertyChanged;
    }

    #endregion Constructor

    #region Private Methods

    private void AxisModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
    }

    #endregion Private Methods

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}