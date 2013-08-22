
Public NotInheritable Class ConverterBoolToVisibility
    Implements IValueConverter

    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        Dim param As Boolean = Boolean.Parse(TryCast(parameter, String))
        Dim val As Boolean = CBool(value)
        Return If(val = param, Visibility.Visible, Visibility.Collapsed)
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function

End Class
