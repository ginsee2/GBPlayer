
Public NotInheritable Class ConverterListBoxBackground
    Implements IValueConverter
    Public Function Convert(ByVal value As Object, ByVal targetType As Type,
                            ByVal parameter As Object,
                            ByVal culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Dim item As ListBoxItem = CType(value, ListBoxItem)
        Dim Liste As ListBox = TryCast(ItemsControl.ItemsControlFromItemContainer(item), ListBox)
        Dim index = Liste.ItemContainerGenerator.IndexFromContainer(item)
        'Debug.Print(index)
        If index Mod 2 = 0 Then
            Return CType(Liste.FindResource("ItemNotSelectedFill"), LinearGradientBrush)
        Else
            Return CType(Liste.FindResource("ContainerBackground"), LinearGradientBrush)
        End If
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As Type, ByVal parameter As Object,
                                ByVal culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function

End Class

