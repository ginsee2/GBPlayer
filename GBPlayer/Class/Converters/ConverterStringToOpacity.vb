Option Compare Text
Public Class ConverterStringToOpacity
    Implements IValueConverter

    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        Dim param As String = TryCast(parameter, String)
        Dim val As String = CStr(value)
        If Left(param, 4) = "null" Then Return If(val = "", 1, 0.1)
        If param = "notNull" Then Return If(val <> "", 1, 0.1)
        param = RemplaceOccurences(param, "_", " ")
        If Left(param, 3) = "not" Then
            param = Right(param, param.Length - 4)
            Return If(val <> param, 1, 0.1)
        Else
            Return If(val = param, 1, 0.1)
        End If
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function

End Class
