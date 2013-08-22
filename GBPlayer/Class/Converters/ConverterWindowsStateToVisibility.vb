
Public NotInheritable Class ConverterWindowStateToVisibility
    Implements IValueConverter

    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        Dim val As Integer = CType(value, Integer)
        Select Case parameter
            Case "Minimized"
                Return If(val = WindowState.Minimized, Visibility.Visible, Visibility.Collapsed)
            Case "NotMinimized"
                Return If(val <> WindowState.Minimized, Visibility.Visible, Visibility.Collapsed)
            Case Else
                Return Visibility.Visible
        End Select
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function

End Class
