Imports System.Collections.ObjectModel
Imports System.Xml

Public NotInheritable Class ConverterDiscogsConcatArtistsCollection
    Implements IValueConverter
    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
         Dim NewCollection As ObservableCollection(Of DockPanel) = New ObservableCollection(Of DockPanel)
        If CType(value, IEnumerable) IsNot Nothing Then
            Dim Ensemble As DockPanel = New DockPanel
            For Each i As XmlElement In CType(value, IEnumerable)
                If Ensemble.Children.Count > 0 Then
                    Dim Bloc As TextBlock = New TextBlock()
                    Bloc.Foreground = Brushes.Black
                    Bloc.Text = " & "
                    Ensemble.Children.Add(Bloc)
                End If
                Dim Nom As String = ""
                Dim anv As String = ""
                Dim id As String = ""
                For Each j As XmlElement In i
                    If j.Name = "name" Then Nom = Trim(ExtraitChaine(j.InnerText, "", "(", , True))
                    If j.Name = "anv" Then anv = Trim(ExtraitChaine(j.InnerText, "", "(", , True))
                    If j.Name = "id" Then id = j.InnerText
                Next
                If Nom <> "" Then
                    Dim Bloc As TextBlock = New TextBlock()
                    Bloc.Foreground = Brushes.Black
                    Bloc.Name = "tag" & IIf(id = "", "No", "") & "LinkArtiste"
                    If anv <> "" Then
                        Bloc.Text = anv & "*"
                        Bloc.Tag = i.Item("id")
                        Dim NewToolTip As ToolTip = New ToolTip
                        NewToolTip.Content = Nom
                        Bloc.ToolTip = NewToolTip
                        Ensemble.Children.Add(Bloc)
                    Else
                        Bloc.Text = Nom
                        Bloc.Tag = i.Item("id")
                        Ensemble.Children.Add(Bloc)
                    End If
                End If
            Next
            NewCollection.Add(Ensemble)
            Return NewCollection
        End If
    End Function
    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class
