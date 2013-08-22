
Public NotInheritable Class ConverterListViewBackground
    Implements IValueConverter
    Public Function Convert(ByVal value As Object, ByVal targetType As Type,
                            ByVal parameter As Object,
                            ByVal culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Dim item As ListViewItem = CType(value, ListViewItem)
        Dim listView As ListView = TryCast(ItemsControl.ItemsControlFromItemContainer(item), ListView)
        Dim index As Integer = listView.ItemContainerGenerator.IndexFromContainer(item)
        'Debug.Print(index)
        If index Mod 2 = 0 Then
            Return CType(listView.FindResource("ItemNotSelectedFill"), LinearGradientBrush)
        Else
            Return CType(listView.FindResource("ContainerBackground"), LinearGradientBrush)
        End If
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As Type, ByVal parameter As Object,
                                ByVal culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function

End Class
