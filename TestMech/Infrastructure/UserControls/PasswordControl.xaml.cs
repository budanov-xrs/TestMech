using System.Windows;

namespace TestMech.Infrastructure.UserControls;

public partial class PasswordControl
{
    public PasswordControl()
    {
        InitializeComponent();
    }

    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.Register(nameof(Password), typeof(string), typeof(PasswordControl), new PropertyMetadata());

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        Password = PbPassword.Password;
    }
}