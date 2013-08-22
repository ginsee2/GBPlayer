

Imports System.Windows.Controls.Primitives


Public Class EqualSlider
    Inherits Slider
    Private ValueDisplay As Popup
    Private BorderDisplay As Border
    Private TextBlockDisplay As TextBlock
    Shared Sub New()
        'Cet appel OverrideMetadata indique au système que cet élément souhaite apporter un style différent de celui de sa classe de base.
        'Ce style est défini dans Styles/GBStyleSlider.xaml
        DefaultStyleKeyProperty.OverrideMetadata(GetType(EqualSlider), New FrameworkPropertyMetadata(GetType(EqualSlider)))
    End Sub

    Sub New()
        MyBase.New()
        Me.AddHandler(Slider.MouseEnterEvent, New RoutedEventHandler(AddressOf Slider_MouseEnter))
        Me.AddHandler(Slider.MouseLeaveEvent, New RoutedEventHandler(AddressOf Slider_MouseLeave))
        BorderDisplay = New Border
        BorderDisplay.CornerRadius = New CornerRadius(3)
        BorderDisplay.Background = Brushes.White
        BorderDisplay.BorderBrush = Brushes.Black
        BorderDisplay.BorderThickness = New Thickness(1)
        TextBlockDisplay = New TextBlock
        TextBlockDisplay.Background = Brushes.Transparent
        TextBlockDisplay.Foreground = Brushes.Black
        TextBlockDisplay.HorizontalAlignment = HorizontalAlignment.Center
        TextBlockDisplay.FontFamily = New FontFamily("Calibri")
        TextBlockDisplay.FontSize = 11
        ValueDisplay = New Popup
        ValueDisplay.Child = BorderDisplay
        BorderDisplay.Child = TextBlockDisplay
    End Sub
    Private Sub Slider_MouseEnter(ByVal Sender As Object, ByVal e As RoutedEventArgs)
        TextBlockDisplay.Text = Tag.ToString & " : " & Value.ToString("0.0")
        ValueDisplay.PlacementTarget = Me
        ValueDisplay.IsOpen = True
        'ToolTip = Value
    End Sub
    Private Sub Slider_MouseLeave(ByVal Sender As Object, ByVal e As RoutedEventArgs)
        ValueDisplay.IsOpen = False
        'Value = 0
    End Sub
    Private Sub CenterSlider_MouseDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.MouseDoubleClick
        If TypeOf (e.OriginalSource) Is Ellipse Then
            Me.Value = 0
            e.Handled = True
        End If
    End Sub

    Private Sub EqualSlider_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles Me.MouseMove
        'If e.LeftButton = MouseButtonState.Pressed Then
        ' Value = ((ActualHeight - e.GetPosition(Me).Y) / ActualHeight) * 30 - 15
        ' End If
    End Sub

    Private Sub EqualSlider_MouseWheel(ByVal sender As Object, ByVal e As System.Windows.Input.MouseWheelEventArgs) Handles Me.MouseWheel
        Me.Value = Me.Value + IIf(e.Delta > 0, 1, -1)
    End Sub
    Private Sub CenterSlider_ValueChanged(ByVal sender As Object, ByVal e As System.Windows.RoutedPropertyChangedEventArgs(Of Double)) Handles Me.ValueChanged
        TextBlockDisplay.Text = Tag.ToString & " : " & Value.ToString("0.0")
    End Sub
End Class
