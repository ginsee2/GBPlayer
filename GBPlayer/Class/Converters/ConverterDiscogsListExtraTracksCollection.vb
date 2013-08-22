Imports System.Collections.ObjectModel
Imports System.Xml

Public NotInheritable Class ConverterDiscogsListExtraTracksCollection
    Implements IValueConverter
    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        Dim NewCollection As ObservableCollection(Of DockPanel) = New ObservableCollection(Of DockPanel)
        Dim DictionnaireInfos As Dictionary(Of String, List(Of XmlElement)) = New Dictionary(Of String, List(Of XmlElement))
        If CType(value, IEnumerable) IsNot Nothing Then
            For Each i As XmlElement In CType(value, IEnumerable)
                Dim anv As String = ""
                Dim Nom As String = ""
                Dim Role As String = ""
                Dim Pistes As String = ""
                Dim id As String = ""
                For Each j As XmlElement In i
                    If j.Name = "name" Then Nom = Trim(ExtraitChaine(j.InnerText, "", "(", , True))
                    If j.Name = "anv" Then anv = Trim(ExtraitChaine(j.InnerText, "", "(", , True))
                    If j.Name = "role" Then Role = j.InnerText
                    If j.Name = "tracks" Then Pistes = j.InnerText
                    If j.Name = "id" Then id = j.InnerText
                Next
                If Nom <> "" Then
                    If DictionnaireInfos.ContainsKey(Role) Then
                        DictionnaireInfos.Item(Role).Add(i)
                    Else
                        Dim NouvelleListe As List(Of XmlElement) = New List(Of XmlElement)
                        NouvelleListe.Add(i)
                        DictionnaireInfos.Add(Role, NouvelleListe)
                    End If
                End If
            Next
            For Each i In DictionnaireInfos
                Dim Ensemble As DockPanel = New DockPanel
                Ensemble.Width = Double.NaN
                Ensemble.HorizontalAlignment = HorizontalAlignment.Left
                Ensemble.Background = Brushes.Transparent
                Dim BlocR As TextBlock = New TextBlock()
                BlocR.Name = "tagRoleExtraartist"
                BlocR.Foreground = Brushes.Brown
                BlocR.Background = Brushes.Transparent
                BlocR.FontSize = 10
                BlocR.Text = i.Key & " : "
                Ensemble.Children.Add(BlocR)
                Dim SousEnsemble As StackPanel = New StackPanel
                For Each j As XmlElement In i.Value
                    Dim SousEnsembleDock As DockPanel = New DockPanel
                    Dim Separateur As String = ""
                    'If SousEnsemble.Children.Count > 0 Then Separateur = ", "
                    Dim Bloc As TextBlock = New TextBlock()
                    Bloc.Name = "tag" & IIf(CInt(j.Item("id").InnerText) = 0, "No", "") & "LinkArtiste"
                    Bloc.Foreground = Brushes.Black
                    Bloc.Background = Brushes.Transparent
                    If j.Item("anv").InnerText <> "" Then
                        Bloc.Text = Separateur & Trim(ExtraitChaine(j.Item("anv").InnerText, "", "(", , True)) & "*"
                        Bloc.Tag = j.Item("id")
                        Dim NewToolTip As ToolTip = New ToolTip
                        NewToolTip.Content = Trim(ExtraitChaine(j.Item("name").InnerText, "", "(", , True))
                        Bloc.ToolTip = NewToolTip
                    Else
                        Bloc.Text = Separateur & Trim(ExtraitChaine(j.Item("name").InnerText, "", "(", , True))
                        Bloc.Tag = j.Item("id")
                    End If
                    SousEnsembleDock.Children.Add(Bloc)
                    If j.Item("tracks").InnerText <> "" Then
                        Dim BlocP As TextBlock = New TextBlock()
                        BlocP.Name = "tagPistesExtraartist"
                        BlocP.Foreground = Brushes.Black
                        BlocP.Background = Brushes.Transparent
                        BlocP.Foreground = Brushes.BurlyWood
                        BlocP.VerticalAlignment = VerticalAlignment.Center
                        BlocP.FontSize = 10
                        BlocP.Text = " [ " & j.Item("tracks").InnerText & " ]"
                        SousEnsembleDock.Children.Add(BlocP)
                    End If
                    SousEnsemble.Children.Add(SousEnsembleDock)
                Next
                Ensemble.Children.Add(SousEnsemble)
                NewCollection.Add(Ensemble)
            Next
            Return NewCollection
        End If
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class
