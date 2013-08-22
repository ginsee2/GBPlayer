Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Xml

Public NotInheritable Class ConverterDiscogsConcatImagesCollection
    Inherits FrameworkElement
    Implements IValueConverter
    'Shared MemErreur As List(Of String) = New List(Of String)
    Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        Dim NewCollection As ObservableCollection(Of StackPanel) = New ObservableCollection(Of StackPanel)
        Dim Ensemble As StackPanel = New StackPanel
        If CType(value, IEnumerable) IsNot Nothing Then
            Dim Compteur As Integer
            For Each i As XmlElement In CType(value, IEnumerable)
                Compteur += 1
                Dim uriImage As String = ""
                Dim hauteur As String = ""
                Dim largeur As String = ""
                Dim typeImage As String = ""
                For Each j As XmlElement In i
                    If j.Name = "uri" Then uriImage = j.InnerText
                    If j.Name = "height" Then hauteur = j.InnerText
                    If j.Name = "width" Then largeur = j.InnerText
                    If j.Name = "type" Then typeImage = j.InnerText
                Next
                If uriImage <> "" Then
                    Dim InfosImages As ToolTip = New ToolTip
                    Dim ImageToolTip As Image = New Image
                    ImageToolTip.Height = 300
                    ImageToolTip.Width = 300
                    ImageToolTip.Stretch = Stretch.Fill
                    ImageToolTip.Source = DownloadBitmapImage(uriImage)
                    ImageToolTip.Style = CType(Me.FindResource("GBImage"), Style)
                    ImageToolTip.Margin = New Thickness(4)
                    InfosImages.Content = ImageToolTip
                    Dim ImageDisque As Image = New Image
                    'InfosImages.Content = "Image : " & hauteur & " x " & largeur & " : " & typeImage
                    ImageDisque.ToolTip = InfosImages
                    ImageDisque.Height = 70
                    ImageDisque.Width = 70
                    ImageDisque.Stretch = Stretch.Fill
                    ImageDisque.Source = DownloadBitmapImage(uriImage)
                    ImageDisque.Style = CType(Me.FindResource("GBImage"), Style)
                    ImageDisque.Name = "iDiscogs"
                    ImageDisque.AllowDrop = True
                    Dim BordureImage As Border = New Border
                    BordureImage.Margin = New Thickness(4)
                    BordureImage.Child = ImageDisque
                    If typeImage = "Primary" Then
                        Ensemble.Children.Insert(0, BordureImage)
                    Else
                        Ensemble.Children.Add(BordureImage)
                    End If
                End If
            Next
            Dim CompteurImages As TextBlock = New TextBlock
            CompteurImages.Name = "tagImageCount"
            CompteurImages.Foreground = Brushes.Blue
            CompteurImages.Background = Brushes.Transparent
            CompteurImages.Text = Compteur & " Image" & IIf(Compteur > 1, "s...", "...")
            CompteurImages.HorizontalAlignment = AlignmentX.Center
            Ensemble.Children.Add(CompteurImages)
            NewCollection.Add(Ensemble)
            Return NewCollection
        End If
    End Function
    Private Function DownloadBitmapImage(ByVal NomImage As String) As BitmapImage
        Dim RepDest = Discogs.GetRepertoireImages
        Dim NomFichier As String = Split(NomImage, "/").Last
        Dim CheminAccesImage As String = ""
        If NomFichier <> "" Then
            If (File.Exists(RepDest & "\" & NomFichier)) And (NomFichier <> "-1") Then
                CheminAccesImage = RepDest & "\" & NomFichier
            Else
                Return Nothing
                '     CheminAccesImage = NomImage 'RepDest & "\" & NomFichier
            End If
        Else
            Return Nothing
        End If
        If CheminAccesImage <> "" Then
            ' If TagID3.tagID3Object.FonctionUtilite.UploadImage(CheminAccesImage) IsNot Nothing Then
            Dim bi3 As New BitmapImage
            'If Not MemErreur.Contains(CheminAccesImage) Then
            Try
                bi3.BeginInit()
                Dim StreamData() As Byte = TagID3.tagID3Object.FonctionUtilite.UploadImage(CheminAccesImage)
                bi3.StreamSource = New MemoryStream(StreamData) ' New Uri(CheminAccesImage, UriKind.RelativeOrAbsolute)
                bi3.EndInit()
                ' If Not File.Exists(RepDest & "\" & NomFichier) Then _
                'TagID3.tagID3Object.FonctionUtilite.SaveImage(bi3, RepDest & "\" & NomFichier)
            Catch ex As Exception
                '    MemErreur.Add(CheminAccesImage)
            End Try
            'End If
            ' AddHandler bi3.DownloadCompleted, AddressOf Image_downloadComplete
            Return bi3
            'End If
        End If
    End Function
    ' Private Sub Image_downloadComplete(ByVal sender As Object, ByVal e As EventArgs)
    'Dim NomImage As String() = Split(sender.ToString, "/")
    'Dim RepDest = DiscogsInfos.GetRepertoireImages
    'Dim NomFichier As String = RepDest & "/" & NomImage.Last
    '    If Not File.Exists(NomFichier) Then _
    '        TagID3.tagID3Object.FonctionUtilite.SaveImage(CType(sender, BitmapImage), NomFichier)
    'End Sub
    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class
