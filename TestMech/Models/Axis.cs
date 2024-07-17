using System.Collections;
using System.Diagnostics;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using Timer = System.Timers.Timer;

namespace TestMech.Models;

public enum State
{
    Ready, Running, Error
}

public class Axis : ObservableObject
{
    private int _nominalSpeed;
    public int NominalSpeed
    {
        get => _nominalSpeed;
        set => SetProperty(ref _nominalSpeed, value);
    }
    public ModBusTcp? Master { get; }

    public byte Slave { get; }

    public bool IsOnline { get; set; }

    public event EventHandler StateChanged;

    #region Speed : ushort - Скорость

    private ushort _speed = 100;

    /// <summary> Скорость </summary>
    public ushort Speed
    {
        get => _speed;
        set
        {
            if (SetProperty(ref _speed, value))
            {
                OnPropertyChanged("ActualSpeed");
            }
        }
    }

    #endregion Speed

    #region Speed : ushort - Скорость

    private ushort _relativeSpeed = 100;

    /// <summary> Скорость </summary>
    public ushort RelativeSpeed
    {
        get => _relativeSpeed;
        set
        {
            if (SetProperty(ref _relativeSpeed, value))
            {
                OnPropertyChanged("ActualSpeed");
            }
        }
    }

    #endregion Speed

    public static ushort Acc => 2500;

    public static ushort Dec => 1500;

    #region TargetPosition : double - Целевое значение позиции

    private double _targetPosition;

    /// <summary> Целевое значение позиции </summary>
    public double TargetPosition
    {
        get => _targetPosition;
        set => SetProperty(ref _targetPosition, value);
    }

    #endregion TargetPosition

    #region ActualPosition : double - Актуальное значение позиции

    private double _actualPosition;

    /// <summary> Актуальное значение позиции </summary>
    public double ActualPosition
    {
        get => _actualPosition;
        set => SetProperty(ref _actualPosition, value);
    }

    #endregion ActualPosition

    #region IsRunning : bool - Указывает что ось перемещается

    private bool _isRunning;

    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    #endregion

    #region State : State - Отображает состояние

    private State _state;

    public State State
    {
        get => _state;
        set
        {
            if (SetProperty(ref _state, value)) StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion

    #region DigitalInput : int - Отображает состояние

    private int _digitalInput;

    public int DigitalInput
    {
        get => _digitalInput;
        set
        {
            SetProperty(ref _digitalInput, value);
        }
    }

    #endregion

    private BitArray _digitalArray;

    public BitArray DigitalArray
    {
        get => _digitalArray;
        set
        {
            SetProperty(ref _digitalArray, value);
        }
    }

    public double FirstParam { get; set; }
    public double SecondParam { get; set; }

    // TODO Минимальная и максимальная позиция
    // TODO Отображать статус цветом
    // Перемещается - Синий 0E73F6
    // Ошибка - Красный F2271C
    // Готов - Зеленный 22C348

    private ushort _readActual;
    private ushort _setTarget;
    private ushort _move;
    
    public Axis(ModBusTcp master, ushort readActual, ushort setTarget, ushort move)
    {
        Master = master;
        IsOnline = true;

        _readActual = readActual;
        _setTarget = setTarget;
        _move = move;
        
        StartRead();
    }

    private void StartRead()
    {
        // Task.Run(ReadAsync);
        //var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, Callback, Dispatcher.CurrentDispatcher);
        var timer = new Timer(100);
        timer.Elapsed += Timer_Elapsed;
        timer.Start();
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        Read();
    }

    private void Callback(object? sender, EventArgs e)
    {
        Read();
    }

    public void Move()
    {
        MovePosition();
    }

    private void MovePosition()
    {
        Master?.WriteSingleRegister(_setTarget, (ushort)TargetPosition);

        Master?.WriteSingleRegister(_move, 1);
    }

    private short[] actualPosition = new short[2];
    private bool _readAndSetTarget;
    
    private void Read()
    {
        if (Master == null) return;

        actualPosition = new short[2];
        
        Master.ReadInputRegister(_readActual, 1, actualPosition);
        ActualPosition = actualPosition[0];

        if (_readAndSetTarget == false)
        {
            short[] targetPosition = new short[1];
            Master.ReadInputRegister(_readActual, 1, targetPosition);

            if (ActualPosition != targetPosition[0])
            {
                SetTargetPosition(ActualPosition);
                _readAndSetTarget = true;
            }
        }
    }

    private async void ReadAsync()
    {
        while (IsOnline)
        {
            if (Master == null) return;

            var bytes = new byte[2];
            Master.ReadHoldingRegisters(0, 2, bytes);
            var value = (bytes[0] << 16 | bytes[1]) * FirstParam / SecondParam;
            Task.Delay(100);
        }
    }

    /// <summary> Установка скорости для оси </summary>
    public void SetSpeed(int speed = 0)
    {
        // 25088 - function
        // 25089 - position H
        // 25090 - position L
        // 25091 - speed
        // TODO Адрес в класс Addresses

        if (speed == 0)
        {
            if (State == State.Running) MovePosition();
            else Master?.WriteSingleRegister(25091, Speed);
        }
        else
        {
            Speed = (ushort)speed;
            if (State == State.Running) MovePosition();
            else Master?.WriteSingleRegister(25091, (ushort)speed);
        }
    }

    public void SetRelativeSpeed()
    {
        if (State == State.Running) MovePosition();
        else Master?.WriteSingleRegister(25091, Speed);
    }

    public void Homing()
    {
        Master.WriteSingleRegister(24578, 32);
    }

    public void SetZero()
    {
        Master.WriteMultipleRegister(Slave, 24587, new ushort[] { 0, 0 });
        Master.WriteSingleRegister(24578, 33);
    }

    public void Stop()
    {
        Master.WriteSingleRegister(_move, 0);
    }

    public void SetManualSpeed(int speed)
    {
        var value = (ushort)speed;
        var manual = (ushort)(NominalSpeed * value / 100);
        Master.WriteSingleRegister(0x6027, manual);
    }

    public void SetTargetPosition(double targetPosition)
    {
        Master?.WriteSingleRegister(_setTarget, (ushort)targetPosition);
    }
}