Option Compare Text
Public Class ConverterStringToVisibility
    Implements IValueConverter

    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        Dim param As String = TryCast(parameter, String)
        Dim val As String = CStr(value)
        If Left(param, 4) = "null" Then Return If(val = "", Visibility.Visible, Visibility.Hidden)
        If param = "notNull" Then Return If(val <> "", Visibility.Visible, Visibility.Hidden)
        If Left(param, 3) = "not" Then
            param = Right(param, param.Length - 4)
            Return If(val <> param, Visibility.Visible, Visibility.Hidden)
        Else
            Return If(val = param, Visibility.Visible, Visibility.Hidden)
        End If
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function

End Class
