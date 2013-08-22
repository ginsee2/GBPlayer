Imports System.Xml

Public NotInheritable Class ConverterDecomposeString
    Implements IValueConverter
    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        'Dim NewCollection As ObservableCollection(Of DockPanel) = New ObservableCollection(Of DockPanel)
        ' If CType(value, string) IsNot Nothing Then
        Dim TabComp As String() ' = Split(DirectCast(value, String), ",")
        If (TypeOf value Is String) Then
            TabComp = Split(DirectCast(value, String), ",")
            Return CreationEnsembleChaine(TabComp, ", ", "tagLinkCompositeur")
        ElseIf (TypeOf value Is XmlElement) Then
            Dim Titre As String = Trim(ExtraitChaine(CType(value, XmlElement).InnerText, "-", ""))
            Dim Artiste As String = Trim(ExtraitChaine(CType(value, XmlElement).InnerText, "", "-"))
            Dim Reste As String = " " & Trim(ExtraitChaine(Titre, "(", "", 0, True))
            Titre = Trim(ExtraitChaine(Titre, "", "("))
            Dim TabArtiste As String() = Split(Artiste, "/")
            Dim TabTitre As String() = Split(Titre, "/")
            Dim Ensemble As WrapPanel = CreationEnsembleChaine(TabArtiste, " / ", "tagLinkArtiste")
            Ensemble = CreationEnsembleChaine(Nothing, " - ", "", Ensemble)
            Ensemble = CreationEnsembleChaine(TabTitre, " / ", "tagLinkTitre", Ensemble)
            Ensemble = CreationEnsembleChaine(Nothing, Reste, "", Ensemble)
            Return Ensemble
        Else
            Return Nothing
        End If
    End Function
    Private Function CreationEnsembleChaine(ByVal TabComp As String(), ByVal Separateur As String, NomBloc As String,
                                            Optional ByVal WrapAEtendre As WrapPanel = Nothing) As WrapPanel
        Dim Ensemble As WrapPanel
        If WrapAEtendre Is Nothing Then Ensemble = New WrapPanel Else Ensemble = WrapAEtendre
        Ensemble.Margin = New Thickness(0, 0, 0, 0)
        If TabComp Is Nothing Then
            Dim BlocSep As TextBlock = New TextBlock()
            BlocSep.Foreground = Brushes.Black
            BlocSep.Text = Separateur
            Ensemble.Children.Add(BlocSep)
            Return Ensemble
        End If
        Dim Compteur As Integer = 0
        For Each i As String In TabComp
            If Compteur > 0 Then
                Dim BlocSep As TextBlock = New TextBlock()
                BlocSep.Foreground = Brushes.Black
                BlocSep.Text = Separateur
                Ensemble.Children.Add(BlocSep)
            End If
            Compteur += 1
            Dim Bloc As TextBlock = New TextBlock()
            Bloc.TextWrapping = TextWrapping.WrapWithOverflow
            Bloc.Foreground = Brushes.Black
            Bloc.Name = NomBloc
            Bloc.Text = Trim(i)
            Bloc.Margin = New Thickness(0)
            Ensemble.Children.Add(Bloc)
        Next
        Return Ensemble
    End Function
    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class

