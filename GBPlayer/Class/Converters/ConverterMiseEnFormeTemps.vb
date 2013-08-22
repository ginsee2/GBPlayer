
Public NotInheritable Class ConverterMiseEnFormeTemps
    Implements IValueConverter
    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        Dim val As Integer = CInt(value)
        Dim Minutes As Integer = Int(val / 60)
        Dim Secondes As Integer = val - Minutes * 60
        Return Minutes.ToString("00") & ":" & Secondes.ToString("00")
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class


