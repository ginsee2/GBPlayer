
Public NotInheritable Class ConverterGetBitmapImage
    Implements IValueConverter
    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        If TypeOf (value) Is BitmapImage Then
            Return value
        Else
            If parameter Is Nothing Then
                Return "/GBPlayer;component/Images/ImageVinylVierge.png"
            Else
                Return parameter.ToString
            End If
        End If
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class
