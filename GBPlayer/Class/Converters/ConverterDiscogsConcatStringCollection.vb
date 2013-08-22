Imports System.Collections.ObjectModel
Imports System.Xml

Public NotInheritable Class ConverterDiscogsConcatStringCollection
    Implements IValueConverter
    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        Dim Bloc As TextBlock = New TextBlock()
        Bloc.Name = "tagConcat"
        Bloc.Foreground = Brushes.Black
        Dim NewCollection As ObservableCollection(Of TextBlock) = New ObservableCollection(Of TextBlock)
        If CType(value, IEnumerable) IsNot Nothing Then
            For Each i As XmlElement In CType(value, IEnumerable)
                If Bloc.Text <> "" Then Bloc.Text &= ", " & i.InnerText Else Bloc.Text = i.InnerText
            Next
        End If
        NewCollection.Add(Bloc)
        Return NewCollection
    End Function
    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class
