
Public NotInheritable Class ConverterWindowStateToBoolean
    Implements IValueConverter

    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        Dim val As Integer = CType(value, Integer)
        Select Case parameter
            Case "Minimized"
                Return If(val = WindowState.Minimized, True, False)
            Case "NotMinimized"
                Return If(val <> WindowState.Minimized, True, False)
            Case Else
                Return True
        End Select
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function

End Class
