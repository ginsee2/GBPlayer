Public NotInheritable Class ConverterBoolToImageSource
    Implements IValueConverter
    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        Dim TabParam() As String = Split(CType(parameter, String), "!")
        Dim val As Boolean = CBool(value)
        If val Then
            Return IIf(TabParam.Count > 0, IIf(TabParam(0) = "", Nothing, TabParam(0)), Nothing)
        Else
            If (TabParam.Count > 1) Then Return IIf(TabParam(1) = "", Nothing, TabParam(1)) Else Return Nothing
        End If
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class
